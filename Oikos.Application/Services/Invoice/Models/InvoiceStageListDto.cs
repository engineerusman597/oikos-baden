namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceStageListDto(
    int Id,
    string Name,
    string Slug,
    string Summary,
    int Order,
    DateTime UpdatedAt,
    bool IsFirst,
    bool IsLast);
