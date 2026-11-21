using System.Runtime.InteropServices;
using DxTools.Interop;

namespace DxTools.Core.Tests;

public sealed class NativeApiTests
{
    [Fact]
    public void Probe_ReturnsStructuredResult()
    {
        var result = NativeApi.Probe();

        Assert.NotNull(result);
        Assert.EndsWith(NativeLibraryInfo.FileName, result.LibraryPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HotkeyBindingLayout_MatchesNativeV6()
    {
        Assert.Equal(4, NativeHotkeyBinding.MaxSteps);
        Assert.Equal(176, Marshal.SizeOf<NativeHotkeyBinding>());
        Assert.Equal(6u, NativeLibraryInfo.ExpectedApiVersion);
    }

    [Fact]
    public void HotkeyBinding_MarshalsStepsAndProcessName()
    {
        var binding = new NativeHotkeyBinding
        {
            Id = 7,
            StepCount = 2,
            Steps =
            [
                new NativeHotkeyStep { Modifiers = 0x4002, VirtualKey = 0x4B },
                new NativeHotkeyStep { Modifiers = 0x4003, VirtualKey = 0x27 },
                default,
                default,
            ],
            Scope = NativeHotkey.ProcessScope,
            ProcessName = "notepad.exe",
            Reserved = 0,
        };

        var size = Marshal.SizeOf<NativeHotkeyBinding>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(binding, buffer, fDeleteOld: false);
            var roundTrip = Marshal.PtrToStructure<NativeHotkeyBinding>(buffer);

            Assert.Equal(7u, roundTrip.Id);
            Assert.Equal(2u, roundTrip.StepCount);
            Assert.Equal(0x4Bu, roundTrip.Steps[0].VirtualKey);
            Assert.Equal(0x27u, roundTrip.Steps[1].VirtualKey);
            Assert.Equal("notepad.exe", roundTrip.ProcessName);
            Assert.Equal(NativeHotkey.ProcessScope, roundTrip.Scope);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
