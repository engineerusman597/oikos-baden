namespace Oikos.Application.Services.User.Models;

public class CreateUserRequest
{
    public string Name { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? CustomerNumber { get; set; }
    public string? AcademicTitle { get; set; }
    public string? Gender { get; set; }
    public int? PartnerId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? RoleId { get; set; }
}
