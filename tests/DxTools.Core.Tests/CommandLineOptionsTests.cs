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
    public void Parse_Null_ReturnsDefaults()
    {
        var options = CommandLineOptions.Parse(null);

        Assert.False(options.Autostart);
        Assert.False(options.SafeMode);
        Assert.False(options.ShowSettings);
        Assert.Empty(options.Errors);
    }
}
