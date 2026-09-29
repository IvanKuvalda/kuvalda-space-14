// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.Overlays;
using Content.Shared.DeadSpace.MedicalRecords.Overlays;

namespace Content.Client.DeadSpace.MedicalRecords.Overlays;

/// <summary>
/// Tracks whether the local player is currently looking through a security HUD, so that
/// <see cref="ShowMedicalStatusIconsSystem"/> can fall back to the security-visible subset of the
/// medical icons when no medical HUD is equipped.
///
/// It deliberately does not subscribe to <c>GetStatusIconsEvent</c> itself: the event bus allows a
/// single subscription per (component, event) pair, and the icons are read off
/// <c>MedicalRecordComponent</c> for both layers, so collecting them has to live in one place.
/// </summary>
public sealed class ShowSecurityMedicalStatusIconsSystem : EquipmentHudSystem<ShowSecurityMedicalStatusIconsComponent>
{
}
