namespace Content.Shared.Surgery.Conditions;

/// <summary>
/// Условие хирургии: «у хирурга нет перчаток», «рядом мусор», «пациент не на столе», «это диона».
/// </summary>
/// <remarks>
/// Один и тот же набор условий используется в двух местах:
/// <list type="bullet">
/// <item>выбор варианта шага (развилки операций от ситуации);</item>
/// <item>включение модификаторов осложнений.</item>
/// </list>
/// Новые условия добавляются отдельным <see cref="SurgeryConditionSystem{TCon}"/> и не требуют
/// правок ядра. Классы условий живут в данных (YAML) как <c>!type:</c>.
/// </remarks>
[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryCondition
{
    /// <summary>
    /// Инвертировать результат условия.
    /// </summary>
    [DataField]
    public bool Inverted;

    /// <summary>
    /// Проверить условие в контексте операции.
    /// </summary>
    public bool Check(SurgeryContext context, ISurgeryConditionRaiser raiser)
    {
        return Inverted != RaiseEvent(context, raiser);
    }

    /// <summary>
    /// Отправить событие проверки своим системам-обработчикам.
    /// </summary>
    public abstract bool RaiseEvent(SurgeryContext context, ISurgeryConditionRaiser raiser);
}

/// <summary>
/// Базовый тип условия, который умеет поднимать строго типизированное событие.
/// </summary>
public abstract partial class SurgeryConditionBase<T> : SurgeryCondition where T : SurgeryConditionBase<T>
{
    public override bool RaiseEvent(SurgeryContext context, ISurgeryConditionRaiser raiser)
    {
        return this is T type && raiser.RaiseConditionEvent(type, context);
    }
}

/// <summary>
/// Событие проверки условия. Обработчик выставляет <see cref="Result"/>.
/// </summary>
[ByRefEvent]
public record struct SurgeryConditionEvent<T>(T Condition, SurgeryContext Context) where T : SurgeryConditionBase<T>
{
    /// <summary>
    /// Результат проверки. По умолчанию false: если обработчика нет, условие не выполняется.
    /// </summary>
    public bool Result;

    public readonly T Condition = Condition;
    public readonly SurgeryContext Context = Context;
}

/// <summary>
/// Тот, кто умеет рассылать события условий (реализуется <c>SharedSurgerySystem</c>).
/// </summary>
public interface ISurgeryConditionRaiser
{
    bool RaiseConditionEvent<T>(T condition, SurgeryContext context) where T : SurgeryConditionBase<T>;
}

/// <summary>
/// Базовая система одного условия. Наследник просто подписывается и реализует <see cref="OnCondition"/>.
/// </summary>
public abstract partial class SurgeryConditionSystem<TCon> : EntitySystem where TCon : SurgeryConditionBase<TCon>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryConditionEvent<TCon>>(OnCondition);
    }

    /// <summary>
    /// Проверить условие и выставить <c>args.Result</c>.
    /// </summary>
    protected abstract void OnCondition(ref SurgeryConditionEvent<TCon> args);
}
