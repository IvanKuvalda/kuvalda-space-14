using Content.Shared.Surgery.Conditions;
using Content.Shared.Surgery.Effects;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Prototypes;

/// <summary>
/// Шаг операции: «разрез», «растянуть», «зажать сосуд», «зашить» и т.д.
/// </summary>
/// <remarks>
/// Сам шаг ничего не делает — он лишь набор вариантов выполнения. Так одна и та же операция
/// сама разветвляется по ситуации: у дионы подходящим окажется вариант с топором,
/// у человека — со скальпелем.
/// </remarks>
[Prototype("surgeryStep")]
public sealed partial class SurgeryStepPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Варианты выполнения. Используется первый, у которого подходит инструмент и проходят условия.
    /// </summary>
    [DataField(required: true)]
    public SurgeryStepVariant[] Variants = [];

    /// <summary>
    /// Осложнения, которые проверяются после этого шага (в дополнение к осложнениям операции).
    /// </summary>
    [DataField]
    public ProtoId<SurgeryComplicationPrototype>[] Complications = [];
}

/// <summary>
/// Один из способов выполнить шаг: свой инструмент, свои условия, своё время и последствия.
/// </summary>
[DataDefinition]
public sealed partial class SurgeryStepVariant
{
    /// <summary>
    /// Каким инструментом это можно сделать. Можно перечислять теги, компоненты или размеры.
    /// Если не задано — шаг делается голыми руками.
    /// </summary>
    [DataField]
    public EntityWhitelist? Tool;

    /// <summary>
    /// Какие качества инструмента обязательны (Slicing, Sawing, ...).
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ToolQualityPrototype>> Qualities = [];

    /// <summary>
    /// Дополнительные условия: анатомия пациента, окружение, экипировка хирурга, состояние инструмента.
    /// </summary>
    [DataField]
    public SurgeryCondition[] Conditions = [];

    /// <summary>
    /// Базовое время шага в секундах. Делится на <c>SpeedModifier</c> инструмента.
    /// </summary>
    [DataField]
    public float Duration = 3f;

    /// <summary>
    /// Прерывать ли шаг, если хирург или пациент сдвинулся.
    /// </summary>
    [DataField]
    public bool BreakOnMove = true;

    /// <summary>
    /// Прерывать ли шаг от урона.
    /// </summary>
    [DataField]
    public bool BreakOnDamage = true;

    /// <summary>
    /// Нужна ли рука (и предмет в ней) для выполнения.
    /// </summary>
    [DataField]
    public bool NeedHand = true;

    /// <summary>
    /// Сообщение хирургу в начале шага.
    /// </summary>
    [DataField]
    public LocId? StartMessage;

    /// <summary>
    /// Сообщение хирургу после успешного шага.
    /// </summary>
    [DataField]
    public LocId? SuccessMessage;

    /// <summary>
    /// Звук шага.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Эффекты успешного шага.
    /// </summary>
    [DataField]
    public SurgeryEffect[] Effects = [];

    /// <summary>
    /// Эффекты прерванного шага: «дёрнулся — порезал глубже».
    /// </summary>
    [DataField]
    public SurgeryEffect[] FailEffects = [];

    /// <summary>
    /// Осложнения, которые проверяются после этого варианта шага.
    /// </summary>
    [DataField]
    public ProtoId<SurgeryComplicationPrototype>[] Complications = [];
}
