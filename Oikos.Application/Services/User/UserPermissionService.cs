using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Domain.Entities.Rbac;

namespace Oikos.Application.Services.User;

public class UserPermissionService : IUserPermissionService
{
    private readonly IAppDbContextFactory _dbFactory;

    public UserPermissionService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task SetUserPermissionsAsync(int userId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        context.UserPermissions.RemoveRange(existing);

        foreach (var permission in permissions.Distinct())
        {
            context.UserPermissions.Add(new UserPermission { UserId = userId, Permission = permission });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
