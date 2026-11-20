using System.Runtime.InteropServices;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.App.Services;

public sealed class ThumbnailPreviewWindow : IDisposable
{
    private const int WsPopup = unchecked((int)0x80000000);
    private const int WsVisible = 0x10000000;
    private const int WsBorder = 0x00800000;
    private const int WsClipChildren = 0x02000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExTopmost = 0x00000008;
    private const int WsExNoActivate = 0x08000000;
    private const int SwHide = 0;
    private const int SwShowNoActivate = 8;
    private const int Width = 340;
    private const int Height = 200;

    private const uint DwmTnpRectDestination = 0x00000001;
    private const uint DwmTnpOpacity = 0x00000004;
    private const uint DwmTnpVisible = 0x00000008;
    private const uint DwmTnpSourceClientAreaOnly = 0x00000010;

    private IntPtr _host;
    private IntPtr _thumbnail;
    private IntPtr _source;

    public void Show(IntPtr source, int x, int y)
    {
        if (source == IntPtr.Zero || !Win32Window.IsWindow(source))
        {
            Hide();
            return;
        }

        EnsureWindow();
        if (_host == IntPtr.Zero)
        {
            return;
        }

        _source = source;
        UpdateThumbnail();

        _ = SetWindowPos(
            _host,
            Win32Window.HwndTopmost,
            x,
            y,
            Width,
            Height,
            Win32Window.SwpNoActivate | Win32Window.SwpShowWindow);

        _ = ShowWindow(_host, SwShowNoActivate);
    }

    public void Hide()
    {
        if (_host == IntPtr.Zero)
        {
            return;
        }

        _ = ShowWindow(_host, SwHide);
        UnregisterThumbnail();
        _source = IntPtr.Zero;
    }

    private void EnsureWindow()
    {
        if (_host != IntPtr.Zero)
        {
            return;
        }

        _host = CreateWindowExW(
            WsExToolWindow | WsExTopmost | WsExNoActivate,
            "STATIC",
            string.Empty,
            WsPopup | WsVisible | WsBorder | WsClipChildren,
            0,
            0,
            Width,
            Height,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    private void UpdateThumbnail()
    {
        if (_host == IntPtr.Zero || _source == IntPtr.Zero)
        {
            return;
        }

        if (_thumbnail == IntPtr.Zero)
        {
            var result = DwmRegisterThumbnail(_host, _source, out var thumbnail);
            if (result != 0)
            {
                DxLog.Warn($"DwmRegisterThumbnail failed: 0x{result:X8}.");
                return;
            }

            _thumbnail = thumbnail;
        }

        if (!GetClientRect(_host, out var client))
        {
            return;
        }

        var properties = new DwmThumbnailProperties
        {
            Flags = DwmTnpRectDestination | DwmTnpOpacity | DwmTnpVisible | DwmTnpSourceClientAreaOnly,
            Destination = client,
            Opacity = 255,
            Visible = 1,
            SourceClientAreaOnly = 1,
        };

        var updateResult = DwmUpdateThumbnailProperties(_thumbnail, ref properties);
        if (updateResult != 0)
        {
            DxLog.Warn($"DwmUpdateThumbnailProperties failed: 0x{updateResult:X8}.");
        }
    }

    private void UnregisterThumbnail()
    {
        if (_thumbnail == IntPtr.Zero)
        {
            return;
        }

        _ = DwmUnregisterThumbnail(_thumbnail);
        _thumbnail = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_host == IntPtr.Zero)
        {
            return;
        }

        UnregisterThumbnail();
        _ = DestroyWindow(_host);
        _host = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DwmThumbnailProperties
    {
        public uint Flags;
        public WindowRect Destination;
        public WindowRect Source;
        public byte Opacity;
        public int Visible;
        public int SourceClientAreaOnly;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        int exStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hwnd, out WindowRect rect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmRegisterThumbnail(IntPtr destination, IntPtr source, out IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    private static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    private static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail, ref DwmThumbnailProperties properties);
}
