using System.IO.Compression;

namespace DxTools.Core;

public static class SettingsBackupService
{
    public static void Export(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        AppPaths.EnsureDirectories();

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ZipFile.CreateFromDirectory(
            AppPaths.AppDataDirectory,
            destinationPath,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);
    }

    public static void Import(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        AppPaths.EnsureDirectories();

        ZipFile.ExtractToDirectory(sourcePath, AppPaths.AppDataDirectory, overwriteFiles: true);
    }
}
