namespace DxTools.Core;

public sealed record HotkeyInfo(
    HotkeyAction Action,
    string Gesture,
    string PrefixGesture,
    string Description,
    bool Registered,
    string? TargetProcess = null)
{
    public string DisplayGesture => string.IsNullOrWhiteSpace(TargetProcess)
        ? Gesture
        : $"{Gesture} ({TargetProcess})";
}
