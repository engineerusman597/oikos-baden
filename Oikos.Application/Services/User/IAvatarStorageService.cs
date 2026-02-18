namespace Oikos.Application.Services.User;

public interface IAvatarStorageService
{
    Task<string?> SaveAvatarAsync(string dataUrl, string? previousFileName, CancellationToken cancellationToken = default);
    Task DeleteAvatarAsync(string fileName, CancellationToken cancellationToken = default);
}
