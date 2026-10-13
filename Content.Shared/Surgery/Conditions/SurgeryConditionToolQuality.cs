using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Проверяет качества предмета (Slicing, Sawing, Prying, ...) — в первую очередь инструмента.
/// </summary>
/// <remarks>
/// Полезно в осложнениях: «резали не тем инструментом» можно описать условием на качество,
/// не перечисляя конкретные прототипы.
/// </remarks>
public sealed partial class SurgeryConditionToolQuality : SurgeryConditionBase<SurgeryConditionToolQuality>
{
    /// <summary>
    /// Кого проверяем.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Tool;

    /// <summary>
    /// Требуемые качества.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ToolQualityPrototype>> Qualities = [];

    /// <summary>
    /// Требовать все качества (<c>true</c>) или любое из них (<c>false</c>).
    /// </summary>
    [DataField]
    public bool RequireAll = true;
}

public sealed partial class SurgeryConditionToolQualitySystem : SurgeryConditionSystem<SurgeryConditionToolQuality>
{
    protected override void OnCondition(ref SurgeryConditionEvent<SurgeryConditionToolQuality> args)
    {
        args.Result = false;

        if (args.Context.GetEntity(args.Condition.Target) is not { } ent)
            return;

        if (!TryComp<ToolComponent>(ent, out var tool))
            return;

        if (args.Condition.Qualities.Count == 0)
            return;

        foreach (var quality in args.Condition.Qualities)
        {
            var matches = tool.Qualities.Contains(quality.Id);

            if (args.Condition.RequireAll && !matches)
                return;

            if (!args.Condition.RequireAll && matches)
            {
                args.Result = true;
                return;
            }
        }

        args.Result = args.Condition.RequireAll;
    }
}
