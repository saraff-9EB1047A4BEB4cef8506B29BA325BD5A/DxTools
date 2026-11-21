using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DxTools.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct NativeHotkeyStep
{
    public uint Modifiers;
    public uint VirtualKey;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct NativeHotkeyBinding
{
    public const int MaxSteps = 4;

    public const int ProcessNameLength = 64;

    public uint Id;
    public uint StepCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSteps)]
    public NativeHotkeyStep[] Steps;

    public uint Scope;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ProcessNameLength)]
    public string ProcessName;

    public uint Reserved;
}

public static partial class NativeHotkey
{
    public const uint GlobalScope = 0;

    public const uint ProcessScope = 1;

    public static bool TryStart(
        IntPtr windowHandle,
        uint message,
        IReadOnlyList<NativeHotkeyBinding> bindings,
        out string? error)
    {
        error = null;

        try
        {
            var result = HotkeyStart(windowHandle, message);
            if (result != 0)
            {
                error = new Win32Exception(result).Message;
                return false;
            }

            result = SetBindings(bindings);
            if (result != 0)
            {
                error = new Win32Exception(result).Message;
                _ = HotkeyStop();
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

    public static int SetBindings(IReadOnlyList<NativeHotkeyBinding> bindings)
    {
        if (bindings.Count == 0)
        {
            return HotkeySetBindings(IntPtr.Zero, 0);
        }

        var size = Marshal.SizeOf<NativeHotkeyBinding>();
        var buffer = Marshal.AllocHGlobal(size * bindings.Count);
        try
        {
            for (var index = 0; index < bindings.Count; index++)
            {
                Marshal.StructureToPtr(bindings[index], buffer + (index * size), fDeleteOld: false);
            }

            return HotkeySetBindings(buffer, (uint)bindings.Count);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void Stop()
    {
        try
        {
            _ = HotkeyStop();
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
        }
    }

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Hotkey_Start")]
    private static partial int HotkeyStart(IntPtr hwnd, uint message);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Hotkey_Stop")]
    private static partial int HotkeyStop();

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Hotkey_SetBindings")]
    private static partial int HotkeySetBindings(IntPtr bindings, uint count);
}
