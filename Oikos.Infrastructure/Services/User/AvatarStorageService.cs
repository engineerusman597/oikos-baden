using System.Text.RegularExpressions;
using Oikos.Application.Services.User;

namespace Oikos.Infrastructure.Services.User;

public class AvatarStorageService : IAvatarStorageService
{
    public Task<string?> SaveAvatarAsync(string dataUrl, string? previousFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            return Task.FromResult<string?>(null);
        }

        var fileName = SaveDataUrlToFile(dataUrl, Path.Combine(AppContext.BaseDirectory, "Avatars"));

        if (!string.IsNullOrEmpty(previousFileName) &&
            !string.Equals(previousFileName, fileName, StringComparison.OrdinalIgnoreCase))
        {
            DeleteFile(previousFileName);
        }

        return Task.FromResult<string?>(fileName);
    }

    public Task DeleteAvatarAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            DeleteFile(fileName);
        }

        return Task.CompletedTask;
    }

    private static string SaveDataUrlToFile(string dataUrl, string savePath)
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        var matchGroups = Regex.Match(dataUrl, @"^data:((?<type>[\w\/]+))?;base64,(?<data>.+)$").Groups;
        var base64Data = matchGroups["data"].Value;
        var binData = Convert.FromBase64String(base64Data);

        var suffix = matchGroups["type"].Value switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpeg",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        var newFileName = Guid.NewGuid().ToString("N") + suffix;
        File.WriteAllBytes(Path.Combine(savePath, newFileName), binData);
        return newFileName;
    }

    private static void DeleteFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Avatars", fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
