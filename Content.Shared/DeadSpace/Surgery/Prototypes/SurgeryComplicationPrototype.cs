using Content.Shared.DeadSpace.Surgery.Conditions;
using Content.Shared.DeadSpace.Surgery.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Surgery.Prototypes;

/// <summary>Осложнение: шанс плюс модификаторы, которые включаются условиями.</summary>
[Prototype("surgeryComplication")]
public sealed partial class SurgeryComplicationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Name;

    [DataField]
    public float Chance;

    [DataField]
    public SurgeryChanceModifier[] Modifiers = [];

    [DataField]
    public SurgeryEffect[] Effects = [];

    /// <summary>Сообщение хирургу.</summary>
    [DataField]
    public LocId? Message;

    /// <summary>Сообщение пациенту.</summary>
    [DataField]
    public LocId? PatientMessage;
}

/// <summary>Итог считается как (база + add) * multiply по каждому подошедшему модификатору.</summary>
[DataDefinition]
public sealed partial class SurgeryChanceModifier
{
    [DataField]
    public SurgeryCondition[] Conditions = [];

    [DataField]
    public float Add;

    [DataField]
    public float Multiply = 1f;
}