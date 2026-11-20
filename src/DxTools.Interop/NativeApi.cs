using System.Reflection;
using System.Runtime.InteropServices;

namespace DxTools.Interop;

public static class NativeLibraryInfo
{
    public const string FileName = "DxTools.Native.dll";
    public const string LibraryName = "DxTools.Native";
    public const uint ExpectedApiVersion = 4;
    public const uint ExpectedPing = 0x4458544F;
}

public sealed record NativeProbeResult(
    bool IsAvailable,
    uint ApiVersion,
    string? LibraryVersion,
    bool PingOk,
    string LibraryPath,
    string? Error)
{
    public bool IsHealthy => IsAvailable && PingOk && ApiVersion == NativeLibraryInfo.ExpectedApiVersion;

    public static NativeProbeResult NotChecked { get; } = new(
        IsAvailable: false,
        ApiVersion: 0,
        LibraryVersion: null,
        PingOk: false,
        LibraryPath: Path.Combine(AppContext.BaseDirectory, NativeLibraryInfo.FileName),
        Error: "Not checked.");

    public static NativeProbeResult Skipped(string reason) => new(
        IsAvailable: false,
        ApiVersion: 0,
        LibraryVersion: null,
        PingOk: false,
        LibraryPath: Path.Combine(AppContext.BaseDirectory, NativeLibraryInfo.FileName),
        Error: reason);
}

public static partial class NativeApi
{
    static NativeApi()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeApi).Assembly, ResolveLibrary);
    }

    public static NativeProbeResult Probe()
    {
        var path = Path.Combine(AppContext.BaseDirectory, NativeLibraryInfo.FileName);

        try
        {
            var apiVersion = GetApiVersion();
            var libraryVersion = GetVersion();
            var ping = Ping();

            return new NativeProbeResult(
                IsAvailable: true,
                ApiVersion: apiVersion,
                LibraryVersion: FormatVersion(libraryVersion),
                PingOk: ping == NativeLibraryInfo.ExpectedPing,
                LibraryPath: path,
                Error: null);
        }
        catch (Exception ex) when (ex is DllNotFoundException
            or BadImageFormatException
            or EntryPointNotFoundException
            or FileLoadException
            or InvalidOperationException)
        {
            return new NativeProbeResult(
                IsAvailable: false,
                ApiVersion: 0,
                LibraryVersion: null,
                PingOk: false,
                LibraryPath: path,
                Error: ex.Message);
        }
    }

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, NativeLibraryInfo.LibraryName, StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        var path = Path.Combine(AppContext.BaseDirectory, NativeLibraryInfo.FileName);
        if (!File.Exists(path))
        {
            return IntPtr.Zero;
        }

        try
        {
            return NativeLibrary.Load(path);
        }
        catch (Exception ex) when (ex is BadImageFormatException or DllNotFoundException or IOException)
        {
            return IntPtr.Zero;
        }
    }

    private static string FormatVersion(uint packedVersion)
    {
        var major = (packedVersion >> 16) & 0xFF;
        var minor = (packedVersion >> 8) & 0xFF;
        var patch = packedVersion & 0xFF;
        return $"{major}.{minor}.{patch}";
    }

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_GetApiVersion")]
    private static partial uint GetApiVersion();

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_GetVersion")]
    private static partial uint GetVersion();

    [LibraryImport(NativeLibraryInfo.LibraryName, EntryPoint = "DxTools_Ping")]
    private static partial uint Ping();
}
