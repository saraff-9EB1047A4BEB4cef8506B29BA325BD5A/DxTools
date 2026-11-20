using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Threading;
using DxTools.App.Services;
using DxTools.App.ViewModels;
using DxTools.Core;
using DxTools.Interop;

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

    public TaskbarWindow(TaskbarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _autoHideTimer.Tick += (_, _) => UpdateAutoHide();
    }

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
}
