namespace Oikos.Application.Services.Registration.Models;

public class RegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public int? UserId { get; set; }
}
