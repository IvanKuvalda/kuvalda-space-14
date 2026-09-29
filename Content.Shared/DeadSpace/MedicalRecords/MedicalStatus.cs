// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.MedicalRecords;

/// <summary>
/// Patient status, tracked by the medical records console and rendered on
/// <c>MedicalRecordComponent</c>. Deliberately separate from <c>SecurityStatus</c>: a medical status
/// never reaches the criminal records console and never raises a wanted flag.
/// </summary>
[Serializable, NetSerializable]
public enum MedicalStatus : byte
{
    /// <summary>
    /// Healthy, or every case requiring follow-up has been closed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Undergoing treatment. Medical HUD only - of no interest to security.
    /// </summary>
    OnTreatment,

    /// <summary>
    /// Psychologically unstable. Shown on the security HUD too, and by itself counts as needing
    /// forced treatment.
    /// </summary>
    PsychUnstable,

    /// <summary>
    /// Completed a course of treatment. Medical HUD only, and purely informational.
    /// </summary>
    CompletedTreatment,
}

[Serializable, NetSerializable]
public enum MedicalCaseKind : byte
{
    /// <summary>
    /// An innate deviation the humanoid was born with. Auto-populated at record creation, and only
    /// deletable through <c>MedicalRecordsConsoleComponent.DeleteAccess</c>.
    /// </summary>
    Deviation = 0,

    /// <summary>
    /// A disease or injury treated during the shift, filed by hand through the console.
    /// </summary>
    Illness,
}
