namespace Content.Shared.Surgery.Effects;

/// <summary>
/// Меняет загрязнение сущности: испачкал руки о пациента, занёс грязь на инструмент,
/// помыл руки (отрицательное значение).
/// </summary>
/// <remarks>
/// Если у сущности нет <see cref="SurgeryDirtinessComponent"/>, он добавляется автоматически —
/// так «грязные руки» работают у любого моба без правок его прототипа.
/// </remarks>
public sealed partial class SurgeryDirtEffect : SurgeryEffectBase<SurgeryDirtEffect>
{
    /// <summary>
    /// Кому меняем загрязнение.
    /// </summary>
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Surgeon;

    /// <summary>
    /// Насколько изменить загрязнение. Отрицательное значение отмывает.
    /// </summary>
    [DataField]
    public float Amount = 10f;
}

public sealed partial class SurgeryDirtEffectSystem : SurgeryEffectSystem<SurgeryDirtEffect>
{
    protected override void OnEffect(ref SurgeryEffectEvent<SurgeryDirtEffect> args)
    {
        if (args.Context.GetEntity(args.Effect.Target) is not { } ent)
            return;

        var dirt = EnsureComp<SurgeryDirtinessComponent>(ent);
        dirt.Amount = Math.Clamp(dirt.Amount + args.Effect.Amount * args.Scale, 0f, dirt.Max);
        Dirty(ent, dirt);
    }
}
