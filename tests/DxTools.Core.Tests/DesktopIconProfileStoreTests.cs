using System.IO;
using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class DesktopIconProfileStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsPositions()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "desktop-icons.json");
        var store = new DesktopIconProfileStore(path);
        var profile = new DesktopIconProfile
        {
            Name = "Default",
            Icons =
            [
                new DesktopIconPosition(0, 10, 20),
                new DesktopIconPosition(1, 80, 40),
            ],
        };

        store.Save([profile]);
        var loaded = store.Load();

        var result = Assert.Single(loaded);
        Assert.Equal("Default", result.Name);
        Assert.Equal(2, result.Icons.Count);
        Assert.Equal(80, result.Icons[1].X);
    }

    [Fact]
    public void Load_CorruptFile_BacksUpAndReturnsEmpty()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "desktop-icons.json");
        File.WriteAllText(path, "{ broken");

        var store = new DesktopIconProfileStore(path);

        Assert.Empty(store.Load());
        Assert.Single(Directory.GetFiles(folder.Path, "desktop-icons.corrupt-*.json"));
    }
}
