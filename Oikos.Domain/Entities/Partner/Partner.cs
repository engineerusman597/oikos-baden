using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.Partner;

[Comment("Lead partners")]
[Index(nameof(Code), IsUnique = true)]
public class Partner
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Comment("Partner display name")]
    public string Name { get; set; } = null!;

    [MaxLength(50)]
    [Comment("Partner type: Rechtsanwalt, Steuerberater, Notar, Unternehmensberater, Sonstige")]
    public string? PartnerType { get; set; }

    [MaxLength(200)]
    [Comment("Official business/company name")]
    public string? BusinessName { get; set; }

    [Required]
    [MaxLength(50)]
    [Comment("Unique referral code")]
    public string Code { get; set; } = null!;

    [MaxLength(200)]
    [Comment("Primary contact email")]
    public string? ContactEmail { get; set; }

    [MaxLength(100)]
    [Comment("Contact phone number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    [Comment("Name of the contact person at the partner")]
    public string? ContactPerson { get; set; }

    [MaxLength(500)]
    [Comment("Business address")]
    public string? Address { get; set; }

    [MaxLength(500)]
    [Comment("Internal notes")]
    public string? Notes { get; set; }

    [MaxLength(50)]
    [Comment("Commission type: Monthly, Yearly, OneTime")]
    public string? CommissionType { get; set; }

    [Comment("Commission rate as a percentage")]
    public decimal? CommissionRate { get; set; }

    [Comment("Months commissions are paid after membership start (null = unlimited)")]
    public int? CommissionPeriodMonths { get; set; }

    [Comment("Whether the partner is active")]
    public bool IsActive { get; set; } = true;

    [Comment("Parent partner id for sub-partners")]
    public int? ParentPartnerId { get; set; }

    public Partner? ParentPartner { get; set; }

    [Comment("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rbac.User>? Users { get; set; }

    public ICollection<Partner>? SubPartners { get; set; }
}
