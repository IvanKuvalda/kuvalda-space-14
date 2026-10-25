using System.Numerics;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Jelly;

[RegisterComponent, NetworkedComponent]
public sealed partial class JellyWobbleComponent : Component
{
    /// <summary>частота колебаний в герцах, НЕЗАТУХАЮЩИЕ</summary>
    [DataField]
    public float WobbleFrequency = 2.8f;

    /// <summary>коэфициент затухания колебаний от нуля до однёрки</summary>
    [DataField]
    public float WobbleDecay = 0.3f;

    /// <summary>импульс запаздывания на единицу дельты ускорения, в метрах на метрах в секунду квадрат, это больно</summary>
    [DataField]
    public float MotionKick = 0.45f;

    [DataField]
    public float VelocityLag = 0.045f;

    /// <summary>максимум сколько строка может задержатся в пикселях текстуры</summary>
    [DataField]
    public float MaxLag = 6f;

    /// <summary>импульс запаздывания вниз в верхних строках на единицу полученного урона, в пикселях текстуры в секунду</summary>
    [DataField]
    public float DamageKick = 90f;

    /// <summary>Амплитуда пульсации в режиме ожидания, в пикселях текстуры, нолик тоже можно</summary>
    [DataField]
    public float IdleBreath = 1.2f;

    [DataField]
    public float IdleCycle = 2.6f;

    [ViewVariables]
    public Vector2 Lag;

    [ViewVariables]
    public Vector2 Rate;

    /// <summary локальная скорость на предыдущем кадре, м/с.</summary>
    [ViewVariables]
    public Vector2 LastVelocity;

    [ViewVariables]
    public float LastDamage = -1f;

    /// <summary>холостой ход фаза пульсации, не знаю как это обьяснить, проверьте сами)))</summary>
    [ViewVariables]
    public float BreathPhase;
}

// P.s - когда я лежал в больнице с гидронефрозом, часто гэгал с поца у которого порвалась жопа. теперь, когда я это пишу, я понимаю, насколько ему было обидно.
// P.P.s - третий ранг, кувалда, победа