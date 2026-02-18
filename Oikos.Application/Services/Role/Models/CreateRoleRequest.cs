namespace Oikos.Application.Services.Role.Models;

public class CreateRoleRequest
{
    public string Name { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}
