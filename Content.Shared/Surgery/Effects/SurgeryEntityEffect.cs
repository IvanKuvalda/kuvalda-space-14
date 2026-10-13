using Content.Shared.EntityEffects;

namespace Content.Shared.Surgery.Effects;

/// <summary>
/// Прогоняет список обычных <see cref="EntityEffect"/> по выбранной сущности.
/// </summary>
/// <remarks>
/// Благодаря этому последствия операции описываются уже существующими эффектами:
/// урон, кровотечение, реагенты, статусы, спавн сущностей. Например:
/// <code>
/// - !type: SurgeryEntityEffect
///   target: Patient
///   effects:
///   - !type:ModifyBleed
///     amount: 10
/// </code>
/// </remarks>
public sealed partial class SurgeryEntityEffect : SurgeryEffectBase<SurgeryEntityEffect>
{
    /// <summary>
    /// На кого применяется эффект.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Patient;

    /// <summary>
    /// Сами эффекты.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = [];

    /// <summary>
    /// Множитель силы эффектов.
    /// </summary>
    [DataField]
    public float Scale = 1f;
}

public sealed partial class SurgeryEntityEffectSystem : SurgeryEffectSystem<SurgeryEntityEffect>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void OnEffect(ref SurgeryEffectEvent<SurgeryEntityEffect> args)
    {
        if (args.Context.GetEntity(args.Effect.Target) is not { } ent)
            return;

        _effects.ApplyEffects(ent, args.Effect.Effects, args.Effect.Scale * args.Scale, args.User);
    }
}
