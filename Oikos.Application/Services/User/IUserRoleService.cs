using Oikos.Application.Services.Role.Models;

namespace Oikos.Application.Services.User;

public interface IUserRoleService
{
    Task<List<RoleDto>> GetUserRolesAsync(int userId);
    Task<List<RoleDto>> GetAvailableRolesAsync();
    Task<bool> UpdateUserRolesAsync(int userId, List<int> roleIds);
}
