namespace Oikos.Application.Services.User.Models;

public class CurrentUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? AcademicTitle { get; set; }
    public string? CustomerNumber { get; set; }
    public string? Avatar { get; set; }
    public string? Email { get; set; }
    public bool IsEnabled { get; set; }
    
    public string DisplayName => !string.IsNullOrWhiteSpace(RealName) 
        ? (!string.IsNullOrWhiteSpace(AcademicTitle) ? $"{AcademicTitle} {RealName}" : RealName)
        : Name;
}
