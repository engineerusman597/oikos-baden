using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Oikos.Domain.Entities.CompanyCheck;

[Comment("Company check requests and reports")]
public class CompanyCheckRequest
{
    [Key]
    public int Id { get; set; }

    [Comment("User identifier")]
    public int? UserId { get; set; }

    [Required]
    [MaxLength(200)]
    [Comment("Original company name provided by user")]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Comment("Creditsafe company identifier")]
    public string? CreditsafeCompanyId { get; set; }

    [MaxLength(10)]
    [Comment("Creditsafe country code")]
    public string? CreditsafeCountryCode { get; set; }

    [Comment("Request status")]
    public CompanyCheckStatus Status { get; set; } = CompanyCheckStatus.AwaitingPayment;

    [Precision(18, 2)]
    [Comment("Charged amount")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    [Comment("Currency code")]
    public string Currency { get; set; } = "EUR";

    [MaxLength(200)]
    [Comment("Customer email for Stripe")]
    public string? CustomerEmail { get; set; }

    [Comment("Serialized summary of selected company")]
    public string? SelectedCompanyJson { get; set; }

    [Comment("Serialized detailed report")]
    public string? ReportJson { get; set; }

    [MaxLength(500)]
    [Comment("Relative path of the generated PDF report")]
    public string? ReportPdfPath { get; set; }

    [MaxLength(100)]
    [Comment("Unique download token for the report PDF")]
    public string? ReportDownloadToken { get; set; }

    [MaxLength(50)]
    [Comment("Business purpose for the company search")]
    public string? SearchReason { get; set; }

    [Comment("Timestamp when the PDF report was generated")]
    public DateTime? ReportPdfGeneratedAt { get; set; }

    [MaxLength(200)]
    [Comment("Stripe checkout session identifier")]
    public string? StripeCheckoutSessionId { get; set; }

    [MaxLength(200)]
    [Comment("Stripe payment intent identifier")]
    public string? StripePaymentIntentId { get; set; }

    [Comment("Complete Stripe payment details as JSON")]
    public string? StripePaymentDataJson { get; set; }

    [Comment("Request creation time")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Comment("Last update time")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Comment("Payment confirmation time")]
    public DateTime? PaymentConfirmedAt { get; set; }
}

public enum CompanyCheckStatus
{
    AwaitingPayment = 0,
    PaymentInitiated = 1,
    PaymentConfirmed = 2,
    Completed = 3
}
