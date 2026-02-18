namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceDetailsDto(
    string? InvoiceNumber,
    string? Amount,
    string? Currency,
    DateTime? InvoiceDate,
    string? Description);
