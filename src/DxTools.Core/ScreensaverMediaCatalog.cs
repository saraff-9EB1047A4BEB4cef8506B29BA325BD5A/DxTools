namespace DxTools.Core;

public sealed record ScreensaverMediaItem(string Path, bool IsVideo);

public static class ScreensaverMediaCatalog
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".wmv",
        ".avi",
        ".mov",
        ".m4v",
    };

    public static IReadOnlyList<ScreensaverMediaItem> Enumerate(string? folder, bool includeVideos)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Array.Empty<ScreensaverMediaItem>();
        }

        try
        {
            return Directory
                .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Select(path => Create(path, includeVideos))
                .Where(item => item is not null)
                .Cast<ScreensaverMediaItem>()
                .OrderBy(item => item.Path, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DxLog.Warn($"Screensaver media enumeration failed: {ex.Message}");
            return Array.Empty<ScreensaverMediaItem>();
        }
    }

    private static ScreensaverMediaItem? Create(string path, bool includeVideos)
    {
        var extension = Path.GetExtension(path);
        if (ImageExtensions.Contains(extension))
        {
            return new ScreensaverMediaItem(path, IsVideo: false);
        }

        if (includeVideos && VideoExtensions.Contains(extension))
        {
            return new ScreensaverMediaItem(path, IsVideo: true);
        }

        return null;
    }
}
