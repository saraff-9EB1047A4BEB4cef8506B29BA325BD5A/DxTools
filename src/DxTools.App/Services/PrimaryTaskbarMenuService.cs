using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;
using DxTools.Interop;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace DxTools.App.Services;

public sealed class PrimaryTaskbarMenuService : IDisposable
{
    public const int HookMessage = 0x8000 + 78;

    private const long WsExTransparent = 0x00000020;

    private readonly IntPtr _windowHandle;
    private readonly Window _owner;
    private readonly AppSettings _settings;
    private readonly DisplayService _display;
    private readonly WindowManager _windowManager;
    private readonly WindowCatalog _catalog;
    private readonly Action _showSettings;
    private readonly Action _windowsChanged;
    private readonly HwndSource? _source;
    private readonly DispatcherTimer _rectTimer;
    private Window? _popupHost;
    private ContextMenu? _activeMenu;
    private bool _started;
    private bool _disposed;

    public PrimaryTaskbarMenuService(
        IntPtr windowHandle,
        Window owner,
        EngineHost engine,
        AppSettings settings,
        Action showSettings,
        Action windowsChanged)
    {
        _windowHandle = windowHandle;
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _display = engine?.Display ?? throw new ArgumentNullException(nameof(engine));
        _windowManager = engine.Windows;
        _catalog = new WindowCatalog(engine.Display, engine.UIAccess);
        _showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
        _windowsChanged = windowsChanged ?? throw new ArgumentNullException(nameof(windowsChanged));
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(WindowProcedure);
        _rectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _rectTimer.Tick += (_, _) => RefreshTaskbarRect();
    }

    public void ApplySettings()
    {
        if (_disposed)
        {
            return;
        }

        if (!_settings.EnablePrimaryTaskbarMenu)
        {
            Stop();
            return;
        }

        if (!_started && !Start())
        {
            return;
        }

        RefreshTaskbarRect();
    }

    public void RefreshTaskbarRect()
    {
        if (!_started)
        {
            return;
        }

        var taskbar = ShellTaskbars.FindPrimary();
        if (taskbar is null)
        {
            NativeMouseHook.SetEnabled(false);
            DxLog.Warn("Primary taskbar was not found; primary taskbar menu is paused.");
            return;
        }

        NativeMouseHook.SetTaskbarRect(taskbar.Rect);
        NativeMouseHook.SetEnabled(true);
    }

    private bool Start()
    {
        if (_source is null)
        {
            DxLog.Warn("Primary taskbar menu could not attach to the application window.");
            return false;
        }

        var taskbar = ShellTaskbars.FindPrimary();
        if (taskbar is null)
        {
            DxLog.Warn("Primary taskbar was not found; primary taskbar menu is unavailable.");
            return false;
        }

        if (!NativeMouseHook.TryStart(_windowHandle, HookMessage, out var error))
        {
            DxLog.Warn($"Primary taskbar mouse hook could not be started: {error}");
            return false;
        }

        NativeMouseHook.SetTaskbarRect(taskbar.Rect);
        NativeMouseHook.SetEnabled(true);
        _started = true;
        _rectTimer.Start();
        DxLog.Info("Primary taskbar menu started (Ctrl+right-click).");
        return true;
    }

    private void Stop()
    {
        if (!_started)
        {
            return;
        }

        _rectTimer.Stop();
        NativeMouseHook.Stop();
        _started = false;
        DxLog.Info("Primary taskbar menu stopped.");
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != HookMessage)
        {
            return IntPtr.Zero;
        }

