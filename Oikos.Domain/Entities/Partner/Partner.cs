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

    [Required]
    [MaxLength(50)]
    [Comment("Unique referral code")]
    public string Code { get; set; } = null!;

    [MaxLength(200)]
    [Comment("Primary contact email")]
    public string? ContactEmail { get; set; }

    [MaxLength(500)]
    [Comment("Internal notes")]
    public string? Notes { get; set; }

    [Comment("Creation timestamp (UTC)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rbac.User>? Users { get; set; }
}
