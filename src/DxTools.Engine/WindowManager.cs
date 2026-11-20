using DxTools.Core;
using DxTools.Interop;

namespace DxTools.Engine;

public sealed class WindowManager
{
    private readonly DisplayService _display;

    public WindowManager(DisplayService display)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
    }

    public bool MoveForegroundWindow(int direction) => MoveWindow(Win32Window.GetForegroundWindow(), direction);

    public bool MoveWindow(IntPtr window, int direction)
    {
        if (window == IntPtr.Zero
            || window == Win32Window.GetShellWindow()
            || !Win32Window.IsWindow(window))
        {
            return false;
        }

        _ = Win32Window.GetWindowThreadProcessId(window, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        var monitors = _display.GetMonitors();
        if (monitors.Count < 2)
        {
            return false;
        }

        if (!Win32Window.GetWindowRect(window, out var rect))
        {
            return false;
        }

        var sourceIndex = FindMonitorIndex(monitors, rect.CenterX, rect.CenterY);
        if (sourceIndex < 0)
        {
            return false;
        }

        var targetIndex = (sourceIndex + Math.Sign(direction) + monitors.Count) % monitors.Count;
        if (targetIndex == sourceIndex)
        {
            return false;
        }

        var source = monitors[sourceIndex];
        var target = monitors[targetIndex];

        var wasMaximized = Win32Window.IsZoomed(window);
        if (wasMaximized)
        {
            _ = Win32Window.ShowWindow(window, Win32Window.SwRestore);
            if (!Win32Window.GetWindowRect(window, out rect))
            {
                return false;
            }
        }

        var dpiScale = source.Dpi > 0 && target.Dpi > 0
            ? (double)target.Dpi / source.Dpi
            : 1.0;

        var width = Math.Max(200, (int)Math.Round(rect.Width * dpiScale));
        var height = Math.Max(120, (int)Math.Round(rect.Height * dpiScale));

        var relativeX = source.Width > 0 ? (rect.CenterX - source.X) / source.Width : 0.5;
        var relativeY = source.Height > 0 ? (rect.CenterY - source.Y) / source.Height : 0.5;

        var centerX = target.X + (relativeX * target.Width);
        var centerY = target.Y + (relativeY * target.Height);

        var x = (int)Math.Round(centerX - (width / 2.0));
        var y = (int)Math.Round(centerY - (height / 2.0));

        x = Math.Clamp(x, target.X, Math.Max(target.X, target.Right - width));
        y = Math.Clamp(y, target.Y, Math.Max(target.Y, target.Bottom - height));

        var moved = Win32Window.SetWindowPos(
            window,
            IntPtr.Zero,
            x,
            y,
            width,
            height,
            Win32Window.SwpNoZOrder | Win32Window.SwpNoActivate);

        if (moved && wasMaximized)
        {
            _ = Win32Window.ShowWindow(window, Win32Window.SwMaximize);
        }

        if (moved)
        {
            DxLog.Info(
                $"Moved window to monitor {target.DisplayName}: " +
                $"source={source.GeometryText}, target={target.GeometryText}, rect={x},{y} {width}x{height}.");
        }

        return moved;
    }

    public bool SnapForegroundWindowToSplit(int slot, int rows, int columns)
    {
        var window = Win32Window.GetForegroundWindow();
        if (window == IntPtr.Zero
            || window == Win32Window.GetShellWindow()
            || !Win32Window.IsWindow(window))
        {
            return false;
        }

        _ = Win32Window.GetWindowThreadProcessId(window, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        var monitors = _display.GetMonitors();
        if (monitors.Count == 0 || !Win32Window.GetWindowRect(window, out var currentRect))
        {
            return false;
        }

        var monitorIndex = FindMonitorIndex(monitors, currentRect.CenterX, currentRect.CenterY);
        if (monitorIndex < 0)
        {
            return false;
        }

        rows = Math.Clamp(rows, 1, 3);
        columns = Math.Clamp(columns, 1, 3);
        if (slot < 1 || slot > rows * columns)
        {
            return false;
        }

        var monitor = monitors[monitorIndex];
        var cellWidth = monitor.Width / columns;
        var cellHeight = monitor.Height / rows;
        var index = slot - 1;
        var row = index / columns;
        var column = index % columns;
        var x = monitor.X + (column * cellWidth);
        var y = monitor.Y + (row * cellHeight);
        var width = column == columns - 1 ? monitor.Right - x : cellWidth;
        var height = row == rows - 1 ? monitor.Bottom - y : cellHeight;

        if (Win32Window.IsZoomed(window))
        {
            _ = Win32Window.ShowWindow(window, Win32Window.SwRestore);
        }

        var moved = Win32Window.SetWindowPos(
            window,
            IntPtr.Zero,
            x,
            y,
            Math.Max(150, width),
            Math.Max(100, height),
            Win32Window.SwpNoZOrder | Win32Window.SwpNoActivate);

        if (moved)
        {
            DxLog.Info($"Window snapped to split {slot}/{rows}x{columns} on {monitor.DisplayName}.");
        }

        return moved;
    }
    public bool ToggleTopmost() => ToggleTopmost(Win32Window.GetForegroundWindow());

    public bool ToggleTopmost(IntPtr window)
    {
        if (window == IntPtr.Zero
            || window == Win32Window.GetShellWindow()
            || !Win32Window.IsWindow(window))
        {
            return false;
        }

        _ = Win32Window.GetWindowThreadProcessId(window, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        var style = Win32Window.GetWindowLongPtr(window, Win32Window.GwlExStyle).ToInt64();
        var isTopmost = (style & Win32Window.WsExTopmost) != 0;
        var target = isTopmost ? Win32Window.HwndNotTopmost : Win32Window.HwndTopmost;

        var changed = Win32Window.SetWindowPos(
            window,
            target,
            0,
            0,
            0,
            0,
            Win32Window.SwpNoMove | Win32Window.SwpNoSize | Win32Window.SwpNoActivate);

        if (changed)
        {
            DxLog.Info($"Topmost toggled to {!isTopmost}.");
        }

        return changed;
    }

    private static int FindMonitorIndex(IReadOnlyList<MonitorInfo> monitors, double centerX, double centerY)
    {
        for (var i = 0; i < monitors.Count; i++)
        {
            var monitor = monitors[i];
            if (centerX >= monitor.X
                && centerX < monitor.Right
                && centerY >= monitor.Y
                && centerY < monitor.Bottom)
            {
                return i;
            }
        }

        var nearest = -1;
        var nearestDistance = double.MaxValue;

        for (var i = 0; i < monitors.Count; i++)
        {
            var monitor = monitors[i];
            var dx = centerX - monitor.CenterX;
            var dy = centerY - monitor.CenterY;
            var distance = (dx * dx) + (dy * dy);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }
}
