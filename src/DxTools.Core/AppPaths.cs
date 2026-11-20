namespace DxTools.Core;

public static class AppPaths
{
    public const string ProductName = DxToolsInfo.ProductName;

    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ProductName);

    public static string LocalAppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductName);

    public static string SettingsFile { get; } = Path.Combine(AppDataDirectory, "settings.json");

    public static string LogDirectory { get; } = Path.Combine(LocalAppDataDirectory, "logs");

    public static string CrashDirectory { get; } = Path.Combine(LocalAppDataDirectory, "crashes");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(CrashDirectory);
    }
}
