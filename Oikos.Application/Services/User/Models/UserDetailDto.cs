namespace Oikos.Application.Services.User.Models;

public class UserDetailDto
{
    public int Id { get; set; }
    public string? RealName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? CustomerNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsEnabled { get; set; }
    public string? Title { get; set; }
    public string? Gender { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
    public DateTime? PrivacyAcceptedAt { get; set; }
    public string? SepaMandatePath { get; set; }
    public DateTime? SepaMandateGeneratedAt { get; set; }
    public string? PartnerName { get; set; }
    public string? PartnerCode { get; set; }
    public string? Avatar { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<SubscriptionInfo> Subscriptions { get; set; } = new();
    public List<InvoiceInfo> Invoices { get; set; } = new();
}

public class SubscriptionInfo
{
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BillingInterval { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool AutoRenew { get; set; }
}

public class InvoiceInfo
{
    public int Id { get; set; }
    public string? Company { get; set; }
    public string? Amount { get; set; }
    public string? Currency { get; set; }
    public string StageName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
