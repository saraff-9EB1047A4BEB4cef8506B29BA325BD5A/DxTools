using System.Runtime.InteropServices;

namespace DxTools.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey
{
    public Guid FormatId;
    public uint PropertyId;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PropVariant
{
    [FieldOffset(0)]
    public ushort VarType;

    [FieldOffset(8)]
    public IntPtr Pointer;
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int GetAt(uint index, out PropertyKey key);

    [PreserveSig]
    int GetValue(ref PropertyKey key, out PropVariant value);

    [PreserveSig]
    int SetValue(ref PropertyKey key, ref PropVariant value);

    [PreserveSig]
    int Commit();
}

public static class WindowAppId
{
    private const ushort VtLpwstr = 31;
    private const int ErrorInsufficientBuffer = 122;

    private static readonly Guid PropertyStoreIid = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
    private static readonly PropertyKey AppUserModelIdKey = new()
    {
        FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        PropertyId = 5,
    };

    public static string Get(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return string.Empty;
        }

        var processId = GetProcessId(window);
        if (processId != 0)
        {
            var processAppId = GetFromProcess(processId);
            if (processAppId.Length > 0)
            {
                return processAppId;
            }
        }

        return GetFromWindow(window);
    }

    private static uint GetProcessId(IntPtr window)
    {
        _ = Win32Window.GetWindowThreadProcessId(window, out var processId);
        return processId;
    }

    private static string GetFromProcess(uint processId)
    {
        var process = Win32Taskbar.OpenProcess(
            Win32Taskbar.ProcessQueryLimitedInformation,
            inheritHandle: false,
            processId);

        if (process == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var capacity = 1024;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var buffer = new System.Text.StringBuilder(capacity);
                var length = (uint)capacity;
                var result = GetApplicationUserModelId(process, ref length, buffer);
                if (result == 0)
                {
                    return buffer.ToString();
                }

                if (result != ErrorInsufficientBuffer || length <= capacity)
                {
                    return string.Empty;
                }

                capacity = checked((int)length + 1);
            }

            return string.Empty;
        }
        catch (Exception ex) when (ex is DllNotFoundException
            or EntryPointNotFoundException
            or BadImageFormatException)
        {
            return string.Empty;
        }
        finally
        {
            _ = Win32Taskbar.CloseHandle(process);
        }
    }

    private static string GetFromWindow(IntPtr window)
    {
        IPropertyStore? store = null;
        try
        {
            var iid = PropertyStoreIid;
            if (SHGetPropertyStoreForWindow(window, ref iid, out store) != 0 || store is null)
            {
                return string.Empty;
            }

            var key = AppUserModelIdKey;
            if (store.GetValue(ref key, out var value) != 0)
            {
                return string.Empty;
            }

            try
            {
                return value.VarType == VtLpwstr && value.Pointer != IntPtr.Zero
                    ? Marshal.PtrToStringUni(value.Pointer) ?? string.Empty
                    : string.Empty;
            }
            finally
            {
                _ = PropVariantClear(ref value);
            }
        }
        catch (Exception ex) when (ex is COMException
            or InvalidCastException
            or UnauthorizedAccessException
            or DllNotFoundException
            or EntryPointNotFoundException)
        {
            return string.Empty;
        }
        finally
        {
            if (store is not null)
            {
                _ = Marshal.ReleaseComObject(store);
            }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(
        IntPtr process,
        ref uint applicationUserModelIdLength,
        System.Text.StringBuilder applicationUserModelId);

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(
        IntPtr hwnd,
        ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);
}
