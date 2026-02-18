namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceAiExtractionResult(
    string? InvoiceNumber,
    string? Amount,
    string? Currency,
    DateTime? InvoiceDate,
    string? Description,
    string? DebtorCompany,
    string? DebtorStreet,
    string? DebtorPostalCode,
    string? DebtorCity,
    string? DebtorContactName,
    string? DebtorContactEmail,
    string? DebtorContactPhone)
{
    public bool HasInvoiceValues =>
        !string.IsNullOrWhiteSpace(InvoiceNumber) ||
        !string.IsNullOrWhiteSpace(Amount) ||
        !string.IsNullOrWhiteSpace(Currency) ||
        InvoiceDate.HasValue ||
        !string.IsNullOrWhiteSpace(Description);

    public bool HasDebtorValues =>
        !string.IsNullOrWhiteSpace(DebtorCompany) ||
        !string.IsNullOrWhiteSpace(DebtorStreet) ||
        !string.IsNullOrWhiteSpace(DebtorPostalCode) ||
        !string.IsNullOrWhiteSpace(DebtorCity) ||
        !string.IsNullOrWhiteSpace(DebtorContactName) ||
        !string.IsNullOrWhiteSpace(DebtorContactEmail) ||
        !string.IsNullOrWhiteSpace(DebtorContactPhone);
}
