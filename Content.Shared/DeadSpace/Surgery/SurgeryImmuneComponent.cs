using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Surgery;

/// <summary>
/// Запрещает операции над сущностью: роботы, КПБ и прочие синтетики.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryImmuneComponent : Component;
