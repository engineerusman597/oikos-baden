using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Application.Services.Invoice;

public interface IInvoiceExtractionService
{
    Task<InvoiceAiExtractionResult?> ExtractAsync(InvoiceAiExtractionRequest request, CancellationToken cancellationToken = default);
}
