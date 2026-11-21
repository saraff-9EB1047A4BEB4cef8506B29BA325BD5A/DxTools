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

    public EngineHost(bool safeMode, AppSettings settings)
    {
        _safeMode = safeMode;
        Status = EngineStatus.Unknown with { SafeMode = safeMode };

        Display = new DisplayService(safeMode);
        Profiles = new DisplayProfileService(Display);
        UIAccess = new UIAccessClient(enabled: !safeMode);
        Windows = new WindowManager(Display, UIAccess);
        Wallpaper = new WallpaperService(Display);
        WindowPlacements = new WindowPlacementService(Display, uiAccess: UIAccess);
        DesktopIcons = new DesktopIconManager();
        Audio = new AudioService(safeMode);
        VirtualDesktops = new VirtualDesktopService();
        Triggers = new TriggerService(Display, Profiles, WindowPlacements, Wallpaper, settings);
    }

    public EngineStatus Status { get; private set; }

    public DisplayService Display { get; }

    public DisplayProfileService Profiles { get; }

    public UIAccessClient UIAccess { get; }

    public WindowManager Windows { get; }

    public WallpaperService Wallpaper { get; }

    public WindowPlacementService WindowPlacements { get; }

    public DesktopIconManager DesktopIcons { get; }

    public AudioService Audio { get; }

    public VirtualDesktopService VirtualDesktops { get; }

    public TriggerService Triggers { get; }

    public event EventHandler<EngineStatus>? StatusChanged;

    public void Start()
    {
        DxLog.Info($"Engine starting. SafeMode={_safeMode}.");
        _ = Task.Run(UIAccess.Warmup);
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
        UIAccess.Dispose();
        DxLog.Info("Engine stopped.");
    }
}
