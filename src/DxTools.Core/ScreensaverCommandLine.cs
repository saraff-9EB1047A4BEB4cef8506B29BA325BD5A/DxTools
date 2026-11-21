using System.Globalization;

namespace DxTools.Core;

public enum ScreensaverMode
{
    Configure,
    Run,
    Preview,
}

public sealed record ScreensaverCommandOptions(
    ScreensaverMode Mode,
    IntPtr PreviewWindow,
    bool IsValid,
    string? Error);

public static class ScreensaverCommandLine
{
    public static ScreensaverCommandOptions Parse(IReadOnlyList<string>? args)
    {
        if (args is null || args.Count == 0)
        {
            return new ScreensaverCommandOptions(ScreensaverMode.Configure, IntPtr.Zero, true, null);
        }

        var first = args[0]?.Trim() ?? string.Empty;
        if (first.Length == 0)
        {
            return new ScreensaverCommandOptions(ScreensaverMode.Configure, IntPtr.Zero, true, null);
        }

        var lower = first.ToLowerInvariant();
        if (lower is "/s" or "-s" or "/start")
        {
            return new ScreensaverCommandOptions(ScreensaverMode.Run, IntPtr.Zero, true, null);
        }

        if (lower is "/c" or "-c" or "/config")
        {
            return new ScreensaverCommandOptions(ScreensaverMode.Configure, IntPtr.Zero, true, null);
        }

        if (lower.StartsWith("/c:", StringComparison.Ordinal) || lower.StartsWith("-c:", StringComparison.Ordinal))
        {
            return new ScreensaverCommandOptions(ScreensaverMode.Configure, IntPtr.Zero, true, null);
        }

        if (lower is "/p" or "-p" or "/preview")
        {
            var value = args.Count > 1 ? args[1] : null;
            return ParsePreview(value);
        }

        if (lower.StartsWith("/p:", StringComparison.Ordinal) || lower.StartsWith("-p:", StringComparison.Ordinal))
        {
            return ParsePreview(first[(first.IndexOf(':') + 1)..]);
        }

        return new ScreensaverCommandOptions(
            ScreensaverMode.Configure,
            IntPtr.Zero,
            false,
            $"Unknown screensaver argument '{first}'.");
    }

    private static ScreensaverCommandOptions ParsePreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ScreensaverCommandOptions(
                ScreensaverMode.Preview,
                IntPtr.Zero,
                false,
                "Missing preview window handle.");
        }

        var text = value.Trim();
        var style = NumberStyles.Integer;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
            style = NumberStyles.HexNumber;
        }

        if (!long.TryParse(text, style, CultureInfo.InvariantCulture, out var handle)
            || handle <= 0)
        {
            return new ScreensaverCommandOptions(
                ScreensaverMode.Preview,
                IntPtr.Zero,
                false,
                $"Invalid preview window handle '{value}'.");
        }

        return new ScreensaverCommandOptions(
            ScreensaverMode.Preview,
            new IntPtr(handle),
            true,
            null);
    }
}
