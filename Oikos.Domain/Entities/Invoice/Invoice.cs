using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Oikos.Domain.Enums;

namespace Oikos.Domain.Entities.Invoice;

[Comment("User Invoices")] 
public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Comment("Owner User Id")]
    public int UserId { get; set; }

    [MaxLength(500)]
    [Comment("Stored file path (relative to web root)")]
    public string FilePath { get; set; } = null!;

    [MaxLength(500)]
    [Comment("Stored power of attorney file path (relative to web root)")]
    public string? PowerOfAttorneyPath { get; set; }

    [MaxLength(200)]
    [Comment("Company name")]
    public string? Company { get; set; }

    [MaxLength(200)]
    [Comment("Amount as provided")]
    public string? Amount { get; set; }

    [Comment("Invoice date")]
    public DateTime? InvoiceDate { get; set; }

    [MaxLength(20)]
    [Comment("Currency code")]
    public string? Currency { get; set; }

    [MaxLength(1000)]
    [Comment("Description")]
    public string? Description { get; set; }

    [MaxLength(50)]
    [Comment("Ticket number")]
    public string? TicketNumber { get; set; }

    [Comment("Current workflow stage identifier")]
    public int StageId { get; set; }

    [ForeignKey(nameof(StageId))]
    public InvoiceStage Stage { get; set; } = null!;

    [Comment("Cached current primary status")]
    public InvoicePrimaryStatus PrimaryStatus { get; set; } = InvoicePrimaryStatus.InReview;

    [Comment("Create time")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Comment("Update time")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
