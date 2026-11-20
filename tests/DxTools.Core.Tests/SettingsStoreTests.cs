using System.IO;
using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(Path.Combine(folder.Path, "settings.json"));

        var result = store.Load();

        Assert.True(result.CreatedDefault);
        Assert.False(result.RecoveredFromCorruption);
        Assert.Equal(AppLanguage.Auto, result.Settings.Language);
        Assert.Equal(AppTheme.System, result.Settings.Theme);
        Assert.False(File.Exists(store.FilePath));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(Path.Combine(folder.Path, "nested", "settings.json"));
        var settings = new AppSettings
        {
            Language = AppLanguage.Russian,
            Theme = AppTheme.Dark,
            StartWithWindows = true,
            StartMinimized = false,
            FirstRunCompleted = true,
        };

        store.Save(settings);
        var result = store.Load();

        Assert.False(result.CreatedDefault);
        Assert.False(result.RecoveredFromCorruption);
        Assert.Equal(AppLanguage.Russian, result.Settings.Language);
        Assert.Equal(AppTheme.Dark, result.Settings.Theme);
        Assert.True(result.Settings.StartWithWindows);
        Assert.False(result.Settings.StartMinimized);
        Assert.True(result.Settings.FirstRunCompleted);
    }

    [Fact]
    public void Load_CorruptJson_BacksUpFileAndReturnsDefaults()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path, "{ this is not json");

        var store = new SettingsStore(path);
        var result = store.Load();

        Assert.True(result.RecoveredFromCorruption);
        Assert.True(result.CreatedDefault);
        Assert.NotNull(result.Error);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(folder.Path, "settings.corrupt-*.json"));
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        var store = new SettingsStore(path);

        store.Save(new AppSettings { Theme = AppTheme.Light });
        store.Save(new AppSettings { Theme = AppTheme.Dark });

        var result = store.Load();
        Assert.Equal(AppTheme.Dark, result.Settings.Theme);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Normalize_InvalidValues_FallBackToDefaults()
    {
        var settings = new AppSettings
        {
            SchemaVersion = 999,
            Language = (AppLanguage)99,
            Theme = (AppTheme)99,
        };

        settings.Normalize();

        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Equal(AppLanguage.Auto, settings.Language);
        Assert.Equal(AppTheme.System, settings.Theme);
    }
}
