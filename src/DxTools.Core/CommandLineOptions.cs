namespace DxTools.Core;

public sealed record CommandLineOptions(
    bool Autostart,
    bool SafeMode,
    bool ShowSettings,
    bool ShowVersion,
    bool ShowHelp,
    bool RequestExit,
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
        string? settingsPath = null;
        var errors = new List<string>();

        if (args is null)
        {
            return new CommandLineOptions(false, false, false, false, false, false, null, errors);
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
            settingsPath,
            errors);
    }
}
