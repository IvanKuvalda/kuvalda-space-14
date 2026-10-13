using Content.Shared.Surgery.Conditions;
using Content.Shared.Surgery.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Prototypes;

/// <summary>
/// Операция — это просто список шагов плюс условия и последствия.
/// </summary>
/// <remarks>
/// Пример добавления операции без единой строчки кода:
/// <code>
/// - type: surgery
///   id: Appendectomy
///   steps:
///   - SurgeryStepIncision
///   - SurgeryStepRetract
///   - SurgeryStepSuture
///   complications:
///   - SurgeryComplicationInfection
///   effects:
///   - !type: SurgeryEntityEffect
///     target: Patient
///     effects:
///     - !type:HealthChange
///       damage:
///         types:
///           Slash: -10
/// </code>
/// </remarks>
[Prototype("surgery")]
public sealed partial class SurgeryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название для UI и гайдбука.
    /// </summary>
    [DataField]
    public LocId? Name;

    /// <summary>
    /// Описание для UI и гайдбука.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Шаги по порядку. Каждый шаг — отдельный прототип, у которого могут быть варианты выполнения.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SurgeryStepPrototype>[] Steps = [];

    /// <summary>
    /// Операции, которые должны быть завершены раньше.
    /// </summary>
    [DataField]
    public ProtoId<SurgeryPrototype>[] Requires = [];

    /// <summary>
    /// Условия доступности операции (анатомия, состояние пациента и т.д.).
    /// </summary>
    [DataField]
    public SurgeryCondition[] Conditions = [];

    /// <summary>
    /// Осложнения, которые проверяются на каждом шаге этой операции.
    /// </summary>
    [DataField]
    public ProtoId<SurgeryComplicationPrototype>[] Complications = [];

    /// <summary>
    /// Эффекты при успешном завершении операции.
    /// </summary>
    [DataField]
    public SurgeryEffect[] Effects = [];

    /// <summary>
    /// Можно ли повторять операцию (иначе после завершения она больше не предлагается).
    /// </summary>
    [DataField]
    public bool Repeatable;

    /// <summary>
    /// Приоритет выбора, когда подходит несколько операций. Больше — раньше.
    /// </summary>
    [DataField]
    public int Priority;
}
