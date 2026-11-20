using DxTools.Core;
using DxTools.Engine;

namespace DxTools.Core.Tests;

public sealed class TopologyHashTests
{
    private static readonly MonitorInfo[] Monitors =
    [
        new("\\\\.\\DISPLAY1", "MONITOR\\A", "Left", 0, 0, 1920, 1080, true, 96, 60, 32),
        new("\\\\.\\DISPLAY2", "MONITOR\\B", "Right", 1920, 0, 2560, 1440, false, 144, 60, 32),
    ];

    [Fact]
    public void Hash_IsStableRegardlessOfOrder()
    {
        var service = new DisplayService(safeMode: true);

        var first = service.ComputeTopologyHash(Monitors);
        var second = service.ComputeTopologyHash(Monitors.Reverse());

        Assert.Equal(first, second);
        Assert.Equal(16, first.Length);
    }

    [Fact]
    public void Hash_ChangesWhenMonitorSetChanges()
    {
        var service = new DisplayService(safeMode: true);

        var first = service.ComputeTopologyHash(Monitors);
        var second = service.ComputeTopologyHash([Monitors[0]]);

        Assert.NotEqual(first, second);
    }
}
