using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceListItemDto(
    int Id,
    int Number,
    string UserName,
    string? UserEmail,
    string? Company,
    string? Amount,
    string? Currency,
    string? TicketNumber,
    int StageId,
    string StageName,
    InvoicePrimaryStatus PrimaryStatus,
    string? StageColor,
    DateTime UpdatedAt,
    DateTime CreatedAt);
