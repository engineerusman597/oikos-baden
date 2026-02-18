namespace Oikos.Domain.Enums;

/// <summary>
/// Exactly one per case. Used for overview list and filtering.
/// </summary>
public enum InvoicePrimaryStatus
{
    /// <summary>
    /// Entwurf
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Eingereicht
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// In Prüfung
    /// </summary>
    InReview = 2,

    /// <summary>
    /// Rückfrage
    /// </summary>
    Inquiry = 3,

    /// <summary>
    /// Akzeptiert
    /// </summary>
    Accepted = 4,

    /// <summary>
    /// Gericht
    /// </summary>
    Court = 5,

    /// <summary>
    /// Abgeschlossen
    /// </summary>
    Completed = 6
}
