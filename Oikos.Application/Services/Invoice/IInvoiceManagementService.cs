using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Application.Services.Invoice;

public interface IInvoiceManagementService
{
    Task<PagedResult<InvoiceListItemDto>> SearchInvoicesAsync(InvoiceSearchRequest request, string culture);
    Task<List<InvoiceStageDto>> GetInvoiceStagesAsync(string culture);
    Task<bool> ChangeInvoiceStageAsync(int invoiceId, int stageId, int userId, string userName, string? note = null);
    Task<bool> AddInvoiceNoteAsync(int invoiceId, int userId, string userName, string note);
    Task<bool> DeleteInvoiceAsync(int invoiceId, string storageRoot);

    // Client Documents
    Task<InvoiceClientDocumentDto?> UploadClientDocumentAsync(int invoiceId, int userId, string fileName, Stream stream, string storageRoot);
    Task<bool> DeleteClientDocumentAsync(int documentId, int userId, string storageRoot);
    
    // Stage Management
    Task<List<InvoiceStageListDto>> GetStageListAsync();
    Task<bool> DeleteStageAsync(int stageId);
    Task<bool> MoveStageAsync(int stageId, int offset);
    
    // Invoice Detail
    Task<InvoiceDetailDto?> GetInvoiceDetailAsync(int invoiceId, string culture);
    
    // My Invoices
    Task<MyInvoicesDto> GetMyInvoicesAsync(int userId, string culture);
    
    // Stage CRUD
    Task<InvoiceStageEditDto?> GetStageForEditAsync(int stageId);
    Task<SaveStageResult> SaveStageAsync(InvoiceStageEditDto dto);
}
