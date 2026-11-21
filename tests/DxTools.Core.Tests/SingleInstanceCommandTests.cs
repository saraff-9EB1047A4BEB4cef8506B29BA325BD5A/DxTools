using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class SingleInstanceCommandTests
{
    [Fact]
    public void CommandMessage_RoundTripsArguments()
    {
        string[] arguments =
        [
            "--wallpaper",
            @"C:\Images\wall paper.png",
            "--monitor",
            @"\\.\DISPLAY2",
        ];

        var message = SingleInstance.CreateCommandMessage(arguments);

        Assert.True(SingleInstance.TryParseCommandMessage(message, out var parsed));
        Assert.Equal(arguments, parsed);
    }

    [Fact]
    public void TryParse_PlainMessage_ReturnsFalse()
    {
        Assert.False(SingleInstance.TryParseCommandMessage("show", out var parsed));
        Assert.Empty(parsed);
    }

    [Fact]
    public void TryParse_InvalidPayload_ReturnsFalse()
    {
        Assert.False(SingleInstance.TryParseCommandMessage("cmd:not-base64", out var parsed));
        Assert.Empty(parsed);
    }

    [Fact]
    public void CommandMessage_EmptyArguments_ReturnsFalse()
    {
        var message = SingleInstance.CreateCommandMessage([]);

        Assert.False(SingleInstance.TryParseCommandMessage(message, out var parsed));
        Assert.Empty(parsed);
    }
}
