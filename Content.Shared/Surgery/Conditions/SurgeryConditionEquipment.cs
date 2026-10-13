using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет, что на нужном слоте надет (или не надет) подходящий предмет.
/// </summary>
/// <remarks>
/// «Нет масок» и «нет перчаток» — это одно и то же условие с <c>inverted: true</c>:
/// <code>
/// conditions:
/// - !type: SurgeryConditionEquipment
///   slot: gloves
///   inverted: true
/// </code>
/// А «перчатки должны быть именно стерильными» — это тот же слот плюс <c>whitelist</c> по тегу.
/// </remarks>
public sealed partial class SurgeryConditionEquipment : SurgeryConditionBase<SurgeryConditionEquipment>
{
    /// <summary>
    /// Слот инвентаря: gloves, mask, head, outerClothing и т.д.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;

    /// <summary>
    /// Каким должен быть предмет в слоте. Если не задано — подойдёт любой.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Кого проверяем.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Surgeon;
}

public sealed partial class SurgeryConditionEquipmentSystem : SurgeryConditionSystem<SurgeryConditionEquipment>
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionEquipment> args)
    {
        args.Result = false;

        if (args.Context.GetEntity(args.Condition.Target) is not { } wearer)
            return;

        if (!_inventory.TryGetSlotEntity(wearer, args.Condition.Slot, out var item))
            return;

        if (args.Condition.Whitelist is { } whitelist && !_whitelist.IsValid(whitelist, item.Value))
            return;

        args.Result = true;
    }
}
