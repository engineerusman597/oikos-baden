using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Role.Models;

namespace Oikos.Application.Services.Role;

public class RoleManagementService : IRoleManagementService
{
    private readonly IAppDbContextFactory _dbFactory;

    public RoleManagementService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PaginatedResult<RoleDto>> GetRolesAsync(RoleSearchCriteria criteria)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        IQueryable<Domain.Entities.Rbac.Role> query = context.Roles.Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var search = criteria.SearchText.Trim();
            query = query.Where(r => r.Name != null && r.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var skip = (criteria.Page - 1) * criteria.PageSize;

        var roles = await query
            .Skip(skip)
            .Take(criteria.PageSize)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Number = 0, // Will be set below
                Name = r.Name,
                IsEnabled = r.IsEnabled
            })
            .ToListAsync();

        // Set row numbers
        for (var index = 0; index < roles.Count; index++)
        {
            roles[index].Number = skip + index + 1;
        }

        return new PaginatedResult<RoleDto>
        {
            Items = roles,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RoleDto?> GetRoleDetailAsync(int roleId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var role = await context.Roles.AsNoTracking()
            .Where(r => r.Id == roleId && !r.IsDeleted)
            .FirstOrDefaultAsync();

        if (role == null)
            return null;

        return new RoleDto
        {
            Id = role.Id,
            Number = 0,
            Name = role.Name,
            IsEnabled = role.IsEnabled
        };
    }

    public async Task<bool> CreateRoleAsync(CreateRoleRequest request)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var role = new Domain.Entities.Rbac.Role
        {
            Name = request.Name.Trim(),
            IsEnabled = request.IsEnabled
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var role = await context.Roles.FindAsync(roleId);
        if (role == null || role.IsDeleted)
            return false;

        role.Name = request.Name.Trim();
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteRoleAsync(int roleId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        // Check if role is in use
        if (await context.UserRoles.AnyAsync(ur => ur.RoleId == roleId))
            return (false, "RoleInUse");

        var role = await context.Roles.FindAsync(roleId);
        if (role == null)
            return (false, "RoleNotFound");

        // Hard delete the role
        context.Roles.Remove(role);
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ChangeRoleStatusAsync(int roleId, bool isEnabled)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var role = await context.Roles.FindAsync(roleId);
        if (role == null || role.IsDeleted)
            return false;

        role.IsEnabled = isEnabled;
        await context.SaveChangesAsync();
        return true;
    }
}
