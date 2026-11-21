using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DxTools.Interop;

namespace DxTools.Engine;

public static class WindowMatcher
{
    private static readonly Regex CountSuffix = new(@"\s*\(\d+\)\s*$", RegexOptions.Compiled);
    private static readonly Regex WindowCountSuffix = new(
        @"\s*[—–-]\s*[^—–-]*\d+[^—–-]*(?:окн|window)[^—–-]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly ConcurrentDictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase);

    public static string NormalizeButtonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = Regex.Replace(value, @"\s+", " ").Trim();
        text = CountSuffix.Replace(text, string.Empty);
        text = WindowCountSuffix.Replace(text, string.Empty);
        return text.Trim();
    }

    public static string NormalizeAutomationId(string? value)
    {
        var text = NormalizeButtonName(value);
        const string prefix = "Appid:";
        return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? text[prefix.Length..].Trim()
            : text;
    }

    public static IReadOnlyList<AppWindow> Match(
        IReadOnlyList<AppWindow> windows,
        string? buttonName,
        string? automationId)
    {
        ArgumentNullException.ThrowIfNull(windows);

        var name = NormalizeButtonName(buttonName);
        var automation = NormalizeAutomationId(automationId);
        if (name.Length == 0 && automation.Length == 0)
        {
            return Array.Empty<AppWindow>();
        }

        if (automation.Length > 0)
        {
            var byAutomation = windows
                .Where(window => MatchesAutomation(window, automation))
                .ToList();

            if (byAutomation.Count > 0)
            {
                return byAutomation;
            }
        }

        var exact = windows
            .Where(window => name.Length > 0
                && string.Equals(NormalizeButtonName(window.Title), name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exact.Count > 0)
        {
            return exact;
        }

        if (name.Length > 0)
        {
            var byApplication = windows
                .Where(window => MatchesExecutable(window, name) || MatchesDescription(window, name))
                .ToList();

            if (byApplication.Count > 0)
            {
                return byApplication;
            }

            if (name.Length >= 3)
            {
                var partial = windows
                    .Where(window => Contains(window.Title, name) || Contains(name, window.Title))
                    .ToList();

                if (partial.Count > 0)
                {
                    return partial;
                }
            }
        }

        return Array.Empty<AppWindow>();
    }

    private static bool MatchesAutomation(AppWindow window, string automation)
    {
        if (automation.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || automation.Contains('\\')
            || automation.Contains('/'))
        {
            return MatchesExecutable(window, Path.GetFileNameWithoutExtension(automation));
        }

        var appUserModelId = WindowAppId.Get(window.Handle);
        if (appUserModelId.Length > 0
            && string.Equals(appUserModelId, automation, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return MatchesExecutable(window, automation);
    }

    private static bool MatchesExecutable(AppWindow window, string value)
    {
        if (string.IsNullOrWhiteSpace(window.ExecutablePath))
        {
            return false;
        }

        return string.Equals(
            Path.GetFileNameWithoutExtension(window.ExecutablePath),
            value,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDescription(AppWindow window, string value)
    {
        var description = GetDescription(window.ExecutablePath);
        return description.Length > 0
            && string.Equals(description, value, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDescription(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return string.Empty;
        }

        return Descriptions.GetOrAdd(executablePath, static path =>
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(path).FileDescription?.Trim() ?? string.Empty;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return string.Empty;
            }
        });
    }

    private static bool Contains(string value, string needle)
    {
        return value.Length > 0
            && needle.Length > 0
            && value.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
