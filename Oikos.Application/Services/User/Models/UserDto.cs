namespace Oikos.Application.Services.User.Models;

public class UserDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string? Avatar { get; set; }
    public string Name { get; set; } = null!;
    public string? RealName { get; set; }
    public string? Title { get; set; }
    public string? TitleDisplay { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? CustomerNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? PartnerName { get; set; }
    public string? PartnerCode { get; set; }
    public string? PlanName { get; set; }
    public string? PlanExpirationDisplay { get; set; }
    public string? SepaMandatePath { get; set; }
    public DateTime? SepaMandateGeneratedAt { get; set; }
    public string? Gender { get; set; }
    public string? GenderDisplay { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
    public DateTime? PrivacyAcceptedAt { get; set; }
    public bool IsEnabled { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool HasActiveSubscription { get; set; }
    public int InvoiceCount { get; set; }
    public string? SubscriptionPaymentMethod { get; set; }
    public DateTime? LastLoginAt { get; set; }

}
