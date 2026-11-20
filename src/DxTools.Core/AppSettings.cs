using System.Text.Json.Serialization;

namespace DxTools.Core;

[JsonConverter(typeof(JsonStringEnumConverter<AppLanguage>))]
public enum AppLanguage
{
    Auto,
    Russian,
    English,
}

[JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
public enum AppTheme
{
    System,
    Light,
    Dark,
}

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 7;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public AppLanguage Language { get; set; } = AppLanguage.Auto;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public bool StartWithWindows { get; set; }

    public bool StartMinimized { get; set; } = true;

    public bool FirstRunCompleted { get; set; }

    public bool EnableSecondaryTaskbar { get; set; } = true;

    public bool TaskbarAutoHide { get; set; }

    public bool EnableTitleBarButtons { get; set; }

    public WallpaperSettings Wallpaper { get; set; } = new();

    public List<HotkeySetting> Hotkeys { get; set; } = [];

    public List<PinnedApp> PinnedApps { get; set; } = [];

    public void Normalize()
    {
        if (!Enum.IsDefined(Language))
        {
            Language = AppLanguage.Auto;
        }

        if (!Enum.IsDefined(Theme))
        {
            Theme = AppTheme.System;
        }

        if (SchemaVersion <= 0 || SchemaVersion > CurrentSchemaVersion)
        {
            SchemaVersion = CurrentSchemaVersion;
        }

        Wallpaper ??= new WallpaperSettings();
        Wallpaper.Normalize();

        Hotkeys ??= new List<HotkeySetting>();
        Hotkeys.RemoveAll(hotkey => !Enum.IsDefined(hotkey.Action));
        var seenActions = new HashSet<HotkeyAction>();
        Hotkeys.RemoveAll(hotkey => !seenActions.Add(hotkey.Action));
        var defaults = HotkeySetting.CreateDefaults();
        foreach (var defaultHotkey in defaults)
        {
            if (Hotkeys.All(hotkey => hotkey.Action != defaultHotkey.Action))
            {
                Hotkeys.Add(defaultHotkey);
            }
        }

        foreach (var hotkey in Hotkeys)
        {
            if (string.IsNullOrWhiteSpace(hotkey.Gesture))
            {
                hotkey.Gesture = defaults.First(item => item.Action == hotkey.Action).Gesture;
            }
        }

        PinnedApps ??= new List<PinnedApp>();
        PinnedApps.RemoveAll(app => string.IsNullOrWhiteSpace(app.Path));
        foreach (var app in PinnedApps)
        {
            app.Path = app.Path.Trim();
            if (string.IsNullOrWhiteSpace(app.Name))
            {
                app.Name = System.IO.Path.GetFileNameWithoutExtension(app.Path);
            }
        }
    }
}
