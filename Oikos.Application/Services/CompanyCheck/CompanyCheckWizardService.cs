using Microsoft.Extensions.Logging;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Application.Services.Email;
using Oikos.Application.Services.Email.Templates;
using Microsoft.Extensions.Options;
using Oikos.Application.Services.Stripe;
using Stripe;
using Stripe.Checkout;
using Oikos.Domain.Entities.CompanyCheck;

namespace Oikos.Application.Services.CompanyCheck;

/// <summary>
/// Implementation of Company Check Wizard service.
/// Orchestrates multiple services to handle wizard business logic.
/// </summary>
public class CompanyCheckWizardService : ICompanyCheckWizardService
{
    private readonly ICreditSafeClient _creditSafeClient;
    private readonly ICompanyCheckManager _companyCheckManager;
    private readonly ISepaMandateGenerator _sepaMandateGenerator;
    private readonly IEmailSender _emailSender;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<CompanyCheckWizardService> _logger;

    public CompanyCheckWizardService(
        ICreditSafeClient creditSafeClient,
        ICompanyCheckManager companyCheckManager,
        ISepaMandateGenerator sepaMandateGenerator,
        IEmailSender emailSender,
        IOptionsSnapshot<StripeOptions> stripeOptions,
        ILogger<CompanyCheckWizardService> logger)
    {
        _creditSafeClient = creditSafeClient;
        _companyCheckManager = companyCheckManager;
        _sepaMandateGenerator = sepaMandateGenerator;
        _emailSender = emailSender;
        _stripeOptions = stripeOptions.Get("StripeBonix");
        _logger = logger;
    }

    public async Task<WizardInitializationResult> InitializeAsync(int? userId)
    {
        var configuration = await _creditSafeClient.GetConfigurationAsync();
        var price = await _companyCheckManager.GetReportPriceAsync();
        var currency = await _companyCheckManager.GetCurrencyAsync();
        var hasStoredMandate = false;

        if (userId.HasValue)
        {
            try
            {
                hasStoredMandate = await _companyCheckManager.HasStoredSepaMandateAsync(userId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check SEPA mandate for user {UserId}", userId);
            }
        }

        return new WizardInitializationResult(
            configuration.HasCredentials,
            configuration.DefaultCountry,
            price,
            currency,
            hasStoredMandate
        );
    }

    public async Task<CompanySearchResponse> SearchCompaniesAsync(CompanySearchRequest request)
    {
        try
        {
            // Normalize inputs
            var normalizedRequest = NormalizeSearchRequest(request);

            // Map to service model
            var criteria = new CompanySearchCriteria
            {
                CompanyName = normalizedRequest.CompanyName,
                Country = normalizedRequest.Country,
                RegistrationNumber = normalizedRequest.RegistrationNumber,
                VatNumber = normalizedRequest.VatNumber,
                Street = normalizedRequest.Street,
                City = normalizedRequest.City,
                PostCode = normalizedRequest.PostCode,
                PhoneNumber = normalizedRequest.PhoneNumber,
                Status = normalizedRequest.Status,
                CompanyType = normalizedRequest.CompanyType
            };

            var (results, configuration) = await _creditSafeClient.SearchCompaniesAsync(criteria);

            if (!configuration.HasCredentials)
            {
                return new CompanySearchResponse(false, "CreditSafe configuration missing", new List<CompanySummaryDto>());
            }

            // Map results to DTOs
            var summaries = results.Select(r => new CompanySummaryDto(
                r.CompanyId,
                r.Name,
                r.CountryCode,
                r.RegistrationNumber,
                r.Address,
                r.Status,
                r.Type,
                r.LatestAccountsDate
            )).ToList();

            return new CompanySearchResponse(true, null, summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching companies");
            return new CompanySearchResponse(true, ex.Message, new List<CompanySummaryDto>());
        }
    }

    public async Task<OrderConfirmationResult> CreatePendingOrderAsync(CreateOrderRequest request)
    {
        try
        {
            // Map to service model
            var companySummary = new CreditSafeCompanySummary
            {
                CompanyId = request.CompanyId,
                Name = request.CompanyName,
                CountryCode = request.CountryCode,
                RegistrationNumber = request.RegistrationNumber
            };

            var pendingRequest = await _companyCheckManager.CreatePendingRequestAsync(
                request.UserId,
                request.RequestedCompanyName,
                companySummary,
                request.Price,
                request.Currency,
                request.UserEmail,
                request.SearchReason
            );

            return new OrderConfirmationResult(true, null, pendingRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pending order for user {UserId}", request.UserId);
            return new OrderConfirmationResult(false, ex.Message, null);
        }
    }

    public async Task<string> CreateStripeCheckoutSessionAsync(int requestId, string successUrl, string cancelUrl)
    {
        var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
        if (request == null) throw new InvalidOperationException("Request not found");

        if (string.IsNullOrWhiteSpace(_stripeOptions.ApiKey)) throw new InvalidOperationException("Stripe API Key is invalid");

        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "klarna" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(request.Amount * 100),
                            Currency = request.Currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"bonix Auskunft: {request.CompanyName}",
                                Description = "Abruf einer bonix Wirtschaftsauskunft",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = requestId.ToString(),
                CustomerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail) ? request.CustomerEmail : 
                                (await _companyCheckManager.GetRequestByIdAsync(requestId))?.CustomerEmail, // Ensure we have it
                Locale = "de",
                BillingAddressCollection = "required",
                InvoiceCreation = new SessionInvoiceCreationOptions
                {
                    Enabled = true,
                    InvoiceData = new SessionInvoiceCreationInvoiceDataOptions
                    {
                        Description = $"bonix Auskunft: {request.CompanyName}",
                        Footer = "Vielen Dank für Ihren Einkauf bei bonix.",
                    }
                },
                CustomFields = new List<SessionCustomFieldOptions>
                {
                    new SessionCustomFieldOptions
                    {
                        Key = "firmenname",
                        Label = new SessionCustomFieldLabelOptions { Type = "custom", Custom = "Firmenname" },
                        Type = "text",
                        Optional = true
                    }
                },
            };

            var service = new SessionService(new StripeClient(_stripeOptions.ApiKey));
            var session = await service.CreateAsync(options);
            
            // Save session ID to database
            await _companyCheckManager.SaveCheckoutSessionIdAsync(requestId, session.Id);
            
            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API Error: {StripeError}", ex.StripeError.Message);
            throw new InvalidOperationException($"Stripe Error: {ex.StripeError.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe session");
            throw;
        }
    }

