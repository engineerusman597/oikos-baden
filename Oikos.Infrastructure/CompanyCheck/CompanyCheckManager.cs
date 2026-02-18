using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Domain.Entities.CompanyCheck;
using Oikos.Infrastructure.Data;
using Oikos.Application.Common.Storage;

namespace Oikos.Infrastructure.CompanyCheck;

public class CompanyCheckManager : ICompanyCheckManager
{
    private readonly IDbContextFactory<OikosDbContext> _dbFactory;
    private readonly CompanyCheckReportFormatter _reportFormatter;
    private readonly string _storageRootPath;
    private readonly BonixOptions _options;

    public CompanyCheckManager(
        IDbContextFactory<OikosDbContext> dbFactory,
        CompanyCheckReportFormatter reportFormatter,
        IWebHostEnvironment environment,
        IOptionsSnapshot<BonixOptions> options)
    {
        _dbFactory = dbFactory;
        _reportFormatter = reportFormatter;
        _storageRootPath = ResolveStorageRoot(environment);
        _options = options.Value;
    }

    public string StorageRootPath => _storageRootPath;

    public async Task<bool> HasStoredSepaMandateAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.SepaMandateGeneratedAt.HasValue == true && !string.IsNullOrWhiteSpace(user.SepaMandatePath);
    }

    public async Task<decimal?> GetReportPriceAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _options.ReportPrice;
    }

    public async Task<string?> GetCurrencyAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(_options.Currency))
        {
            return "EUR";
        }

        return _options.Currency;
    }

    public async Task<CompanyCheckRequest> CreatePendingRequestAsync(int? userId, string companyName, CreditSafeCompanySummary summary, decimal amount, string currency, string? customerEmail, string? searchReason = null, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = new CompanyCheckRequest
        {
            UserId = userId,
            CompanyName = summary.Name,
            Status = CompanyCheckStatus.AwaitingPayment,
            Amount = amount,
            Currency = currency,
            CustomerEmail = customerEmail,
            SelectedCompanyJson = JsonSerializer.Serialize(summary),
            CreditsafeCompanyId = summary.CompanyId,
            CreditsafeCountryCode = summary.CountryCode,
            SearchReason = searchReason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.CompanyCheckRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<CompanyCheckRequest?> GetRequestByIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CompanyCheckRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<CompanyCheckRequest?> GetRequestByDownloadTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CompanyCheckRequests.FirstOrDefaultAsync(r => r.ReportDownloadToken == token, cancellationToken);
    }

    public async Task MarkPaymentConfirmedAsync(int requestId, string? paymentIntentId, string? paymentDataJson = null, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.CompanyCheckRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null)
        {
            return;
        }

        entity.Status = CompanyCheckStatus.PaymentConfirmed;
        entity.StripePaymentIntentId = paymentIntentId;
        entity.StripePaymentDataJson = paymentDataJson;
        entity.PaymentConfirmedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCheckoutSessionIdAsync(int requestId, string sessionId, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.CompanyCheckRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null)
        {
            return;
        }

        entity.StripeCheckoutSessionId = sessionId;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveReportAsync(int requestId, CreditSafeCompanyDetails details, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.CompanyCheckRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null)
        {
            return;
        }

        entity.ReportJson = details.RawJson ?? JsonSerializer.Serialize(details);
        entity.Status = CompanyCheckStatus.Completed;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CompanyCheckRequest?> SaveReportPdfAsync(int requestId, byte[] pdfData, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.CompanyCheckRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null)
        {
            return null;
        }

        if (pdfData == null || pdfData.Length == 0)
        {
            return null;
        }

        string relativeDirectory;
        if (entity.UserId.HasValue)
        {
            relativeDirectory = UserStoragePath.GetRelativePath(entity.UserId.Value, "company-checks");
        }
        else
        {
            relativeDirectory = Path.Combine("uploads", "guests", "company-checks");
        }
        var physicalDirectory = Path.Combine(_storageRootPath, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        if (!string.IsNullOrWhiteSpace(entity.ReportPdfPath))
        {
            var existingPath = Path.Combine(_storageRootPath, entity.ReportPdfPath);
            if (File.Exists(existingPath))
            {
                File.Delete(existingPath);
            }
        }

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{entity.Id}.pdf";
        var relativePath = Path.Combine(relativeDirectory, fileName);
        var physicalPath = Path.Combine(_storageRootPath, relativePath);

        var brandedPdf = _reportFormatter.ApplyBranding(pdfData);
        if (brandedPdf.Length == 0)
        {
            return null;
        }

        await File.WriteAllBytesAsync(physicalPath, brandedPdf, cancellationToken);

        entity.ReportPdfPath = relativePath;
        entity.ReportDownloadToken = Guid.NewGuid().ToString("N");
        entity.ReportPdfGeneratedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<string?> SaveSignedMandateAsync(int userId, string originalFileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        if (fileStream is null || !fileStream.CanRead)
        {
            return null;
        }

        var relativeDirectory = Path.Combine(UserStoragePath.GetRelativePath(userId, "company-checks"), "sepa-mandates");
        var physicalDirectory = Path.Combine(_storageRootPath, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var fileExtension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = ".pdf";
        }

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{userId}{fileExtension}";
        var relativePath = NormalizeRelativePath(Path.Combine(relativeDirectory, fileName));
        var physicalPath = Path.Combine(_storageRootPath, relativePath);

        await using var outputStream = File.Create(physicalPath);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return relativePath;
    }

    public async Task<string?> SaveGeneratedSepaMandateAsync(int userId, byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            return null;
        }

        var relativeDirectory = Path.Combine(UserStoragePath.GetRelativePath(userId, "company-checks"), "sepa-mandates");
        var physicalDirectory = Path.Combine(_storageRootPath, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var fileName = $"sepa-mandate-{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
        var relativePath = NormalizeRelativePath(Path.Combine(relativeDirectory, fileName));
        var physicalPath = Path.Combine(_storageRootPath, relativePath);

        await File.WriteAllBytesAsync(physicalPath, pdfBytes, cancellationToken);

        await using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(user.SepaMandatePath))
            {
                var existingPath = Path.Combine(_storageRootPath, user.SepaMandatePath);
                if (File.Exists(existingPath))
                {
                    File.Delete(existingPath);
                }
            }

            user.SepaMandatePath = relativePath;
            user.SepaMandateGeneratedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return relativePath;
    }

    public async Task<IReadOnlyList<CompanyCheckHistoryItem>> GetCompletedChecksAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entities = await dbContext.CompanyCheckRequests
            .Where(r => r.UserId == userId && (r.Status == CompanyCheckStatus.Completed || r.Status == CompanyCheckStatus.PaymentConfirmed))
            .OrderByDescending(r => r.ReportPdfGeneratedAt ?? r.PaymentConfirmedAt ?? r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var results = new List<CompanyCheckHistoryItem>();
        var baseDirectory = Path.GetFullPath(_storageRootPath);
        foreach (var entity in entities)
        {
            CompanyCheckReport? report = null;
            CreditSafeCompanySummary? selectedCompany = null;
            if (!string.IsNullOrWhiteSpace(entity.ReportJson))
            {
                try
                {
                    report = JsonSerializer.Deserialize<CompanyCheckReport>(entity.ReportJson);
                }
                catch
                {
                    // ignore parsing issues and keep raw json
                }
            }

            if (!string.IsNullOrWhiteSpace(entity.SelectedCompanyJson))
            {
                try
                {
                    selectedCompany = JsonSerializer.Deserialize<CreditSafeCompanySummary>(entity.SelectedCompanyJson);
                }
                catch
                {
                    // ignore parsing issues and keep raw json
                }
            }

            var downloadToken = entity.ReportDownloadToken;
            if (!string.IsNullOrWhiteSpace(downloadToken) && !string.IsNullOrWhiteSpace(entity.ReportPdfPath))
            {
                var candidatePath = Path.GetFullPath(Path.Combine(_storageRootPath, entity.ReportPdfPath));
                if (!candidatePath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidatePath))
                {
                    downloadToken = null;
                }
            }
            else
            {
                downloadToken = null;
            }

            results.Add(new CompanyCheckHistoryItem
            {
                RequestId = entity.Id,
                CompanyName = report?.Name ?? selectedCompany?.Name ?? entity.CompanyName,
                CountryCode = report?.CountryCode ?? selectedCompany?.CountryCode ?? entity.CreditsafeCountryCode,
                RegistrationNumber = report?.RegistrationNumber ?? selectedCompany?.RegistrationNumber,
                Address = report?.Address ?? selectedCompany?.Address,
                PaidAt = entity.PaymentConfirmedAt ?? entity.ReportPdfGeneratedAt ?? entity.CreatedAt,
                Amount = entity.Amount,
                Currency = entity.Currency,
                Report = report,
                SelectedCompany = selectedCompany,
                ReportJson = entity.ReportJson,
                DownloadToken = downloadToken
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<CompanyCheckHistoryItem>> GetAllCompletedChecksAsync(CancellationToken cancellationToken = default)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = from request in dbContext.CompanyCheckRequests
                    where request.Status == CompanyCheckStatus.Completed || request.Status == CompanyCheckStatus.PaymentConfirmed
                    join user in dbContext.Users on request.UserId equals user.Id into userGroup
                    from user in userGroup.DefaultIfEmpty()
                    orderby request.ReportPdfGeneratedAt ?? request.PaymentConfirmedAt ?? request.CreatedAt descending
                    select new
                    {
                        Request = request,
                        Username = user != null ? user.Name : null
                    };

        var entities = await query.AsNoTracking().ToListAsync(cancellationToken);

        var results = new List<CompanyCheckHistoryItem>();
        var baseDirectory = Path.GetFullPath(_storageRootPath);
        foreach (var item in entities)
        {
            var entity = item.Request;
            CompanyCheckReport? report = null;
            CreditSafeCompanySummary? selectedCompany = null;
            if (!string.IsNullOrWhiteSpace(entity.ReportJson))
            {
                try
                {
                    report = JsonSerializer.Deserialize<CompanyCheckReport>(entity.ReportJson);
                }
                catch
                {
                    // ignore parsing issues and keep raw json
                }
            }

            if (!string.IsNullOrWhiteSpace(entity.SelectedCompanyJson))
            {
                try
                {
                    selectedCompany = JsonSerializer.Deserialize<CreditSafeCompanySummary>(entity.SelectedCompanyJson);
                }
                catch
                {
                    // ignore parsing issues and keep raw json
                }
            }

            var downloadToken = entity.ReportDownloadToken;
            if (!string.IsNullOrWhiteSpace(downloadToken) && !string.IsNullOrWhiteSpace(entity.ReportPdfPath))
            {
                var candidatePath = Path.GetFullPath(Path.Combine(_storageRootPath, entity.ReportPdfPath));
                if (!candidatePath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidatePath))
                {
                    downloadToken = null;
                }
            }
            else
            {
                downloadToken = null;
            }

            results.Add(new CompanyCheckHistoryItem
            {
                RequestId = entity.Id,
                CompanyName = report?.Name ?? selectedCompany?.Name ?? entity.CompanyName,
                CountryCode = report?.CountryCode ?? selectedCompany?.CountryCode ?? entity.CreditsafeCountryCode,
                RegistrationNumber = report?.RegistrationNumber ?? selectedCompany?.RegistrationNumber,
                Address = report?.Address ?? selectedCompany?.Address,
                Username = item.Username,
                PaidAt = entity.PaymentConfirmedAt ?? entity.ReportPdfGeneratedAt ?? entity.CreatedAt,
                Amount = entity.Amount,
                Currency = entity.Currency,
                Report = report,
                SelectedCompany = selectedCompany,
                ReportJson = entity.ReportJson,
                DownloadToken = downloadToken
            });
        }

        return results;
    }

    private static string ResolveStorageRoot(IWebHostEnvironment environment)
    {
        var candidates = new[]
        {
            environment.WebRootPath,
            string.IsNullOrWhiteSpace(environment.ContentRootPath)
                ? null
                : Path.Combine(environment.ContentRootPath, "wwwroot"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot")
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var uploadsDirectory = Path.Combine(candidate, "uploads");

            try
            {
                Directory.CreateDirectory(uploadsDirectory);

                var probePath = Path.Combine(uploadsDirectory, ".write-test");
                File.WriteAllText(probePath, "ok");
                File.Delete(probePath);

                return candidate;
            }
            catch
            {
                // Move on to the next candidate if this path is not writable.
            }
        }

        throw new InvalidOperationException("No writable storage root could be determined for company check assets.");
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace('\\', '/');
    }
}
