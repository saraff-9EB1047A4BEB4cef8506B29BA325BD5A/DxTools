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
    DesktopPrevious,
    DesktopNext,
    MoveWindowToPreviousDesktop,
    MoveWindowToNextDesktop,
}

public sealed class HotkeyStep
{
    public uint Modifiers { get; set; }

    public uint VirtualKey { get; set; }

    public string Gesture { get; set; } = string.Empty;

    public HotkeyStep Clone() => new()
    {
        Modifiers = Modifiers,
        VirtualKey = VirtualKey,
        Gesture = Gesture,
    };
}

public sealed class HotkeySetting
{
    public const int MaxSteps = 4;

    public const uint ModifierMask = 0x000F;

    public HotkeyAction Action { get; set; }

    public List<HotkeyStep> Steps { get; set; } = [];

    public string? TargetProcess { get; set; }

    public uint Modifiers { get; set; }

    public uint VirtualKey { get; set; }

    public string Gesture { get; set; } = string.Empty;

    public uint PrefixModifiers { get; set; }

    public uint PrefixVirtualKey { get; set; }

    public string PrefixGesture { get; set; } = string.Empty;

    public static List<HotkeySetting> CreateDefaults() =>
    [
        Create(HotkeyAction.MoveWindowPrevious, 0x4003, 0x25, "Ctrl+Alt+Left"),
        Create(HotkeyAction.MoveWindowNext, 0x4003, 0x27, "Ctrl+Alt+Right"),
        Create(HotkeyAction.ToggleTopmost, 0x4003, 0x26, "Ctrl+Alt+Up"),
        Create(HotkeyAction.SnapToSplit1, 0x4003, 0x31, "Ctrl+Alt+1"),
        Create(HotkeyAction.SnapToSplit2, 0x4003, 0x32, "Ctrl+Alt+2"),
        Create(HotkeyAction.SnapToSplit3, 0x4003, 0x33, "Ctrl+Alt+3"),
        Create(HotkeyAction.SnapToSplit4, 0x4003, 0x34, "Ctrl+Alt+4"),
        Create(HotkeyAction.DesktopPrevious, 0x4003, 0x21, "Ctrl+Alt+PageUp"),
        Create(HotkeyAction.DesktopNext, 0x4003, 0x22, "Ctrl+Alt+PageDown"),
        Create(HotkeyAction.MoveWindowToPreviousDesktop, 0x4007, 0x21, "Ctrl+Alt+Shift+PageUp"),
        Create(HotkeyAction.MoveWindowToNextDesktop, 0x4007, 0x22, "Ctrl+Alt+Shift+PageDown"),
    ];

    public static string? NormalizeProcessName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var fileName = Path.GetFileName(trimmed);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
        {
            fileName += ".exe";
        }

        return fileName;
    }

    public void NormalizeSteps(IReadOnlyList<HotkeyStep> fallback)
    {
        Steps ??= [];

        if (Steps.Count == 0)
        {
            if (PrefixVirtualKey != 0)
            {
                Steps.Add(new HotkeyStep
                {
                    Modifiers = PrefixModifiers,
                    VirtualKey = PrefixVirtualKey,
                    Gesture = PrefixGesture,
                });
            }

            if (VirtualKey != 0)
            {
                Steps.Add(new HotkeyStep
                {
                    Modifiers = Modifiers,
                    VirtualKey = VirtualKey,
                    Gesture = Gesture,
                });
            }
        }

        Steps.RemoveAll(step => step is null
            || step.VirtualKey == 0
            || (step.Modifiers & ModifierMask) == 0);

        if (Steps.Count > MaxSteps)
        {
            Steps.RemoveRange(MaxSteps, Steps.Count - MaxSteps);
        }

        if (Steps.Count == 0 && fallback.Count > 0)
        {
            Steps.AddRange(fallback.Select(step => step.Clone()));
        }

        foreach (var step in Steps.Where(step => string.IsNullOrWhiteSpace(step.Gesture)))
        {
            step.Gesture = HotkeySequence.FormatSingle(step);
        }

        var last = Steps[^1];
        Modifiers = last.Modifiers;
        VirtualKey = last.VirtualKey;
        Gesture = last.Gesture;

        if (Steps.Count >= 2)
        {
            PrefixModifiers = Steps[0].Modifiers;
            PrefixVirtualKey = Steps[0].VirtualKey;
            PrefixGesture = Steps[0].Gesture;
        }
        else
        {
            PrefixModifiers = 0;
            PrefixVirtualKey = 0;
            PrefixGesture = string.Empty;
        }
    }

    private static HotkeySetting Create(
        HotkeyAction action,
        uint modifiers,
        uint virtualKey,
        string gesture) =>
        new()
        {
            Action = action,
            Modifiers = modifiers,
            VirtualKey = virtualKey,
            Gesture = gesture,
            Steps =
            [
                new HotkeyStep
                {
                    Modifiers = modifiers,
                    VirtualKey = virtualKey,
                    Gesture = gesture,
                },
            ],
        };
}

public static class HotkeySequence
{
    public static string Format(IEnumerable<HotkeyStep> steps) =>
        string.Join(" → ", steps.Select(FormatSingle));

    public static string FormatSingle(HotkeyStep step) =>
        string.IsNullOrWhiteSpace(step.Gesture)
            ? $"Key 0x{step.VirtualKey:X2}"
            : step.Gesture;

    public static bool StepsEqual(IReadOnlyList<HotkeyStep> left, IReadOnlyList<HotkeyStep> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if ((left[index].Modifiers & HotkeySetting.ModifierMask)
                    != (right[index].Modifiers & HotkeySetting.ModifierMask)
                || left[index].VirtualKey != right[index].VirtualKey)
            {
                return false;
            }
        }

        return true;
    }

    public static bool ScopesOverlap(string? left, string? right) =>
        string.IsNullOrWhiteSpace(left)
        || string.IsNullOrWhiteSpace(right)
        || string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    public static HotkeySetting? FindConflict(
        IEnumerable<HotkeySetting> hotkeys,
        HotkeySetting candidate) =>
        hotkeys.FirstOrDefault(hotkey =>
            hotkey.Action != candidate.Action
            && ScopesOverlap(hotkey.TargetProcess, candidate.TargetProcess)
            && StepsEqual(hotkey.Steps, candidate.Steps));
}
