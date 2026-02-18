using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Domain.Entities.CompanyCheck;

namespace Oikos.Application.Services.CompanyCheck;

/// <summary>
/// Service interface for Company Check Wizard operations.
/// Encapsulates all business logic for the wizard flow.
/// </summary>
public interface ICompanyCheckWizardService
{
    /// <summary>
    /// Initializes wizard with configuration, pricing, and user's SEPA mandate status.
    /// </summary>
    Task<WizardInitializationResult> InitializeAsync(int? userId);

    /// <summary>
    /// Searches for companies based on provided criteria.
    /// </summary>
    Task<CompanySearchResponse> SearchCompaniesAsync(CompanySearchRequest request);

    /// <summary>
    /// Creates a pending order for a company check.
    /// </summary>
    Task<OrderConfirmationResult> CreatePendingOrderAsync(CreateOrderRequest request);

    /// <summary>
    /// Confirms and processes an order, marking payment as confirmed.
    /// </summary>
    Task<OrderConfirmationResult> ConfirmOrderAsync(int requestId, string paymentReference);
    Task<string> CreateStripeCheckoutSessionAsync(int requestId, string successUrl, string cancelUrl);
    Task<CompanyCheckStatus?> GetRequestStatusAsync(int requestId);
    Task<bool> SyncPaymentStatusAsync(int requestId, string sessionId);

    /// <summary>
    /// Loads the company report for a given request.
    /// </summary>
    Task<CompanyReportDto?> LoadReportAsync(int requestId);

    /// <summary>
    /// Generates and stores the PDF report for a company check request.
    /// </summary>
    Task<ReportGenerationResult> GenerateReportPdfAsync(int requestId, string language = "de", string template = "full");

    /// <summary>
    /// Sends the company check report via email to the specified recipient.
    /// </summary>
    Task<bool> SendReportEmailAsync(int requestId, string recipientEmail, string recipientName);

    /// <summary>
    /// Checks if the user has a stored SEPA mandate.
    /// </summary>
    Task<bool> HasStoredSepaMandateAsync(int userId);

    /// <summary>
    /// Creates and stores a SEPA mandate for the user.
    /// </summary>
    Task<SepaMandateResult> CreateSepaMandateAsync(int userId, SepaMandateDetails details);

    /// <summary>
    /// Gets the configured report price.
    /// </summary>
    Task<decimal?> GetReportPriceAsync();

    /// <summary>
    /// Gets the configured currency.
    /// </summary>
    Task<string?> GetCurrencyAsync();
}
