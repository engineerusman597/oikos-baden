using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceStageEditDto(
    int? Id,
    string Name,
    string? NameDe,
    string Slug,
    string? Summary,
    string? SummaryDe,
    string? Description,
    string? DescriptionDe,
    string? NextSteps,
    string? NextStepsDe,
    string? Icon,
    string? Color,
    InvoicePrimaryStatus PrimaryStatus);

public sealed record SaveStageResult(
    bool Success,
    string? ErrorMessage,
    int? StageId);
