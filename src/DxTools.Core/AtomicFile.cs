using System.Text;

namespace DxTools.Core;

public static class AtomicFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";

        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tempPath, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Move(tempPath, path, overwrite: true);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public static string? TryBackupCorrupt(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(directory ?? string.Empty, $"{fileName}.corrupt-{stamp}{extension}");

            if (File.Exists(backupPath))
            {
                backupPath = Path.Combine(
                    directory ?? string.Empty,
                    $"{fileName}.corrupt-{stamp}-{Guid.NewGuid():N}{extension}");
            }

            File.Move(path, backupPath);
            return backupPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
