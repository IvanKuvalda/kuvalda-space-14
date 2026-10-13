using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Surgery;

/// <summary>
/// Помечает тело, над которым в принципе можно проводить операции.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTargetComponent : Component;