    public async Task<CompanyCheckStatus?> GetRequestStatusAsync(int requestId)
    {
        var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
        return request?.Status;
    }

    public async Task<bool> SyncPaymentStatusAsync(int requestId, string sessionId)
    {
        try
        {
            var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
            if (request == null || request.Status == CompanyCheckStatus.PaymentConfirmed || request.Status == CompanyCheckStatus.Completed)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(_stripeOptions.ApiKey)) return false;

            var service = new SessionService(new StripeClient(_stripeOptions.ApiKey));
            var session = await service.GetAsync(sessionId);

            if (session != null && session.PaymentStatus == "paid" && session.ClientReferenceId == requestId.ToString())
            {
                // Extract payment data including billing address and custom fields
                var paymentData = new
                {
                    AmountTotal = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : (decimal?)null,
                    Currency = session.Currency,
                    PaymentStatus = session.PaymentStatus,
                    PaymentMethodTypes = session.PaymentMethodTypes,
                    CustomerEmail = session.CustomerEmail,
                    Created = session.Created,
                    PaymentIntentId = session.PaymentIntentId ?? session.PaymentIntent?.Id,
                    BillingAddress = session.CustomerDetails?.Address != null ? new
                    {
                        Line1 = session.CustomerDetails.Address.Line1,
                        Line2 = session.CustomerDetails.Address.Line2,
                        City = session.CustomerDetails.Address.City,
                        PostalCode = session.CustomerDetails.Address.PostalCode,
                        State = session.CustomerDetails.Address.State,
                        Country = session.CustomerDetails.Address.Country
                    } : null,
                    CompanyName = session.CustomFields?.FirstOrDefault(f => f.Key == "firmenname")?.Text?.Value
                };

                var paymentDataJson = System.Text.Json.JsonSerializer.Serialize(paymentData);
                await _companyCheckManager.MarkPaymentConfirmedAsync(
                    requestId, 
                    session.PaymentIntentId ?? session.PaymentIntent?.Id,
                    paymentDataJson
                );
                 return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing payment status for request {RequestId}", requestId);
            return false;
        }
    }

