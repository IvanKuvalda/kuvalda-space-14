using Content.Shared.DeadSpace.Surgery.Prototypes;

namespace Content.Shared.DeadSpace.Surgery;

/// <summary>
/// Полный контекст операции в момент проверки условия или применения эффекта.
/// </summary>
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
