using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed record SettingsLoadResult(
    AppSettings Settings,
    bool CreatedDefault,
    bool RecoveredFromCorruption,
    string? Error);

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private readonly object _gate = new();
    private readonly string _filePath;

    public SettingsStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath) ? AppPaths.SettingsFile : filePath;
    }

    public string FilePath => _filePath;

    public SettingsLoadResult Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
            {
                return new SettingsLoadResult(new AppSettings(), CreatedDefault: true, RecoveredFromCorruption: false, Error: null);
            }

            try
            {
                var json = File.ReadAllText(_filePath, Utf8NoBom);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                    ?? throw new JsonException("Settings document is empty.");

                settings.Normalize();
                return new SettingsLoadResult(settings, CreatedDefault: false, RecoveredFromCorruption: false, Error: null);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                var backup = TryBackupCorruptFile();
                DxLog.Warn($"Settings could not be loaded from '{_filePath}'. Corrupt copy: '{backup ?? "none"}'. {ex.Message}");
                return new SettingsLoadResult(
                    new AppSettings(),
                    CreatedDefault: true,
                    RecoveredFromCorruption: true,
                    Error: ex.Message);
            }
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            settings.Normalize();

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = _filePath + ".tmp";

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
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(_filePath))
                {
                    try
                    {
                        File.Replace(tempPath, _filePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Move(tempPath, _filePath, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tempPath, _filePath);
                }
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }
    }

    private string? TryBackupCorruptFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            var fileName = Path.GetFileNameWithoutExtension(_filePath);
            var extension = Path.GetExtension(_filePath);
            var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(directory ?? string.Empty, $"{fileName}.corrupt-{stamp}{extension}");

            if (File.Exists(backupPath))
            {
                backupPath = Path.Combine(directory ?? string.Empty, $"{fileName}.corrupt-{stamp}-{Guid.NewGuid():N}{extension}");
            }

            File.Move(_filePath, backupPath);
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
