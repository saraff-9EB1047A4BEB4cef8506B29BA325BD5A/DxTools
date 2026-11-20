using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class WallpaperSettingsTests
{
    [Fact]
    public void Normalize_ClampsIntervalAndFallsBackToFill()
    {
        var settings = new WallpaperSettings
        {
            FitMode = (WallpaperFitMode)99,
            SlideshowIntervalSeconds = 1,
            MonitorImages = null!,
        };

        settings.Normalize();

        Assert.Equal(WallpaperFitMode.Fill, settings.FitMode);
        Assert.Equal(10, settings.SlideshowIntervalSeconds);
        Assert.NotNull(settings.MonitorImages);
    }

    [Fact]
    public void AppSettings_Normalize_CreatesWallpaperSettings()
    {
        var settings = new AppSettings
        {
            Wallpaper = null!,
        };

        settings.Normalize();

        Assert.NotNull(settings.Wallpaper);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }
}
