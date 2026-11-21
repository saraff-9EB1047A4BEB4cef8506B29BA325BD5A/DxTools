using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.App;

public partial class TitleBarOverlayWindow : System.Windows.Window
{
    private readonly WindowManager _windowManager;
    private bool? _darkMode;
    private bool _pointerInside;

    public TitleBarOverlayWindow(WindowManager windowManager)
    {
        InitializeComponent();
        _windowManager = windowManager;
    }

    public IntPtr TargetHandle { get; set; }

    public void SetDarkMode(bool dark)
    {
        if (_darkMode == dark)
        {
            return;
        }

        _darkMode = dark;
        Resources["TitleBarGlyphBrush"] = dark ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        var style = Win32Window.GetWindowLongPtr(handle, Win32Taskbar.GwlExStyle).ToInt64();
        _ = Win32Window.SetWindowLongPtr(
            handle,
            Win32Taskbar.GwlExStyle,
            new IntPtr(style | Win32Taskbar.WsExToolWindow | Win32Taskbar.WsExNoActivate));

        HwndSource.FromHwnd(handle)?.AddHook(WindowProcedure);
    }

    public void SyncPointerState(bool force = false)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || !IsVisible)
        {
            _pointerInside = false;
            return;
        }

        if (!Win32Taskbar.GetCursorPos(out var cursor)
            || !Win32Window.GetWindowRect(handle, out var rect))
        {
            return;
        }

        var inside = cursor.X >= rect.Left
            && cursor.X < rect.Right
            && cursor.Y >= rect.Top
            && cursor.Y < rect.Bottom;

        if (inside)
        {
            if (force || !_pointerInside)
            {
                var x = cursor.X - rect.Left;
                var y = cursor.Y - rect.Top;
                var lParam = new IntPtr((y << 16) | (x & 0xFFFF));
                _ = Win32Window.PostMessage(handle, Win32Window.WmMouseMove, IntPtr.Zero, lParam);
            }
        }
        else if (_pointerInside)
        {
            _ = Win32Window.PostMessage(handle, Win32Window.WmMouseLeave, IntPtr.Zero, IntPtr.Zero);
        }

        _pointerInside = inside;
    }

    public void ResetPointerState() => _pointerInside = false;

    private static IntPtr WindowProcedure(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message != Win32Window.WmMouseActivate)
        {
            return IntPtr.Zero;
        }

        handled = true;
        return new IntPtr(Win32Window.MaNoActivate);
    }

    private void Move_Click(object sender, RoutedEventArgs e)
    {
        if (TargetHandle != IntPtr.Zero)
        {
            _ = _windowManager.MoveWindow(TargetHandle, 1);
        }
    }

    private void Topmost_Click(object sender, RoutedEventArgs e)
    {
        if (TargetHandle != IntPtr.Zero)
        {
            _ = _windowManager.ToggleTopmost(TargetHandle);
        }
    }
}
