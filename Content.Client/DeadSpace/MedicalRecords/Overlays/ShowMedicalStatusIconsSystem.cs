// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.Overlays;
using Content.Shared.DeadSpace.MedicalRecords.Components;
using Content.Shared.DeadSpace.MedicalRecords.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.MedicalRecords.Overlays;

/// <summary>
/// Renders the medical status set on the HUD: the full set on a medical HUD, and the
/// security-visible subset on a security-only HUD. Mirrors
/// <c>Content.Client.DeadSpace.PersonnelRecords.Overlays.ShowPersonnelRecordIconsSystem</c>, except
/// that a record can imply more than one icon at once (a patient both on treatment and flagged for
/// forced treatment), so the whole list is walked rather than a single prototype resolved.
///
/// This is also the *only* subscriber to <see cref="GetStatusIconsEvent"/> for
/// <see cref="MedicalRecordComponent"/>. The event bus permits one subscription per
/// (component, event) pair, so the security layer is handled here too, keyed off
/// <see cref="ShowSecurityMedicalStatusIconsSystem.IsActive"/>.
/// </summary>
public sealed class ShowMedicalStatusIconsSystem : EquipmentHudSystem<ShowMedicalStatusIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Queried for the security HUD layer's active state rather than the component, because "is
    /// security currently looking through its own goggles" is exactly that system's job.
    /// </summary>
    [Dependency] private readonly ShowSecurityMedicalStatusIconsSystem _security = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedicalRecordComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<MedicalRecordComponent> ent, ref GetStatusIconsEvent ev)
    {
        if (IsActive)
        {
            foreach (var icon in ent.Comp.Icons)
            {
                if (_prototype.Resolve(icon, out MedicalStatusIconPrototype? iconPrototype))
                    ev.StatusIcons.Add(iconPrototype);
            }

            return;
        }

        // A medical HUD already draws everything the record implies, so a combined medsec HUD -
        // which is active on both layers - gets the full set exactly once instead of the shared
        // icons being stacked underneath themselves.
        if (!_security.IsActive)
            return;

        foreach (var icon in ent.Comp.Icons)
        {
            if (_prototype.Resolve(icon, out MedicalStatusIconPrototype? iconPrototype)
                && iconPrototype.SecurityVisible)
            {
                ev.StatusIcons.Add(iconPrototype);
            }
        }
    }
}
