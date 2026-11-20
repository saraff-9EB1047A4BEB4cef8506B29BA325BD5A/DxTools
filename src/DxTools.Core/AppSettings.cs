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
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public AppLanguage Language { get; set; } = AppLanguage.Auto;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public bool StartWithWindows { get; set; }

    public bool StartMinimized { get; set; } = true;

    public bool FirstRunCompleted { get; set; }

    public WallpaperSettings Wallpaper { get; set; } = new();

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
    }
}
