using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Role.Models;

namespace Oikos.Application.Services.User;

public class UserRoleService : IUserRoleService
{
    private readonly IAppDbContextFactory _dbFactory;

    public UserRoleService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<RoleDto>> GetUserRolesAsync(int userId)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var roles = await (from ur in context.UserRoles
                          join role in context.Roles on ur.RoleId equals role.Id
                          where ur.UserId == userId && !role.IsDeleted
                          select new RoleDto
                          {
                              Id = role.Id,
                              Number = 0,
                              Name = role.Name,
                              IsEnabled = role.IsEnabled
                          }).ToListAsync();

        return roles;
    }

    public async Task<List<RoleDto>> GetAvailableRolesAsync()
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var roles = await context.Roles
            .Where(r => !r.IsDeleted && r.IsEnabled)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Number = 0,
                Name = r.Name,
                IsEnabled = r.IsEnabled
            })
            .ToListAsync();

        return roles;
    }

    public async Task<bool> UpdateUserRolesAsync(int userId, List<int> roleIds)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        // Check if user exists
        var userExists = await context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return false;

        // Remove existing roles
        var existingUserRoles = context.UserRoles.Where(ur => ur.UserId == userId);
        context.UserRoles.RemoveRange(existingUserRoles);

        // Add new roles
        foreach (var roleId in roleIds)
        {
            context.UserRoles.Add(new Domain.Entities.Rbac.UserRole
            {
                UserId = userId,
                RoleId = roleId
            });
        }

        await context.SaveChangesAsync();
        return true;
    }
}
