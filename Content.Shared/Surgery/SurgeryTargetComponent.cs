using Robust.Shared.GameStates;

namespace Content.Shared.Surgery;

/// <summary>
/// Помечает тело, над которым в принципе можно проводить операции.
/// </summary>
/// <remarks>
/// Само по себе ничего не делает: какие операции доступны и с каким инструментом,
/// решают прототипы операций, их условия и состояние цели.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTargetComponent : Component;
