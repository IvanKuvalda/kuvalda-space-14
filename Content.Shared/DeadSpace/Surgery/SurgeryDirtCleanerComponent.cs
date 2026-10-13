using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Surgery;

/// <summary>Предмет, которым смывают загрязнение: мыло, тряпка со стерилином, спирт.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryDirtCleanerComponent : Component
{
    [DataField]
    public float Amount = 25f;

    [DataField]
    public LocId? Popup;
}
