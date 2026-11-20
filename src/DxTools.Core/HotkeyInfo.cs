namespace DxTools.Core;

public sealed record HotkeyInfo(
    HotkeyAction Action,
    string Gesture,
    string PrefixGesture,
    string Description,
    bool Registered)
{
    public string DisplayGesture => string.IsNullOrWhiteSpace(PrefixGesture)
        ? Gesture
        : $"{PrefixGesture} > {Gesture}";
}
