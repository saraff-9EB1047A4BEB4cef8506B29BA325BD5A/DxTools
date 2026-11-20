using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DxTools.App.ViewModels;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.App;

public partial class TaskbarWindow : System.Windows.Window
{
    private readonly TaskbarViewModel _viewModel;
    private IntPtr _handle;
    private bool _appBarRegistered;

    public TaskbarWindow(TaskbarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void Place(MonitorInfo monitor, bool useAppBar)
    {
        if (_handle == IntPtr.Zero)
        {
            _handle = new WindowInteropHelper(this).Handle;
        }

        if (_handle == IntPtr.Zero)
        {
            return;
        }

        var height = Math.Max(36, (int)Math.Round(48 * monitor.Dpi / 96.0));
        var desired = new WindowRect
        {
            Left = monitor.X,
            Top = monitor.Bottom - height,
            Right = monitor.Right,
            Bottom = monitor.Bottom,
        };

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
        _viewModel.Start();
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
        if (_appBarRegistered && _handle != IntPtr.Zero)
        {
            Win32Taskbar.RemoveAppBar(_handle);
            _appBarRegistered = false;
        }

        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        WindowScroller.ScrollToHorizontalOffset(WindowScroller.HorizontalOffset - e.Delta);
        e.Handled = true;
    }
}
