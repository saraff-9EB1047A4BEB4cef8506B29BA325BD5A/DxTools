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

        var dpi = Win32Window.GetDpiForWindow(target);
        if (dpi == 0)
        {
            dpi = 96;
        }

        var scale = dpi / 96.0;
        if (frame.Width < (int)Math.Round(360 * scale)
            || frame.Height < (int)Math.Round(80 * scale))
        {
            Hide();
            return;
        }
        var physicalWidth = (int)Math.Round(2 * 46 * scale);
        var physicalHeight = (int)Math.Round(32 * scale);
        var captionButtons = (int)Math.Round(3 * 46 * scale);
        var x = frame.Right - captionButtons - physicalWidth;
        var y = frame.Top;

        if (x < frame.Left + (int)Math.Round(120 * scale))
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
        _window.SetDarkMode(IsDarkWindow(target));

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

    private bool IsDarkWindow(IntPtr hwnd)
    {
        var value = 0;
        if (TryGetDwmValue(hwnd, DwmUseImmersiveDarkMode, out value)
            || TryGetDwmValue(hwnd, DwmUseImmersiveDarkModeBefore20H1, out value))
        {
            return value != 0;
        }

        return ThemeManager.Resolve(_settings.Theme) == AppTheme.Dark;
    }

    private static bool TryGetDwmValue(IntPtr hwnd, int attribute, out int value)
    {
        value = 0;
        try
        {
            return Win32Window.DwmGetWindowAttribute(
                hwnd,
                attribute,
                out value,
                sizeof(int)) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private const int DwmWindowAttributeExtendedFrameBounds = 9;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
}
