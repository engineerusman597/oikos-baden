namespace Oikos.Application.Common.Storage;

public static class UserStoragePath
{
    private static readonly string[] BaseSegments = ["uploads", "users"];

    public static string GetRelativePath(int userId, params string[]? subDirectories)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User id must be greater than zero.");
        }

        var segments = new List<string>(BaseSegments.Length + 1 + (subDirectories?.Length ?? 0));
        segments.AddRange(BaseSegments);
        segments.Add(userId.ToString());

        if (subDirectories is { Length: > 0 })
        {
            foreach (var directory in subDirectories)
            {
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    segments.Add(directory);
                }
            }
        }

        return Path.Combine(segments.ToArray());
    }
}
