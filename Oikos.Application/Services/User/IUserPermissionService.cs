namespace Oikos.Application.Services.User;

public interface IUserPermissionService
{
    Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);
    Task SetUserPermissionsAsync(int userId, List<string> permissions, CancellationToken cancellationToken = default);
}
