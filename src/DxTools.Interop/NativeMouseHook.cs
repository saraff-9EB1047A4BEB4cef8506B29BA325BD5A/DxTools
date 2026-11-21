using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DxTools.Interop;

public static partial class NativeMouseHook
{
    public static bool TryStart(IntPtr windowHandle, uint message, out string? error)
    {
        error = null;

        try
        {
            var result = Start(windowHandle, message);
            if (result != 0)
            {
                error = new Win32Exception(result).Message;
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException
            or BadImageFormatException
            or EntryPointNotFoundException
            or FileLoadException)
        {
            error = ex.Message;
            return false;
        }
    }

    public static void SetTaskbarRect(WindowRect rect)
    {
        try
        {
            _ = SetRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            _ = SetHookEnabled(enabled ? 1 : 0);
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
        }
    }

    public static void Stop()
    {
        try
        {
            _ = StopHook();
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
        }
    }

    private static bool IsUnavailable(Exception ex) =>
        ex is DllNotFoundException
            or BadImageFormatException
            or EntryPointNotFoundException
            or FileLoadException;

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_MouseHook_Start")]
    private static partial int Start(IntPtr hwnd, uint message);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_MouseHook_Stop")]
    private static partial int StopHook();

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_MouseHook_SetTaskbarRect")]
    private static partial int SetRect(int left, int top, int right, int bottom);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_MouseHook_SetEnabled")]
    private static partial int SetHookEnabled(int enabled);
}
