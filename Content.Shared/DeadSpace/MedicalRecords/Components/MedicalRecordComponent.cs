// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.MedicalRecords.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.MedicalRecords.Components;

/// <summary>
/// Holds the medical status icons currently shown for a mob. Added/removed by
/// <see cref="SharedMedicalRecordsSystem"/>, never present when the patient has no medical status.
///
/// Mirrors <c>CriminalRecordComponent</c> and <c>PersonnelRecordComponent</c>: the status itself
/// lives in the station's <see cref="MedicalRecord"/>, and all this networked component has to
/// carry is the icon ids, because the HUD gathers icons entirely clientside from replicated data.
///
/// A list rather than a single id because the two conditions are independent - a psychically
/// unstable patient who is also being held for forced treatment shows both.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMedicalRecordsSystem))]
public sealed partial class MedicalRecordComponent : Component
{
    /// <summary>
    /// Icons representing the patient's current medical status. Which of them a given HUD actually
    /// shows is decided clientside by <see cref="MedicalStatusIconPrototype.SecurityVisible"/> -
    /// see <c>ShowMedicalStatusIconsSystem</c> and <c>ShowSecurityMedicalStatusIconsSystem</c>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<MedicalStatusIconPrototype>> Icons = new();
}
