using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed class DisplayProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public string TopologyHash { get; set; } = string.Empty;

    public string StateBase64 { get; set; } = string.Empty;

    public List<MonitorInfo> Monitors { get; set; } = [];

    public bool AutoApply { get; set; }

    [JsonIgnore]
    public string MonitorSummary => Monitors.Count == 0
        ? "No monitor data"
        : string.Join(" · ", Monitors.Select(monitor => $"{monitor.DisplayName} {monitor.Width}×{monitor.Height}"));

    [JsonIgnore]
    public bool HasState => !string.IsNullOrWhiteSpace(StateBase64);
}
