namespace Oikos.Common.Helpers;

public static class FileHelper
{
    /// <summary>
    /// Attempts to delete a file. If the file does not exist or an error occurs, it is ignored.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    public static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore exceptions as this is a "Try" method intended for cleanup
        }
    }
}
