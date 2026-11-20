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
}
