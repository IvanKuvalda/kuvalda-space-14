using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет состояние мобика: живой, в крите, мёртвый.
/// </summary>
/// <remarks>
/// Операции обычно требуют <c>Alive</c> (или Alive+Critical), а осложнения наоборот
/// могут проверять, что пациент уже мёртв — тогда «операция» бессмысленна.
/// </remarks>
public sealed partial class SurgeryConditionMobState : SurgeryConditionBase<SurgeryConditionMobState>
{
    /// <summary>
    /// Кого проверяем.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Patient;

    /// <summary>
    /// Подходящие состояния.
    /// </summary>
    [DataField]
    public HashSet<MobState> States = [MobState.Alive];
}

public sealed partial class SurgeryConditionMobStateSystem : SurgeryConditionSystem<SurgeryConditionMobState>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionMobState> args)
    {
        args.Result = false;

        if (args.Context.GetEntity(args.Condition.Target) is not { } ent)
            return;

        foreach (var state in args.Condition.States)
        {
            var matches = state switch
            {
                MobState.Alive => _mobState.IsAlive(ent),
                MobState.Critical => _mobState.IsCritical(ent),
                MobState.Dead => _mobState.IsDead(ent),
                MobState.PreCritical => _mobState.IsPreCritical(ent),
                _ => false,
            };

            if (!matches)
                continue;

            args.Result = true;
            return;
        }
    }
}
