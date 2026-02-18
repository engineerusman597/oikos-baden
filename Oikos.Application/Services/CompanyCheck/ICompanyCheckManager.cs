using Oikos.Domain.Entities.CompanyCheck;
using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Application.Services.CompanyCheck;

public interface ICompanyCheckManager
{
    string StorageRootPath { get; }
    
    Task<bool> HasStoredSepaMandateAsync(int userId, CancellationToken cancellationToken = default);
    Task<decimal?> GetReportPriceAsync(CancellationToken cancellationToken = default);
    Task<string?> GetCurrencyAsync(CancellationToken cancellationToken = default);
    
    Task<CompanyCheckRequest> CreatePendingRequestAsync(
        int? userId, 
        string companyName, 
        CreditSafeCompanySummary summary, 
        decimal amount, 
        string currency, 
        string? customerEmail,
        string? searchReason = null,
        CancellationToken cancellationToken = default);

    Task<CompanyCheckRequest?> GetRequestByIdAsync(int requestId, CancellationToken cancellationToken = default);
    Task<CompanyCheckRequest?> GetRequestByDownloadTokenAsync(string token, CancellationToken cancellationToken = default);
    
    Task MarkPaymentConfirmedAsync(int requestId, string? paymentIntentId, string? paymentDataJson = null, CancellationToken cancellationToken = default);
    Task SaveCheckoutSessionIdAsync(int requestId, string sessionId, CancellationToken cancellationToken = default);

    
    Task SaveReportAsync(int requestId, CreditSafeCompanyDetails details, CancellationToken cancellationToken = default);
    Task<CompanyCheckRequest?> SaveReportPdfAsync(int requestId, byte[] pdfData, CancellationToken cancellationToken = default);
    
    Task<string?> SaveSignedMandateAsync(int userId, string originalFileName, Stream fileStream, CancellationToken cancellationToken = default);
    Task<string?> SaveGeneratedSepaMandateAsync(int userId, byte[] pdfBytes, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<CompanyCheckHistoryItem>> GetCompletedChecksAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyCheckHistoryItem>> GetAllCompletedChecksAsync(CancellationToken cancellationToken = default);
}
