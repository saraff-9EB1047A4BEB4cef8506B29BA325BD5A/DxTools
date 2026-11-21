using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DxTools.App.Services;
using DxTools.Core;
using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.App.ViewModels;

public sealed record WindowMoveRequest(IntPtr Handle, int TargetMonitorIndex);

public sealed class TaskbarViewModel : ObservableObject, IDisposable
{
    private readonly WindowCatalog _catalog;
    private readonly IReadOnlyList<MonitorInfo> _monitors;
    private readonly int _monitorIndex;
    private readonly TaskbarIconCache _icons;
    private readonly Action _showSettings;
    private readonly IReadOnlyList<PinnedApp> _pinnedApps;
    private readonly WindowManager _windowManager;
    private readonly TrayMirrorService _trayMirror;
    private readonly Action? _windowsChanged;
    private readonly Rect? _filterBounds;
    private bool _isTrayOpen;
    private DateTimeOffset _lastTrayRefresh;
    private readonly DispatcherTimer _timer;
    private string _signature = string.Empty;
    private string _clockText = string.Empty;
    private bool _isFullscreen;
    private bool _disposed;

    public TaskbarViewModel(
        WindowCatalog catalog,
        IReadOnlyList<MonitorInfo> monitors,
        int monitorIndex,
        TaskbarIconCache icons,
        Action showSettings,
        IReadOnlyList<PinnedApp> pinnedApps,
        WindowManager windowManager,
        bool contextMenusEnabled,
        bool autoHide,
        TrayMirrorService trayMirror,
        Action? windowsChanged = null,
        Rect? filterBounds = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
        _monitorIndex = monitorIndex;
        _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        _showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
        _pinnedApps = pinnedApps ?? throw new ArgumentNullException(nameof(pinnedApps));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        ContextMenusEnabled = contextMenusEnabled;
        AutoHide = autoHide;
        _trayMirror = trayMirror ?? throw new ArgumentNullException(nameof(trayMirror));
        _windowsChanged = windowsChanged;
        _filterBounds = filterBounds;

        ActivateCommand = new RelayCommand(parameter =>
        {
            if (parameter is TaskbarGroupItemViewModel { IsPinnedOnly: true, PinnedPath: { } pinnedPath })
            {
                try
                {
                    Process.Start(new ProcessStartInfo(pinnedPath) { UseShellExecute = true });
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    DxLog.Exception("Failed to launch pinned app '{pinnedPath}'.", ex);
                }
            }
            else if (parameter is TaskbarGroupItemViewModel group && group.Windows.Count > 0)
            {
                var windows = group.Windows;
                var activeIndex = -1;

                for (var index = 0; index < windows.Count; index++)
                {
                    if (windows[index].IsForeground)
                    {
                        activeIndex = index;
                        break;
                    }
                }

                var target = activeIndex >= 0
                    ? windows[(activeIndex + 1) % windows.Count]
                    : windows.FirstOrDefault(window => !window.IsMinimized) ?? windows[0];

                _ = _catalog.Activate(target.Handle);
                Refresh();
            }
        });
        OpenSettingsCommand = new RelayCommand(_showSettings);
        ToggleTrayCommand = new RelayCommand(ToggleTray);
        MoveWindowCommand = new RelayCommand(parameter =>
        {
            if (parameter is WindowMoveRequest request)
            {
                MoveWindow(request.Handle, request.TargetMonitorIndex);
            }
        });

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTimerTick;

        Monitor = _monitors[_monitorIndex];
        ClockText = DateTime.Now.ToString("HH:mm");
        Refresh();
    }

    public ObservableCollection<TaskbarGroupItemViewModel> Windows { get; } = [];

    public ICommand ActivateCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand ToggleTrayCommand { get; }

    public ICommand MoveWindowCommand { get; }

    public IReadOnlyList<MonitorInfo> Monitors => _monitors;

    public int MonitorIndex => _monitorIndex;

    public bool ContextMenusEnabled { get; }

    public ObservableCollection<TrayItemViewModel> TrayItems { get; } = [];

    public bool IsTrayOpen
    {
        get => _isTrayOpen;
        set => SetProperty(ref _isTrayOpen, value);
    }

    public MonitorInfo Monitor { get; }

    public bool AutoHide { get; }

    public string MonitorLabel => Monitor.DisplayName;

    public bool IsFullscreen
    {
        get => _isFullscreen;
        private set => SetProperty(ref _isFullscreen, value);
    }

    public string ClockText
    {
        get => _clockText;
        private set => SetProperty(ref _clockText, value);
    }

    public void Start() => _timer.Start();

    public IReadOnlyList<AppWindow> GetAllWindows() => _catalog.GetWindows();

