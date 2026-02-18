namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceSubmissionRequest(
    List<InvoiceDraftDto> Drafts,
    DebtorDetailsDto Debtor,
    PowerOfAttorneyDto PowerOfAttorney,
    ClaimPreferencesDto Preferences,
    int UserId,
    string UserName,
    string? UserEmail);

public sealed record InvoiceSubmissionResult(
    bool Success,
    List<int> CreatedInvoiceIds,
    string? ErrorMessageKey);
