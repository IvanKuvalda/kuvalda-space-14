// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Access;
using Content.Shared.DeadSpace.MedicalRecords.Systems;
using Content.Shared.DeadSpace.Photocopier;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.MedicalRecords.Components;

/// <summary>
/// The medical records console, placed in the medbay. Reading and editing a card needs medical
/// access; deleting a case additionally needs <see cref="DeleteAccess"/>, since a deleted deviation
/// cannot be re-added without a round restart.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SharedMedicalRecordsConsoleSystem))]
public sealed partial class MedicalRecordsConsoleComponent : Component
{
    [DataField]
    public uint? ActiveKey;

    [DataField]
    public StationRecordsFilter? Filter;

    /// <summary>
    /// <see cref="MedicalStatus.None"/> means "don't filter by status".
    /// </summary>
    [DataField]
    public MedicalStatus FilterStatus;

    /// <summary>
    /// Access levels allowed to see the whole crew. A list anyway, so a future chief-only console is
    /// a one-line YAML change.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> FullAccess = new()
    {
        "ChiefMedicalOfficer",
    };

    /// <summary>
    /// Access level required to delete a case, on top of the console's own access. The client greys
    /// the button out without it, but the server re-checks independently.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype> DeleteAccess = "ChiefMedicalOfficer";

    /// <summary>
    /// Jobs never shown. Silicons are deliberately not on this list - a cyborg's implant history is
    /// real medical history.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> ExcludedJobs = new()
    {
        "Dismissed",
        "Visitor",
        "Magistrat",
    };

    /// <summary>
    /// Maximum length of any single free-text field on a case.
    /// </summary>
    [DataField]
    public uint MaxStringLength = 256;

    [DataField]
    public int MaxSpecialists = 8;

    /// <summary>
    /// Cap on a single patient's history, so one client cannot balloon the shared station record.
    /// </summary>
    [DataField]
    public int MaxCases = 64;

    /// <summary>
    /// Minimum time between state-changing actions.
    /// </summary>
    [DataField]
    public TimeSpan ActionDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextActionTime = TimeSpan.Zero;

    [DataField]
    public ProtoId<PaperworkFormPrototype> ConclusionForm = "MedicalConclusion";

    [DataField]
    public SoundSpecifier PrintSound = new SoundCollectionSpecifier("PrinterPrint");

    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPrintTime = TimeSpan.Zero;
}
