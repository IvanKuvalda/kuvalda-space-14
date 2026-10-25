using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.Jelly;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.DeadSpace.Jelly;

public sealed class JellyWobbleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ShaderPrototype> SoftBodyShader = "SoftBodyWobble";

    private const float PixelsPerMeter = EyeManager.PixelsPerMeter;
    private const float MaxRate = 90f;
    private const float AttachLag = 0.08f;
    private const float AttachRate = 0.5f;

    private readonly Dictionary<EntityUid, (ShaderInstance Shader, bool Attached)> _shaders = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JellyWobbleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<JellyWobbleComponent> ent, ref ComponentShutdown args)
    {
        if (!_shaders.Remove(ent.Owner, out var entry))
            return;

        if (!TerminatingOrDeleted(ent) && TryComp<SpriteComponent>(ent, out var sprite) && sprite.PostShader == entry.Shader)
            sprite.PostShader = null;

        entry.Shader.Dispose();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (frameTime <= 0f)
            return;

        var query = EntityQueryEnumerator<JellyWobbleComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var wobble, out var sprite))
        {
            Simulate(uid, wobble, frameTime);
            Render(uid, wobble, sprite, frameTime);
        }
    }

    private void Simulate(EntityUid uid, JellyWobbleComponent wobble, float dt)
    {
        var velocity = Vector2.Zero;

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            var worldRot = _transform.GetWorldRotation(uid);
            velocity = (worldRot - Transform(uid).LocalRotation).RotateVec(physics.LinearVelocity);
        }

        // резкий разворот, бег или тд колышет тело
        var deltaV = velocity - wobble.LastVelocity;
        wobble.LastVelocity = velocity;
        if (deltaV != Vector2.Zero)
        {
            wobble.Rate -= deltaV * wobble.MotionKick * PixelsPerMeter;
            wobble.Rate = Vector2.Clamp(wobble.Rate, new Vector2(-MaxRate), new Vector2(MaxRate));
        }

        // при получении урона, верх прям колышется, наверное, не тестил
        if (TryComp<DamageableComponent>(uid, out var damageable))
        {
            var total = damageable.TotalDamage.Float();
            if (wobble.LastDamage >= 0f && total > wobble.LastDamage)
                wobble.Rate.Y -= (total - wobble.LastDamage) * wobble.DamageKick;
            wobble.LastDamage = total;
        }

        var target = -velocity * wobble.VelocityLag * PixelsPerMeter;
        var omega = MathF.Tau * MathF.Max(wobble.WobbleFrequency, 0.01f);
        var zeta = Math.Clamp(wobble.WobbleDecay, 0.01f, 1f);
        var decay = MathF.Exp(-zeta * omega * dt);
        var damped = omega * MathF.Sqrt(1f - zeta * zeta);
        var sin = MathF.Sin(damped * dt);
        var cos = MathF.Cos(damped * dt);

        var rel0 = wobble.Lag - target;
        var rate0 = wobble.Rate;
        wobble.Lag = target + decay * (rel0 * cos + (rate0 + zeta * omega * rel0) / damped * sin);
        wobble.Rate = decay * (rate0 * cos - (omega * omega * rel0 + zeta * omega * rate0) / damped * sin);

        if (wobble.Lag.LengthSquared() > wobble.MaxLag * wobble.MaxLag)
            wobble.Lag *= wobble.MaxLag / wobble.Lag.Length();
    }

    private void Render(EntityUid uid, JellyWobbleComponent wobble, SpriteComponent sprite, float frameTime)
    {
        var alive = !TryComp<MobStateComponent>(uid, out var mob) || mob.CurrentState == MobState.Alive;

        if (alive && wobble.IdleBreath > 0f && wobble.IdleCycle > 0f)
            wobble.BreathPhase = (wobble.BreathPhase + frameTime / wobble.IdleCycle) % 1f;

        var breath = alive
            ? MathF.Sin(wobble.BreathPhase * MathF.Tau) * wobble.IdleBreath
            : 0f;

        var shift = wobble.Lag + new Vector2(0f, breath);
        var excited = shift.Length() > AttachLag || wobble.Rate.Length() > AttachRate;

        if (!excited)
        {
            if (_shaders.TryGetValue(uid, out var entry) && entry.Attached)
            {
                sprite.PostShader = null;
                _shaders[uid] = (entry.Shader, false);
            }
            return;
        }

        if (!_shaders.TryGetValue(uid, out var e))
        {
            e = (_proto.Index(SoftBodyShader).InstanceUnique(), false);
            _shaders[uid] = e;
        }

        if (!e.Attached || sprite.PostShader != e.Shader)
        {
            sprite.PostShader = e.Shader;
            _shaders[uid] = (e.Shader, true);
        }

        e.Shader.SetParameter("lag", shift);
    }
}
