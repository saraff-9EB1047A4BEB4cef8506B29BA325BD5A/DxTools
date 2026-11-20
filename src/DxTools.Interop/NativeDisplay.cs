using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DxTools.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeMonitorInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string MonitorId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string FriendlyName;

    public int X;
    public int Y;
    public int Width;
    public int Height;
    public uint IsPrimary;
    public uint Dpi;
    public uint RefreshRate;
    public uint BitsPerPixel;
}

public sealed record NativeMonitor(
    string DeviceName,
    string MonitorId,
    string FriendlyName,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary,
    uint Dpi,
    uint RefreshRate,
    uint BitsPerPixel);

public sealed class NativeDisplayException : Exception
{
    public NativeDisplayException(string operation, int errorCode)
        : base($"{operation} failed with Win32 error {errorCode}: {new Win32Exception(errorCode).Message}")
    {
        Operation = operation;
        ErrorCode = errorCode;
    }

    public string Operation { get; }

    public int ErrorCode { get; }
}

public static partial class NativeDisplay
{
    public static IReadOnlyList<NativeMonitor> GetMonitors()
    {
        var countResult = DisplayGetMonitorCount(out var count);
        if (countResult != 0)
        {
            throw new NativeDisplayException("GetMonitorCount", countResult);
        }

        if (count == 0)
        {
            return Array.Empty<NativeMonitor>();
        }

        var size = Marshal.SizeOf<NativeMonitorInfo>();
        var buffer = Marshal.AllocHGlobal(checked((int)(size * count)));
        try
        {
            var result = DisplayGetMonitors(buffer, count, out var written);
            if (result != 0)
            {
                throw new NativeDisplayException("GetMonitors", result);
            }

            var monitors = new NativeMonitor[written];
            for (var i = 0; i < written; i++)
            {
                var info = Marshal.PtrToStructure<NativeMonitorInfo>(buffer + (i * size));
                monitors[i] = new NativeMonitor(
                    info.DeviceName ?? string.Empty,
                    info.MonitorId ?? string.Empty,
                    info.FriendlyName ?? string.Empty,
                    info.X,
                    info.Y,
                    info.Width,
                    info.Height,
                    info.IsPrimary != 0,
                    info.Dpi,
                    info.RefreshRate,
                    info.BitsPerPixel);
            }

            return monitors;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static byte[] CaptureState()
    {
        var result = DisplayGetState(out var data, out var size);
        if (result != 0)
        {
            throw new NativeDisplayException("GetState", result);
        }

        try
        {
            if (data == IntPtr.Zero || size == 0)
            {
                throw new NativeDisplayException("GetState", 1168);
            }

            var state = new byte[size];
            Marshal.Copy(data, state, 0, checked((int)size));
            return state;
        }
        finally
        {
            if (data != IntPtr.Zero)
            {
                FreeMemory(data);
            }
        }
    }

    public static void ApplyState(ReadOnlySpan<byte> state)
    {
        if (state.IsEmpty)
        {
            throw new ArgumentException("Display state is empty.", nameof(state));
        }

        var buffer = Marshal.AllocHGlobal(state.Length);
        try
        {
            unsafe
            {
                state.CopyTo(new Span<byte>((void*)buffer, state.Length));
            }

            var result = DisplayApplyState(buffer, checked((uint)state.Length), 0);
            if (result != 0)
            {
                throw new NativeDisplayException("ApplyState", result);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Display_GetMonitorCount")]
    private static partial int DisplayGetMonitorCount(out uint count);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Display_GetMonitors")]
    private static partial int DisplayGetMonitors(IntPtr buffer, uint capacity, out uint written);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Display_GetState")]
    private static partial int DisplayGetState(out IntPtr data, out uint size);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Display_ApplyState")]
    private static partial int DisplayApplyState(IntPtr data, uint size, uint flags);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_FreeMemory")]
    private static partial void FreeMemory(IntPtr data);
}
