using Content.Shared.Surgery.Conditions;
using Content.Shared.Surgery.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Prototypes;

/// <summary>
/// Осложнение: то, что может пойти не так. Шанс задаётся базовым значением и модификаторами,
/// а модификаторы включаются условиями — мусор у стола, грязные руки, отсутствие маски и т.д.
/// </summary>
/// <remarks>
/// Условие может быть и обязательным: если базовый <c>chance: 0</c>, а модификатор со своими
/// условиями добавляет шанс, осложнение сработает только при этих условиях.
/// <code>
/// - type: surgeryComplication
///   id: SurgeryComplicationInfection
///   chance: 0.02
///   modifiers:
///   - conditions:
///     - !type: SurgeryConditionEquipment
///       slot: gloves
///       inverted: true
///     add: 0.15
/// </code>
/// </remarks>
[Prototype("surgeryComplication")]
public sealed partial class SurgeryComplicationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название для UI, логов и гайдбука.
    /// </summary>
    [DataField]
    public LocId? Name;

    /// <summary>
    /// Базовый шанс срабатывания, 0..1.
    /// </summary>
    [DataField]
    public float Chance;

    /// <summary>
    /// Модификаторы шанса, срабатывающие по условиям.
    /// </summary>
    [DataField]
    public SurgeryChanceModifier[] Modifiers = [];

    /// <summary>
    /// Что происходит, если осложнение сработало.
    /// </summary>
    [DataField]
    public SurgeryEffect[] Effects = [];

    /// <summary>
    /// Сообщение хирургу.
    /// </summary>
    [DataField]
    public LocId? Message;

    /// <summary>
    /// Сообщение пациенту.
    /// </summary>
    [DataField]
    public LocId? PatientMessage;
}

/// <summary>
/// Правка шанса осложнения при выполнении условий.
/// Итог считается так: <c>(база + add) * multiply</c> по каждому подошедшему модификатору.
/// </summary>
[DataDefinition]
public sealed partial class SurgeryChanceModifier
{
    /// <summary>
    /// Условия, при которых модификатор применяется.
    /// </summary>
    [DataField]
    public SurgeryCondition[] Conditions = [];

    /// <summary>
    /// Прибавка к шансу (может быть отрицательной).
    /// </summary>
    [DataField]
    public float Add;

    /// <summary>
    /// Множитель шанса.
    /// </summary>
    [DataField]
    public float Multiply = 1f;
}
