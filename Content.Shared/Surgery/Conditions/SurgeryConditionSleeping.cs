using Content.Shared.Bed.Sleep;

namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет, спит ли цель (наркоз/сон).
/// </summary>
/// <remarks>
/// С <c>inverted: true</c> — «пациент в сознании». Именно на этом строятся осложнения вида
/// «дёрнулся под скальпелем».
/// </remarks>
public sealed partial class SurgeryConditionSleeping : SurgeryConditionBase<SurgeryConditionSleeping>
{
    /// <summary>
    /// Кого проверяем.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Patient;
}

public sealed partial class SurgeryConditionSleepingSystem : SurgeryConditionSystem<SurgeryConditionSleeping>
{
    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionSleeping> args)
    {
        args.Result = false;

        if (args.Context.GetEntity(args.Condition.Target) is not { } ent)
            return;

        args.Result = HasComp<SleepingComponent>(ent);
    }
}
