using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DxTools.App.Properties;
using DxTools.App.Services;
using DxTools.App.ViewModels;
using DxTools.Core;
using DxTools.Engine;
using DxTools.Interop;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace DxTools.App;

public partial class TaskbarWindow : System.Windows.Window
{
    private readonly TaskbarViewModel _viewModel;
    private IntPtr _handle;
    private bool _appBarRegistered;
    private readonly ThumbnailPreviewWindow _previewWindow = new();
    private readonly DispatcherTimer _autoHideTimer;
    private MonitorInfo? _monitor;
    private WindowRect? _bounds;
    private bool _useAppBar;
    private bool _isAutoHidden;
    private ContextMenu? _dxMenu;

    public TaskbarWindow(TaskbarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _autoHideTimer.Tick += (_, _) => UpdateAutoHide();
    }

    public void Refresh() => _viewModel.Refresh();

    public void Place(MonitorInfo monitor, bool useAppBar, WindowRect? bounds = null)
    {
        _monitor = monitor;
        _useAppBar = useAppBar;
        _bounds = bounds;

        if (_handle == IntPtr.Zero)
        {
            _handle = new WindowInteropHelper(this).Handle;
        }

        if (_handle == IntPtr.Zero)
        {
            return;
        }

        ApplyPlacement(useAppBar);
        _viewModel.Start();

        if (_viewModel.AutoHide)
        {
            _autoHideTimer.Start();
        }
    }

