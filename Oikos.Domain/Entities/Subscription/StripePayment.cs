using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Subscription;

[Comment("Raw Stripe webhook data captured for purchases")]
public class StripePayment
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("Customer email address")]
    public string Email { get; set; } = null!;

    [Comment("Customer full name provided by Stripe")]
    public string? Name { get; set; }

    [Comment("Stripe subscription id associated with the checkout session")]
    public string StripeSubscriptionId { get; set; } = string.Empty;

    [Comment("Stripe customer id provided during checkout")]
    public string? StripeCustomerId { get; set; }

    [Comment("Raw webhook payload for auditing")]
    public string RawPayload { get; set; } = string.Empty;

    [Comment("Created timestamp")]
    public DateTime CreatedAt { get; set; }

    [Comment("Last update timestamp")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Subscription>? Subscriptions { get; set; }
}
