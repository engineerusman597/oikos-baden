using System.ComponentModel.DataAnnotations;

namespace Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models;

public class ClaimPreferencesModel
{
    public ClaimStartOption StartOption { get; set; } = ClaimStartOption.Immediate;

    public DateTime? StartAfterReminderAt { get; set; }

    [Required]
    public bool AgreeTerms { get; set; }

    [Required]
    public bool AgreePrivacy { get; set; }

    public string? AdditionalNotes { get; set; }
}

public enum ClaimStartOption
{
    Immediate,
    AfterSevenDays,
    AfterReminder
}
