namespace Oikos.Application.Services.Invoice.Models;

using Oikos.Domain.Enums;

public sealed record MyInvoicesDto(
    List<MyInvoiceItemDto> Invoices,
   List<InvoiceStageWithCountDto> Stages);

public sealed record MyInvoiceItemDto(
    int Id,
    string? TicketNumber,
    string? Company,
    string? Amount,
    string? Currency,
    DateTime? InvoiceDate,
    int StageId,
    InvoicePrimaryStatus PrimaryStatus,
    DateTime UpdatedAt,
    DateTime CreatedAt);

public sealed record InvoiceStageWithCountDto(
    int Id,
    string Slug,
    string Name,
    string? Summary,
    string? Description,
    string? NextSteps,
    string Icon,
    string? Color,
    InvoicePrimaryStatus PrimaryStatus,
    int Count);
