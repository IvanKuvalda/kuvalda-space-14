// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.MedicalRecords;

[Serializable, NetSerializable]
public enum MedicalRecordsConsoleKey : byte
{
    Key
}

/// <summary>
/// Medical Records console state. Selecting and filtering reuse <see cref="SelectStationRecord"/>
/// and <see cref="SetStationRecordFilter"/>, the same messages the other records consoles use.
///
/// The action flags are computed server-side on every state rebuild and only grey out buttons; the
/// server re-validates every action on receipt regardless.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Currently selected crewmember record key.
    /// </summary>
    public uint? SelectedKey;

    /// <summary>
    /// The selected crewmember's general record, supplying the name, job, age, species and DNA
    /// shown on the card. Sex and height come from <see cref="MedicalRecord"/> instead.
    /// </summary>
    public GeneralStationRecord? StationRecord;

    /// <summary>
    /// The selected crewmember's medical record.
    /// </summary>
    public MedicalRecord? MedicalRecord;

    public MedicalStatus FilterStatus;
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly StationRecordsFilter? Filter;

    /// <summary>
    /// True if the acting user may add and edit cases and change the patient status. Requires the
    /// console's own access level, i.e. the same check as being allowed to open it at all.
    /// </summary>
    public bool CanEdit;

    /// <summary>
    /// True if the acting user may delete cases. Narrower than <see cref="CanEdit"/>: deletion is
    /// reserved for the head of Medical.
    /// </summary>
    public bool CanDelete;

    /// <summary>
    /// True if the acting user may print a health conclusion for a case. Printing does not change
    /// any game state, so it follows visibility rather than edit rights.
    /// </summary>
    public bool CanPrint;

    public MedicalRecordsConsoleState(Dictionary<uint, string>? recordListing, StationRecordsFilter? filter)
    {
        RecordListing = recordListing;
        Filter = filter;
    }

    /// <summary>
    /// Default state for opening the console.
    /// </summary>
    public MedicalRecordsConsoleState() : this(null, null)
    {
    }

    public bool IsEmpty() => SelectedKey == null && StationRecord == null && MedicalRecord == null && RecordListing == null;
}

/// <summary>
/// Sets the patient-status filter for the crew listing (mirrors
/// <c>PersonnelRecordSetStatusFilter</c>). <see cref="MedicalStatus.None"/> clears the filter.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordSetStatusFilter : BoundUserInterfaceMessage
{
    public readonly MedicalStatus FilterStatus;

    public MedicalRecordSetStatusFilter(MedicalStatus filterStatus)
    {
        FilterStatus = filterStatus;
    }
}

/// <summary>
/// Sets the selected patient's status. The server refuses any transition it can't justify from the
/// record itself - <see cref="MedicalStatus.OnTreatment"/> needs an open case requiring continued
/// treatment, and <see cref="MedicalStatus.CompletedTreatment"/> needs there to be no such case -
/// so a client cannot simply declare a healthy crewmember cured or under treatment.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordChangeStatus : BoundUserInterfaceMessage
{
    public readonly MedicalStatus Status;

    public MedicalRecordChangeStatus(MedicalStatus status)
    {
        Status = status;
    }
}

/// <summary>
/// Appends a new case to the selected patient's history. The server re-validates every field and
/// owns <see cref="MedicalCase.AddTime"/> and <see cref="MedicalCase.AuthorName"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordAddCase : BoundUserInterfaceMessage
{
    public readonly string AdmissionState;
    public readonly string Diagnosis;
    public readonly string Treatment;
    public readonly bool NeedsContinuedTreatment;
    public readonly bool NeedsForcedTreatment;
    public readonly List<string> Specialists;
    public readonly string Recommendations;
    public readonly string DischargeState;

    public MedicalRecordAddCase(
        string admissionState,
        string diagnosis,
        string treatment,
        bool needsContinuedTreatment,
        bool needsForcedTreatment,
        List<string> specialists,
        string recommendations,
        string dischargeState)
    {
        AdmissionState = admissionState;
        Diagnosis = diagnosis;
        Treatment = treatment;
        NeedsContinuedTreatment = needsContinuedTreatment;
        NeedsForcedTreatment = needsForcedTreatment;
        Specialists = specialists;
        Recommendations = recommendations;
        DischargeState = dischargeState;
    }
}

/// <summary>
/// Overwrites the case at <see cref="Index"/>. Same field set as
/// <see cref="MedicalRecordAddCase"/>, minus kind and author, which an edit never rewrites.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordEditCase : BoundUserInterfaceMessage
{
    public readonly int Index;
    public readonly string AdmissionState;
    public readonly string Diagnosis;
    public readonly string Treatment;
    public readonly bool NeedsContinuedTreatment;
    public readonly bool NeedsForcedTreatment;
    public readonly List<string> Specialists;
    public readonly string Recommendations;
    public readonly string DischargeState;
    public readonly bool Open;

    public MedicalRecordEditCase(
        int index,
        string admissionState,
        string diagnosis,
        string treatment,
        bool needsContinuedTreatment,
        bool needsForcedTreatment,
        List<string> specialists,
        string recommendations,
        string dischargeState,
        bool open)
    {
        Index = index;
        AdmissionState = admissionState;
        Diagnosis = diagnosis;
        Treatment = treatment;
        NeedsContinuedTreatment = needsContinuedTreatment;
        NeedsForcedTreatment = needsForcedTreatment;
        Specialists = specialists;
        Recommendations = recommendations;
        DischargeState = dischargeState;
        Open = open;
    }
}

/// <summary>
/// Removes the case at <see cref="Index"/>. Rejected unless the actor holds the head of Medical's
/// access level - see <see cref="MedicalRecordsConsoleState.CanDelete"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordDeleteCase : BoundUserInterfaceMessage
{
    public readonly int Index;

    public MedicalRecordDeleteCase(int index)
    {
        Index = index;
    }
}

/// <summary>
/// Prints a health conclusion for a single case. Separate from
/// <see cref="MedicalRecordEditCase"/> so printing a closed case months later needs no edit rights.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordPrintCase : BoundUserInterfaceMessage
{
    public readonly int Index;

    public MedicalRecordPrintCase(int index)
    {
        Index = index;
    }
}
