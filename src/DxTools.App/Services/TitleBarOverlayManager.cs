using System.Runtime.InteropServices;
using System.Windows.Threading;
using DxTools.Core;
using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.App.Services;

public sealed class TitleBarOverlayManager : IDisposable
{
    private readonly AppSettings _settings;
    private readonly TitleBarOverlayWindow _window;
    private readonly DispatcherTimer _timer;
    private bool _visible;
    private bool _disposed;

    public TitleBarOverlayManager(WindowManager windowManager, AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _window = new TitleBarOverlayWindow(windowManager);
        _window.Show();
        _window.Hide();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };
        _timer.Tick += (_, _) => Update();
        _timer.Start();
    }

    public void Refresh() => Update();

    private void Update()
    {
        if (_disposed)
        {
            return;
        }

        if (!_settings.EnableTitleBarButtons)
        {
            Hide();
            return;
        }

        var target = Win32Window.GetForegroundWindow();
        if (target == IntPtr.Zero
            || target == Win32Window.GetShellWindow()
            || !Win32Window.IsWindow(target))
        {
            Hide();
            return;
        }

        _ = Win32Window.GetWindowThreadProcessId(target, out var processId);
        if (processId == Environment.ProcessId)
        {
            Hide();
            return;
        }

        var style = Win32Window.GetWindowLongPtr(target, Win32Window.GwlStyle).ToInt64();
        if ((style & Win32Window.WsCaption) != Win32Window.WsCaption || Win32Taskbar.IsIconic(target))
        {
            Hide();
            return;
        }

        WindowRect frame;
        if (Win32Window.DwmGetWindowAttribute(
                target,
                DwmWindowAttributeExtendedFrameBounds,
                out frame,
                Marshal.SizeOf<WindowRect>()) != 0
            && !Win32Window.GetWindowRect(target, out frame))
        {
            Hide();
            return;
        }

        if (frame.Width < 360 || frame.Height < 80)
        {
            Hide();
            return;
        }

        var dpi = Win32Window.GetDpiForWindow(target);
        if (dpi == 0)
        {
            dpi = 96;
        }

        var scale = dpi / 96.0;
        var physicalWidth = (int)Math.Round(76 * scale);
        var physicalHeight = (int)Math.Round(30 * scale);
        var captionButtons = (int)Math.Round(3 * 46 * scale);
        var x = frame.Right - captionButtons - physicalWidth - (int)Math.Round(8 * scale);
        var y = frame.Top + (int)Math.Round(4 * scale);

        if (x < frame.Left + 120)
        {
            Hide();
            return;
        }

        var handle = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _window.TargetHandle = target;

        if (!_visible)
        {
            _window.Show();
            _visible = true;
        }

        _ = Win32Window.SetWindowPos(
            handle,
            Win32Window.HwndTopmost,
            x,
            y,
            physicalWidth,
            physicalHeight,
            Win32Window.SwpNoActivate | Win32Window.SwpShowWindow);
    }

    private void Hide()
    {
        if (!_visible)
        {
            return;
        }

        _window.Hide();
        _window.TargetHandle = IntPtr.Zero;
        _visible = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _window.Close();
    }

    private const int DwmWindowAttributeExtendedFrameBounds = 9;
}
