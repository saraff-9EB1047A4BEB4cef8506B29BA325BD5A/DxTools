using DxTools.Core;

namespace DxTools.Core.Tests;

public sealed class HotkeySettingsTests
{
    [Fact]
    public void Normalize_MigratesLegacyPrefixAndMainCombination()
    {
        var settings = new AppSettings
        {
            Hotkeys =
            [
                new HotkeySetting
                {
                    Action = HotkeyAction.MoveWindowNext,
                    Modifiers = 0x4003,
                    VirtualKey = 0x27,
                    Gesture = "Ctrl+Alt+Right",
                    PrefixModifiers = 0x4002,
                    PrefixVirtualKey = 0x4B,
                    PrefixGesture = "Ctrl+K",
                },
            ],
        };

        settings.Normalize();

        var hotkey = settings.Hotkeys.Single(item => item.Action == HotkeyAction.MoveWindowNext);
        Assert.Equal(2, hotkey.Steps.Count);
        Assert.Equal("Ctrl+K", hotkey.Steps[0].Gesture);
        Assert.Equal("Ctrl+Alt+Right", hotkey.Steps[1].Gesture);
        Assert.Equal("Ctrl+K", hotkey.PrefixGesture);
    }

    [Fact]
    public void Normalize_KeepsFourStepSequenceAndTruncatesMore()
    {
        var settings = new AppSettings
        {
            Hotkeys =
            [
                new HotkeySetting
                {
                    Action = HotkeyAction.ToggleTopmost,
                    Steps = Enumerable.Range(0, 6)
                        .Select(index => new HotkeyStep
                        {
                            Modifiers = 0x4002,
                            VirtualKey = (uint)(0x41 + index),
                            Gesture = $"Ctrl+{index}",
                        })
                        .ToList(),
                },
            ],
        };

        settings.Normalize();

        var hotkey = settings.Hotkeys.Single(item => item.Action == HotkeyAction.ToggleTopmost);
        Assert.Equal(HotkeySetting.MaxSteps, hotkey.Steps.Count);
    }

    [Fact]
    public void Normalize_ReplacesInvalidSequenceWithDefault()
    {
        var settings = new AppSettings
        {
            Hotkeys =
            [
                new HotkeySetting
                {
                    Action = HotkeyAction.MoveWindowPrevious,
                    Steps = [new HotkeyStep { VirtualKey = 0x25, Gesture = "Left" }],
                },
            ],
        };

        settings.Normalize();

        var hotkey = settings.Hotkeys.Single(item => item.Action == HotkeyAction.MoveWindowPrevious);
        Assert.Equal("Ctrl+Alt+Left", hotkey.Steps.Single().Gesture);
    }

    [Theory]
    [InlineData(@"C:\Tools\MyApp.exe", "MyApp.exe")]
    [InlineData("notepad", "notepad.exe")]
    [InlineData("  ", null)]
    public void NormalizeProcessName_StripsPathAndAddsExtension(string input, string? expected)
    {
        Assert.Equal(expected, HotkeySetting.NormalizeProcessName(input));
    }

    [Fact]
    public void FindConflict_DetectsSameSequenceWithOverlappingScope()
    {
        var first = new HotkeySetting
        {
            Action = HotkeyAction.MoveWindowNext,
            Steps = [new HotkeyStep { Modifiers = 0x4002, VirtualKey = 0x27, Gesture = "Ctrl+Right" }],
        };
        var second = new HotkeySetting
        {
            Action = HotkeyAction.MoveWindowPrevious,
            TargetProcess = "notepad.exe",
            Steps = [new HotkeyStep { Modifiers = 0x4002, VirtualKey = 0x27, Gesture = "Ctrl+Right" }],
        };

        Assert.Same(first, HotkeySequence.FindConflict([first, second], second));
    }

    [Fact]
    public void FindConflict_AllowsSameSequenceForDifferentApplications()
    {
        var first = new HotkeySetting
        {
            Action = HotkeyAction.MoveWindowNext,
            TargetProcess = "notepad.exe",
            Steps = [new HotkeyStep { Modifiers = 0x4002, VirtualKey = 0x27, Gesture = "Ctrl+Right" }],
        };
        var second = new HotkeySetting
        {
            Action = HotkeyAction.MoveWindowPrevious,
            TargetProcess = "devenv.exe",
            Steps = [new HotkeyStep { Modifiers = 0x4002, VirtualKey = 0x27, Gesture = "Ctrl+Right" }],
        };

        Assert.Null(HotkeySequence.FindConflict([first, second], second));
    }

    [Fact]
    public void SettingsStore_RoundTripsSequenceAndApplicationScope()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(Path.Combine(folder.Path, "settings.json"));
        var settings = new AppSettings
        {
            Hotkeys =
            [
                new HotkeySetting
                {
                    Action = HotkeyAction.ToggleTopmost,
                    TargetProcess = "notepad.exe",
                    Steps =
                    [
                        new HotkeyStep { Modifiers = 0x4002, VirtualKey = 0x4B, Gesture = "Ctrl+K" },
                        new HotkeyStep { Modifiers = 0x4006, VirtualKey = 0x54, Gesture = "Ctrl+Shift+T" },
                        new HotkeyStep { Modifiers = 0x4002, VirtualKey = 0x42, Gesture = "Ctrl+B" },
                    ],
                },
            ],
        };

        store.Save(settings);
        var loaded = store.Load().Settings;
        var hotkey = loaded.Hotkeys.Single(item => item.Action == HotkeyAction.ToggleTopmost);

        Assert.Equal(3, hotkey.Steps.Count);
        Assert.Equal("notepad.exe", hotkey.TargetProcess);
        Assert.Equal("Ctrl+Shift+T", hotkey.Steps[1].Gesture);
    }
}
