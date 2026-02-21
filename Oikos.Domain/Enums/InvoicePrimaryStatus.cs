namespace Oikos.Domain.Enums;

/// <summary>
/// Exactly one per case. Used for overview list and filtering.
/// Mirrors the Replit adminStatusEnum (stored as int, backward-compatible).
/// </summary>
public enum InvoicePrimaryStatus
{
    /// <summary>Entwurf</summary>
    Draft = 0,

    /// <summary>Eingereicht</summary>
    Submitted = 1,

    /// <summary>In Prüfung</summary>
    InReview = 2,

    /// <summary>Rückfrage / Dokumente fehlen</summary>
    Inquiry = 3,

    /// <summary>Akzeptiert</summary>
    Accepted = 4,

    /// <summary>Ans Gericht versendet</summary>
    Court = 5,

    /// <summary>Abgeschlossen</summary>
    Completed = 6,

    /// <summary>Gericht vorbereiten (COURT_PREP) — shown to users as Akzeptiert</summary>
    CourtPrep = 7,

    /// <summary>Warten auf Gericht (WAITING_COURT)</summary>
    WaitingCourt = 8,

    /// <summary>Frist läuft (DEADLINE_RUNNING)</summary>
    DeadlineRunning = 9,

    /// <summary>Gerichtsantwort erhalten (COURT_RESPONSE)</summary>
    CourtResponse = 10,

    /// <summary>Vollstreckung möglich (ENFORCEMENT_READY)</summary>
    EnforcementReady = 11,

    /// <summary>Vollstreckung läuft (ENFORCEMENT_IN_PROGRESS)</summary>
    EnforcementInProgress = 12,

    /// <summary>Storniert (CANCELLED)</summary>
    Cancelled = 13,

    /// <summary>Abgelehnt (REJECTED)</summary>
    Rejected = 14
}
