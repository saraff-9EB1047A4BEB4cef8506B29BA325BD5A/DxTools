using System.Text.Json;
using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed class TriggerStore
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

    public TriggerStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(AppPaths.AppDataDirectory, "triggers.json")
            : filePath;
    }

    public string FilePath => _filePath;

    public IReadOnlyList<TriggerDefinition> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<TriggerDefinition>();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var triggers = JsonSerializer.Deserialize<List<TriggerDefinition>>(json, JsonOptions);
                return triggers ?? new List<TriggerDefinition>();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                var backup = AtomicFile.TryBackupCorrupt(_filePath);
                DxLog.Warn(
                    $"Triggers could not be loaded from '{_filePath}'. " +
                    $"Corrupt copy: '{backup ?? "none"}'. {ex.Message}");
                return Array.Empty<TriggerDefinition>();
            }
        }
    }

    public void Save(IEnumerable<TriggerDefinition> triggers)
    {
        ArgumentNullException.ThrowIfNull(triggers);

        lock (_gate)
        {
            var json = JsonSerializer.Serialize(triggers.ToList(), JsonOptions);
            AtomicFile.WriteAllText(_filePath, json);
        }
    }
}
