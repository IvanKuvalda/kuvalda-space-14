using Content.Shared.DeadSpace.Surgery.Conditions;
using Content.Shared.DeadSpace.Surgery.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Surgery.Prototypes;

/// <summary>Операция: список шагов плюс условия и последствия.</summary>
[Prototype("surgery")]
public sealed partial class SurgeryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Name;

    [DataField]
    public LocId? Description;

    [DataField(required: true)]
    public ProtoId<SurgeryStepPrototype>[] Steps = [];

    /// <summary>Операции, которые должны быть завершены раньше.</summary>
    [DataField]
    public ProtoId<SurgeryPrototype>[] Requires = [];

    [DataField]
    public SurgeryCondition[] Conditions = [];

    [DataField]
    public ProtoId<SurgeryComplicationPrototype>[] Complications = [];

    [DataField]
    public SurgeryEffect[] Effects = [];

    /// <summary>Если false, после завершения операция больше не предлагается.</summary>
    [DataField]
    public bool Repeatable;

    /// <summary>Приоритет выбора, когда подходит несколько операций. Больше — раньше.</summary>
    [DataField]
    public int Priority;
}