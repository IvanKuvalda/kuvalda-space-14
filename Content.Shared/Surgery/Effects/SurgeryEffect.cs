using Content.Shared.Surgery.Prototypes;

namespace Content.Shared.Surgery.Effects;

/// <summary>
/// Последствие шага, операции или осложнения: урон, кровотечение, реагент, загрязнение и т.д.
/// </summary>
/// <remarks>
/// Два готовых вида эффектов покрывают почти всё:
/// <list type="bullet">
/// <item><see cref="SurgeryEntityEffect"/> — прогоняет обычные <c>EntityEffect</c>
/// (урон, реагенты, статусы) по выбранной сущности;</item>
/// <item><see cref="SurgeryDirtEffect"/> — меняет загрязнение.</item>
/// </list>
/// Новый вид последствия добавляется своим <see cref="SurgeryEffectSystem{T}"/>.
/// </remarks>
[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryEffect
{
    /// <summary>
    /// Шанс применения эффекта, 0..1.
    /// </summary>
    [DataField]
    public float Probability = 1f;

    /// <summary>
    /// Отправить событие применения своим системам-обработчикам.
    /// </summary>
    public abstract void RaiseEvent(SurgeryContext context, ISurgeryEffectRaiser raiser, float scale, EntityUid? user);
}

/// <summary>
/// Базовый тип эффекта, который умеет поднимать строго типизированное событие.
/// </summary>
public abstract partial class SurgeryEffectBase<T> : SurgeryEffect where T : SurgeryEffectBase<T>
{
    public override void RaiseEvent(SurgeryContext context, ISurgeryEffectRaiser raiser, float scale, EntityUid? user)
    {
        if (this is T type)
            raiser.RaiseEffectEvent(type, context, scale, user);
    }
}

/// <summary>
/// Событие применения эффекта.
/// </summary>
[ByRefEvent]
public record struct SurgeryEffectEvent<T>(T Effect, SurgeryContext Context, float Scale, EntityUid? User)
    where T : SurgeryEffectBase<T>
{
    public readonly T Effect = Effect;
    public readonly SurgeryContext Context = Context;
    public readonly float Scale = Scale;
    public readonly EntityUid? User = User;
}

/// <summary>
/// Тот, кто умеет рассылать события эффектов (реализуется <c>SharedSurgerySystem</c>).
/// </summary>
public interface ISurgeryEffectRaiser
{
    void RaiseEffectEvent<T>(T effect, SurgeryContext context, float scale, EntityUid? user) where T : SurgeryEffectBase<T>;
}

/// <summary>
/// Базовая система одного вида эффекта.
/// </summary>
public abstract partial class SurgeryEffectSystem<TEff> : EntitySystem where TEff : SurgeryEffectBase<TEff>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurgeryEffectEvent<TEff>>(OnEffect);
    }

    /// <summary>
    /// Применить эффект.
    /// </summary>
    protected abstract void OnEffect(ref SurgeryEffectEvent<TEff> args);
}
