using System.Windows;
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
    private readonly WindowManager _windowManager;
    private readonly Action _showSettings;
    private readonly bool _safeMode;
    private readonly WindowCatalog _catalog;
    private readonly TrayMirrorService _trayMirror = new();
    private readonly List<TaskbarWindow> _windows = [];
    private readonly List<IntPtr> _hiddenShellTaskbars = [];
    private bool _disposed;

    public TaskbarManager(
        DisplayService display,
        AppSettings settings,
        TaskbarIconCache icons,
        WindowManager windowManager,
        Action showSettings,
        bool safeMode,
        UIAccessClient uiAccess)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        _showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
        _safeMode = safeMode;
        _catalog = new WindowCatalog(display, uiAccess);
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

                var rows = Math.Clamp(_settings.SplitRows, 1, 3);
                var columns = Math.Clamp(_settings.SplitColumns, 1, 3);

                if (_settings.EnableSplitTaskbars && rows * columns > 1)
                {
                    var cellWidth = monitor.Width / columns;
                    var cellHeight = monitor.Height / rows;
                    var height = Math.Max(36, (int)Math.Round(48 * monitor.Dpi / 96.0));

                    for (var row = 0; row < rows; row++)
                    {
                        for (var column = 0; column < columns; column++)
                        {
                            var left = monitor.X + (column * cellWidth);
                            var top = monitor.Y + (row * cellHeight);
                            var width = column == columns - 1 ? monitor.Right - left : cellWidth;
                            var cellHeightActual = row == rows - 1 ? monitor.Bottom - top : cellHeight;
                            var bounds = new WindowRect
                            {
                                Left = left,
                                Top = top + cellHeightActual - height,
                                Right = left + width,
                                Bottom = top + cellHeightActual,
                            };

                            var splitViewModel = new TaskbarViewModel(
                                _catalog,
                                monitors,
                                index,
                                _icons,
                                _showSettings,
                                _settings.PinnedApps,
                                _windowManager,
                                contextMenusEnabled: _settings.EnableTaskbarContextMenu,
                                autoHide: false,
                                trayMirror: _trayMirror,
                                windowsChanged: RefreshAll,
                                filterBounds: new Rect(left, top, width, cellHeightActual));

                            var splitWindow = new TaskbarWindow(splitViewModel);
                            splitWindow.Show();
                            splitWindow.Place(monitor, false, bounds);
                            _windows.Add(splitWindow);
                        }
                    }

                    continue;
                }

                var viewModel = new TaskbarViewModel(
                    _catalog,
                    monitors,
                    index,
                    _icons,
                    _showSettings,
                    _settings.PinnedApps,
                    _windowManager,
                    contextMenusEnabled: _settings.EnableTaskbarContextMenu,
                    autoHide: _settings.TaskbarAutoHide,
                    trayMirror: _trayMirror,
                    windowsChanged: RefreshAll);
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

    public void RefreshAll()
    {
        foreach (var window in _windows)
        {
            try
            {
                window.Refresh();
            }
            catch (Exception ex)
            {
                DxLog.Exception("Failed to refresh a taskbar window.", ex);
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