        handled = true;
        var x = (int)wParam.ToInt64();
        var y = (int)lParam.ToInt64();
        _ = _owner.Dispatcher.BeginInvoke(new Action(() => ShowMenu(x, y)));
        return IntPtr.Zero;
    }

    private void ShowMenu(int x, int y)
    {
        if (_disposed || !_settings.EnablePrimaryTaskbarMenu)
        {
            return;
        }

        var button = FindTaskbarButton(x, y);
        if (button is null)
        {
            DxLog.Warn("Primary taskbar menu could not identify the taskbar button under the cursor.");
            return;
        }

        string name;
        string automationId;
        try
        {
            name = button.Current.Name ?? string.Empty;
            automationId = button.Current.AutomationId ?? string.Empty;
        }
        catch (Exception ex) when (IsAutomationError(ex))
        {
            DxLog.Warn($"Primary taskbar button could not be read: {ex.Message}");
            return;
        }

        var windows = WindowMatcher.Match(_catalog.GetWindows(), name, automationId);
        var menu = BuildMenu(name, windows, _display.GetMonitors());
        var host = EnsurePopupHost();
        var local = host.PointFromScreen(new System.Windows.Point(x, y));
        menu.PlacementTarget = host;
        menu.Placement = PlacementMode.RelativePoint;
        menu.HorizontalOffset = local.X;
        menu.VerticalOffset = local.Y;

        if (_activeMenu is { IsOpen: true })
        {
            _activeMenu.IsOpen = false;
        }

        _activeMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_activeMenu, menu))
            {
                _activeMenu = null;
            }
        };
        menu.IsOpen = true;
    }

    private Window EnsurePopupHost()
    {
        if (_popupHost is not null)
        {
            return _popupHost;
        }

        var host = new Window
        {
            Width = 1,
            Height = 1,
            Left = -10000,
            Top = -10000,
            Title = "DxTools.MenuHost",
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            ResizeMode = ResizeMode.NoResize,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            Opacity = 0,
        };

        host.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(host).Handle;
            var style = Win32Window.GetWindowLongPtr(hwnd, Win32Window.GwlExStyle).ToInt64();
            _ = Win32Window.SetWindowLongPtr(
                hwnd,
                Win32Window.GwlExStyle,
                new IntPtr(style
                    | Win32Taskbar.WsExToolWindow
                    | Win32Taskbar.WsExNoActivate
                    | WsExTransparent));
        };

        host.Show();
        _popupHost = host;
        return host;
    }

    private ContextMenu BuildMenu(
        string buttonName,
        IReadOnlyList<AppWindow> windows,
        IReadOnlyList<MonitorInfo> monitors)
    {
        var menu = new ContextMenu();
        var title = string.IsNullOrWhiteSpace(buttonName)
            ? Strings.TaskbarMenu
            : WindowMatcher.NormalizeButtonName(buttonName);
        menu.Items.Add(new MenuItem
        {
            Header = MenuHeaderFactory.Create(title),
            IsEnabled = false,
        });

        if (windows.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = Strings.NoWindowDetected, IsEnabled = false });
        }
        else if (windows.Count == 1)
        {
            AddWindowItems(menu.Items, windows[0], monitors);
        }
        else
        {
            foreach (var window in windows)
            {
                var item = new MenuItem
                {
                    Header = MenuHeaderFactory.Create(window.Title),
                };
                AddWindowItems(item.Items, window, monitors);
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(Strings.TaskbarMenuSettings, _showSettings));
        return menu;
    }

    private void AddWindowItems(ItemCollection items, AppWindow window, IReadOnlyList<MonitorInfo> monitors)
    {
        var moveItem = new MenuItem { Header = Strings.MoveWindowToMonitor };

        for (var index = 0; index < monitors.Count; index++)
        {
            var monitor = monitors[index];
            var isCurrent = index == window.MonitorIndex;
            var markers = new List<string>();

            if (monitor.IsPrimary)
            {
                markers.Add(Strings.PrimaryMonitor);
            }

            if (isCurrent)
            {
                markers.Add(Strings.CurrentMonitor);
            }

            var label = markers.Count == 0
                ? monitor.DisplayName
                : $"{monitor.DisplayName} ({string.Join(", ", markers)})";

            var item = new MenuItem
            {
                Header = MenuHeaderFactory.Create(label),
                IsCheckable = true,
                IsChecked = isCurrent,
                IsEnabled = !isCurrent,
            };

            var targetIndex = index;
            item.Click += (_, _) => MoveWindow(window, targetIndex);
            moveItem.Items.Add(item);
        }

        items.Add(moveItem);

        var primaryIndex = FindPrimaryIndex(monitors);
        if (primaryIndex >= 0)
        {
            var returnItem = new MenuItem
            {
                Header = Strings.MoveToThisMonitor,
                IsEnabled = window.MonitorIndex != primaryIndex,
            };
            returnItem.Click += (_, _) => MoveWindow(window, primaryIndex);
            items.Add(returnItem);
        }

        items.Add(CreateMenuItem(
            Strings.ActivateWindow,
            () => _catalog.Activate(window.Handle)));
    }

    private void MoveWindow(AppWindow window, int targetMonitorIndex)
    {
        if (_windowManager.MoveWindowToMonitor(window.Handle, targetMonitorIndex))
        {
            _windowsChanged();
        }
    }

    private static int FindPrimaryIndex(IReadOnlyList<MonitorInfo> monitors)
    {
        for (var index = 0; index < monitors.Count; index++)
        {
            if (monitors[index].IsPrimary)
            {
                return index;
            }
        }

        return monitors.Count > 0 ? 0 : -1;
    }

    private static MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private static AutomationElement? FindTaskbarButton(int x, int y)
    {
        var taskbar = ShellTaskbars.FindPrimary();
        if (taskbar is null)
        {
            return null;
        }

        try
        {
            var root = AutomationElement.FromHandle(taskbar.Handle);
            var condition = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Button);
            var buttons = root.FindAll(TreeScope.Descendants, condition);

            AutomationElement? fallback = null;
            var fallbackArea = double.MaxValue;

            foreach (AutomationElement button in buttons)
            {
                var bounds = button.Current.BoundingRectangle;
                if (bounds.IsEmpty
                    || bounds.Width <= 0
                    || bounds.Height <= 0
                    || x < bounds.Left
                    || x >= bounds.Right
                    || y < bounds.Top
                    || y >= bounds.Bottom)
                {
                    continue;
                }

                if (string.Equals(
                    button.Current.ClassName,
                    "Taskbar.TaskListButtonAutomationPeer",
                    StringComparison.Ordinal))
                {
                    return button;
                }

                var area = bounds.Width * bounds.Height;
                if (area < fallbackArea)
                {
                    fallbackArea = area;
                    fallback = button;
                }
            }

            return fallback;
        }
        catch (Exception ex) when (IsAutomationError(ex))
        {
            DxLog.Warn($"Taskbar UI Automation lookup failed: {ex.Message}");
        }

        return null;
    }

    private static bool IsAutomationError(Exception ex) =>
        ex is ElementNotAvailableException
            or InvalidOperationException
            or COMException
            or ArgumentException
            or UnauthorizedAccessException;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _source?.RemoveHook(WindowProcedure);

        if (_activeMenu is { IsOpen: true })
        {
            _activeMenu.IsOpen = false;
        }

        if (_popupHost is not null)
        {
            _popupHost.Close();
            _popupHost = null;
        }
    }
}
