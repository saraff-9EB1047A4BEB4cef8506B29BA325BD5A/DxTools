using System.Diagnostics;
using System.IO;
using DxTools.Core;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DxTools.Engine;

public sealed class TriggerService : IDisposable
{
    private readonly object _gate = new();
    private readonly object _pollGate = new();
    private readonly DisplayService _display;
    private readonly DisplayProfileService _profiles;
    private readonly WindowPlacementService _windowPlacements;
    private readonly WallpaperService _wallpaper;
    private readonly AppSettings _settings;
    private readonly TriggerStore _store;
    private readonly WindowCatalog _catalog;
    private readonly List<TriggerDefinition> _triggers;
    private readonly HashSet<IntPtr> _seenWindows = [];
    private readonly HashSet<uint> _seenProcesses = [];
    private Timer? _pollTimer;
    private bool _disposed;

    public TriggerService(
        DisplayService display,
        DisplayProfileService profiles,
        WindowPlacementService windowPlacements,
        WallpaperService wallpaper,
        AppSettings settings,
        TriggerStore? store = null)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _windowPlacements = windowPlacements ?? throw new ArgumentNullException(nameof(windowPlacements));
        _wallpaper = wallpaper ?? throw new ArgumentNullException(nameof(wallpaper));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _store = store ?? new TriggerStore();
        _catalog = new WindowCatalog(display);
        _triggers = _store.Load().ToList();
    }

    public IReadOnlyList<TriggerDefinition> Triggers => _triggers;

    public string ScriptsDirectory => Path.Combine(AppPaths.AppDataDirectory, "scripts");

    public void Start()
    {
        Directory.CreateDirectory(ScriptsDirectory);
        CaptureSeenState();
        Raise(TriggerEventType.Startup);
        _pollTimer = new Timer(_ => Poll(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public void Add(TriggerDefinition trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        lock (_gate)
        {
            _triggers.Add(trigger);
        }

        Save();
    }

    public void Remove(TriggerDefinition trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        lock (_gate)
        {
            _triggers.Remove(trigger);
        }

        Save();
    }

    public void Save()
    {
        try
        {
            lock (_gate)
            {
                _store.Save(_triggers);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            DxLog.Exception("Failed to save triggers.", ex);
        }
    }

    public void Raise(TriggerEventType eventType, string? processName = null)
    {
        List<TriggerDefinition> matches;
        lock (_gate)
        {
            matches = _triggers
                .Where(trigger => trigger.Enabled && trigger.Event == eventType)
                .Where(trigger => MatchesCondition(trigger, processName))
                .ToList();
        }

        foreach (var trigger in matches)
        {
            _ = Task.Run(() => Execute(trigger, processName));
        }
    }

    public bool ApplyDisplayProfile(string nameOrId)
    {
        var profile = _profiles.Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Id, nameOrId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile.Name, nameOrId, StringComparison.CurrentCultureIgnoreCase));

        if (profile is null)
        {
            DxLog.Warn($"Trigger/script: display profile '{nameOrId}' was not found.");
            return false;
        }

        var result = _profiles.Apply(profile);
        DxLog.Info(result.Success
            ? $"Trigger/script: display profile '{profile.Name}' applied."
            : $"Trigger/script: display profile '{profile.Name}' failed: {result.Error}");
        return result.Success;
    }

    public bool ApplyWindowProfile(string nameOrId)
    {
        var profile = _windowPlacements.Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Id, nameOrId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile.Name, nameOrId, StringComparison.CurrentCultureIgnoreCase));

        if (profile is null)
        {
            DxLog.Warn($"Trigger/script: window profile '{nameOrId}' was not found.");
            return false;
        }

        var result = _windowPlacements.Apply(profile);
        DxLog.Info(result.Success
            ? $"Trigger/script: window profile '{profile.Name}' applied: {result.Applied}/{result.Total}."
            : $"Trigger/script: window profile '{profile.Name}' failed: {result.Error}");
        return result.Success;
    }

    public void ApplyWallpaper()
    {
        var result = _wallpaper.Apply(_settings.Wallpaper);
        DxLog.Info(result.Success
            ? "Trigger/script: wallpaper applied."
            : $"Trigger/script: wallpaper failed: {result.Error}");
    }

    public void RunProgram(string fileName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            DxLog.Exception($"Trigger/script: failed to start '{fileName}'.", ex);
        }
    }

    public IReadOnlyList<MonitorInfo> GetMonitors() => _display.GetMonitors();

    public async Task RunScriptAsync(string path)
    {
        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(ScriptsDirectory, path);

        if (!File.Exists(fullPath))
        {
            DxLog.Warn($"Trigger/script: script '{fullPath}' was not found.");
            return;
        }

        try
        {
            var code = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
            var options = ScriptOptions.Default
                .WithReferences(
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(DxToolsInfo).Assembly,
                    typeof(TriggerService).Assembly)
                .WithImports("System", "System.Linq", "DxTools.Core");

            var globals = new DxScriptGlobals(this);
            await CSharpScript.RunAsync(code, options, globals, typeof(DxScriptGlobals)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DxLog.Exception($"Trigger/script: failed to run '{fullPath}'.", ex);
        }
    }

    private static bool MatchesCondition(TriggerDefinition trigger, string? processName)
    {
        if (string.IsNullOrWhiteSpace(trigger.Condition))
        {
            return true;
        }

        if (trigger.Event is not (TriggerEventType.WindowCreated or TriggerEventType.ProcessStarted))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(processName)
            && processName.Contains(trigger.Condition, StringComparison.OrdinalIgnoreCase);
    }

    private void Execute(TriggerDefinition trigger, string? processName)
    {
        try
        {
            switch (trigger.Action)
            {
                case TriggerActionType.ApplyDisplayProfile:
                    _ = ApplyDisplayProfile(trigger.Parameter);
                    break;
                case TriggerActionType.ApplyWindowProfile:
                    _ = ApplyWindowProfile(trigger.Parameter);
                    break;
                case TriggerActionType.ApplyWallpaper:
                    ApplyWallpaper();
                    break;
                case TriggerActionType.RunProgram:
                    RunProgram(trigger.Parameter);
                    break;
                case TriggerActionType.RunScript:
                    _ = RunScriptAsync(trigger.Parameter);
                    break;
            }

            DxLog.Info($"Trigger '{trigger.Name}' executed for {trigger.Event}.");
        }
        catch (Exception ex)
        {
            DxLog.Exception($"Trigger '{trigger.Name}' failed.", ex);
        }
    }

    private void CaptureSeenState()
    {
        lock (_pollGate)
        {
            foreach (var window in _catalog.GetWindows())
            {
                _seenWindows.Add(window.Handle);
            }

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    _seenProcesses.Add((uint)process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }

    private void Poll()
    {
        if (_disposed)
        {
            return;
        }

        lock (_pollGate)
        {
            try
            {
                var hasWindowTriggers = HasEnabledTrigger(TriggerEventType.WindowCreated);
                var hasProcessTriggers = HasEnabledTrigger(TriggerEventType.ProcessStarted);

                if (hasWindowTriggers)
                {
                    foreach (var window in _catalog.GetWindows())
                    {
                        if (_seenWindows.Add(window.Handle))
                        {
                            var name = string.IsNullOrWhiteSpace(window.ExecutablePath)
                                ? null
                                : Path.GetFileNameWithoutExtension(window.ExecutablePath);
                            Raise(TriggerEventType.WindowCreated, name);
                        }
                    }
                }

                if (hasProcessTriggers)
                {
                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            if (_seenProcesses.Add((uint)process.Id))
                            {
                                Raise(TriggerEventType.ProcessStarted, process.ProcessName);
                            }
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DxLog.Exception("Trigger polling failed.", ex);
            }
        }
    }

    private bool HasEnabledTrigger(TriggerEventType eventType)
    {
        lock (_gate)
        {
            return _triggers.Any(trigger => trigger.Enabled && trigger.Event == eventType);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pollTimer?.Dispose();
    }
}

public sealed class DxScriptGlobals
{
    private readonly TriggerService _service;

    public DxScriptGlobals(TriggerService service)
    {
        _service = service;
    }

    public IReadOnlyList<MonitorInfo> Monitors => _service.GetMonitors();

    public void Log(string message) => DxLog.Info($"Script: {message}");

    public bool ApplyDisplayProfile(string nameOrId) => _service.ApplyDisplayProfile(nameOrId);

    public bool ApplyWindowProfile(string nameOrId) => _service.ApplyWindowProfile(nameOrId);

    public void ApplyWallpaper() => _service.ApplyWallpaper();

    public void Run(string fileName) => _service.RunProgram(fileName);
}
