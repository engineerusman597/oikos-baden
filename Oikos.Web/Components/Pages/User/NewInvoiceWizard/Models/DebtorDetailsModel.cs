using System.ComponentModel.DataAnnotations;

namespace Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models;

public class DebtorDetailsModel
{
    public DebtorType DebtorType { get; set; } = DebtorType.Company;

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    public string Street { get; set; } = string.Empty;

    [Required]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }
}

public enum DebtorType
{
    Company,
    Individual
}