    private void ApplyPlacement(bool useAppBar)
    {
        if (_monitor is null || _handle == IntPtr.Zero)
        {
            return;
        }

        WindowRect desired;
        if (_bounds is { } bounds)
        {
            desired = bounds;
        }
        else
        {
            var height = Math.Max(36, (int)Math.Round(48 * _monitor.Dpi / 96.0));
            desired = new WindowRect
            {
                Left = _monitor.X,
                Top = _monitor.Bottom - height,
                Right = _monitor.Right,
                Bottom = _monitor.Bottom,
            };
        }

        var actual = useAppBar ? Win32Taskbar.RegisterAppBar(_handle, desired) : desired;
        _ = Win32Window.SetWindowPos(
            _handle,
            Win32Window.HwndTopmost,
            actual.Left,
            actual.Top,
            actual.Width,
            actual.Height,
            Win32Window.SwpNoActivate | Win32Window.SwpShowWindow);

        _appBarRegistered = useAppBar;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _handle = new WindowInteropHelper(this).Handle;
        var style = Win32Window.GetWindowLongPtr(_handle, Win32Taskbar.GwlExStyle).ToInt64();
        _ = Win32Window.SetWindowLongPtr(
            _handle,
            Win32Taskbar.GwlExStyle,
            new IntPtr(style | Win32Taskbar.WsExToolWindow | Win32Taskbar.WsExNoActivate));
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoHideTimer.Stop();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (_dxMenu is { IsOpen: true })
        {
            _dxMenu.IsOpen = false;
        }

        ClosePreview();
        _previewWindow.Dispose();

        if (_appBarRegistered && _handle != IntPtr.Zero)
        {
            Win32Taskbar.RemoveAppBar(_handle);
            _appBarRegistered = false;
        }

        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnDxButtonClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ClosePreview();

        if (_dxMenu is { IsOpen: true })
        {
            _dxMenu.IsOpen = false;
        }

        var menu = BuildDxMenu();
        _dxMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_dxMenu, menu))
            {
                _dxMenu = null;
            }
        };

        menu.PlacementTarget = DxButton;
        menu.Placement = PlacementMode.Top;
        menu.IsOpen = true;
    }

    private ContextMenu BuildDxMenu()
    {
        var menu = new ContextMenu();
        var monitors = _viewModel.Monitors;

        if (monitors.Count == 0)
        {
            menu.Items.Add(new MenuItem
            {
                Header = Strings.NoMonitorsAvailable,
                IsEnabled = false,
            });
        }
        else
        {
            var windows = _viewModel.GetAllWindows();

            for (var index = 0; index < monitors.Count; index++)
            {
                var monitorWindows = windows
                    .Where(window => window.MonitorIndex == index)
                    .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                var monitorItem = new MenuItem
                {
                    Header = BuildMonitorLabel(monitors[index], index == _viewModel.MonitorIndex),
                };

                if (monitorWindows.Count == 0)
                {
                    monitorItem.Items.Add(new MenuItem
                    {
                        Header = Strings.NoWindowsOnMonitor,
                        IsEnabled = false,
                    });
                }
                else
                {
                    foreach (var window in monitorWindows)
                    {
                        var windowItem = new MenuItem { Header = window.Title };
                        windowItem.Items.Add(CreateMenuItem(
                            Strings.ActivateWindow,
                            () => _viewModel.ActivateWindow(window.Handle)));
                        AddWindowMenuItems(windowItem.Items, window);
                        windowItem.Items.Add(CreateMenuItem(
                            Strings.TitleBarTopmost,
                            () => _viewModel.ToggleTopmost(window.Handle)));
                        monitorItem.Items.Add(windowItem);
                    }
                }

                menu.Items.Add(monitorItem);
            }
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(
            Strings.TaskbarMenuSettings,
            () => _viewModel.OpenSettingsCommand.Execute(null)));
        return menu;
    }

    private void OnWindowButtonMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TaskbarGroupItemViewModel group }
            || group.PreviewHandle == IntPtr.Zero)
        {
            return;
        }

        var element = (FrameworkElement)sender;
        var origin = element.PointToScreen(new System.Windows.Point(0, 0));
        var x = (int)origin.X + ((int)element.ActualWidth / 2) - 170;
        var y = Math.Max(0, (int)origin.Y - 208);
        _previewWindow.Show(group.PreviewHandle, x, y);
    }

    private void OnWindowButtonMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ClosePreview();
    }

    private void ClosePreview()
    {
        _previewWindow.Hide();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskbarViewModel.IsFullscreen))
        {
            Visibility = _viewModel.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
            if (_viewModel.IsFullscreen) { ClosePreview(); }
        }
    }

    private void UpdateAutoHide()
    {
        if (_monitor is null || !_viewModel.AutoHide)
        {
            return;
        }

        if (_viewModel.IsFullscreen)
        {
            HideTaskbar();
            return;
        }

        if (_dxMenu is { IsOpen: true })
        {
            ShowTaskbar();
            return;
        }

        if (IsCursorOnTaskbar())
        {
            ShowTaskbar();
        }
        else
        {
            HideTaskbar();
        }
    }

    private bool IsCursorOnTaskbar()
    {
        if (!Win32Taskbar.GetCursorPos(out var cursor) || _monitor is null)
        {
            return true;
        }

        if (cursor.X >= _monitor.X
            && cursor.X < _monitor.Right
            && cursor.Y >= _monitor.Bottom - 3)
        {
            return true;
        }

        return Win32Window.GetWindowRect(_handle, out var rect)
            && cursor.X >= rect.Left
            && cursor.X < rect.Right
            && cursor.Y >= rect.Top
            && cursor.Y < rect.Bottom;
    }

    private void HideTaskbar()
    {
        if (_isAutoHidden || _handle == IntPtr.Zero)
        {
            return;
        }

        if (_dxMenu is { IsOpen: true })
        {
            _dxMenu.IsOpen = false;
        }

        ClosePreview();

        if (_useAppBar && _appBarRegistered)
        {
            Win32Taskbar.RemoveAppBar(_handle);
            _appBarRegistered = false;
        }

        _ = Win32Window.ShowWindow(_handle, Win32Taskbar.SwHide);
        _isAutoHidden = true;
    }

    private void ShowTaskbar()
    {
        if (!_isAutoHidden || _handle == IntPtr.Zero)
        {
            return;
        }

        ApplyPlacement(_useAppBar);
        _ = Win32Window.ShowWindow(_handle, Win32Taskbar.SwShowNoActivate);
        Visibility = Visibility.Visible;
        _isAutoHidden = false;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        WindowScroller.ScrollToHorizontalOffset(WindowScroller.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void OnWindowButtonPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_viewModel.ContextMenusEnabled
            || sender is not FrameworkElement { DataContext: TaskbarGroupItemViewModel group } element)
        {
            return;
        }

        var menu = BuildContextMenu(group);
        if (menu is null)
        {
            return;
        }

        ClosePreview();
        menu.PlacementTarget = element;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private ContextMenu? BuildContextMenu(TaskbarGroupItemViewModel group)
    {
        var menu = new ContextMenu();

        if (group.IsPinnedOnly)
        {
            menu.Items.Add(CreateMenuItem(
                Strings.RunPinnedApp,
                () => _viewModel.ActivateCommand.Execute(group)));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem(
                Strings.TaskbarMenuSettings,
                () => _viewModel.OpenSettingsCommand.Execute(null)));
            return menu;
        }

        if (group.Windows.Count == 0)
        {
            return null;
        }

        if (group.Windows.Count == 1)
        {
            AddWindowMenuItems(menu.Items, group.Windows[0]);
        }
        else
        {
            foreach (var window in group.Windows)
            {
                var windowItem = new MenuItem { Header = window.Title };
                AddWindowMenuItems(windowItem.Items, window);
                menu.Items.Add(windowItem);
            }
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(
            Strings.TaskbarMenuSettings,
            () => _viewModel.OpenSettingsCommand.Execute(null)));
        return menu;
    }

    private void AddWindowMenuItems(ItemCollection items, AppWindow window)
    {
        var moveItem = new MenuItem { Header = Strings.MoveWindowToMonitor };

        for (var index = 0; index < _viewModel.Monitors.Count; index++)
        {
            var monitor = _viewModel.Monitors[index];
            var isCurrent = index == window.MonitorIndex;

            var item = new MenuItem
            {
                Header = BuildMonitorLabel(monitor, isCurrent),
                IsCheckable = true,
                IsChecked = isCurrent,
                IsEnabled = !isCurrent,
            };

            var targetIndex = index;
            item.Click += (_, _) => _viewModel.MoveWindow(window.Handle, targetIndex);
            moveItem.Items.Add(item);
        }

        items.Add(moveItem);

        var returnItem = new MenuItem
        {
            Header = Strings.MoveToThisMonitor,
            IsEnabled = window.MonitorIndex != _viewModel.MonitorIndex,
        };
        returnItem.Click += (_, _) => _viewModel.MoveWindow(window.Handle, _viewModel.MonitorIndex);
        items.Add(returnItem);
    }

    private static string BuildMonitorLabel(MonitorInfo monitor, bool isCurrent)
    {
        var markers = new List<string>();

        if (monitor.IsPrimary)
        {
            markers.Add(Strings.PrimaryMonitor);
        }

        if (isCurrent)
        {
            markers.Add(Strings.CurrentMonitor);
        }

        return markers.Count == 0
            ? monitor.DisplayName
            : $"{monitor.DisplayName} ({string.Join(", ", markers)})";
    }

    private static MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }
}
