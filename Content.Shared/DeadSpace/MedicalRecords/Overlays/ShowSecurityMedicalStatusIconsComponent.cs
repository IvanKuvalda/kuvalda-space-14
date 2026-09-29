// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.MedicalRecords.Overlays;

/// <summary>
/// Marker component granting HUD visibility of the subset of medical statuses that security has a
/// reason to act on - "psychically unstable" and "needs forced treatment" - on a security HUD.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowSecurityMedicalStatusIconsComponent : Component
{
}
