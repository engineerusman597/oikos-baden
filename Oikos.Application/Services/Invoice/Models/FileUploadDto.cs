namespace Oikos.Application.Services.Invoice.Models;

public sealed record FileUploadRequest(
    Stream FileStream,
    string FileName,
    long FileSize,
    int UserId);

public sealed record FileUploadResult(
    bool Success,
    InvoiceDraftDto? Draft,
    InvoiceAiExtractionResult? Extraction,
    string? ErrorMessageKey);
