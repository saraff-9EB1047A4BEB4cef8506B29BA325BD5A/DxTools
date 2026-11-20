using System.Globalization;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.Engine;

public sealed record DisplayStateResult(bool Success, byte[]? State, string? Error);

public sealed record DisplayApplyResult(bool Success, string? Error, byte[]? RollbackState);

public sealed class DisplayService
{
    private readonly bool _safeMode;

    public DisplayService(bool safeMode)
    {
        _safeMode = safeMode;
    }

    public bool IsEnabled => !_safeMode;

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        if (_safeMode)
        {
            return Array.Empty<MonitorInfo>();
        }

        try
        {
            return NativeDisplay.GetMonitors()
                .Select(monitor => new MonitorInfo(
                    monitor.DeviceName,
                    monitor.MonitorId,
                    monitor.FriendlyName,
                    monitor.X,
                    monitor.Y,
                    monitor.Width,
                    monitor.Height,
                    monitor.IsPrimary,
                    monitor.Dpi,
                    monitor.RefreshRate,
                    monitor.BitsPerPixel))
                .ToList();
        }
        catch (Exception ex) when (IsNativeFailure(ex))
        {
            DxLog.Warn($"Display enumeration failed: {ex.Message}");
            return Array.Empty<MonitorInfo>();
        }
    }

    public DisplayStateResult CaptureState()
    {
        if (_safeMode)
        {
            return new DisplayStateResult(false, null, "Safe mode: display capture is disabled.");
        }

        try
        {
            return new DisplayStateResult(true, NativeDisplay.CaptureState(), null);
        }
        catch (Exception ex) when (IsNativeFailure(ex))
        {
            DxLog.Warn($"Display state capture failed: {ex.Message}");
            return new DisplayStateResult(false, null, ex.Message);
        }
    }

    public DisplayApplyResult ApplyState(byte[] state)
    {
        if (_safeMode)
        {
            return new DisplayApplyResult(false, "Safe mode: display changes are disabled.", null);
        }

        ArgumentNullException.ThrowIfNull(state);

        var rollback = CaptureState();

        try
        {
            NativeDisplay.ApplyState(state);
            return new DisplayApplyResult(true, null, rollback.Success ? rollback.State : null);
        }
        catch (Exception ex) when (IsNativeFailure(ex))
        {
            DxLog.Warn($"Display state apply failed: {ex.Message}");

            if (rollback.Success && rollback.State is not null)
            {
                try
                {
                    NativeDisplay.ApplyState(rollback.State);
                }
                catch (Exception rollbackException) when (IsNativeFailure(rollbackException))
                {
                    DxLog.Exception("Display rollback failed.", rollbackException);
                }
            }

            return new DisplayApplyResult(false, ex.Message, rollback.State);
        }
    }

    public string ComputeTopologyHash(IEnumerable<MonitorInfo> monitors)
    {
        var ids = monitors
            .Select(monitor => string.IsNullOrWhiteSpace(monitor.MonitorId) ? monitor.DeviceName : monitor.MonitorId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.ToUpperInvariant())
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return "0000000000000000";
        }

        ulong hash = 14695981039346656037UL;
        foreach (var id in ids)
        {
            foreach (var character in id)
            {
                hash ^= character;
                hash *= 1099511628211UL;
            }

            hash ^= '|';
            hash *= 1099511628211UL;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    public string GetTopologyHash() => ComputeTopologyHash(GetMonitors());

    private static bool IsNativeFailure(Exception exception) =>
        exception is NativeDisplayException
            or DllNotFoundException
            or BadImageFormatException
            or EntryPointNotFoundException
            or FileLoadException;
}
