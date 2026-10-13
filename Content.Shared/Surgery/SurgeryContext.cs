using Content.Shared.Surgery.Prototypes;

namespace Content.Shared.Surgery;

/// <summary>
/// Полный контекст операции в момент проверки условия или применения эффекта.
/// </summary>
/// <remarks>
/// Ядро хирургии ничего не знает о конкретных операциях: и условия, и эффекты получают
/// этот контекст и достают из него всё, что им нужно. Благодаря этому новые операции,
/// условия и исходы добавляются данными (YAML), а не правками систем.
/// </remarks>
public readonly record struct SurgeryContext(
    EntityUid Patient,
    EntityUid? Surgeon = null,
    EntityUid? Tool = null,
    EntityUid? OperatingTable = null,
    SurgeryPrototype? Surgery = null,
    SurgeryStepPrototype? Step = null,
    int StepIndex = 0)
{
    /// <summary>
    /// Достаёт сущность по селектору. Может вернуть null, если в контексте её нет.
    /// </summary>
    public EntityUid? GetEntity(SurgeryEntityTarget target)
    {
        return target switch
        {
            SurgeryEntityTarget.Surgeon => Surgeon,
            SurgeryEntityTarget.Tool => Tool,
            SurgeryEntityTarget.OperatingTable => OperatingTable,
            _ => Patient,
        };
    }

    /// <summary>
    /// Копия контекста с подставленным шагом (нужно при проверке условий шага).
    /// </summary>
    public SurgeryContext WithStep(SurgeryStepPrototype? step, int stepIndex = 0)
    {
        return this with { Step = step, StepIndex = stepIndex };
    }
}

/// <summary>
/// На кого направлено условие или эффект. Используется в YAML как <c>target: Surgeon</c>.
/// </summary>
public enum SurgeryEntityTarget : byte
{
    /// <summary>Пациент (тело, над которым проводится операция).</summary>
    Patient,

    /// <summary>Хирург (тот, кто держит инструмент).</summary>
    Surgeon,

    /// <summary>Инструмент в руке хирурга.</summary>
    Tool,

    /// <summary>Операционный стол, к которому пристёгнут пациент.</summary>
    OperatingTable,
}
