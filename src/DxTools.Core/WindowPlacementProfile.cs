using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed class WindowPlacementProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public List<WindowPlacementEntry> Windows { get; set; } = [];

    [JsonIgnore]
    public string Summary => Windows.Count == 0
        ? "No windows"
        : $"{Windows.Count} window(s)";
}

public sealed class WindowPlacementEntry
{
    public string ExecutablePath { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public bool IsMaximized { get; set; }

    public string MonitorDeviceName { get; set; } = string.Empty;
}
