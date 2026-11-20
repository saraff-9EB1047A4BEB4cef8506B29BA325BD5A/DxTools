using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed class WindowDisplayItemViewModel
{
    public WindowDisplayItemViewModel(AppWindow window, string monitorLabel)
    {
        Window = window;
        ProcessName = string.IsNullOrWhiteSpace(window.ExecutablePath)
            ? $"PID {window.ProcessId}"
            : Path.GetFileNameWithoutExtension(window.ExecutablePath);
        MonitorLabel = monitorLabel;
    }

    public AppWindow Window { get; }

    public string Title => Window.Title;

    public string ProcessName { get; }

    public string MonitorLabel { get; }

    public string GeometryText => $"{Window.Bounds.Width}×{Window.Bounds.Height} @ {Window.Bounds.Left},{Window.Bounds.Top}";
}

public sealed class WindowProfileItemViewModel
{
    public WindowProfileItemViewModel(WindowPlacementProfile profile)
    {
        Profile = profile;
    }

    public WindowPlacementProfile Profile { get; }

    public string Name => Profile.Name;

    public string Summary => Profile.Summary;

    public string CreatedText => Profile.CreatedAt.ToLocalTime().ToString("g");
}

public sealed class WindowProfilesViewModel : ObservableObject
{
    private readonly WindowPlacementService _service;
    private readonly DisplayService _display;
    private string _newProfileName = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public WindowProfilesViewModel(WindowPlacementService service, DisplayService display)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _display = display ?? throw new ArgumentNullException(nameof(display));

        SaveProfileCommand = new RelayCommand(SaveProfile);
        RefreshCommand = new RelayCommand(Refresh);
        ApplyProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is WindowPlacementProfile profile)
            {
                ApplyProfile(profile);
            }
        });
        DeleteProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is WindowPlacementProfile profile)
            {
                DeleteProfile(profile);
            }
        });

        Refresh();
    }

    public ObservableCollection<WindowDisplayItemViewModel> Windows { get; } = [];

    public ObservableCollection<WindowProfileItemViewModel> Profiles { get; } = [];

    public ICommand SaveProfileCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand ApplyProfileCommand { get; }

    public ICommand DeleteProfileCommand { get; }

    public string NewProfileName
    {
        get => _newProfileName;
        set => SetProperty(ref _newProfileName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool HasProfiles => Profiles.Count > 0;

    public void Refresh()
    {
        var monitors = _display.GetMonitors();
        var windows = _service.GetCurrentWindows();

        Windows.Clear();
        foreach (var window in windows)
        {
            var monitorLabel = window.MonitorIndex >= 0 && window.MonitorIndex < monitors.Count
                ? monitors[window.MonitorIndex].DisplayName
                : string.Empty;

            Windows.Add(new WindowDisplayItemViewModel(window, monitorLabel));
        }

        ReloadProfiles();
    }

    private void SaveProfile()
    {
        if (IsBusy)
        {
            return;
        }

        var result = _service.Capture(NewProfileName);
        StatusMessage = result.Success
            ? Strings.WindowProfileSaved
            : $"{Strings.WindowProfileSaveFailed} {result.Error}";

        if (result.Success)
        {
            NewProfileName = string.Empty;
            ReloadProfiles();
        }
    }

    private void ApplyProfile(WindowPlacementProfile profile)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var result = _service.Apply(profile);
            StatusMessage = result.Success
                ? $"{Strings.WindowProfileApplied} {result.Applied}/{result.Total}"
                : $"{Strings.WindowProfileApplyFailed} {result.Error}";
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    private void DeleteProfile(WindowPlacementProfile profile)
    {
        if (IsBusy)
        {
            return;
        }

        var result = _service.Delete(profile.Id);
        StatusMessage = result.Success
            ? Strings.SettingsSaved
            : $"{Strings.WindowProfileSaveFailed} {result.Error}";
        ReloadProfiles();
    }

    private void ReloadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _service.Profiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Profiles.Add(new WindowProfileItemViewModel(profile));
        }

        OnPropertyChanged(nameof(HasProfiles));
    }
}
