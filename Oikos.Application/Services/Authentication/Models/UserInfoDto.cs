namespace Oikos.Application.Services.Authentication.Models;

public class UserInfoDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
