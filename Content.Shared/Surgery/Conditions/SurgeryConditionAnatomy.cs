using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет анатомию цели по <see cref="BodyPrototype"/> (Human, Diona, Skeleton, ...).
/// </summary>
/// <remarks>
/// Именно это условие делает развилки «одной и той же» операции: вариант шага со скальпелем
/// ставит <c>inverted: true</c> и список нечеловеческих тел, а вариант с топором — наоборот.
/// Чтобы добавить новую расу, достаточно дописать её body-прототип в YAML.
/// </remarks>
public sealed partial class SurgeryConditionAnatomy : SurgeryConditionBase<SurgeryConditionAnatomy>
{
    /// <summary>
    /// Кто проверяется.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Patient;

    /// <summary>
    /// Подходящие body-прототипы.
    /// </summary>
    [DataField]
    public List<ProtoId<BodyPrototype>> Bodies = [];
}

public sealed partial class SurgeryConditionAnatomySystem : SurgeryConditionSystem<SurgeryConditionAnatomy>
{
    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionAnatomy> args)
    {
        args.Result = false;

        if (args.Context.GetEntity(args.Condition.Target) is not { } ent)
            return;

        if (!TryComp<BodyComponent>(ent, out var body) || body.Prototype is not { } prototype)
            return;

        args.Result = args.Condition.Bodies.Contains(prototype);
    }
}
