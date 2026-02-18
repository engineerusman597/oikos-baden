using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models;

public class InvoiceDraft
{
    public InvoiceDraft(IBrowserFile file, string tempFilePath, string previewUrl)
    {
        File = file;
        TempFilePath = tempFilePath;
        PreviewUrl = previewUrl;
        Details = new InvoiceDraftDetails();
    }

    public Guid Id { get; } = Guid.NewGuid();
    public IBrowserFile File { get; }
    public string TempFilePath { get; }
    public string PreviewUrl { get; }
    public string FileName => File.Name;
    public long Size => File.Size;
    public InvoiceDraftDetails Details { get; }
}

public class InvoiceDraftDetails
{
    [Required]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public string Amount { get; set; } = string.Empty;

    public string? VatAmount { get; set; }

    [Required]
    public string Currency { get; set; } = "EUR";

    [Required]
    public DateTime? InvoiceDate { get; set; }

    public string? Description { get; set; }
}
