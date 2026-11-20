using System.Runtime.InteropServices;
using System.Text;

namespace DxTools.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct AppBarData
{
    public int cbSize;
    public IntPtr hWnd;
    public uint uCallbackMessage;
    public uint uEdge;
    public WindowRect rc;
    public IntPtr lParam;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct WindowPlacement
{
    public int Length;
    public int Flags;
    public int ShowCommand;
    public NativePoint MinPosition;
    public NativePoint MaxPosition;
    public WindowRect NormalPosition;
}

public static class Win32Taskbar
{
    public const uint AbmNew = 0;
    public const uint AbmRemove = 1;
    public const uint AbmQueryPos = 2;
    public const uint AbmSetPos = 3;
    public const uint AbeBottom = 3;

    public const int GwlExStyle = -20;
    public const long WsExToolWindow = 0x00000080;
    public const long WsExNoActivate = 0x08000000;
    public const int DwmwaCloaked = 14;

    public const uint GwOwner = 4;
    public const uint ProcessQueryLimitedInformation = 0x1000;
    public const int SwMinimize = 6;
    public const int SwRestore = 9;

    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLengthW(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextW(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowPlacement(IntPtr hwnd, ref WindowPlacement placement);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(uint attach, uint attachTo, [MarshalAs(UnmanagedType.Bool)] bool attachFlag);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageNameW(
        IntPtr process,
        uint flags,
        StringBuilder name,
        ref int size);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassNameW(IntPtr hwnd, StringBuilder name, int maxCount);

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern UIntPtr SHAppBarMessage(uint message, ref AppBarData data);

    public static bool IsCloaked(IntPtr hwnd)
    {
        try
        {
            return DwmGetWindowAttribute(hwnd, DwmwaCloaked, out var cloaked, sizeof(int)) == 0 && cloaked != 0;
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

    public static string GetProcessPath(uint processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, inheritHandle: false, processId);
        if (process == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var buffer = new StringBuilder(1024);
            var size = buffer.Capacity;
            return QueryFullProcessImageNameW(process, 0, buffer, ref size)
                ? buffer.ToString(0, size)
                : string.Empty;
        }
        finally
        {
            _ = CloseHandle(process);
        }
    }

    public static WindowRect RegisterAppBar(IntPtr hwnd, WindowRect desired)
    {
        var data = new AppBarData
        {
            cbSize = Marshal.SizeOf<AppBarData>(),
            hWnd = hwnd,
            uEdge = AbeBottom,
            rc = desired,
        };

        _ = SHAppBarMessage(AbmNew, ref data);

        data.rc = desired;
        _ = SHAppBarMessage(AbmQueryPos, ref data);

        var height = desired.Bottom - desired.Top;
        data.rc.Top = data.rc.Bottom - height;
        _ = SHAppBarMessage(AbmSetPos, ref data);

        return data.rc;
    }

    public static void RemoveAppBar(IntPtr hwnd)
    {
        var data = new AppBarData
        {
            cbSize = Marshal.SizeOf<AppBarData>(),
            hWnd = hwnd,
        };

        _ = SHAppBarMessage(AbmRemove, ref data);
    }
}

public sealed record ShellTaskbarInfo(IntPtr Handle, WindowRect Rect);

public static class ShellTaskbars
{
    public static IReadOnlyList<ShellTaskbarInfo> FindSecondary()
    {
        var result = new List<ShellTaskbarInfo>();

        _ = Win32Taskbar.EnumWindows((hwnd, _) =>
        {
            var name = new StringBuilder(128);
            if (Win32Taskbar.GetClassNameW(hwnd, name, name.Capacity) <= 0
                || !string.Equals(name.ToString(), "Shell_SecondaryTrayWnd", StringComparison.Ordinal))
            {
                return true;
            }

            if (Win32Window.GetWindowRect(hwnd, out var rect) && rect.Width > 0 && rect.Height > 0)
            {
                result.Add(new ShellTaskbarInfo(hwnd, rect));
            }

            return true;
        }, IntPtr.Zero);

        return result;
    }

    public static void SetVisible(IntPtr hwnd, bool visible)
    {
        if (Win32Window.IsWindow(hwnd))
        {
            _ = Win32Window.ShowWindow(hwnd, visible ? 5 : 0);
        }
    }
}