// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.StationRecords.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.DeadSpace.MedicalRecords;
using Content.Shared.DeadSpace.MedicalRecords.Systems;
using Content.Shared.Preferences;
using Content.Shared.StationRecords;
using Content.Shared.Traits;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.MedicalRecords.Systems;

/// <summary>
/// Owns the <see cref="MedicalRecord"/> riding alongside every crewmember's
/// <c>GeneralStationRecord</c>, and the history-editing logic the console system calls into. No
/// permission checking happens here - <c>MedicalRecordsConsoleSystem</c> checks every action first.
/// </summary>
public sealed class MedicalRecordsSystem : SharedMedicalRecordsSystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        // Respawning as the same character reuses the existing general record and re-raises this
        // event. Keep whatever the record already has - re-seeding it would wipe a treatment the
        // patient was given earlier in the shift.
        if (_records.TryGetRecord<MedicalRecord>(ev.Key, out _))
            return;

        var record = new MedicalRecord
        {
            Sex = ev.Profile.Sex,
            Height = GetHeight(ev.Profile.Species),
            History = BuildDeviations(ev.Profile),
        };

        _records.AddRecordEntry(ev.Key, record);
        _records.Synchronize(ev.Key);
    }

    /// <summary>
    /// Average height in meters for a species, from <see cref="SpeciesPrototype.Height"/>.
    /// </summary>
    public float GetHeight(ProtoId<SpeciesPrototype> species)
    {
        if (_prototypeManager.TryIndex(species, out var prototype))
            return prototype.Height;

        return 1.8f;
    }

    /// <summary>
    /// Turns the player's chosen traits into the innate deviations the patient is born with. Traits
    /// without a <c>Category</c> are species baseline rather than something the player rolled, so
    /// they are not recorded.
    /// </summary>
    private List<MedicalCase> BuildDeviations(HumanoidCharacterProfile profile)
    {
        var deviations = new List<MedicalCase>();

        foreach (var traitId in profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex(traitId, out var trait))
                continue;

            if (trait.Category == null)
                continue;

            deviations.Add(new MedicalCase
            {
                Kind = MedicalCaseKind.Deviation,
                Diagnosis = _loc.GetString(trait.Name),
                // Deviations are standing facts rather than something being treated right now, so
                // they start closed. A doctor ticks "needs continued treatment" when they take the
                // patient on for management of one.
                Open = false,
            });
        }

        return deviations;
    }

    /// <summary>
    /// Appends a case filed by a member of staff. Timestamps and attributes it to the author, then
    /// re-derives the patient's status.
    /// </summary>
    /// <param name="key">Record of the patient being treated.</param>
    /// <param name="admissionState">Condition on admission.</param>
    /// <param name="diagnosis">Diagnosis. Mandatory - a case with no diagnosis is not a record.</param>
    /// <param name="treatment">Treatment administered so far.</param>
    /// <param name="needsContinuedTreatment">Whether the patient needs ongoing care.</param>
    /// <param name="needsForcedTreatment">Whether the patient has to be treated without consent.</param>
    /// <param name="specialists">Names of the specialists who treated the case.</param>
    /// <param name="recommendations">Recommendations left behind.</param>
    /// <param name="dischargeState">Condition at discharge.</param>
    /// <param name="authorName">Name of whoever filed the case.</param>
    public bool TryAddCase(
        StationRecordKey key,
        string admissionState,
        string diagnosis,
        string treatment,
        bool needsContinuedTreatment,
        bool needsForcedTreatment,
        List<string> specialists,
        string recommendations,
        string dischargeState,
        string? authorName)
    {
        if (!_records.TryGetRecord<MedicalRecord>(key, out var record))
            return false;

        record.History.Add(new MedicalCase
        {
            Kind = MedicalCaseKind.Illness,
            AddTime = _gameTiming.CurTime,
            AdmissionState = admissionState,
            Diagnosis = diagnosis,
            Treatment = treatment,
            NeedsContinuedTreatment = needsContinuedTreatment,
            NeedsForcedTreatment = needsForcedTreatment,
            Specialists = specialists,
            Recommendations = recommendations,
            DischargeState = dischargeState,
            AuthorName = authorName,
            Open = true,
        });

        Finalize(key, record);
        return true;
    }

    /// <summary>
    /// Overwrites an existing case in place, leaving its kind and author alone.
    /// </summary>
    public bool TryEditCase(
        StationRecordKey key,
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
        if (!_records.TryGetRecord<MedicalRecord>(key, out var record))
            return false;

        if (!TryGetCase(record, index, out var medicalCase))
            return false;

        medicalCase.AdmissionState = admissionState;
        medicalCase.Diagnosis = diagnosis;
        medicalCase.Treatment = treatment;
        medicalCase.NeedsContinuedTreatment = needsContinuedTreatment;
        medicalCase.NeedsForcedTreatment = needsForcedTreatment;
        medicalCase.Specialists = specialists;
        medicalCase.Recommendations = recommendations;
        medicalCase.DischargeState = dischargeState;
        medicalCase.Open = open;

        Finalize(key, record);
        return true;
    }

    /// <summary>
    /// Removes a case outright. The console system has already checked the actor's access.
    /// </summary>
    public bool TryDeleteCase(StationRecordKey key, int index)
    {
        if (!_records.TryGetRecord<MedicalRecord>(key, out var record))
            return false;

        if (!TryGetCase(record, index, out _))
            return false;

        record.History.RemoveAt(index);

        Finalize(key, record);
        return true;
    }

    /// <summary>
    /// Sets the patient status. The doctor's choice is authoritative and is deliberately not run
    /// through <see cref="RecalculateStatus"/> - that would immediately overwrite "on treatment"
    /// whenever no open case demands follow-up.
    /// </summary>
    public bool TryChangeStatus(StationRecordKey key, MedicalStatus status)
    {
        if (!_records.TryGetRecord<MedicalRecord>(key, out var record))
            return false;

        record.Status = status;
        record.StatusManuallySet = true;

        Finalize(key, record, recalculate: false);
        return true;
    }

    private bool TryGetCase(MedicalRecord record, int index, out MedicalCase medicalCase)
    {
        if (index >= 0 && index < record.History.Count)
        {
            medicalCase = record.History[index];
            return true;
        }

        medicalCase = null!;
        return false;
    }

    /// <summary>
    /// Pulls the status back in line with the history, after the history itself changed: an open
    /// continued-treatment case puts a patient on treatment, losing the last one moves them to
    /// completed treatment. A status set by hand is left alone - see
    /// <see cref="MedicalRecord.StatusManuallySet"/>.
    /// </summary>
    private MedicalStatus RecalculateStatus(MedicalRecord record)
    {
        var needsTreatment = record.History.Exists(medicalCase => medicalCase.Open && medicalCase.NeedsContinuedTreatment);

        switch (record.Status)
        {
            case MedicalStatus.None or MedicalStatus.CompletedTreatment when needsTreatment:
                record.Status = MedicalStatus.OnTreatment;
                record.StatusManuallySet = false;
                break;
            case MedicalStatus.OnTreatment when !needsTreatment && !record.StatusManuallySet:
                record.Status = record.History.Count > 0
                    ? MedicalStatus.CompletedTreatment
                    : MedicalStatus.None;
                break;
        }

        return record.Status;
    }

    /// <summary>
    /// Synchronizes the record and refreshes the patient's HUD icons.
    /// </summary>
    private void Finalize(StationRecordKey key, MedicalRecord record, bool recalculate = true)
    {
        if (recalculate)
            RecalculateStatus(record);

        _records.Synchronize(key);

        if (_records.TryGetRecord<GeneralStationRecord>(key, out var general))
            SetMedicalIcons(general.Name, record);
    }
}
