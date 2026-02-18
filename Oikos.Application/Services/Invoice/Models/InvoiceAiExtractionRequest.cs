namespace Oikos.Application.Services.Invoice.Models;

public sealed record InvoiceAiExtractionRequest(string RawText, string? Culture, string? FileName, byte[]? PdfData);
