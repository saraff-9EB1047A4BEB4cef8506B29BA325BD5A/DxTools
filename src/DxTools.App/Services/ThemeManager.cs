using System.IO;
using DxTools.Core;

namespace DxTools.App.Services;

public static class ThemeManager
{
    public static AppTheme Resolve(AppTheme theme) => theme == AppTheme.System ? GetSystemTheme() : theme;

    public static void Apply(AppTheme theme)
    {
        var effectiveTheme = Resolve(theme);
        var dictionary = new System.Windows.ResourceDictionary
        {
            Source = new Uri($"Themes/{effectiveTheme}.xaml", UriKind.Relative),
        };

        var merged = System.Windows.Application.Current?.Resources.MergedDictionaries;
        if (merged is null)
        {
            return;
        }

        if (merged.Count == 0)
        {
            merged.Add(dictionary);
        }
        else
        {
            merged[0] = dictionary;
        }

        DxLog.Info($"Theme applied: requested={theme}, effective={effectiveTheme}.");
    }

    private static AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                writable: false);

            var value = key?.GetValue("AppsUseLightTheme");
            return value is int light && light == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return AppTheme.Light;
        }
    }
}
