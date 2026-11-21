using System.Windows;
using System.Windows.Media;
using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.App;

public partial class TitleBarOverlayWindow : System.Windows.Window
{
    private readonly WindowManager _windowManager;
    private bool? _darkMode;

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

        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var style = Win32Window.GetWindowLongPtr(handle, Win32Taskbar.GwlExStyle).ToInt64();
        _ = Win32Window.SetWindowLongPtr(
            handle,
            Win32Taskbar.GwlExStyle,
            new IntPtr(style | Win32Taskbar.WsExToolWindow | Win32Taskbar.WsExNoActivate));
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
