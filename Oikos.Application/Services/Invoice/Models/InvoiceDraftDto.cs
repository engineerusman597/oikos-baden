namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceDraftDto(
    Guid Id,
    string TempFilePath,
    string WebPath,
    string FileName,
    long FileSize,
    InvoiceDetailsDto Details);
