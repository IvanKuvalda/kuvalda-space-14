// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.MedicalRecords.Overlays;

/// <summary>
/// Marker component granting HUD visibility of the full medical status set, including the purely
/// medical ones ("on treatment", "completed treatment") that are hidden from security.
///
/// Worn by anything parenting <c>ShowMedicalIcons</c>, so it is enabled whenever a medical HUD is
/// equipped - including a combined medsec HUD, in which case the security system deliberately
/// stands down rather than stacking a second copy of the shared icons underneath.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowMedicalStatusIconsComponent : Component
{
}
