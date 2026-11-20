namespace DxTools.Core;

public enum HotkeyAction
{
    MoveWindowPrevious,
    MoveWindowNext,
    ToggleTopmost,
}

public sealed class HotkeySetting
{
    public HotkeyAction Action { get; set; }

    public uint Modifiers { get; set; }

    public uint VirtualKey { get; set; }

    public string Gesture { get; set; } = string.Empty;

    public uint PrefixModifiers { get; set; }

    public uint PrefixVirtualKey { get; set; }

    public string PrefixGesture { get; set; } = string.Empty;

    public static List<HotkeySetting> CreateDefaults() =>
    [
        new() { Action = HotkeyAction.MoveWindowPrevious, Modifiers = 0x4003, VirtualKey = 0x25, Gesture = "Ctrl+Alt+Left" },
        new() { Action = HotkeyAction.MoveWindowNext, Modifiers = 0x4003, VirtualKey = 0x27, Gesture = "Ctrl+Alt+Right" },
        new() { Action = HotkeyAction.ToggleTopmost, Modifiers = 0x4003, VirtualKey = 0x26, Gesture = "Ctrl+Alt+Up" },
    ];
}
