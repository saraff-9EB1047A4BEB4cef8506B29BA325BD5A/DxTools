using DxTools.Core;
using DxTools.App.ViewModels;
using DxTools.Interop;
using DxTools.Engine;

namespace DxTools.App.Services;

public sealed class TaskbarManager : IDisposable
{
    private readonly DisplayService _display;
    private readonly AppSettings _settings;
    private readonly TaskbarIconCache _icons;
    private readonly Action _showSettings;
    private readonly bool _safeMode;
    private readonly WindowCatalog _catalog;
    private readonly List<TaskbarWindow> _windows = [];
    private readonly List<IntPtr> _hiddenShellTaskbars = [];
    private bool _disposed;

    public TaskbarManager(
        DisplayService display,
        AppSettings settings,
        TaskbarIconCache icons,
        Action showSettings,
        bool safeMode)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        _showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
        _safeMode = safeMode;
        _catalog = new WindowCatalog(display);
    }

    public void Rebuild()
    {
        if (_disposed)
        {
            return;
        }

        CloseWindows();
        RestoreShellTaskbars();

        if (_safeMode || !_settings.EnableSecondaryTaskbar)
        {
            return;
        }

        var monitors = _display.GetMonitors();
        if (monitors.Count <= 1)
        {
            return;
        }

        var shellTaskbars = ShellTaskbars.FindSecondary();
        var usedShellTaskbars = new HashSet<IntPtr>();

        for (var index = 0; index < monitors.Count; index++)
        {
            if (monitors[index].IsPrimary)
            {
                continue;
            }

            var monitor = monitors[index];
            var shellTaskbar = shellTaskbars.FirstOrDefault(
                candidate => !usedShellTaskbars.Contains(candidate.Handle)
                    && ContainsMonitor(candidate.Rect, monitor));

            try
            {
                var useAppBar = shellTaskbar is null;

                if (shellTaskbar is not null)
                {
                    ShellTaskbars.SetVisible(shellTaskbar.Handle, visible: false);
                    _hiddenShellTaskbars.Add(shellTaskbar.Handle);
                    usedShellTaskbars.Add(shellTaskbar.Handle);
                }

                var viewModel = new TaskbarViewModel(_catalog, monitors, index, _icons, _showSettings);
                var window = new TaskbarWindow(viewModel);
                window.Show();
                window.Place(monitor, useAppBar);
                _windows.Add(window);
            }
            catch (Exception ex)
            {
                DxLog.Exception($"Failed to create taskbar for monitor {monitor.DisplayName}.", ex);
            }
        }
    }

    private static bool ContainsMonitor(WindowRect rect, MonitorInfo monitor)
    {
        var x = rect.Left + (rect.Width / 2.0);
        var y = rect.Top + (rect.Height / 2.0);
        return x >= monitor.X && x < monitor.Right && y >= monitor.Y && y < monitor.Bottom;
    }

    private void RestoreShellTaskbars()
    {
        foreach (var handle in _hiddenShellTaskbars)
        {
            ShellTaskbars.SetVisible(handle, visible: true);
        }

        _hiddenShellTaskbars.Clear();
    }

    private void CloseWindows()
    {
        foreach (var window in _windows)
        {
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                DxLog.Exception("Failed to close a taskbar window.", ex);
            }
        }

        _windows.Clear();
    }

    public void EmergencyRestore()
    {
        RestoreShellTaskbars();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CloseWindows();
        RestoreShellTaskbars();
    }
}
