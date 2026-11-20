using System.IO;
using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class TriggerStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsTriggers()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "triggers.json");
        var store = new TriggerStore(path);
        var trigger = new TriggerDefinition
        {
            Name = "Startup wallpaper",
            Event = TriggerEventType.Startup,
            Action = TriggerActionType.ApplyWallpaper,
            Enabled = true,
        };

        store.Save([trigger]);
        var loaded = store.Load();

        var result = Assert.Single(loaded);
        Assert.Equal("Startup wallpaper", result.Name);
        Assert.Equal(TriggerEventType.Startup, result.Event);
        Assert.Equal(TriggerActionType.ApplyWallpaper, result.Action);
    }

    [Fact]
    public void Load_CorruptFile_BacksUpAndReturnsEmpty()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "triggers.json");
        File.WriteAllText(path, "{ broken");

        var store = new TriggerStore(path);

        Assert.Empty(store.Load());
        Assert.Single(Directory.GetFiles(folder.Path, "triggers.corrupt-*.json"));
    }
}
