using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceDetailDto(
    int Id,
    string? Company,
    string? Amount,
    string? Currency,
    DateTime? InvoiceDate,
    string? TicketNumber,
    int StageId,
    InvoicePrimaryStatus PrimaryStatus,
    string StageSlug,
    string StageName,
    string? StageSummary,
    string? StageDescription,
    string? StageNextSteps,
    string? StageColor,
    string StageIcon,
    string? UserName,
    string? UserEmail,
    string? CustomerNumber,
    string? FilePath,
    string? FileName,
    string? PowerOfAttorneyPath,
    string? PowerOfAttorneyFileName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<InvoiceHistoryDto> History,
    List<InvoiceClientDocumentDto> ClientDocuments);
