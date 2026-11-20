using System.Text.Json.Serialization;

namespace DxTools.Core;

public sealed record MonitorInfo(
    string DeviceName,
    string MonitorId,
    string FriendlyName,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary,
    uint Dpi,
    uint RefreshRate,
    uint BitsPerPixel)
{
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(FriendlyName) ? DeviceName : FriendlyName;

    [JsonIgnore]
    public int Right => X + Width;

    [JsonIgnore]
    public int Bottom => Y + Height;

    [JsonIgnore]
    public double CenterX => X + (Width / 2.0);

    [JsonIgnore]
    public double CenterY => Y + (Height / 2.0);

    [JsonIgnore]
    public string GeometryText => $"{Width}×{Height} @ {X},{Y}";

    [JsonIgnore]
    public string DpiText => $"{Dpi} DPI";

    [JsonIgnore]
    public string RefreshText => RefreshRate == 0 ? "—" : $"{RefreshRate} Hz";
}
