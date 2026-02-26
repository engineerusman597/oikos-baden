namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceClientDocumentDto(
    int Id,
    string FileName,
    string FilePath,
    DateTime UploadedAt);
