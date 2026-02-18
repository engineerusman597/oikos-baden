namespace Oikos.Application.Services.Invoice.Models;

public sealed record PowerOfAttorneyPdfDetails(
    string CreditorName,
    string? CreditorStreet,
    string? CreditorPostalCode,
    string? CreditorCity,
    string? DebtorCompany,
    string? DebtorStreet,
    string? DebtorPostalCode,
    string? DebtorCity,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    string? InvoiceAmount,
    string? InvoiceCurrency,
    string? Signature,
    DateTimeOffset SignatureDate);
