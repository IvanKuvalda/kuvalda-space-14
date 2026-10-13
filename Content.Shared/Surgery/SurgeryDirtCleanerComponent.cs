using Robust.Shared.GameStates;

namespace Content.Shared.Surgery;

/// <summary>
/// Предмет, которым можно смывать загрязнение (мыло, тряпка со стерилином, спирт).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryDirtCleanerComponent : Component
{
    /// <summary>
    /// Сколько загрязнения смывается за одно применение.
    /// </summary>
    [DataField]
    public float Amount = 25f;

    /// <summary>
    /// Сообщение тому, кого чистят. Если не задано, используется общее.
    /// </summary>
    [DataField]
    public LocId? Popup;
}
