using Robust.Shared.GameStates;

namespace Content.Shared.Surgery;

/// <summary>
/// Загрязнение сущности: руки и перчатки хирурга, инструмент, стол, тело пациента.
/// </summary>
/// <remarks>
/// Само значение ничего не значит: условия (<c>SurgeryConditionDirt</c>) сравнивают его с порогом,
/// а эффекты (<c>SurgeryDirtEffect</c>) его меняют. Так «грязные руки» и «грязный инструмент»
/// описываются данными, а не проверками в коде.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryDirtinessComponent : Component
{
    /// <summary>
    /// Текущий уровень загрязнения.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Amount;

    /// <summary>
    /// Верхняя граница загрязнения.
    /// </summary>
    [DataField]
    public float Max = 100f;
}
