using System.IO.Compression;

namespace DxTools.Core;

public static class SettingsBackupService
{
    public static void Export(string destinationPath, string? sourceDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        AppPaths.EnsureDirectories();

        var source = string.IsNullOrWhiteSpace(sourceDirectory) ? AppPaths.AppDataDirectory : sourceDirectory;
        Directory.CreateDirectory(source);

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ZipFile.CreateFromDirectory(
            source,
            destinationPath,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);
    }

    public static void Import(string sourcePath, string? destinationDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        AppPaths.EnsureDirectories();

        var destination = string.IsNullOrWhiteSpace(destinationDirectory) ? AppPaths.AppDataDirectory : destinationDirectory;
        Directory.CreateDirectory(destination);

        ZipFile.ExtractToDirectory(sourcePath, destination, overwriteFiles: true);
    }
}
