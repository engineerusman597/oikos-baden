using Oikos.Application.Services.User.Models;

namespace Oikos.Application.Services.User;

public interface IUserProfileService
{
    Task<UserProfileSummary?> GetProfileSummaryAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserProfileDetails?> GetProfileDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<ChangePasswordResult> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAvatarAsync(int userId, string? avatarFileName, CancellationToken cancellationToken = default);
}
