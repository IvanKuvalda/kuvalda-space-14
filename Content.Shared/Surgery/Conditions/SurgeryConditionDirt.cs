namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет уровень загрязнения сущности: руки хирурга, инструмент, стол, тело пациента.
/// </summary>
/// <remarks>
/// Нет компонента загрязнения — значит сущность чистая (условие вернёт false).
/// Для «руки чистые» используется <c>inverted: true</c>.
/// </remarks>
public sealed partial class SurgeryConditionDirt : SurgeryConditionBase<SurgeryConditionDirt>
{
    /// <summary>
    /// Кого проверяем.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Surgeon;

    /// <summary>
    /// Порог, начиная с которого сущность считается грязной.
    /// </summary>
    [DataField]
    public float Threshold = 20f;
}

public sealed partial class SurgeryConditionDirtSystem : SurgeryConditionSystem<SurgeryConditionDirt>
{
    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionDirt> args)
    {
        args.Result = false;

        if (args.Context.GetEntity(args.Condition.Target) is not { } ent)
            return;

        if (!TryComp<SurgeryDirtinessComponent>(ent, out var dirt))
            return;

        args.Result = dirt.Amount >= args.Condition.Threshold;
    }
}
