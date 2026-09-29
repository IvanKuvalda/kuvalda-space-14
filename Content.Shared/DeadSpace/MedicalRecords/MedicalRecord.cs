// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.MedicalRecords;

/// Medical record of a crewmember, sitting alongside <c>GeneralStationRecord</c> under the same
/// <c>StationRecordKey</c>. Species, age, gender and DNA stay in the general record - only sex and
/// height live here.
[Serializable, NetSerializable, DataRecord]
public sealed partial record MedicalRecord
{
    [DataField]
    public Sex Sex = Sex.Male;

    [DataField]
    public float Height = 1.8f;

    [DataField]
    public MedicalStatus Status = MedicalStatus.None;

    [DataField]
    public bool StatusManuallySet;

    [DataField]
    public List<MedicalCase> History = new();
}

/// One entry in a patient's history - an innate deviation or an admitted illness. Its index in
/// <see cref="MedicalRecord.History"/> is its identity, and is what the console's edit and delete
/// messages refer to.
[Serializable, NetSerializable, DataRecord]
public sealed partial record MedicalCase
{
    [DataField]
    public MedicalCaseKind Kind = MedicalCaseKind.Illness;

    [DataField]
    public TimeSpan AddTime = TimeSpan.Zero;

    [DataField]
    public string AdmissionState = string.Empty;

    [DataField]
    public string Diagnosis = string.Empty;

    [DataField]
    public string Treatment = string.Empty;

    [DataField]
    public bool NeedsContinuedTreatment;

    [DataField]
    public bool NeedsForcedTreatment;

    [DataField]
    public List<string> Specialists = new();

    [DataField]
    public string Recommendations = string.Empty;

    [DataField]
    public string DischargeState = string.Empty;

    [DataField]
    public string? AuthorName;

    [DataField]
    public bool Open = true;
}
