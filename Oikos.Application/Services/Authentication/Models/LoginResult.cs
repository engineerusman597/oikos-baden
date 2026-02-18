namespace Oikos.Application.Services.Authentication.Models;

public class LoginResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLockedOut { get; set; }
    public int? LockoutMinutes { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? RealName { get; set; }
    public List<string> Roles { get; set; } = new();

}
