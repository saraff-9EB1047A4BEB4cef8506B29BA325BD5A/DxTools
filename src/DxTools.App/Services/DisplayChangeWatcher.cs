using System.Windows.Interop;
using System.Windows.Threading;
using DxTools.Core;

namespace DxTools.App.Services;

public sealed class DisplayChangeWatcher : IDisposable
{
    private const int WmDisplayChange = 0x007E;

    private readonly HwndSource? _source;
    private readonly DispatcherTimer _debounce;
    private readonly Action _changed;
    private bool _disposed;

    public DisplayChangeWatcher(IntPtr windowHandle, Action changed)
    {
        _changed = changed ?? throw new ArgumentNullException(nameof(changed));
        _source = HwndSource.FromHwnd(windowHandle);

        if (_source is not null)
        {
            _source.AddHook(WindowProcedure);
        }
        else
        {
            DxLog.Warn("Display change watcher could not attach to the application window.");
        }

        _debounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5),
        };
        _debounce.Tick += OnDebounceTick;
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmDisplayChange)
        {
            return IntPtr.Zero;
        }

        _debounce.Stop();
        _debounce.Start();
        return IntPtr.Zero;
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();

        try
        {
            _changed();
        }
        catch (Exception ex)
        {
            DxLog.Exception("Display change handler failed.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _debounce.Stop();
        _source?.RemoveHook(WindowProcedure);
    }
}
