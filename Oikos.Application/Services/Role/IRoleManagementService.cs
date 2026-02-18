using Oikos.Application.Services.Role.Models;

namespace Oikos.Application.Services.Role;

public interface IRoleManagementService
{
    Task<PaginatedResult<RoleDto>> GetRolesAsync(RoleSearchCriteria criteria);
    Task<RoleDto?> GetRoleDetailAsync(int roleId);
    Task<bool> CreateRoleAsync(CreateRoleRequest request);
    Task<bool> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
    Task<(bool Success, string? ErrorMessage)> DeleteRoleAsync(int roleId);
    Task<bool> ChangeRoleStatusAsync(int roleId, bool isEnabled);
}
