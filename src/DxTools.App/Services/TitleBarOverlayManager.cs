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
    private readonly Win32Window.WinEventDelegate _winEventCallback;
    private bool _visible;
    private bool _disposed;
    private bool _refreshQueued;
    private IntPtr _foregroundHook;
    private IntPtr _locationHook;
    private IntPtr _lastTarget;
    private int _lastX;
    private int _lastY;
    private int _lastWidth;
    private int _lastHeight;

    public TitleBarOverlayManager(WindowManager windowManager, AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _window = new TitleBarOverlayWindow(windowManager);
        _window.Show();
        _window.Hide();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120),
        };
        _timer.Tick += (_, _) => Update();
        _timer.Start();

        _winEventCallback = OnWinEvent;
        _foregroundHook = Win32Window.SetWinEventHook(
            Win32Window.EventSystemForeground,
            Win32Window.EventSystemForeground,
            IntPtr.Zero,
            _winEventCallback,
            0,
            0,
            Win32Window.WineventOutOfContext);
        _locationHook = Win32Window.SetWinEventHook(
            Win32Window.EventObjectLocationChange,
            Win32Window.EventObjectLocationChange,
            IntPtr.Zero,
            _winEventCallback,
            0,
            0,
            Win32Window.WineventOutOfContext);
    }

    private void OnWinEvent(
        IntPtr hook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime)
    {
        if (_disposed || idObject != 0 || idChild != 0)
        {
            return;
        }

        if (eventType == Win32Window.EventObjectLocationChange
            && hwnd != Win32Window.GetForegroundWindow())
        {
            return;
        }

        QueueRefresh();
    }

    private void QueueRefresh()
    {
        if (_refreshQueued || _disposed)
        {
            return;
        }

        _refreshQueued = true;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            _refreshQueued = false;
            Update();
            return;
        }

        _ = dispatcher.BeginInvoke(
            new Action(() =>
            {
                _refreshQueued = false;
                if (!_disposed)
                {
                    Update();
                }
            }),
            DispatcherPriority.Background);
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

        var wasVisible = _visible;
        if (!_visible)
        {
            _window.Show();
            _visible = true;
        }

        var targetChanged = target != _lastTarget;
        var geometryChanged = targetChanged
            || !wasVisible
            || x != _lastX
            || y != _lastY
            || physicalWidth != _lastWidth
            || physicalHeight != _lastHeight;

        _ = Win32Window.SetWindowPos(
            handle,
            Win32Window.HwndTopmost,
            x,
            y,
            physicalWidth,
            physicalHeight,
            Win32Window.SwpNoActivate | Win32Window.SwpShowWindow);

        if (geometryChanged)
        {
            if (targetChanged)
            {
                DxLog.Info(
                    $"Titlebar overlay target changed: hwnd=0x{target.ToInt64():X}, pid={processId}.");
            }

            _lastTarget = target;
            _lastX = x;
            _lastY = y;
            _lastWidth = physicalWidth;
            _lastHeight = physicalHeight;
            _window.InvalidateVisual();
        }

        _window.SyncPointerState(geometryChanged);
    }

    private void Hide()
    {
        if (!_visible)
        {
            return;
        }

        _window.Hide();
        _window.TargetHandle = IntPtr.Zero;
        _window.ResetPointerState();
        _lastTarget = IntPtr.Zero;
        _lastX = 0;
        _lastY = 0;
        _lastWidth = 0;
        _lastHeight = 0;
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

        if (_foregroundHook != IntPtr.Zero)
        {
            _ = Win32Window.UnhookWinEvent(_foregroundHook);
            _foregroundHook = IntPtr.Zero;
        }

        if (_locationHook != IntPtr.Zero)
        {
            _ = Win32Window.UnhookWinEvent(_locationHook);
            _locationHook = IntPtr.Zero;
        }

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
