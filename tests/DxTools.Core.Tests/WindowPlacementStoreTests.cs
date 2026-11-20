using System.IO;
using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class WindowPlacementStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsEntries()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "window-profiles.json");
        var store = new WindowPlacementStore(path);
        var profile = new WindowPlacementProfile
        {
            Name = "Work",
            Windows =
            [
                new WindowPlacementEntry
                {
                    ExecutablePath = @"C:\Apps\Editor.exe",
                    Title = "Project — Editor",
                    X = 100,
                    Y = 80,
                    Width = 1200,
                    Height = 900,
                    IsMaximized = true,
                    MonitorDeviceName = @"\\.\DISPLAY1",
                },
            ],
        };

        store.Save([profile]);
        var loaded = store.Load();

        var result = Assert.Single(loaded);
        Assert.Equal("Work", result.Name);
        var entry = Assert.Single(result.Windows);
        Assert.Equal(@"C:\Apps\Editor.exe", entry.ExecutablePath);
        Assert.True(entry.IsMaximized);
        Assert.Equal(1200, entry.Width);
    }

    [Fact]
    public void Load_CorruptFile_BacksUpAndReturnsEmpty()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "window-profiles.json");
        File.WriteAllText(path, "{ broken");

        var store = new WindowPlacementStore(path);

        Assert.Empty(store.Load());
        Assert.Single(Directory.GetFiles(folder.Path, "window-profiles.corrupt-*.json"));
    }
}
