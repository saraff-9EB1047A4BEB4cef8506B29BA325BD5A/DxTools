namespace DxTools.Core;

public enum TriggerEventType
{
    Startup,
    DisplayChanged,
    WindowCreated,
    ProcessStarted,
}

public enum TriggerActionType
{
    ApplyDisplayProfile,
    ApplyWindowProfile,
    ApplyWallpaper,
    RunProgram,
    RunScript,
}

public sealed class TriggerDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "New trigger";

    public bool Enabled { get; set; } = true;

    public TriggerEventType Event { get; set; } = TriggerEventType.Startup;

    public TriggerActionType Action { get; set; } = TriggerActionType.ApplyWallpaper;

    public string Condition { get; set; } = string.Empty;

    public string Parameter { get; set; } = string.Empty;
}
