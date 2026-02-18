using System.ComponentModel.DataAnnotations;

namespace Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models;

public class PowerOfAttorneyModel
{
    private string _signature = string.Empty;

    [Required]
    public bool Accepted { get; set; }

    [Required]
    public string Signature
    {
        get => _signature;
        set => _signature = value?.Trim() ?? string.Empty;
    }

    public DateTimeOffset? SignedAt { get; set; }
}
