using Microsoft.EntityFrameworkCore;
using Oikos.Domain.Entities.Rbac;

namespace Oikos.Domain.Entities.Subscription;

[Comment("Tracks the active subscription plan for a user")]
public class UserSubscription
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("User id")]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [Comment("Subscribed plan")]
    public int SubscriptionPlanId { get; set; }

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    [Comment("Foreign key to the purchase record")]
    public int? SubscriptionId { get; set; }

    public Subscription? Subscription { get; set; }

    [Comment("Activation timestamp (UTC)")]
    public DateTime ActivationDate { get; set; }

    [Comment("Optional expiration timestamp (UTC)")]
    public DateTime? ExpirationDate { get; set; }

    [Comment("Billing interval: monthly/yearly")]
    public string BillingInterval { get; set; } = "monthly";

    [Comment("Status: active, cancelled, expired")]
    public string Status { get; set; } = "active";

    [Comment("Auto renew flag")]
    public bool AutoRenew { get; set; }

    [Comment("Creation timestamp")]
    public DateTime CreatedAt { get; set; }

    [Comment("Last update timestamp")]
    public DateTime UpdatedAt { get; set; }
}
