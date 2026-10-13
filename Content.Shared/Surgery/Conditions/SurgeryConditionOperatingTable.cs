using Content.Shared.Buckle.Components;

namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет, что пациент лежит на операционном столе (<see cref="OperatingTableComponent"/>).
/// </summary>
/// <remarks>
/// С <c>inverted: true</c> получается «пациент не на столе» — самый простой способ навесить
/// осложнения и штрафы за операцию на коленке.
/// </remarks>
public sealed partial class SurgeryConditionOperatingTable : SurgeryConditionBase<SurgeryConditionOperatingTable>
{
    /// <summary>
    /// Требовать именно пристёгнутого к столу пациента (а не просто стол рядом).
    /// </summary>
    [DataField]
    public bool RequireBuckle = true;

    /// <summary>
    /// Радиус поиска стола, если <see cref="RequireBuckle"/> выключен.
    /// </summary>
    [DataField]
    public float Radius = 1f;
}

public sealed partial class SurgeryConditionOperatingTableSystem : SurgeryConditionSystem<SurgeryConditionOperatingTable>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<EntityUid> _found = [];

    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionOperatingTable> args)
    {
        args.Result = false;

        var patient = args.Context.Patient;

        if (args.Condition.RequireBuckle)
        {
            if (!TryComp<BuckleComponent>(patient, out var buckle) || buckle.BuckledTo is not { } strap)
                return;

            args.Result = HasComp<OperatingTableComponent>(strap);
            return;
        }

        _found.Clear();
        _lookup.GetEntitiesInRange(patient, args.Condition.Radius, _found);

        foreach (var ent in _found)
        {
            if (!HasComp<OperatingTableComponent>(ent))
                continue;

            args.Result = true;
            return;
        }
    }
}
