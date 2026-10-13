using Content.Shared.DeadSpace.Surgery.Conditions;
using Content.Shared.DeadSpace.Surgery.Effects;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Surgery.Prototypes;

/// <summary>Шаг операции: разрез, растянуть, зажать сосуд, зашить.</summary>
[Prototype("surgeryStep")]
public sealed partial class SurgeryStepPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Используется первый вариант, у которого подходит инструмент и проходят условия.</summary>
    [DataField(required: true)]
    public SurgeryStepVariant[] Variants = [];

    [DataField]
    public ProtoId<SurgeryComplicationPrototype>[] Complications = [];
}

/// <summary>Один способ выполнить шаг: свой инструмент, условия, время и последствия.</summary>
[DataDefinition]
public sealed partial class SurgeryStepVariant
{
    /// <summary>Если не задано вместе с Qualities — шаг делается голыми руками.</summary>
    [DataField]
    public EntityWhitelist? Tool;

    [DataField]
    public HashSet<ProtoId<ToolQualityPrototype>> Qualities = [];

    [DataField]
    public SurgeryCondition[] Conditions = [];

    /// <summary>Базовое время в секундах, делится на скорость инструмента.</summary>
    [DataField]
    public float Duration = 3f;

    [DataField]
    public bool BreakOnMove = true;

    [DataField]
    public bool BreakOnDamage = true;

    [DataField]
    public bool NeedHand = true;

    [DataField]
    public LocId? StartMessage;

    [DataField]
    public LocId? SuccessMessage;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public SurgeryEffect[] Effects = [];

    /// <summary>Эффекты прерванного шага: «дёрнулся — порезал глубже».</summary>
    [DataField]
    public SurgeryEffect[] FailEffects = [];

    [DataField]
    public ProtoId<SurgeryComplicationPrototype>[] Complications = [];
}