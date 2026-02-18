namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceHistoryDto(
    string StageSlug,
    string StageName,
    string? StageColor,
    string StageIcon,
    DateTime ChangedAt,
    string? ChangedBy,
    string? Note = null);
