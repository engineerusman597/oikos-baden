using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oikos.Domain.Entities.Invoice;

[Comment("Documents uploaded by the client for a specific invoice")]
public class InvoiceClientDocument
{
    [Key]
    public int Id { get; set; }

    [Comment("Invoice this document belongs to")]
    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [Comment("User who uploaded this document")]
    public int UploadedByUserId { get; set; }

    [MaxLength(200)]
    [Comment("Original file name")]
    public string FileName { get; set; } = null!;

    [MaxLength(500)]
    [Comment("Stored file path (relative to web root)")]
    public string FilePath { get; set; } = null!;

    [Comment("Upload time")]
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
