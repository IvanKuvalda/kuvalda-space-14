namespace Content.Shared.DeadSpace.Surgery.Effects;

/// <summary>Меняет загрязнение сущности. Отрицательное значение отмывает.</summary>
public sealed partial class SurgeryDirtEffect : SurgeryEffectBase<SurgeryDirtEffect>
{
    [DataField]
    public SurgeryEntityTarget Target = SurgeryEntityTarget.Surgeon;

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