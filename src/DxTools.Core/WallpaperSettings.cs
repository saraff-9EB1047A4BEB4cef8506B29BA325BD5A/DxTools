using System.Text.Json.Serialization;

namespace DxTools.Core;

[JsonConverter(typeof(JsonStringEnumConverter<WallpaperFitMode>))]
public enum WallpaperFitMode
{
    Fill,
    Fit,
    Stretch,
    Center,
    Tile,
}

public sealed class WallpaperSettings
{
    public WallpaperFitMode FitMode { get; set; } = WallpaperFitMode.Fill;

    public Dictionary<string, string> MonitorImages { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? SlideshowFolder { get; set; }

    public int SlideshowIntervalSeconds { get; set; } = 300;

    public bool ScreensaverIncludeVideos { get; set; } = true;

    public void Normalize()
    {
        if (!Enum.IsDefined(FitMode))
        {
            FitMode = WallpaperFitMode.Fill;
        }

        SlideshowIntervalSeconds = Math.Clamp(SlideshowIntervalSeconds, 10, 86400);
        MonitorImages ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
