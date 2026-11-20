using System.Text.Json;
using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed class DesktopIconProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly object _gate = new();
    private readonly string _filePath;

    public DesktopIconProfileStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(AppPaths.AppDataDirectory, "desktop-icons.json")
            : filePath;
    }

    public string FilePath => _filePath;

    public IReadOnlyList<DesktopIconProfile> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<DesktopIconProfile>();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var profiles = JsonSerializer.Deserialize<List<DesktopIconProfile>>(json, JsonOptions);
                return profiles ?? new List<DesktopIconProfile>();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                var backup = AtomicFile.TryBackupCorrupt(_filePath);
                DxLog.Warn(
                    $"Desktop icon profiles could not be loaded from '{_filePath}'. " +
                    $"Corrupt copy: '{backup ?? "none"}'. {ex.Message}");
                return Array.Empty<DesktopIconProfile>();
            }
        }
    }

    public void Save(IEnumerable<DesktopIconProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        lock (_gate)
        {
            var json = JsonSerializer.Serialize(profiles.ToList(), JsonOptions);
            AtomicFile.WriteAllText(_filePath, json);
        }
    }
}
