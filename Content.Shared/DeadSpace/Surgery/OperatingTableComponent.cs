using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Surgery;

/// <summary>
/// Операционный стол. Условия операции могут требовать, чтобы пациент лежал именно на нём.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OperatingTableComponent : Component;
