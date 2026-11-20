using DxTools.Core;
using DxTools.Interop;

namespace DxTools.Engine;

public sealed record EngineStatus(NativeProbeResult Native, bool SafeMode, DateTimeOffset CheckedAt)
{
    public bool IsHealthy => !SafeMode && Native.IsHealthy;

    public static EngineStatus Unknown { get; } = new(NativeProbeResult.NotChecked, SafeMode: false, CheckedAt: DateTimeOffset.MinValue);
}

public sealed class EngineHost
{
    private readonly bool _safeMode;

    public EngineHost(bool safeMode)
    {
        _safeMode = safeMode;
        Status = EngineStatus.Unknown with { SafeMode = safeMode };

        Display = new DisplayService(safeMode);
        Profiles = new DisplayProfileService(Display);
        Windows = new WindowManager(Display);
        Wallpaper = new WallpaperService(Display);
        WindowPlacements = new WindowPlacementService(Display);
    }

    public EngineStatus Status { get; private set; }

    public DisplayService Display { get; }

    public DisplayProfileService Profiles { get; }

    public WindowManager Windows { get; }

    public WallpaperService Wallpaper { get; }

    public WindowPlacementService WindowPlacements { get; }

    public event EventHandler<EngineStatus>? StatusChanged;

    public void Start()
    {
        DxLog.Info($"Engine starting. SafeMode={_safeMode}.");
        Refresh();
    }

    public void Refresh()
    {
        var native = _safeMode
            ? NativeProbeResult.Skipped("Safe mode: native probe disabled.")
            : NativeApi.Probe();

        Status = new EngineStatus(native, _safeMode, DateTimeOffset.Now);

        if (native.IsAvailable)
        {
            DxLog.Info($"Native runtime: api={native.ApiVersion}, version={native.LibraryVersion}, ping={native.PingOk}, path='{native.LibraryPath}'.");
        }
        else
        {
            DxLog.Warn($"Native runtime unavailable: {native.Error}");
        }

        StatusChanged?.Invoke(this, Status);
    }

    public void Stop()
    {
        DxLog.Info("Engine stopped.");
    }
}
