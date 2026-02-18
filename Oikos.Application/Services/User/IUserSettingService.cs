namespace Oikos.Application.Services.User;

public interface IUserSettingService
{
    Task<string?> GetSettingAsync(int userId, string key, CancellationToken cancellationToken = default);
    Task SaveSettingAsync(int userId, string key, string value, CancellationToken cancellationToken = default);
}
