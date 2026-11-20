using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed record TriggerOption<T>(T Value, string Label)
{
    public override string ToString() => Label;
}

public sealed class TriggerItemViewModel : ObservableObject
{
    private readonly TriggerDefinition _trigger;
    private readonly TriggerService _service;

    public TriggerItemViewModel(TriggerDefinition trigger, TriggerService service, Action<TriggerDefinition> remove)
    {
        _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        DeleteCommand = new RelayCommand(() => remove(_trigger));

        Events =
        [
            new(TriggerEventType.Startup, Strings.TriggerEventStartup),
            new(TriggerEventType.DisplayChanged, Strings.TriggerEventDisplay),
            new(TriggerEventType.WindowCreated, Strings.TriggerEventWindow),
            new(TriggerEventType.ProcessStarted, Strings.TriggerEventProcess),
        ];

        Actions =
        [
            new(TriggerActionType.ApplyDisplayProfile, Strings.TriggerActionDisplay),
            new(TriggerActionType.ApplyWindowProfile, Strings.TriggerActionWindow),
            new(TriggerActionType.ApplyWallpaper, Strings.TriggerActionWallpaper),
            new(TriggerActionType.RunProgram, Strings.TriggerActionProgram),
            new(TriggerActionType.RunScript, Strings.TriggerActionScript),
        ];
    }

    public IReadOnlyList<TriggerOption<TriggerEventType>> Events { get; }

    public ICommand DeleteCommand { get; }

    public IReadOnlyList<TriggerOption<TriggerActionType>> Actions { get; }

    public string Name
    {
        get => _trigger.Name;
        set
        {
            if (_trigger.Name == value) return;
            _trigger.Name = value;
            OnPropertyChanged();
            Save();
        }
    }

    public bool Enabled
    {
        get => _trigger.Enabled;
        set
        {
            if (_trigger.Enabled == value) return;
            _trigger.Enabled = value;
            OnPropertyChanged();
            Save();
        }
    }

    public TriggerEventType Event
    {
        get => _trigger.Event;
        set
        {
            if (_trigger.Event == value) return;
            _trigger.Event = value;
            OnPropertyChanged();
            Save();
        }
    }

    public TriggerActionType Action
    {
        get => _trigger.Action;
        set
        {
            if (_trigger.Action == value) return;
            _trigger.Action = value;
            OnPropertyChanged();
            Save();
        }
    }

    public string Condition
    {
        get => _trigger.Condition;
        set
        {
            var normalized = value ?? string.Empty;
            if (_trigger.Condition == normalized) return;
            _trigger.Condition = normalized;
            OnPropertyChanged();
            Save();
        }
    }

    public string Parameter
    {
        get => _trigger.Parameter;
        set
        {
            var normalized = value ?? string.Empty;
            if (_trigger.Parameter == normalized) return;
            _trigger.Parameter = normalized;
            OnPropertyChanged();
            Save();
        }
    }

    public TriggerDefinition Trigger => _trigger;

    private void Save() => _service.Save();
}

public sealed class TriggersViewModel : ObservableObject
{
    private readonly TriggerService _service;
    private string _statusMessage = string.Empty;

    public TriggersViewModel(TriggerService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        AddCommand = new RelayCommand(AddTrigger);
        OpenScriptsFolderCommand = new RelayCommand(OpenScriptsFolder);
        RefreshTriggers();
    }

    public ObservableCollection<TriggerItemViewModel> Triggers { get; } = [];

    public ICommand AddCommand { get; }

    public ICommand OpenScriptsFolderCommand { get; }

    public string ScriptsDirectory => _service.ScriptsDirectory;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasTriggers => Triggers.Count > 0;

    public void RefreshTriggers()
    {
        Triggers.Clear();
        foreach (var trigger in _service.Triggers.OrderBy(trigger => trigger.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Triggers.Add(new TriggerItemViewModel(trigger, _service, RemoveTrigger));
        }

        OnPropertyChanged(nameof(HasTriggers));
    }

    private void AddTrigger()
    {
        var trigger = new TriggerDefinition
        {
            Name = Strings.NewTrigger,
            Event = TriggerEventType.Startup,
            Action = TriggerActionType.ApplyWallpaper,
        };

        _service.Add(trigger);
        RefreshTriggers();
        StatusMessage = Strings.SettingsSaved;
    }

    private void RemoveTrigger(TriggerDefinition trigger)
    {
        _service.Remove(trigger);
        RefreshTriggers();
        StatusMessage = Strings.SettingsSaved;
    }

    private void OpenScriptsFolder()
    {
        try
        {
            Directory.CreateDirectory(ScriptsDirectory);
            Process.Start(new ProcessStartInfo(ScriptsDirectory) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            StatusMessage = Strings.LogFolderUnavailable;
            DxLog.Exception("Failed to open the scripts folder.", ex);
        }
    }
}
