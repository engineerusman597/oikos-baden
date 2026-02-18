using Microsoft.EntityFrameworkCore;
using PartnerEntity = Oikos.Domain.Entities.Partner.Partner;

namespace Oikos.Domain.Entities.Rbac;

[Comment("User")]
public class User
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Comment("User avatar")]
    public string? Avatar { get; set; }

    [Comment("Username")]
    public string Name { get; set; } = null!;

    [Comment("Full name")]
    public string RealName { get; set; } = null!;

    [Comment("Academic title")]
    public string? AcademicTitle { get; set; }

    [Comment("Gender")]
    public string? Gender { get; set; }

    [Comment("Password hash")]
    public string PasswordHash { get; set; } = null!;

    [Comment("Email address")]
    public string? Email { get; set; }

    [Comment("Company name")]
    public string? Company { get; set; }

    [Comment("Privacy policy accepted")]
    public bool AcceptedPrivacyPolicy { get; set; }

    [Comment("Privacy policy accepted at")]
    public DateTime? PrivacyAcceptedAt { get; set; }

    [Comment("Phone number")]
    public string? PhoneNumber { get; set; }

    [Comment("Is enabled")]
    public bool IsEnabled { get; set; }

    [Comment("Is deleted")]
    public bool IsDeleted { get; set; }

    [Comment("Is special user")]
    public bool IsSpecial { get; set; }

    [Comment("Login failure tracking expiry")]
    public DateTime? LoginValiedTime { get; set; }

    [Comment("Customer number")]
    public string? CustomerNumber { get; set; }

    [Comment("Relative path to the latest generated SEPA mandate for the user")]
    public string? SepaMandatePath { get; set; }

    [Comment("Timestamp of the latest generated SEPA mandate")]
    public DateTime? SepaMandateGeneratedAt { get; set; }

    [Comment("Referring partner id")]
    public int? PartnerId { get; set; }

    public PartnerEntity? Partner { get; set; }

    public ICollection<Subscription.UserSubscription>? Subscriptions { get; set; }


}
