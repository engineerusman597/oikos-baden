using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceStageDto(
    int Id,
    string Name,
    string? Color,
    string? Icon,
    InvoicePrimaryStatus PrimaryStatus);
