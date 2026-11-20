using System.IO;
using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class SettingsBackupServiceTests
{
    [Fact]
    public void ExportAndImport_RoundTripsFiles()
    {
        using var source = new TempFolder();
        using var destination = new TempFolder();
        File.WriteAllText(Path.Combine(source.Path, "settings.json"), "{\"theme\":\"Dark\"}");

        var archive = Path.Combine(destination.Path, "backup.zip");
        SettingsBackupService.Export(archive, source.Path);
        Assert.True(File.Exists(archive));

        var restore = Path.Combine(destination.Path, "restore");
        SettingsBackupService.Import(archive, restore);

        Assert.Equal(
            "{\"theme\":\"Dark\"}",
            File.ReadAllText(Path.Combine(restore, "settings.json")));
    }
}
