namespace Oikos.Application.Services.Invoice;

using Oikos.Application.Services.Invoice.Models;

public interface IInvoiceSubmissionService
{
    Task<FileUploadResult> ProcessFileUploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceSubmissionResult> SubmitInvoicesAsync(InvoiceSubmissionRequest request, CancellationToken cancellationToken = default);

}
