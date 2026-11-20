using System.ComponentModel;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.Engine;

public sealed record AudioOperationResult(bool Success, string? Error);

public sealed class AudioService
{
    private readonly bool _safeMode;

    public AudioService(bool safeMode)
    {
        _safeMode = safeMode;
    }

    public IReadOnlyList<AudioDevice> GetDevices()
    {
        if (_safeMode)
        {
            return Array.Empty<AudioDevice>();
        }

        try
        {
            return NativeAudio.GetDevices();
        }
        catch (Exception ex) when (ex is Win32Exception
            or DllNotFoundException
            or BadImageFormatException
            or EntryPointNotFoundException
            or FileLoadException)
        {
            DxLog.Warn($"Audio device enumeration failed: {ex.Message}");
            return Array.Empty<AudioDevice>();
        }
    }

    public AudioOperationResult SetDefault(string deviceId)
    {
        if (_safeMode)
        {
            return new AudioOperationResult(false, "Safe mode: audio changes are disabled.");
        }

        try
        {
            NativeAudio.SetDefault(deviceId);
            return new AudioOperationResult(true, null);
        }
        catch (Exception ex) when (ex is Win32Exception
            or DllNotFoundException
            or BadImageFormatException
            or EntryPointNotFoundException
            or FileLoadException)
        {
            DxLog.Warn($"Audio default change failed: {ex.Message}");
            return new AudioOperationResult(false, ex.Message);
        }
    }
}