using DxTools.Core;
using DxTools.Interop;

namespace DxTools.Engine;

public sealed record WindowPlacementResult(bool Success, int Applied, int Total, string? Error);

public sealed record WindowPlacementOperationResult(bool Success, string? Error, WindowPlacementProfile? Profile);

public sealed class WindowPlacementService
{
    private readonly object _gate = new();
    private readonly WindowCatalog _catalog;
    private readonly DisplayService _display;
    private readonly WindowPlacementStore _store;
    private readonly List<WindowPlacementProfile> _profiles;

    public WindowPlacementService(DisplayService display, WindowPlacementStore? store = null)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _catalog = new WindowCatalog(display);
        _store = store ?? new WindowPlacementStore();
        _profiles = _store.Load().ToList();
    }

    public IReadOnlyList<WindowPlacementProfile> Profiles => _profiles;

    public IReadOnlyList<AppWindow> GetCurrentWindows() => _catalog.GetWindows();

    public WindowPlacementOperationResult Capture(string? name)
    {
        lock (_gate)
        {
            var windows = _catalog.GetWindows();
            if (windows.Count == 0)
            {
                return new WindowPlacementOperationResult(false, "No windows were detected.", null);
            }

            var monitors = _display.GetMonitors();
            var profile = new WindowPlacementProfile
            {
                Name = string.IsNullOrWhiteSpace(name) ? $"Windows {_profiles.Count + 1}" : name.Trim(),
                CreatedAt = DateTimeOffset.Now,
                Windows = windows.Select(window => new WindowPlacementEntry
                {
                    ExecutablePath = window.ExecutablePath,
                    Title = window.Title,
                    X = window.Bounds.Left,
                    Y = window.Bounds.Top,
                    Width = window.Bounds.Width,
                    Height = window.Bounds.Height,
                    IsMaximized = window.IsMaximized,
                    MonitorDeviceName = window.MonitorIndex >= 0 && window.MonitorIndex < monitors.Count
                        ? monitors[window.MonitorIndex].DeviceName
                        : string.Empty,
                }).ToList(),
            };

            _profiles.Add(profile);
            var saved = TrySave();

            return saved.Success
                ? new WindowPlacementOperationResult(true, null, profile)
                : new WindowPlacementOperationResult(false, saved.Error, profile);
        }
    }

    public WindowPlacementOperationResult Delete(string profileId)
    {
        lock (_gate)
        {
            var profile = _profiles.FirstOrDefault(item => item.Id == profileId);
            if (profile is null)
            {
                return new WindowPlacementOperationResult(false, "Profile not found.", null);
            }

            _profiles.Remove(profile);
            var saved = TrySave();
            return saved.Success
                ? new WindowPlacementOperationResult(true, null, profile)
                : new WindowPlacementOperationResult(false, saved.Error, profile);
        }
    }

    public WindowPlacementResult Apply(WindowPlacementProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var windows = _catalog.GetWindows();
        if (windows.Count == 0)
        {
            return new WindowPlacementResult(false, 0, profile.Windows.Count, "No windows were detected.");
        }

        var used = new HashSet<IntPtr>();
        var applied = 0;

        foreach (var entry in profile.Windows)
        {
            var window = FindWindow(windows, entry, used);
            if (window is null)
            {
                continue;
            }

            used.Add(window.Handle);

            if (window.IsMinimized || entry.IsMaximized)
            {
                _ = Win32Window.ShowWindow(window.Handle, Win32Taskbar.SwRestore);
            }

            var moved = Win32Window.SetWindowPos(
                window.Handle,
                IntPtr.Zero,
                entry.X,
                entry.Y,
                Math.Max(100, entry.Width),
                Math.Max(80, entry.Height),
                Win32Window.SwpNoZOrder | Win32Window.SwpNoActivate);

            if (!moved)
            {
                continue;
            }

            if (entry.IsMaximized)
            {
                _ = Win32Window.ShowWindow(window.Handle, Win32Window.SwMaximize);
            }

            applied++;
        }

        return new WindowPlacementResult(true, applied, profile.Windows.Count, null);
    }

    private static AppWindow? FindWindow(
        IReadOnlyList<AppWindow> windows,
        WindowPlacementEntry entry,
        HashSet<IntPtr> used)
    {
        return windows.FirstOrDefault(window =>
                !used.Contains(window.Handle)
                && string.Equals(window.ExecutablePath, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(window.Title, entry.Title, StringComparison.Ordinal))
            ?? windows.FirstOrDefault(window =>
                !used.Contains(window.Handle)
                && string.Equals(window.ExecutablePath, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase))
            ?? windows.FirstOrDefault(window =>
                !used.Contains(window.Handle)
                && string.Equals(window.Title, entry.Title, StringComparison.Ordinal));
    }

    private (bool Success, string? Error) TrySave()
    {
        try
        {
            _store.Save(_profiles);
            return (true, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            DxLog.Exception("Failed to save window profiles.", ex);
            return (false, ex.Message);
        }
    }
}
