using System.Collections.ObjectModel;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed class DesktopIconProfileItemViewModel
{
    public DesktopIconProfileItemViewModel(DesktopIconProfile profile)
    {
        Profile = profile;
    }

    public DesktopIconProfile Profile { get; }

    public string Name => Profile.Name;

    public string Summary => $"{Profile.Icons.Count} icons";

    public string CreatedText => Profile.CreatedAt.ToLocalTime().ToString("g");
}

public sealed class DesktopIconsViewModel : ObservableObject
{
    private readonly DesktopIconManager _manager;
    private string _newProfileName = string.Empty;
    private string _statusMessage = string.Empty;
    private int _iconCount;

    public DesktopIconsViewModel(DesktopIconManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        SaveProfileCommand = new RelayCommand(SaveProfile);
        RefreshCommand = new RelayCommand(Refresh);
        ApplyProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is DesktopIconProfile profile)
            {
                ApplyProfile(profile);
            }
        });
        DeleteProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is DesktopIconProfile profile)
            {
                DeleteProfile(profile);
            }
        });

        Refresh();
    }

    public ObservableCollection<DesktopIconProfileItemViewModel> Profiles { get; } = [];

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

    public int IconCount
    {
        get => _iconCount;
        private set => SetProperty(ref _iconCount, value);
    }

    public bool HasProfiles => Profiles.Count > 0;

    public void Refresh()
    {
        var result = _manager.GetCurrentCount();
        IconCount = result.Count;
        if (result.Error is not null)
        {
            StatusMessage = result.Error;
        }

        ReloadProfiles();
    }

    private void SaveProfile()
    {
        var result = _manager.Capture(NewProfileName);
        StatusMessage = result.Success
            ? Strings.DesktopProfileSaved
            : $"{Strings.DesktopProfileSaveFailed} {result.Error}";

        if (result.Success)
        {
            NewProfileName = string.Empty;
            ReloadProfiles();
        }
    }

    private void ApplyProfile(DesktopIconProfile profile)
    {
        var result = _manager.Apply(profile);
        StatusMessage = result.Success
            ? $"{Strings.DesktopProfileApplied} {result.Applied}"
            : $"{Strings.DesktopProfileApplyFailed} {result.Error}";
    }

    private void DeleteProfile(DesktopIconProfile profile)
    {
        var result = _manager.Delete(profile.Id);
        StatusMessage = result.Success
            ? Strings.SettingsSaved
            : $"{Strings.DesktopProfileSaveFailed} {result.Error}";
        ReloadProfiles();
    }

    private void ReloadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _manager.Profiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Profiles.Add(new DesktopIconProfileItemViewModel(profile));
        }

        OnPropertyChanged(nameof(HasProfiles));
    }
}
