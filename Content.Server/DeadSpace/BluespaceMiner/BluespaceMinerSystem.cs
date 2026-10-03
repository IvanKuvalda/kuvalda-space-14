using Content.Server.Atmos.EntitySystems;
using Content.Server.Materials;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Containers;
using Content.Shared.Examine;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.DeadSpace.BluespaceMiner;

namespace Content.Server.DeadSpace.BluespaceMiner;

/// <summary>
/// Логика блюспейс-майнера: добывает материалы при соблюдении условий
/// среды (20-60 K, 100-150 кПа) и выделяет горячий углекислый газ.
/// </summary>
public sealed class BluespaceMinerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityManager _spawn = default!;

    private readonly HashSet<Entity<PhysicalCompositionComponent>> _candidates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceMinerComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<BluespaceMinerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAtmosUpdate(Entity<BluespaceMinerComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var comp = ent.Comp;

        if (!_power.IsPowered(ent))
        {
            SetStatus(ent, BluespaceMinerStatus.Unpowered);
            return;
        }

        var environment = _atmosphere.GetContainingMixture((ent, Transform(ent)), true, true);
        if (environment == null || !ConditionsMet(comp, environment.Temperature, environment.Pressure))
        {
            SetStatus(ent, BluespaceMinerStatus.BadConditions);
            return;
        }

        SetStatus(ent, BluespaceMinerStatus.Ok);

        // выделяем горячий газ пропорционально времени
        var moles = comp.GasMolesPerSecond * args.dt;
        ReleaseGas(comp, environment, moles);

        // добыча материала — пачками раз в секунду
        comp.Accumulator += args.dt;
        while (comp.Accumulator >= 1f)
        {
            comp.Accumulator -= 1f;
            MineOnce(ent, comp);
            TeleportNearbyResources(ent, comp);
        }
    }

    /// <summary>
    /// Телепортирует предметы с материалами из зоны 3х3 тайлов в хранилище
    /// машины с блюспейс-эффектом.
    /// </summary>
    private void TeleportNearbyResources(Entity<BluespaceMinerComponent> ent, BluespaceMinerComponent comp)
    {
        var xform = Transform(ent);
        _lookup.GetEntitiesInRange(xform.Coordinates, comp.TeleportRange, _candidates, LookupFlags.Uncontained);

        var teleported = 0;
        foreach (var candidate in _candidates)
        {
            if (teleported >= comp.MaxTeleportsPerSecond)
                break;

            if (candidate.Owner == ent.Owner || TerminatingOrDeleted(candidate))
                continue;

            // не трогаем то, что лежит в контейнерах/инвентаре
            if (_container.IsEntityInContainer(candidate))
                continue;

            // телепортируем только сыпучие ресурсы (руда, листы), а не любые предметы
            if (!HasComp<StackComponent>(candidate))
                continue;

            if (!TryComp<PhysicalCompositionComponent>(candidate, out var composition) || composition.MaterialComposition.Count == 0)
                continue;

            foreach (var (material, amount) in composition.MaterialComposition)
                _materialStorage.TryChangeMaterialAmount(ent, material, amount);

            _spawn.SpawnEntity("EffectBluespaceMinerTeleport", Transform(candidate).Coordinates);
            QueueDel(candidate);
            teleported++;
        }

        _candidates.Clear();
    }

    private void MineOnce(Entity<BluespaceMinerComponent> ent, BluespaceMinerComponent comp)
    {
        if (comp.MaterialWeights.Count == 0)
            return;

        var material = PickMaterial(comp);
        var amount = comp.SheetsPerSecond * comp.MaterialPerSheet;

        _materialStorage.TryChangeMaterialAmount(ent, material, amount);
    }

    private ProtoId<MaterialPrototype> PickMaterial(BluespaceMinerComponent comp)
    {
        var total = 0f;
        foreach (var (_, weight) in comp.MaterialWeights)
            total += weight;

        var roll = _random.NextFloat() * total;
        foreach (var (material, weight) in comp.MaterialWeights)
        {
            roll -= weight;
            if (roll <= 0f)
                return material;
        }

        // страховка от ошибок округления
        foreach (var (material, _) in comp.MaterialWeights)
            return material;

        throw new InvalidOperationException("BluespaceMiner has no materials configured");
    }

    private void ReleaseGas(BluespaceMinerComponent comp, GasMixture environment, float moles)
    {
        if (moles <= 0f)
            return;

        // добавляем газ с температурой comp.GasTemperature:
        // пересчитываем температуру смеси как средневзвешенную по молям
        var totalMoles = environment.TotalMoles;
        var newTemp = totalMoles > 0f
            ? (environment.Temperature * totalMoles + comp.GasTemperature * moles) / (totalMoles + moles)
            : comp.GasTemperature;

        environment.AdjustMoles(comp.ReleasedGas, moles);
        environment.Temperature = newTemp;
    }

    private bool ConditionsMet(BluespaceMinerComponent comp, float temperature, float pressure)
    {
        return temperature >= comp.MinTemperature
            && temperature <= comp.MaxTemperature
            && pressure >= comp.MinPressure
            && pressure <= comp.MaxPressure;
    }

    private void SetStatus(Entity<BluespaceMinerComponent> ent, BluespaceMinerStatus status)
    {
        _appearance.SetData(ent, BluespaceMinerVisuals.Status, status);
    }

    private void OnExamined(Entity<BluespaceMinerComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (!_power.IsPowered(ent))
        {
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-unpowered"));
            return;
        }

        var environment = _atmosphere.GetContainingMixture((ent, Transform(ent)), true, true);
        if (environment == null)
        {
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-pressure-low"));
            return;
        }

        var pushed = false;
        if (environment.Temperature < comp.MinTemperature)
        {
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-temp-low"));
            pushed = true;
        }
        else if (environment.Temperature > comp.MaxTemperature)
        {
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-temp-high"));
            pushed = true;
        }

        if (environment.Pressure < comp.MinPressure)
        {
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-pressure-low"));
            pushed = true;
        }
        else if (environment.Pressure > comp.MaxPressure)
        {
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-pressure-high"));
            pushed = true;
        }

        if (!pushed)
            args.PushMarkup(Loc.GetString("bluespace-miner-examine-ok"));
    }
}
