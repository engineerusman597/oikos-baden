using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Subscription;

[Comment("Defines a purchasable subscription plan with feature limits")]
public class SubscriptionPlan
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("Unique slug for internal references")]
    public string Slug { get; set; } = string.Empty;

    [Comment("Display name shown to users")]
    public string Name { get; set; } = string.Empty;

    [Comment("Plan description")]
    public string? Description { get; set; }

    [Comment("Monthly price in euros")]
    public decimal MonthlyPrice { get; set; }

    [Comment("Yearly price in euros")]
    public decimal YearlyPrice { get; set; }

    [Comment("Monthly claim submission limit")]
    public int? MonthlyClaimLimit { get; set; }

    [Comment("Monthly bionic company check limit")]
    public int? MonthlyBionicCheckLimit { get; set; }

    [Comment("Maximum team members allowed; null means unlimited")]
    public int? TeamSeatLimit { get; set; }

    [Comment("Display ordering")]
    public int DisplayOrder { get; set; }

    [Comment("Whether the plan is available for purchase")]
    public bool IsActive { get; set; }

    [Comment("Creation timestamp")]
    public DateTime CreatedAt { get; set; }

    [Comment("Last update timestamp")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserSubscription>? UserSubscriptions { get; set; }
}
