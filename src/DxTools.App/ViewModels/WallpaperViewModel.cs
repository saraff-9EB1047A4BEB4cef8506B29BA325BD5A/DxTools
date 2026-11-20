using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;
using Microsoft.Win32;

namespace DxTools.App.ViewModels;

public sealed class WallpaperMonitorItemViewModel : ObservableObject
{
    private readonly WallpaperSettings _settings;
    private readonly Action _save;
    private readonly Action<string> _setStatus;

    public WallpaperMonitorItemViewModel(
        MonitorInfo monitor,
        WallpaperSettings settings,
        Action save,
        Action<string> setStatus)
    {
        Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _setStatus = setStatus ?? throw new ArgumentNullException(nameof(setStatus));

        BrowseCommand = new RelayCommand(Browse);
        ClearCommand = new RelayCommand(Clear, () => HasImage);
    }

    public MonitorInfo Monitor { get; }

    public string DisplayName => Monitor.DisplayName;

    public string GeometryText => $"{Monitor.GeometryText} · {Monitor.DpiText}";

    public string ImagePath => _settings.MonitorImages.TryGetValue(Key, out var path) ? path : string.Empty;

    public string ImageDisplay
    {
        get
        {
            var path = ImagePath;
            return string.IsNullOrWhiteSpace(path) ? Strings.NoImage : Path.GetFileName(path);
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public ICommand BrowseCommand { get; }

    public ICommand ClearCommand { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(ImagePath));
        OnPropertyChanged(nameof(ImageDisplay));
        OnPropertyChanged(nameof(HasImage));
    }

    private string Key => string.IsNullOrWhiteSpace(Monitor.MonitorId) ? Monitor.DeviceName : Monitor.MonitorId;

    private void Browse()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Strings.ChooseImage,
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _settings.MonitorImages[Key] = dialog.FileName;
        _settings.Normalize();
        _save();
        Refresh();
        _setStatus(Strings.SettingsSaved);
    }

    private void Clear()
    {
        if (_settings.MonitorImages.Remove(Key))
        {
            _save();
            Refresh();
            _setStatus(Strings.SettingsSaved);
        }
    }
}

public sealed class WallpaperViewModel : ObservableObject
{
    private readonly DisplayService _display;
    private readonly WallpaperService _wallpaper;
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public WallpaperViewModel(
        DisplayService display,
        WallpaperService wallpaper,
        SettingsStore settingsStore,
        AppSettings settings)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _wallpaper = wallpaper ?? throw new ArgumentNullException(nameof(wallpaper));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        ApplyCommand = new RelayCommand(() => _ = ApplyAsync());
        RefreshCommand = new RelayCommand(RefreshMonitors);

        RefreshMonitors();
    }

    public ObservableCollection<WallpaperMonitorItemViewModel> Monitors { get; } = [];

    public ICommand ApplyCommand { get; }

    public ICommand RefreshCommand { get; }

    public WallpaperFitMode FitMode
    {
        get => _settings.Wallpaper.FitMode;
        set
        {
            if (_settings.Wallpaper.FitMode == value)
            {
                return;
            }

            _settings.Wallpaper.FitMode = value;
            OnPropertyChanged();
            SaveSettings();
        }
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

    public void RefreshMonitors()
    {
        var monitors = _display.GetMonitors();

        Monitors.Clear();
        foreach (var monitor in monitors)
        {
            Monitors.Add(new WallpaperMonitorItemViewModel(
                monitor,
                _settings.Wallpaper,
                SaveSettings,
                message => StatusMessage = message));
        }
    }

    private async Task ApplyAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = Strings.WallpaperApplying;

        try
        {
            SaveSettings();
            var result = await Task.Run(() => _wallpaper.Apply(_settings.Wallpaper)).ConfigureAwait(true);
            StatusMessage = result.Success
                ? Strings.WallpaperApplied
                : $"{Strings.WallpaperFailed} {result.Error}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            StatusMessage = Strings.SaveFailed;
            DxLog.Exception("Failed to save wallpaper settings.", ex);
        }
    }
}
