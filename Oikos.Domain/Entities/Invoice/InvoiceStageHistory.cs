using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oikos.Domain.Entities.Invoice;

[Comment("History of invoice stage changes")]
public class InvoiceStageHistory
{
    [Key]
    public int Id { get; set; }

    [Comment("Invoice identifier")]
    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [Comment("Stage identifier")]
    public int StageId { get; set; }

    [ForeignKey(nameof(StageId))]
    public InvoiceStage Stage { get; set; } = null!;

    [Comment("User identifier that performed the change")]
    public int? ChangedByUserId { get; set; }

    [MaxLength(200)]
    [Comment("Name snapshot of the user that performed the change")]
    public string? ChangedByUserName { get; set; }

    [Comment("Change timestamp")]
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [MaxLength(500)]
    [Comment("Optional note about the change")]
    public string? Note { get; set; }
}
