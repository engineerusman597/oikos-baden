using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceSearchRequest(
    string? SearchText,
    int? StageId,
    InvoicePrimaryStatus? PrimaryStatus,
    int Page,
    int PageSize,
    IReadOnlyList<InvoicePrimaryStatus>? PrimaryStatuses = null);
