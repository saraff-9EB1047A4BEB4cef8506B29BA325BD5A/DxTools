using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DxTools.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeAudioDeviceInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Name;

    public uint IsDefault;
    public uint Reserved;
}

public sealed record AudioDevice(string Id, string Name, bool IsDefault);

public static partial class NativeAudio
{
    public static IReadOnlyList<AudioDevice> GetDevices()
    {
        var result = GetDeviceCount(out var count);
        if (result != 0)
        {
            throw new Win32Exception(result);
        }

        if (count == 0)
        {
            return Array.Empty<AudioDevice>();
        }

        var size = Marshal.SizeOf<NativeAudioDeviceInfo>();
        var buffer = Marshal.AllocHGlobal(size * checked((int)count));
        try
        {
            result = GetDevices(buffer, count, out var written);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }

            var devices = new AudioDevice[written];
            for (var index = 0; index < written; index++)
            {
                var info = Marshal.PtrToStructure<NativeAudioDeviceInfo>(buffer + (index * size));
                devices[index] = new AudioDevice(
                    info.Id ?? string.Empty,
                    info.Name ?? string.Empty,
                    info.IsDefault != 0);
            }

            return devices;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void SetDefault(string deviceId)
    {
        var result = SetDefaultEndpoint(deviceId);
        if (result != 0)
        {
            throw new Win32Exception(result);
        }
    }

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Audio_GetDeviceCount")]
    private static partial int GetDeviceCount(out uint count);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Audio_GetDevices")]
    private static partial int GetDevices(IntPtr buffer, uint capacity, out uint written);

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Audio_SetDefault", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetDefaultEndpoint(string deviceId);
}
