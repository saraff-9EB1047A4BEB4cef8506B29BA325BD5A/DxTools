namespace DxTools.Core;

public sealed record CommandLineOptions(
    bool Autostart,
    bool SafeMode,
    bool ShowSettings,
    bool ShowVersion,
    bool ShowHelp,
    bool RequestExit,
    string? ApplyDisplayProfile,
    string? ApplyWindowProfile,
    bool ApplyWallpaper,
    string? SettingsPath,
    IReadOnlyList<string> Errors)
{
    public static CommandLineOptions Parse(IReadOnlyList<string>? args)
    {
        var autostart = false;
        var safeMode = false;
        var showSettings = false;
        var showVersion = false;
        var showHelp = false;
        var requestExit = false;
        string? applyDisplayProfile = null;
        string? applyWindowProfile = null;
        var applyWallpaper = false;
        string? settingsPath = null;
        var errors = new List<string>();

        if (args is null)
        {
            return new CommandLineOptions(false, false, false, false, false, false, null, null, false, null, errors);
        }

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            switch (arg.ToLowerInvariant())
            {
                case "--autostart":
                case "-a":
                    autostart = true;
                    break;
                case "--safe-mode":
                case "--safemode":
                    safeMode = true;
                    break;
                case "--show":
                    showSettings = true;
                    break;
                case "--version":
                case "-v":
                    showVersion = true;
                    break;
                case "--help":
                case "-h":
                case "/?":
                    showHelp = true;
                    break;
                case "--exit":
                    requestExit = true;
                    break;
                case "--apply-wallpaper":
                    applyWallpaper = true;
                    break;
                case "--apply-display":
                    if (i + 1 >= args.Count)
                    {
                        errors.Add("Missing value for --apply-display.");
                        break;
                    }

                    applyDisplayProfile = args[++i];
                    break;
                case "--apply-window":
                    if (i + 1 >= args.Count)
                    {
                        errors.Add("Missing value for --apply-window.");
                        break;
                    }

                    applyWindowProfile = args[++i];
                    break;
                case "--settings":
                    if (i + 1 >= args.Count)
                    {
                        errors.Add("Missing value for --settings.");
                        break;
                    }

                    settingsPath = args[++i];
                    break;
                default:
                    if (arg.StartsWith("--settings=", StringComparison.OrdinalIgnoreCase))
                    {
                        settingsPath = arg["--settings=".Length..];
                        if (string.IsNullOrWhiteSpace(settingsPath))
                        {
                            errors.Add("Missing value for --settings.");
                        }

                        break;
                    }

                    if (arg.StartsWith("--apply-display=", StringComparison.OrdinalIgnoreCase))
                    {
                        applyDisplayProfile = arg["--apply-display=".Length..];
                        if (string.IsNullOrWhiteSpace(applyDisplayProfile))
                        {
                            errors.Add("Missing value for --apply-display.");
                        }

                        break;
                    }

                    if (arg.StartsWith("--apply-window=", StringComparison.OrdinalIgnoreCase))
                    {
                        applyWindowProfile = arg["--apply-window=".Length..];
                        if (string.IsNullOrWhiteSpace(applyWindowProfile))
                        {
                            errors.Add("Missing value for --apply-window.");
                        }

                        break;
                    }

                    errors.Add($"Unknown argument: {arg}");
                    break;
            }
        }

        return new CommandLineOptions(
            autostart,
            safeMode,
            showSettings,
            showVersion,
            showHelp,
            requestExit,
            applyDisplayProfile,
            applyWindowProfile,
            applyWallpaper,
            settingsPath,
            errors);
    }
}
