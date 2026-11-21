using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class ScreensaverCommandLineTests
{
    [Fact]
    public void Parse_NoArguments_Configures()
    {
        var options = ScreensaverCommandLine.Parse([]);

        Assert.Equal(ScreensaverMode.Configure, options.Mode);
        Assert.True(options.IsValid);
    }

    [Fact]
    public void Parse_RunAndConfigureForms()
    {
        Assert.Equal(ScreensaverMode.Run, ScreensaverCommandLine.Parse(["/s"]).Mode);
        Assert.Equal(ScreensaverMode.Run, ScreensaverCommandLine.Parse(["/S"]).Mode);
        Assert.Equal(ScreensaverMode.Configure, ScreensaverCommandLine.Parse(["/c"]).Mode);
        Assert.Equal(ScreensaverMode.Configure, ScreensaverCommandLine.Parse(["/c:12345"]).Mode);
    }

    [Theory]
    [InlineData("/p", "12345", 12345)]
    [InlineData("/p:12345", null, 12345)]
    [InlineData("/P:0x3039", null, 12345)]
    public void Parse_PreviewHandle(string first, string? second, long expected)
    {
        var args = second is null ? new[] { first } : new[] { first, second };
        var options = ScreensaverCommandLine.Parse(args);

        Assert.Equal(ScreensaverMode.Preview, options.Mode);
        Assert.True(options.IsValid);
        Assert.Equal(new IntPtr(expected), options.PreviewWindow);
    }

    [Fact]
    public void Parse_PreviewWithoutHandle_IsInvalid()
    {
        var options = ScreensaverCommandLine.Parse(["/p"]);

        Assert.Equal(ScreensaverMode.Preview, options.Mode);
        Assert.False(options.IsValid);
        Assert.Equal(IntPtr.Zero, options.PreviewWindow);
    }

    [Fact]
    public void Catalog_ListsImagesAndOptionalVideos()
    {
        using var folder = new TempFolder();
        File.WriteAllText(Path.Combine(folder.Path, "a.jpg"), string.Empty);
        File.WriteAllText(Path.Combine(folder.Path, "b.MP4"), string.Empty);
        File.WriteAllText(Path.Combine(folder.Path, "c.txt"), string.Empty);
        File.WriteAllText(Path.Combine(folder.Path, "d.png"), string.Empty);

        var withVideos = ScreensaverMediaCatalog.Enumerate(folder.Path, includeVideos: true);
        var withoutVideos = ScreensaverMediaCatalog.Enumerate(folder.Path, includeVideos: false);

        Assert.Equal(3, withVideos.Count);
        Assert.Contains(withVideos, item => item.Path.EndsWith("b.MP4") && item.IsVideo);
        Assert.Equal(2, withoutVideos.Count);
        Assert.DoesNotContain(withoutVideos, item => item.IsVideo);
    }
}
