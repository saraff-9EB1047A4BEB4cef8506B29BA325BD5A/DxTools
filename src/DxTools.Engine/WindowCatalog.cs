using System.Text;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.Engine;

public sealed record AppWindow(
    IntPtr Handle,
    string Title,
    uint ProcessId,
    string ExecutablePath,
    int MonitorIndex,
    WindowRect Bounds,
    bool IsMinimized,
    bool IsMaximized,
    bool IsForeground);

public sealed class WindowCatalog
{
    private readonly DisplayService _display;
    private readonly UIAccessClient? _uiAccess;

    public WindowCatalog(DisplayService display, UIAccessClient? uiAccess = null)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _uiAccess = uiAccess;
    }

    public IReadOnlyList<AppWindow> GetWindows()
    {
        var monitors = _display.GetMonitors();
        if (monitors.Count == 0)
        {
            return Array.Empty<AppWindow>();
        }

        var foreground = Win32Window.GetForegroundWindow();
        var ownProcess = (uint)Environment.ProcessId;
        var windows = new List<AppWindow>();

        _ = Win32Taskbar.EnumWindows((hwnd, unused) =>
        {
            if (hwnd == Win32Window.GetShellWindow()
                || !Win32Taskbar.IsWindowVisible(hwnd)
                || Win32Taskbar.IsCloaked(hwnd))
            {
                return true;
            }

            var exStyle = Win32Window.GetWindowLongPtr(hwnd, Win32Taskbar.GwlExStyle).ToInt64();
            if ((exStyle & Win32Taskbar.WsExToolWindow) != 0
                || (exStyle & Win32Taskbar.WsExNoActivate) != 0
                || Win32Taskbar.GetWindow(hwnd, Win32Taskbar.GwOwner) != IntPtr.Zero)
            {
                return true;
            }

            var titleLength = Win32Taskbar.GetWindowTextLengthW(hwnd);
            if (titleLength <= 0)
            {
                return true;
            }

            var titleBuffer = new StringBuilder(titleLength + 1);
            var title = Win32Taskbar.GetWindowTextW(hwnd, titleBuffer, titleBuffer.Capacity) > 0
                ? titleBuffer.ToString()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            _ = Win32Window.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == ownProcess)
            {
                return true;
            }

            var minimized = Win32Taskbar.IsIconic(hwnd);
            var maximized = Win32Window.IsZoomed(hwnd);
            WindowRect rect;

            if (minimized)
            {
                var placement = new WindowPlacement
                {
                    Length = System.Runtime.InteropServices.Marshal.SizeOf<WindowPlacement>(),
                };

                if (!Win32Taskbar.GetWindowPlacement(hwnd, ref placement))
                {
                    return true;
                }

                rect = placement.NormalPosition;
            }
            else if (!Win32Window.GetWindowRect(hwnd, out rect))
            {
                return true;
            }

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return true;
            }

            var monitorIndex = FindMonitorIndex(monitors, rect.CenterX, rect.CenterY);
            if (monitorIndex < 0)
            {
                return true;
            }

            windows.Add(new AppWindow(
                hwnd,
                title.Trim(),
                processId,
                Win32Taskbar.GetProcessPath(processId),
                monitorIndex,
                rect,
                minimized,
                maximized,
                hwnd == foreground));

            return true;
        }, IntPtr.Zero);

        return windows
            .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public bool Activate(IntPtr hwnd)
    {
        if (!Win32Window.IsWindow(hwnd))
        {
            return false;
        }

        var foreground = Win32Window.GetForegroundWindow();
        if (foreground == hwnd)
        {
            _ = Win32Window.ShowWindow(hwnd, Win32Taskbar.SwMinimize);
            return true;
        }

        if (Win32Taskbar.IsIconic(hwnd))
        {
            _ = Win32Window.ShowWindow(hwnd, Win32Taskbar.SwRestore);
        }

        if (Win32Taskbar.SetForegroundWindow(hwnd))
        {
            return true;
        }

        var foregroundThread = Win32Window.GetWindowThreadProcessId(foreground, out _);
        var targetThread = Win32Window.GetWindowThreadProcessId(hwnd, out _);

        if (foregroundThread != 0 && targetThread != 0 && foregroundThread != targetThread)
        {
            var attached = false;
            try
            {
                attached = Win32Taskbar.AttachThreadInput(foregroundThread, targetThread, true);
                if (Win32Taskbar.SetForegroundWindow(hwnd))
                {
                    return true;
                }
            }
            finally
            {
                if (attached)
                {
                    _ = Win32Taskbar.AttachThreadInput(foregroundThread, targetThread, false);
                }
            }
        }

        return _uiAccess?.TryActivate(hwnd) ?? false;
    }

    private static int FindMonitorIndex(IReadOnlyList<MonitorInfo> monitors, double x, double y)
    {
        for (var i = 0; i < monitors.Count; i++)
        {
            var monitor = monitors[i];
            if (x >= monitor.X && x < monitor.Right && y >= monitor.Y && y < monitor.Bottom)
            {
                return i;
            }
        }

        return -1;
    }
}
