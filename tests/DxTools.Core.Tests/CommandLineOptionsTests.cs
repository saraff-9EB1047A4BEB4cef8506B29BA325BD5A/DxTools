using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class CommandLineOptionsTests
{
    [Fact]
    public void Parse_AllFlags_AreRecognized()
    {
        var options = CommandLineOptions.Parse(
            ["--autostart", "--safe-mode", "--show", "--settings", @"C:\temp\settings.json"]);

        Assert.True(options.Autostart);
        Assert.True(options.SafeMode);
        Assert.True(options.ShowSettings);
        Assert.Equal(@"C:\temp\settings.json", options.SettingsPath);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_SettingsEqualsForm_IsRecognized()
    {
        var options = CommandLineOptions.Parse(["--settings=D:\\data\\dx.json"]);

        Assert.Equal("D:\\data\\dx.json", options.SettingsPath);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_UnknownArgument_ProducesError()
    {
        var options = CommandLineOptions.Parse(["--unknown"]);

        Assert.Single(options.Errors);
        Assert.Contains("--unknown", options.Errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_MissingSettingsValue_ProducesError()
    {
        var options = CommandLineOptions.Parse(["--settings"]);

        Assert.Null(options.SettingsPath);
        Assert.Single(options.Errors);
    }

    [Fact]
    public void Parse_Exit_IsRecognized()
    {
        var options = CommandLineOptions.Parse(["--exit"]);

        Assert.True(options.RequestExit);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_ApplyActions_AreRecognized()
    {
        var options = CommandLineOptions.Parse(
            ["--apply-display", "Desk", "--apply-window=Work", "--apply-wallpaper"]);

        Assert.Equal("Desk", options.ApplyDisplayProfile);
        Assert.Equal("Work", options.ApplyWindowProfile);
        Assert.True(options.ApplyWallpaper);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_WallpaperFileAndMonitor_AreRecognized()
    {
        var options = CommandLineOptions.Parse(
            ["--wallpaper", @"C:\Images\wall paper.png", "--monitor", @"\\.\DISPLAY2"]);

        Assert.Equal(@"C:\Images\wall paper.png", options.WallpaperPath);
        Assert.Equal(@"\\.\DISPLAY2", options.WallpaperMonitor);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_WallpaperEqualsForm_IsRecognized()
    {
        var options = CommandLineOptions.Parse(
            [@"--wallpaper=C:\Images\wall.png", @"--monitor=\\.\DISPLAY1"]);

        Assert.Equal(@"C:\Images\wall.png", options.WallpaperPath);
        Assert.Equal(@"\\.\DISPLAY1", options.WallpaperMonitor);
        Assert.Empty(options.Errors);
    }

    [Fact]
    public void Parse_MissingWallpaperValue_ProducesError()
    {
        var options = CommandLineOptions.Parse(["--wallpaper"]);

        Assert.Null(options.WallpaperPath);
        Assert.Single(options.Errors);
    }

    [Fact]
    public void Parse_MissingMonitorValue_ProducesError()
    {
        var options = CommandLineOptions.Parse(["--monitor"]);

        Assert.Null(options.WallpaperMonitor);
        Assert.Single(options.Errors);
    }

    [Fact]
    public void Parse_MissingApplyDisplayValue_ProducesError()
    {
        var options = CommandLineOptions.Parse(["--apply-display"]);

        Assert.Null(options.ApplyDisplayProfile);
        Assert.Single(options.Errors);
    }

    [Fact]
    public void Parse_Null_ReturnsDefaults()
    {
        var options = CommandLineOptions.Parse(null);

        Assert.False(options.Autostart);
        Assert.False(options.SafeMode);
        Assert.False(options.ShowSettings);
        Assert.Empty(options.Errors);
    }
}
