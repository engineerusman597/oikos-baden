namespace Oikos.Application.Services.Authentication.Models;

public class LoginRequest
{
    public required string Identifier { get; set; }
    public required string Password { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
