// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.MedicalRecords;

/// <summary>
/// Medical record of a crewmember, sitting alongside <c>GeneralStationRecord</c> under the same
/// <c>StationRecordKey</c>. Species, age, gender and DNA stay in the general record - only sex and
/// height live here.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial record MedicalRecord
{
    [DataField]
    public Sex Sex = Sex.Male;

    /// <summary>
    /// Height in meters, snapshotted at record creation from <c>SpeciesPrototype.Height</c> so a
    /// mid-round morph doesn't rewrite the patient's history.
    /// </summary>
    [DataField]
    public float Height = 1.8f;

    /// <summary>
    /// Current patient status, drives the medical HUD indicator.
    /// </summary>
    [DataField]
    public MedicalStatus Status = MedicalStatus.None;

    /// <summary>
    /// Whether a doctor set <see cref="Status"/> by hand. A manual status is not pulled back just
    /// because no open case demands follow-up; an open continued-treatment case still overrides it.
    /// </summary>
    [DataField]
    public bool StatusManuallySet;

    /// <summary>
    /// Innate deviations the patient was born with, then cases admitted on top. Newest last.
    /// </summary>
    [DataField]
    public List<MedicalCase> History = new();
}

/// <summary>
/// One entry in a patient's history - an innate deviation or an admitted illness. Its index in
/// <see cref="MedicalRecord.History"/> is its identity, and is what the console's edit and delete
/// messages refer to.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial record MedicalCase
{
    [DataField]
    public MedicalCaseKind Kind = MedicalCaseKind.Illness;

    /// <summary>
    /// Time from the start of the shift the case was opened at. Deviations use the patient's spawn
    /// time, so they sort ahead of anything admitted later.
    /// </summary>
    [DataField]
    public TimeSpan AddTime = TimeSpan.Zero;

    [DataField]
    public string AdmissionState = string.Empty;

    [DataField]
    public string Diagnosis = string.Empty;

    [DataField]
    public string Treatment = string.Empty;

    /// <summary>
    /// An open case with this set is what keeps <see cref="MedicalStatus.OnTreatment"/> from
    /// clearing.
    /// </summary>
    [DataField]
    public bool NeedsContinuedTreatment;

    /// <summary>
    /// Whether the patient cannot or will not consent. This is the flag security acts on, and it
    /// is shown on their HUD separately from <see cref="MedicalStatus.PsychUnstable"/>.
    /// </summary>
    [DataField]
    public bool NeedsForcedTreatment;

    /// <summary>
    /// Free text rather than <c>EntityUid</c>s: the doctor filing the case is often not the one who
    /// treated it.
    /// </summary>
    [DataField]
    public List<string> Specialists = new();

    [DataField]
    public string Recommendations = string.Empty;

    /// <summary>
    /// Condition at the end of the case. Empty while the case is open.
    /// </summary>
    [DataField]
    public string DischargeState = string.Empty;

    /// <summary>
    /// Whoever filed the case, or null for auto-populated deviations.
    /// </summary>
    [DataField]
    public string? AuthorName;

    /// <summary>
    /// A closed case is still printed and still counted for
    /// <see cref="MedicalStatus.CompletedTreatment"/>, but no longer requires follow-up.
    /// </summary>
    [DataField]
    public bool Open = true;
}
