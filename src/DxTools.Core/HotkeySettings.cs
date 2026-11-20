namespace DxTools.Core;

public enum HotkeyAction
{
    MoveWindowPrevious,
    MoveWindowNext,
    ToggleTopmost,
    SnapToSplit1,
    SnapToSplit2,
    SnapToSplit3,
    SnapToSplit4,
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
        new() { Action = HotkeyAction.SnapToSplit1, Modifiers = 0x4003, VirtualKey = 0x31, Gesture = "Ctrl+Alt+1" },
        new() { Action = HotkeyAction.SnapToSplit2, Modifiers = 0x4003, VirtualKey = 0x32, Gesture = "Ctrl+Alt+2" },
        new() { Action = HotkeyAction.SnapToSplit3, Modifiers = 0x4003, VirtualKey = 0x33, Gesture = "Ctrl+Alt+3" },
        new() { Action = HotkeyAction.SnapToSplit4, Modifiers = 0x4003, VirtualKey = 0x34, Gesture = "Ctrl+Alt+4" },
    ];
}
