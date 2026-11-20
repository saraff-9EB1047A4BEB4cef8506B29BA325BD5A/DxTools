using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed class DisplayProfileItemViewModel : ObservableObject
{
    private readonly Func<DisplayProfile, bool, bool> _autoApplyChanged;

    public DisplayProfileItemViewModel(DisplayProfile profile, Func<DisplayProfile, bool, bool> autoApplyChanged)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _autoApplyChanged = autoApplyChanged ?? throw new ArgumentNullException(nameof(autoApplyChanged));
    }

    public DisplayProfile Profile { get; }

    public string Name => Profile.Name;

    public string MonitorSummary => Profile.MonitorSummary;

    public string CreatedText => Profile.CreatedAt.ToLocalTime().ToString("g");

    public bool AutoApply
    {
        get => Profile.AutoApply;
        set
        {
            if (Profile.AutoApply == value)
            {
                return;
            }

            _ = _autoApplyChanged(Profile, value);
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(MonitorSummary));
        OnPropertyChanged(nameof(CreatedText));
        OnPropertyChanged(nameof(AutoApply));
    }
}

public sealed class DisplayViewModel : ObservableObject
{
    private readonly DisplayService _display;
    private readonly DisplayProfileService _profiles;
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly Func<string, bool> _confirmDisplayChange;

    private string _newProfileName = string.Empty;
    private bool _newProfileAutoApply;
    private string _statusMessage = string.Empty;
    private string _topologyHash = string.Empty;
    private bool _isBusy;

    public DisplayViewModel(
        DisplayService display,
        DisplayProfileService profiles,
        SettingsStore settingsStore,
        AppSettings settings,
        Func<string, bool> confirmDisplayChange)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _confirmDisplayChange = confirmDisplayChange ?? throw new ArgumentNullException(nameof(confirmDisplayChange));

        SaveProfileCommand = new RelayCommand(SaveProfile);
        RefreshCommand = new RelayCommand(Refresh);
        ApplyProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is DisplayProfile profile)
            {
                _ = ApplyProfileAsync(profile);
            }
        });
        DeleteProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is DisplayProfile profile)
            {
                DeleteProfile(profile);
            }
        });

        Refresh();
    }

    public ObservableCollection<MonitorInfo> Monitors { get; } = [];

    public ObservableCollection<DisplayProfileItemViewModel> Profiles { get; } = [];

    public ICommand SaveProfileCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand ApplyProfileCommand { get; }

    public ICommand DeleteProfileCommand { get; }

    public string NewProfileName
    {
        get => _newProfileName;
        set => SetProperty(ref _newProfileName, value);
    }

    public bool NewProfileAutoApply
    {
        get => _newProfileAutoApply;
        set => SetProperty(ref _newProfileAutoApply, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string TopologyHash
    {
        get => _topologyHash;
        private set => SetProperty(ref _topologyHash, value);
    }

    public int MonitorCount => Monitors.Count;

    public bool HasProfiles => Profiles.Count > 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public void Refresh()
    {
        var monitors = _display.GetMonitors();

        Monitors.Clear();
        foreach (var monitor in monitors)
        {
            Monitors.Add(monitor);
        }

        TopologyHash = _display.ComputeTopologyHash(monitors);
        OnPropertyChanged(nameof(MonitorCount));

        ReloadProfiles();

        if (monitors.Count == 0) { StatusMessage = Strings.DisplayNoMonitors; }
    }

    public async void NotifyTopologyChanged()
    {
        if (_profiles.IsSuppressed || IsBusy)
        {
            return;
        }

        try
        {
            var topologyHash = _display.GetTopologyHash();
            var result = await Task.Run(() => _profiles.TryAutoApply(topologyHash)).ConfigureAwait(true);
            if (result is not null)
            {
                StatusMessage = result.Success
                    ? Strings.DisplayApplied
                    : $"{Strings.DisplayApplyFailed} {result.Error}";
            }
        }
        catch (Exception ex)
        {
            DxLog.Exception("Auto-apply display profile failed.", ex);
        }
        finally
        {
            Refresh();
        }
    }

    private void SaveProfile()
    {
        if (IsBusy)
        {
            return;
        }

        var result = _profiles.CaptureCurrent(NewProfileName, NewProfileAutoApply);
        StatusMessage = result.Success ? Strings.DisplayProfileSaved : $"{Strings.DisplayProfileSaveFailed} {result.Error}";

        if (result.Success)
        {
            NewProfileName = string.Empty;
            NewProfileAutoApply = false;
            ReloadProfiles();
        }

        SaveSettings();
    }

    private async Task ApplyProfileAsync(DisplayProfile profile)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = Strings.DisplayBusy;

        try
        {
            var result = await Task.Run(() => _profiles.Apply(profile)).ConfigureAwait(true);

            if (!result.Success)
            {
                StatusMessage = $"{Strings.DisplayApplyFailed} {result.Error}";
                return;
            }

            var keep = _confirmDisplayChange(profile.Name);
            if (!keep && result.RollbackState is not null)
            {
                var rollback = await Task.Run(() => _profiles.Rollback(result.RollbackState)).ConfigureAwait(true);
                StatusMessage = rollback.Success
                    ? Strings.DisplayReverted
                    : $"{Strings.DisplayApplyFailed} {rollback.Error}";
            }
            else
            {
                StatusMessage = Strings.DisplayApplied;
            }
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    private void DeleteProfile(DisplayProfile profile)
    {
        if (IsBusy)
        {
            return;
        }

        var result = _profiles.Delete(profile.Id);
        StatusMessage = result.Success ? Strings.SettingsSaved : $"{Strings.DisplayProfileSaveFailed} {result.Error}";
        ReloadProfiles();
    }

    private bool SetAutoApply(DisplayProfile profile, bool autoApply)
    {
        var result = _profiles.SetAutoApply(profile.Id, autoApply);
        if (!result.Success)
        {
            StatusMessage = $"{Strings.DisplayProfileSaveFailed} {result.Error}";
            return false;
        }

        StatusMessage = Strings.SettingsSaved;
        return true;
    }

    private void ReloadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profiles.Profiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Profiles.Add(new DisplayProfileItemViewModel(profile, SetAutoApply));
        }

        OnPropertyChanged(nameof(HasProfiles));
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            DxLog.Exception("Failed to save display settings.", ex);
        }
    }
}
