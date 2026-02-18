using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Subscription;

[Comment("Represents a purchased subscription mapped from Stripe checkout sessions")]
public class Subscription
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("Foreign key to the captured Stripe payment")]
    public int StripePaymentId { get; set; }

    public StripePayment StripePayment { get; set; } = null!;

    [Comment("Linked subscription plan derived from Stripe metadata")]
    public int? SubscriptionPlanId { get; set; }

    public SubscriptionPlan? SubscriptionPlan { get; set; }

    [Comment("Subscription type derived from Stripe PriceId")]
    public string SubscriptionType { get; set; } = null!;

    [Comment("Purchase timestamp (UTC)")]
    public DateTime PurchaseDate { get; set; }

    [Comment("Subscription expiration timestamp (UTC)")]
    public DateTime? ExpirationDate { get; set; }

    [Comment("Billing interval reported by Stripe (monthly/yearly)")]
    public string? BillingInterval { get; set; }

    [Comment("Stripe subscription identifier")]
    public string? StripeSubscriptionId { get; set; }

    [Comment("Price amount in the smallest currency unit")]
    public long? PriceAmount { get; set; }

    [Comment("Three-letter ISO currency code for the subscription price")]
    public string? PriceCurrency { get; set; }

    [Comment("Creation timestamp")]
    public DateTime CreatedAt { get; set; }

    [Comment("Last update timestamp")]
    public DateTime UpdatedAt { get; set; }
}
