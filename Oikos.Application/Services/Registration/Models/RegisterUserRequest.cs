namespace Oikos.Application.Services.Registration.Models;

public class RegisterUserRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? Company { get; set; }
    public string? Gender { get; set; }
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PartnerCode { get; set; }
    public bool AcceptedPrivacy { get; set; }
    public bool IsBonixUser { get; set; }
    public bool SkipSubscriptionCheck { get; set; }
}
