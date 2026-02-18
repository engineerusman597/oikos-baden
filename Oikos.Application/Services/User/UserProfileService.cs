using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Security;
using Oikos.Application.Services.User.Models;

namespace Oikos.Application.Services.User;

public class UserProfileService : IUserProfileService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public UserProfileService(IAppDbContextFactory dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileSummary?> GetProfileSummaryAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileSummary(u.RealName, u.Avatar, u.CustomerNumber))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<UserProfileDetails?> GetProfileDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDetails(
                u.Name,
                u.RealName,
                u.AcademicTitle,
                u.Gender,
                u.Company,
                u.Email,
                u.PhoneNumber,
                u.CustomerNumber,
                u.Avatar))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return ChangePasswordResult.Fail(ChangePasswordError.UserNotFound);
        }

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !_passwordHasher.VerifyPassword(user.PasswordHash, currentPassword))
        {
            return ChangePasswordResult.Fail(ChangePasswordError.InvalidCurrentPassword);
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        await context.SaveChangesAsync(cancellationToken);

        return ChangePasswordResult.Ok();
    }

    public async Task<bool> UpdateAvatarAsync(int userId, string? avatarFileName, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.Avatar = avatarFileName;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
