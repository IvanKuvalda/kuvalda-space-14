namespace Content.Shared.DeadSpace.Surgery.Conditions;

/// <summary>
/// Условие хирургии: «нет перчаток», «рядом мусор», «пациент не на столе», «это диона».
/// Одно и то же условие работает и в развилках шага, и в модификаторах осложнений.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryCondition
{
    [DataField]
    public bool Inverted;

    public bool Check(SurgeryContext context, ISurgeryConditionRaiser raiser)
    {
        return Inverted != RaiseEvent(context, raiser);
    }

    public abstract bool RaiseEvent(SurgeryContext context, ISurgeryConditionRaiser raiser);
}

public abstract partial class SurgeryConditionBase<T> : SurgeryCondition where T : SurgeryConditionBase<T>
{
    public override bool RaiseEvent(SurgeryContext context, ISurgeryConditionRaiser raiser)
    {
        return this is T type && raiser.RaiseConditionEvent(type, context);
    }
}

[ByRefEvent]
public record struct SurgeryConditionEvent<T>(T Condition, SurgeryContext Context) where T : SurgeryConditionBase<T>
{
    /// <summary>По умолчанию false: без обработчика условие не выполняется.</summary>
    public bool Result;

    public readonly T Condition = Condition;
    public readonly SurgeryContext Context = Context;
}

public interface ISurgeryConditionRaiser
{
    bool RaiseConditionEvent<T>(T condition, SurgeryContext context) where T : SurgeryConditionBase<T>;
}

/// <summary>Базовая система одного условия.</summary>
public abstract partial class SurgeryConditionSystem<TCon> : EntitySystem where TCon : SurgeryConditionBase<TCon>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryConditionEvent<TCon>>(OnCondition);
    }

    protected abstract void OnCondition(ref SurgeryConditionEvent<TCon> args);
}
