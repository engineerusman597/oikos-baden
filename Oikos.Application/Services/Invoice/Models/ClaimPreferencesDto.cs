namespace Oikos.Application.Services.Invoice.Models;

public enum ClaimStartOption
{
    Immediately = 0,
    AfterReminder = 1
}

public sealed record ClaimPreferencesDto(
    ClaimStartOption StartOption,
    DateTime? StartAfterReminderAt,
    bool AgreeTerms,
    bool AgreePrivacy);
