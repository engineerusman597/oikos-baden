using Oikos.Application.Services.Authentication.Models;

namespace Oikos.Application.Services.Authentication;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task RecordLoginAttemptAsync(string identifier, bool success, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<int> GetUserIdAsync();
    Task<string> GetUserNameAsync();
    Task<string?> GetUserEmailAsync();
    Task<bool> IsAdminAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserInfoDto?> GetUserInfoAsync();
}
