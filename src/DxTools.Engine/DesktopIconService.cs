using System.Runtime.InteropServices;
using DxTools.Core;

namespace DxTools.Engine;

public sealed record DesktopIconSnapshot(bool Success, int IconCount, IReadOnlyList<DesktopIconPosition> Icons, string? Error);

public sealed record DesktopIconApplyResult(bool Success, int Applied, string? Error);

public sealed class DesktopIconService
{
    private const int LvmFirst = 0x1000;
    private const int LvmGetItemCount = LvmFirst + 4;
    private const int LvmGetItemPosition = LvmFirst + 16;
    private const int LvmSetItemPosition32 = LvmFirst + 49;
    private const int SmtoAbortIfHung = 0x0002;
    private const uint ProcessVmOperation = 0x0008;
    private const uint ProcessVmRead = 0x0010;
    private const uint ProcessVmWrite = 0x0020;
    private const uint ProcessQueryInformation = 0x0400;

    public DesktopIconSnapshot Capture()
    {
        var listView = FindDesktopListView();
        if (listView == IntPtr.Zero)
        {
            return new DesktopIconSnapshot(false, 0, [], "Desktop icon list was not found.");
        }

        _ = GetWindowThreadProcessId(listView, out var processId);
        var process = OpenProcess(ProcessVmOperation | ProcessVmRead | ProcessVmWrite | ProcessQueryInformation, false, processId);
        if (process == IntPtr.Zero)
        {
            return new DesktopIconSnapshot(false, 0, [], "Could not open the Explorer process.");
        }

        var remote = IntPtr.Zero;
        try
        {
            var sent = SendMessageTimeout(listView, LvmGetItemCount, IntPtr.Zero, IntPtr.Zero, SmtoAbortIfHung, 2000, out var result);
            var count = sent != IntPtr.Zero ? result.ToInt32() : 0;

            if (count <= 0)
            {
                return new DesktopIconSnapshot(true, 0, [], null);
            }

            remote = VirtualAllocEx(process, IntPtr.Zero, (nuint)Marshal.SizeOf<NativePoint>(), 0x3000, 0x04);
            if (remote == IntPtr.Zero)
            {
                return new DesktopIconSnapshot(false, count, [], "Could not allocate memory in Explorer.");
            }

            var icons = new List<DesktopIconPosition>(count);
            for (var index = 0; index < count; index++)
            {
                if (SendMessageTimeout(listView, LvmGetItemPosition, new IntPtr(index), remote, SmtoAbortIfHung, 2000, out _) == IntPtr.Zero)
                {
                    continue;
                }

                if (ReadProcessMemory(process, remote, out NativePoint point, (nuint)Marshal.SizeOf<NativePoint>(), out _))
                {
                    icons.Add(new DesktopIconPosition(index, point.X, point.Y));
                }
            }

            return new DesktopIconSnapshot(true, count, icons, null);
        }
        catch (Exception ex)
        {
            DxLog.Exception("Desktop icon capture failed.", ex);
            return new DesktopIconSnapshot(false, 0, [], ex.Message);
        }
        finally
        {
            if (remote != IntPtr.Zero)
            {
                _ = VirtualFreeEx(process, remote, 0, 0x8000);
            }

            _ = CloseHandle(process);
        }
    }

    public DesktopIconApplyResult Apply(IReadOnlyList<DesktopIconPosition> icons)
    {
        ArgumentNullException.ThrowIfNull(icons);

        var listView = FindDesktopListView();
        if (listView == IntPtr.Zero)
        {
            return new DesktopIconApplyResult(false, 0, "Desktop icon list was not found.");
        }

        _ = GetWindowThreadProcessId(listView, out var processId);
        var process = OpenProcess(ProcessVmOperation | ProcessVmRead | ProcessVmWrite | ProcessQueryInformation, false, processId);
        if (process == IntPtr.Zero)
        {
            return new DesktopIconApplyResult(false, 0, "Could not open the Explorer process.");
        }

        var remote = IntPtr.Zero;
        try
        {
            remote = VirtualAllocEx(process, IntPtr.Zero, (nuint)Marshal.SizeOf<NativePoint>(), 0x3000, 0x04);
            if (remote == IntPtr.Zero)
            {
                return new DesktopIconApplyResult(false, 0, "Could not allocate memory in Explorer.");
            }

            var applied = 0;
            foreach (var icon in icons)
            {
                var point = new NativePoint { X = icon.X, Y = icon.Y };
                if (!WriteProcessMemory(process, remote, ref point, (nuint)Marshal.SizeOf<NativePoint>(), out _))
                {
                    continue;
                }

                if (SendMessageTimeout(listView, LvmSetItemPosition32, new IntPtr(icon.Index), remote, SmtoAbortIfHung, 2000, out _) != IntPtr.Zero)
                {
                    applied++;
                }
            }

            return new DesktopIconApplyResult(true, applied, null);
        }
        catch (Exception ex)
        {
            DxLog.Exception("Desktop icon apply failed.", ex);
            return new DesktopIconApplyResult(false, 0, ex.Message);
        }
        finally
        {
            if (remote != IntPtr.Zero)
            {
                _ = VirtualFreeEx(process, remote, 0, 0x8000);
            }

            _ = CloseHandle(process);
        }
    }

    private static IntPtr FindDesktopListView()
    {
        var progman = FindWindowW("Progman", null);
        var view = progman == IntPtr.Zero
            ? IntPtr.Zero
            : FindWindowExW(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

        if (view == IntPtr.Zero)
        {
            EnumWindows((hwnd, _) =>
            {
                var className = new System.Text.StringBuilder(128);
                _ = GetClassNameW(hwnd, className, className.Capacity);
                if (className.ToString() != "WorkerW")
                {
                    return true;
                }

                var candidate = FindWindowExW(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (candidate != IntPtr.Zero)
                {
                    view = candidate;
                    return false;
                }

                return true;
            }, IntPtr.Zero);
        }

        return view == IntPtr.Zero
            ? IntPtr.Zero
            : FindWindowExW(view, IntPtr.Zero, "SysListView32", null);
    }

    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowExW(IntPtr parent, IntPtr after, string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr hwnd, System.Text.StringBuilder name, int maxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        int flags,
        int timeout,
        out IntPtr result);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, nuint size, uint allocationType, uint protection);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, nuint size, uint freeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(IntPtr process, IntPtr address, out NativePoint buffer, nuint size, out nuint read);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(IntPtr process, IntPtr address, ref NativePoint buffer, nuint size, out nuint written);
}
