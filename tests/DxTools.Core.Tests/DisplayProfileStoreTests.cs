using System.IO;
using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class DisplayProfileStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsProfiles()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "display-profiles.json");
        var store = new DisplayProfileStore(path);
        var profile = new DisplayProfile
        {
            Name = "Desk",
            TopologyHash = "abc",
            StateBase64 = Convert.ToBase64String([1, 2, 3]),
            AutoApply = true,
            Monitors =
            [
                new MonitorInfo("\\\\.\\DISPLAY1", "MONITOR\\A", "Left", 0, 0, 1920, 1080, true, 96, 60, 32),
                new MonitorInfo("\\\\.\\DISPLAY2", "MONITOR\\B", "Right", 1920, 0, 1920, 1080, false, 144, 60, 32),
            ],
        };

        store.Save([profile]);
        var loaded = store.Load();

        var result = Assert.Single(loaded);
        Assert.Equal("Desk", result.Name);
        Assert.Equal("abc", result.TopologyHash);
        Assert.True(result.AutoApply);
        Assert.Equal(2, result.Monitors.Count);
        Assert.Equal("Left", result.Monitors[0].FriendlyName);
        Assert.DoesNotContain("geometryText", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsEmptyAndBacksUp()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "display-profiles.json");
        File.WriteAllText(path, "{ broken");

        var store = new DisplayProfileStore(path);
        var loaded = store.Load();

        Assert.Empty(loaded);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(folder.Path, "display-profiles.corrupt-*.json"));
    }
}