    public async Task<OrderConfirmationResult> ConfirmOrderAsync(int requestId, string paymentReference)
    {
        try
        {
            await _companyCheckManager.MarkPaymentConfirmedAsync(requestId, paymentReference, null);
            var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
            return new OrderConfirmationResult(true, null, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order {RequestId}", requestId);
            return new OrderConfirmationResult(false, ex.Message, null);
        }
    }

    public async Task<CompanyReportDto?> LoadReportAsync(int requestId)
    {
        try
        {
            var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
            if (request == null)
            {
                return null;
            }

            CreditSafeCompanyDetails? details = null;
            if (!string.IsNullOrWhiteSpace(request.CreditsafeCompanyId))
            {
                var (result, _) = await _creditSafeClient.GetCompanyDetailsAsync(
                    request.CreditsafeCompanyId,
                    request.CreditsafeCountryCode
                );
                details = result;
            }

            if (details != null)
            {
                // Save the report to the database
                await _companyCheckManager.SaveReportAsync(requestId, details);

                return new CompanyReportDto(
                    details.CompanyId,
                    details.Name,
                    details.RegistrationNumber,
                    details.CountryCode,
                    details.Address,
                    details.Status,
                    details.Score,
                    details.FoundedOn,
                    details.VatNumber,
                    details.Industry
                );
            }
            else if (!string.IsNullOrWhiteSpace(request.ReportJson))
            {
                // Try to deserialize from stored JSON
                try
                {
                    var report = System.Text.Json.JsonSerializer.Deserialize<CompanyReportDto>(request.ReportJson);
                    return report;
                }
                catch
                {
                    _logger.LogWarning("Failed to deserialize report JSON for request {RequestId}", requestId);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.SelectedCompanyJson))
            {
                // Fallback to selected company summary
                try
                {
                    var summary = System.Text.Json.JsonSerializer.Deserialize<CreditSafeCompanySummary>(request.SelectedCompanyJson);
                    if (summary != null)
                    {
                        return new CompanyReportDto(
                            summary.CompanyId,
                            summary.Name,
                            summary.RegistrationNumber,
                            summary.CountryCode,
                            summary.Address,
                            summary.Status,
                            null, // Score
                            null, // FoundedOn
                            null, // VatNumber
                            null  // Industry
                        );
                    }
                }
                catch
                {
                    _logger.LogWarning("Failed to deserialize selected company JSON for request {RequestId}", requestId);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading report for request {RequestId}", requestId);
            return null;
        }
    }

    public async Task<ReportGenerationResult> GenerateReportPdfAsync(int requestId, string language = "de", string template = "full")
    {
        try
        {
            var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
            if (request == null)
            {
                return new ReportGenerationResult(false, "Request not found", null, null);
            }

            // Check if report already exists
            if (TryUseExistingReport(request, out var existingUrl))
            {
                return new ReportGenerationResult(true, null, existingUrl, request);
            }

            if (string.IsNullOrWhiteSpace(request.CreditsafeCompanyId))
            {
                return new ReportGenerationResult(false, "Company ID missing", null, null);
            }

            // Download PDF from CreditSafe
            var (pdfData, error) = await _creditSafeClient.DownloadReportAsPdfAsync(
                request.CreditsafeCompanyId,
                language,
                template,
                "de_reason_code::1"
            );

            if (pdfData == null || pdfData.Length == 0)
            {
                return new ReportGenerationResult(false, error ?? "PDF download failed", null, null);
            }

            // Save PDF
            var updatedRequest = await _companyCheckManager.SaveReportPdfAsync(requestId, pdfData);
            if (updatedRequest == null)
            {
                return new ReportGenerationResult(false, "Failed to save PDF", null, null);
            }

            var downloadUrl = !string.IsNullOrWhiteSpace(updatedRequest.ReportDownloadToken)
                ? $"/company-checks/report/{updatedRequest.ReportDownloadToken}"
                : null;

            return new ReportGenerationResult(true, null, downloadUrl, updatedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report for request {RequestId}", requestId);
            return new ReportGenerationResult(false, ex.Message, null, null);
        }
    }

    public async Task<bool> SendReportEmailAsync(int requestId, string recipientEmail, string recipientName)
    {
        try
        {
            var request = await _companyCheckManager.GetRequestByIdAsync(requestId);
            if (request == null || string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(request.ReportPdfPath))
            {
                return false;
            }

            var storageRoot = Path.GetFullPath(_companyCheckManager.StorageRootPath);
            var pdfPath = Path.GetFullPath(Path.Combine(storageRoot, request.ReportPdfPath));

            // Security check
            if (!pdfPath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(pdfPath))
            {
                _logger.LogWarning("Invalid PDF path for request {RequestId}", requestId);
                return false;
            }

            var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
            if (pdfBytes.Length == 0)
            {
                return false;
            }

            var subject = CompanyCheckEmailTemplate.Subject;
            var isLoggedIn = request.UserId.HasValue;
            var body = CompanyCheckEmailTemplate.Render(recipientName, request.CompanyName, isLoggedIn);

            await _emailSender.SendEmailAsync(
                recipientEmail,
                subject,
                body,
                recipientName,
                new[] { new EmailAttachment("bonix_auskunft.pdf", pdfBytes, "application/pdf") },
                EmailConfigurationType.Bonix
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send report email for request {RequestId}", requestId);
            return false;
        }
    }

    public async Task<bool> HasStoredSepaMandateAsync(int userId)
    {
        try
        {
            return await _companyCheckManager.HasStoredSepaMandateAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking SEPA mandate for user {UserId}", userId);
            return false;
        }
    }

    public async Task<SepaMandateResult> CreateSepaMandateAsync(int userId, SepaMandateDetails details)
    {
        try
        {
            var mandateBytes = _sepaMandateGenerator.Generate(details);
            if (mandateBytes.Length == 0)
            {
                return new SepaMandateResult(false, null, "Failed to generate SEPA mandate");
            }

            // Store the mandate
            try
            {
                await _companyCheckManager.SaveGeneratedSepaMandateAsync(userId, mandateBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist SEPA mandate for user {UserId}", userId);
            }

            return new SepaMandateResult(true, mandateBytes, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SEPA mandate for user {UserId}", userId);
            return new SepaMandateResult(false, null, ex.Message);
        }
    }

    public async Task<decimal?> GetReportPriceAsync()
    {
        return await _companyCheckManager.GetReportPriceAsync();
    }

    public async Task<string?> GetCurrencyAsync()
    {
        return await _companyCheckManager.GetCurrencyAsync();
    }

    private static CompanySearchRequest NormalizeSearchRequest(CompanySearchRequest request)
    {
        static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        var country = Normalize(request.Country);
        if (!string.IsNullOrWhiteSpace(country))
        {
            country = country.ToUpperInvariant();
        }

        return request with
        {
            CompanyName = Normalize(request.CompanyName),
            Country = country,
            RegistrationNumber = Normalize(request.RegistrationNumber),
            VatNumber = Normalize(request.VatNumber),
            Street = Normalize(request.Street),
            City = Normalize(request.City),
            PostCode = Normalize(request.PostCode),
            PhoneNumber = Normalize(request.PhoneNumber),
            Status = Normalize(request.Status),
            CompanyType = Normalize(request.CompanyType)
        };
    }

    private bool TryUseExistingReport(Domain.Entities.CompanyCheck.CompanyCheckRequest request, out string? downloadUrl)
    {
        downloadUrl = null;

        if (string.IsNullOrWhiteSpace(request.ReportDownloadToken) || string.IsNullOrWhiteSpace(request.ReportPdfPath))
        {
            return false;
        }

        var absolutePath = Path.Combine(AppContext.BaseDirectory, request.ReportPdfPath);
        if (!System.IO.File.Exists(absolutePath))
        {
            return false;
        }

        downloadUrl = $"/company-checks/report/{request.ReportDownloadToken}";
        return true;
    }
}
