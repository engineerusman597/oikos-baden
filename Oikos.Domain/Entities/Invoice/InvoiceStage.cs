using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

using Oikos.Domain.Enums;

namespace Oikos.Domain.Entities.Invoice;

[Comment("Invoice workflow stages")]
public class InvoiceStage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    [Comment("Stage display name")]
    public string Name { get; set; } = null!;

    [MaxLength(150)]
    [Comment("Stage display name (German)")]
    public string? NameDe { get; set; }

    [Required]
    [MaxLength(150)]
    [Comment("Route slug for navigation")]
    public string Slug { get; set; } = null!;

    [MaxLength(250)]
    [Comment("Short summary shown in navigation")]
    public string? Summary { get; set; }

    [Comment("Short summary shown in navigation (German)")]
    public string? SummaryDe { get; set; }

    [Comment("Detailed description of the stage")]
    public string? Description { get; set; }

    [Comment("Detailed description of the stage (German)")]
    public string? DescriptionDe { get; set; }

    [Comment("Recommended next steps for the user")]
    public string? NextSteps { get; set; }

    [Comment("Recommended next steps for the user (German)")]
    public string? NextStepsDe { get; set; }

    [MaxLength(120)]
    [Comment("MudBlazor icon identifier")]
    public string? Icon { get; set; }

    [MaxLength(60)]
    [Comment("Preferred MudBlazor color")]
    public string? Color { get; set; }

    [Comment("Display order")]
    public int DisplayOrder { get; set; }

    [Comment("Primary status classification")]
    public InvoicePrimaryStatus PrimaryStatus { get; set; } = InvoicePrimaryStatus.InReview;

    [Comment("Create time")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Comment("Update time")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public ICollection<InvoiceStageHistory> Histories { get; set; } = new List<InvoiceStageHistory>();
}
