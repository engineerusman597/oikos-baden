using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Oikos.Domain.Entities.TaxOffice;

[Comment("Tax offices / Steuerberaterbüros")]
[Index(nameof(Code), IsUnique = true)]
public class TaxOffice
{
    [Comment("Primary key")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Comment("Tax office display name")]
    public string Name { get; set; } = null!;

    [MaxLength(200)]
    [Comment("Official business/company name")]
    public string? BusinessName { get; set; }

    [MaxLength(200)]
    [Comment("Contact email")]
    public string? Email { get; set; }

    [MaxLength(100)]
    [Comment("Contact phone number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    [Comment("Name of the contact person")]
    public string? ContactPerson { get; set; }

    [MaxLength(500)]
    [Comment("Business address")]
    public string? Address { get; set; }

    [MaxLength(20)]
    [Comment("Postal code")]
    public string? PostalCode { get; set; }

    [MaxLength(200)]
    [Comment("City")]
    public string? City { get; set; }

    [MaxLength(500)]
    [Comment("Internal notes")]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(50)]
    [Comment("Unique referral code (STB-xxxx format)")]
    public string Code { get; set; } = null!;

    [Comment("Whether the tax office is active")]
    public bool IsActive { get; set; } = true;

    [Comment("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
