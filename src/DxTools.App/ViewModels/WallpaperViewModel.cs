using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;
using Forms = System.Windows.Forms;

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

public sealed class WallpaperViewModel : ObservableObject, IDisposable
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

    private readonly DisplayService _display;
    private readonly WallpaperService _wallpaper;
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _slideshowTimer;
    private readonly RelayCommand _stopSlideshowCommand;

    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool _isSlideshowRunning;
    private bool _frameApplying;
    private int _slideIndex;

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
        ChooseSlideshowFolderCommand = new RelayCommand(ChooseSlideshowFolder);
        ClearSlideshowFolderCommand = new RelayCommand(() => SlideshowFolder = string.Empty);
        StartSlideshowCommand = new RelayCommand(StartSlideshow);
        _stopSlideshowCommand = new RelayCommand(StopSlideshow, () => IsSlideshowRunning);
        StopSlideshowCommand = _stopSlideshowCommand;

        _slideshowTimer = new DispatcherTimer();
        _slideshowTimer.Tick += OnSlideshowTick;

        RefreshMonitors();
    }

    public ObservableCollection<WallpaperMonitorItemViewModel> Monitors { get; } = [];

    public ICommand ApplyCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand ChooseSlideshowFolderCommand { get; }

    public ICommand ClearSlideshowFolderCommand { get; }

    public ICommand StartSlideshowCommand { get; }

    public ICommand StopSlideshowCommand { get; }

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

    public string SlideshowFolder
    {
        get => _settings.Wallpaper.SlideshowFolder ?? string.Empty;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (string.Equals(_settings.Wallpaper.SlideshowFolder, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            StopSlideshow();
            _settings.Wallpaper.SlideshowFolder = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SlideshowFolderDisplay));
            SaveSettings();
        }
    }

    public string SlideshowFolderDisplay => string.IsNullOrWhiteSpace(SlideshowFolder)
        ? Strings.NoImage
        : SlideshowFolder;

    public int SlideshowIntervalSeconds
    {
        get => _settings.Wallpaper.SlideshowIntervalSeconds;
        set
        {
            var normalized = Math.Clamp(value, 10, 86400);
            if (_settings.Wallpaper.SlideshowIntervalSeconds == normalized)
            {
                return;
            }

            _settings.Wallpaper.SlideshowIntervalSeconds = normalized;
            OnPropertyChanged();
            if (IsSlideshowRunning)
            {
                _slideshowTimer.Interval = TimeSpan.FromSeconds(normalized);
            }

            SaveSettings();
        }
    }

    public bool IsSlideshowRunning
    {
        get => _isSlideshowRunning;
        private set
        {
            if (SetProperty(ref _isSlideshowRunning, value))
            {
                _stopSlideshowCommand.RaiseCanExecuteChanged();
            }
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
        if (!string.IsNullOrWhiteSpace(SlideshowFolder))
        {
            StartSlideshow();
            return;
        }

        await ApplyStaticAsync();
    }

    private async Task ApplyStaticAsync()
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

    private void ChooseSlideshowFolder()
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = Strings.SlideshowTitle,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            SlideshowFolder = dialog.SelectedPath;
        }
    }

    private void StartSlideshow()
    {
        var folder = SlideshowFolder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            StatusMessage = Strings.NoSlideshowImages;
            return;
        }

        if (GetSlideshowFiles(folder).Count == 0)
        {
            StatusMessage = Strings.NoSlideshowImages;
            return;
        }

        _slideshowTimer.Interval = TimeSpan.FromSeconds(SlideshowIntervalSeconds);
        _slideshowTimer.Start();
        IsSlideshowRunning = true;
        StatusMessage = Strings.SlideshowStarted;
        _ = ApplySlideshowFrameAsync();
    }

    private void StopSlideshow()
    {
        _slideshowTimer.Stop();
        if (IsSlideshowRunning)
        {
            IsSlideshowRunning = false;
            StatusMessage = Strings.SlideshowStopped;
        }
    }

    private async void OnSlideshowTick(object? sender, EventArgs e)
    {
        await ApplySlideshowFrameAsync();
    }

    private async Task ApplySlideshowFrameAsync()
    {
        if (_frameApplying)
        {
            return;
        }

        var folder = SlideshowFolder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            StopSlideshow();
            return;
        }

        var files = GetSlideshowFiles(folder);
        if (files.Count == 0)
        {
            StopSlideshow();
            StatusMessage = Strings.NoSlideshowImages;
            return;
        }

        var monitors = _display.GetMonitors();
        if (monitors.Count == 0)
        {
            StopSlideshow();
            StatusMessage = Strings.DisplayNoMonitors;
            return;
        }

        var frame = new WallpaperSettings
        {
            FitMode = _settings.Wallpaper.FitMode,
            SlideshowFolder = folder,
            SlideshowIntervalSeconds = SlideshowIntervalSeconds,
        };

        for (var index = 0; index < monitors.Count; index++)
        {
            var monitor = monitors[index];
            var key = string.IsNullOrWhiteSpace(monitor.MonitorId) ? monitor.DeviceName : monitor.MonitorId;
            frame.MonitorImages[key] = files[(_slideIndex + index) % files.Count];
        }

        _slideIndex = (_slideIndex + monitors.Count) % files.Count;
        _frameApplying = true;

        try
        {
            var result = await Task.Run(() => _wallpaper.Apply(frame)).ConfigureAwait(true);
            StatusMessage = result.Success
                ? Strings.WallpaperApplied
                : $"{Strings.WallpaperFailed} {result.Error}";
        }
        finally
        {
            _frameApplying = false;
        }
    }

    private static List<string> GetSlideshowFiles(string folder)
    {
        try
        {
            return Directory
                .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => ImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DxLog.Warn($"Could not enumerate slideshow folder '{folder}': {ex.Message}");
            return [];
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

    public void Dispose()
    {
        _slideshowTimer.Stop();
        _slideshowTimer.Tick -= OnSlideshowTick;
    }
}
