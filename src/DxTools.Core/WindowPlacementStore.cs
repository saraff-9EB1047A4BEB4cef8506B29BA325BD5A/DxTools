using System.Text.Json;
using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed class WindowPlacementStore
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

    public WindowPlacementStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(AppPaths.AppDataDirectory, "window-profiles.json")
            : filePath;
    }

    public string FilePath => _filePath;

    public IReadOnlyList<WindowPlacementProfile> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<WindowPlacementProfile>();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var profiles = JsonSerializer.Deserialize<List<WindowPlacementProfile>>(json, JsonOptions);
                return profiles ?? new List<WindowPlacementProfile>();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                var backup = AtomicFile.TryBackupCorrupt(_filePath);
                DxLog.Warn(
                    $"Window profiles could not be loaded from '{_filePath}'. " +
                    $"Corrupt copy: '{backup ?? "none"}'. {ex.Message}");
                return Array.Empty<WindowPlacementProfile>();
            }
        }
    }

    public void Save(IEnumerable<WindowPlacementProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        lock (_gate)
        {
            var json = JsonSerializer.Serialize(profiles.ToList(), JsonOptions);
            AtomicFile.WriteAllText(_filePath, json);
        }
    }
}