    public bool ActivateWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero || !Win32Window.IsWindow(handle))
        {
            return false;
        }

        var activated = _catalog.Activate(handle, toggleMinimize: false);
        if (activated)
        {
            Refresh();
            _windowsChanged?.Invoke();
        }

        return activated;
    }

    public bool ToggleTopmost(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        var toggled = _windowManager.ToggleTopmost(handle);
        if (toggled)
        {
            Refresh();
        }

        return toggled;
    }

    public bool MoveWindow(IntPtr handle, int targetMonitorIndex)
    {
        if (handle == IntPtr.Zero
            || targetMonitorIndex < 0
            || targetMonitorIndex >= _monitors.Count)
        {
            return false;
        }

        var moved = _windowManager.MoveWindowToMonitor(handle, targetMonitorIndex);
        if (!moved)
        {
            return false;
        }

        Refresh();
        _windowsChanged?.Invoke();
        return true;
    }

    public void Refresh()
    {
        var windows = _catalog.GetWindows()
            .Where(window => window.MonitorIndex == _monitorIndex)
            .Where(window => _filterBounds is null
                || _filterBounds.Value.Contains(new System.Windows.Point(window.Bounds.CenterX, window.Bounds.CenterY)))
            .ToList();

        var groups = windows
            .GroupBy(
                window => string.IsNullOrWhiteSpace(window.ExecutablePath)
                    ? $"pid:{window.ProcessId}"
                    : window.ExecutablePath,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Key = group.Key,
                Windows = group
                    .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
            })
            .OrderBy(group => group.Windows[0].Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var runningKeys = groups
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pinnedGroups = _pinnedApps
            .Where(app => !string.IsNullOrWhiteSpace(app.Path) && File.Exists(app.Path))
            .Where(app => !runningKeys.Contains(app.Path))
            .GroupBy(app => app.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var signature = string.Join(
            "|",
            pinnedGroups.Select(app => $"pinned:{app.Path}").Concat(
                groups.Select(group => $"{group.Key}:{string.Join(",", group.Windows.Select(
                    window => $"{window.Handle}:{window.Title}:{window.IsForeground}:{window.IsMinimized}"))}")));

        if (string.Equals(signature, _signature, StringComparison.Ordinal))
        {
            return;
        }

        var existing = Windows.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        Windows.Clear();

        foreach (var app in pinnedGroups)
        {
            if (existing.TryGetValue(app.Path, out var pinnedItem))
            {
                Windows.Add(pinnedItem);
            }
            else
            {
                var title = string.IsNullOrWhiteSpace(app.Name)
                    ? Path.GetFileNameWithoutExtension(app.Path)
                    : app.Name;

                Windows.Add(new TaskbarGroupItemViewModel(
                    title,
                    app.Path,
                    _icons.GetIcon(app.Path)));
            }
        }

        foreach (var group in groups)
        {
            if (existing.TryGetValue(group.Key, out var item))
            {
                item.Update(group.Windows);
            }
            else
            {
                item = new TaskbarGroupItemViewModel(
                    group.Key,
                    group.Windows,
                    _icons.GetIcon(group.Windows[0].ExecutablePath));
            }

            Windows.Add(item);
        }

        _signature = signature;
    }

    private void ToggleTray()
    {
        if (IsTrayOpen)
        {
            IsTrayOpen = false;
            return;
        }

        RefreshTrayItems();
        IsTrayOpen = true;
    }

    private void RefreshTrayItems()
    {
        TrayItems.Clear();
        foreach (var item in _trayMirror.Capture())
        {
            TrayItems.Add(new TrayItemViewModel(item, ClickTrayItem, OpenTrayContextMenu));
        }

        _lastTrayRefresh = DateTimeOffset.Now;
    }

    private void ClickTrayItem(TrayItem item)
    {
        IsTrayOpen = false;
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            new Action(() => _trayMirror.Click(item)));
    }

    private void OpenTrayContextMenu(TrayItem item)
    {
        IsTrayOpen = false;

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        var timer = new DispatcherTimer(DispatcherPriority.Input, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(80),
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _trayMirror.OpenContextMenu(item);
        };

        timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        ClockText = DateTime.Now.ToString("HH:mm");
        Refresh();

        if (IsTrayOpen && DateTimeOffset.Now - _lastTrayRefresh > TimeSpan.FromSeconds(2))
        {
            RefreshTrayItems();
        }
        UpdateFullscreenState();
    }

    private void UpdateFullscreenState()
    {
        var foreground = Win32Window.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            IsFullscreen = false;
            return;
        }

        _ = Win32Window.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == Environment.ProcessId)
        {
            IsFullscreen = false;
            return;
        }

        if (!Win32Window.GetWindowRect(foreground, out var rect))
        {
            IsFullscreen = false;
            return;
        }

        var centerX = rect.CenterX;
        var centerY = rect.CenterY;
        if (centerX < Monitor.X || centerX >= Monitor.Right || centerY < Monitor.Y || centerY >= Monitor.Bottom)
        {
            IsFullscreen = false;
            return;
        }

        IsFullscreen = rect.Left <= Monitor.X + 1
            && rect.Top <= Monitor.Y + 1
            && rect.Right >= Monitor.Right - 1
            && rect.Bottom >= Monitor.Bottom - 1;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
