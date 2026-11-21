using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.Core.Tests;

public sealed class WindowMatcherTests
{
    [Fact]
    public void NormalizeButtonName_RemovesWindowCount()
    {
        Assert.Equal("Visual Studio", WindowMatcher.NormalizeButtonName("  Visual   Studio (3) "));
        Assert.Equal("Блокнот", WindowMatcher.NormalizeButtonName("Блокнот —1 запущенное окно"));
        Assert.Equal("App", WindowMatcher.NormalizeButtonName("App — 2 running windows"));
    }

    [Fact]
    public void NormalizeAutomationId_RemovesAppIdPrefix()
    {
        Assert.Equal("MSEdge", WindowMatcher.NormalizeAutomationId("Appid: MSEdge"));
        Assert.Equal(string.Empty, WindowMatcher.NormalizeAutomationId(null));
    }

    [Fact]
    public void Match_ExactTitleWins()
    {
        var first = Create("Report - Notepad", @"C:\Apps\notepad.exe", 1);
        var second = Create("Other document", @"C:\Apps\other.exe", 2);

        var match = WindowMatcher.Match([first, second], "Report - Notepad", null);

        Assert.Single(match);
        Assert.Equal(first.Handle, match[0].Handle);
    }

    [Fact]
    public void Match_ApplicationNameReturnsAllWindowsOfExecutable()
    {
        var first = Create("Document 1", @"C:\Apps\notepad.exe", 1);
        var second = Create("Document 2", @"C:\Apps\notepad.exe", 2);
        var other = Create("Document 3", @"C:\Apps\other.exe", 3);

        var match = WindowMatcher.Match([first, second, other], "notepad", null);

        Assert.Equal(2, match.Count);
        Assert.DoesNotContain(other, match);
    }

    [Fact]
    public void Match_AutomationIdMapsToExecutable()
    {
        var first = Create("Document 1", @"C:\Apps\chrome.exe", 1);
        var other = Create("Document 2", @"C:\Apps\other.exe", 2);

        var match = WindowMatcher.Match([first, other], "Google Chrome", "Appid: chrome");

        Assert.Single(match);
        Assert.Equal(first.Handle, match[0].Handle);
    }

    [Fact]
    public void Match_UnknownNameReturnsEmpty()
    {
        var window = Create("Document 1", @"C:\Apps\notepad.exe", 1);

        Assert.Empty(WindowMatcher.Match([window], "Unknown", null));
        Assert.Empty(WindowMatcher.Match([window], null, null));
        Assert.Empty(WindowMatcher.Match([], "Notepad", null));
    }

    private static AppWindow Create(string title, string path, int index)
    {
        return new AppWindow(
            new IntPtr(index),
            title,
            100 + (uint)index,
            path,
            0,
            new WindowRect { Left = 0, Top = 0, Right = 800, Bottom = 600 },
            IsMinimized: false,
            IsMaximized: false,
            IsForeground: false);
    }
}
