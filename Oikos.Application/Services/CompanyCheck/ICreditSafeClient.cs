using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Application.Services.CompanyCheck;

public interface ICreditSafeClient
{
    Task<(IReadOnlyList<CreditSafeCompanySummary> Companies, CreditSafeConfiguration Configuration)> SearchCompaniesAsync(CompanySearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<CreditSafeConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<(CreditSafeCompanyDetails? Details, CreditSafeConfiguration Configuration)> GetCompanyDetailsAsync(string companyId, string? countryCode = null, CancellationToken cancellationToken = default);
    Task<(byte[]? PdfData, string? Error)> DownloadReportAsPdfAsync(string companyId, string language = "de", string template = "full", string? customData = null, CancellationToken cancellationToken = default);
}
