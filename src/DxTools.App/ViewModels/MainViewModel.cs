using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Security;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly EngineHost _engine;
    private readonly AutostartService _autostart;

    private string _statusMessage = string.Empty;
    private string _backupStatusMessage = string.Empty;
    private bool _restartRequired;
    private NativeProbeResult _native;
    private bool _safeMode;
    private DateTimeOffset _checkedAt = DateTimeOffset.MinValue;

    public MainViewModel(
        SettingsStore settingsStore,
        AppSettings settings,
        EngineHost engine,
        AutostartService autostart,
        Func<string, bool>? confirmDisplayChange = null)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _autostart = autostart ?? throw new ArgumentNullException(nameof(autostart));

        Display = new DisplayViewModel(
            engine.Display,
            engine.Profiles,
            settingsStore,
            settings,
            confirmDisplayChange ?? (_ => true));
        Wallpaper = new WallpaperViewModel(engine.Display, engine.Wallpaper, settingsStore, settings);
        WindowProfiles = new WindowProfilesViewModel(engine.WindowPlacements, engine.Display);

        _native = engine.Status.Native;
        _safeMode = engine.Status.SafeMode;
        _checkedAt = engine.Status.CheckedAt;
        _engine.StatusChanged += OnEngineStatusChanged;

        OpenLogsCommand = new RelayCommand(OpenLogs);
        RefreshPinnedApps();
        OpenRepositoryCommand = new RelayCommand(OpenRepository);
        SelfCheckCommand = new RelayCommand(RunSelfCheck);
        ExportBackupCommand = new RelayCommand(ExportBackup);
        AddPinnedAppCommand = new RelayCommand(AddPinnedApp);
        ChangeHotkeyCommand = new RelayCommand(parameter =>
        {
            if (parameter is HotkeyInfo hotkey)
            {
                HotkeyChangeRequested?.Invoke(hotkey.Action);
            }
        });
        ImportBackupCommand = new RelayCommand(ImportBackup);
    }

    public Action<AppTheme>? ThemeChanged { get; set; }

    public event Action? BackupImported;

    public event Action? TaskbarSettingsChanged;

    public event Action<HotkeyAction>? HotkeyChangeRequested;

    public event Action? PinnedAppsChanged;

    public DisplayViewModel Display { get; }

    public WallpaperViewModel Wallpaper { get; }

    public WindowProfilesViewModel WindowProfiles { get; }

    public IReadOnlyList<HotkeyInfo> Hotkeys { get; private set; } = Array.Empty<HotkeyInfo>();

    public void SetHotkeys(IEnumerable<HotkeyInfo> hotkeys)
    {
        Hotkeys = hotkeys.ToList();
        OnPropertyChanged(nameof(Hotkeys));
    }

    public ICommand OpenLogsCommand { get; }

    public ICommand OpenRepositoryCommand { get; }

    public ICommand SelfCheckCommand { get; }

    public ICommand ExportBackupCommand { get; }

    public ICommand ChangeHotkeyCommand { get; }

    public ICommand AddPinnedAppCommand { get; }

    public ObservableCollection<PinnedAppItemViewModel> PinnedApps { get; } = [];

    public ICommand ImportBackupCommand { get; }

    public string VersionText => DxToolsInfo.InformationalVersion;

    public string LicenseText => $"{Strings.LicenseName} ({DxToolsInfo.LicenseId})";

    public string CopyrightText => DxToolsInfo.Copyright;

    public string RepositoryUrl => DxToolsInfo.RepositoryUrl;

    public string LogDirectory => AppPaths.LogDirectory;

    public string PhaseNote => Strings.Phase0Note;

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows == value)
            {
                return;
            }

            var previous = _settings.StartWithWindows;
            try
            {
                _autostart.SetEnabled(value);
                _settings.StartWithWindows = value;
                OnPropertyChanged();
                SaveSettings();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                or IOException
                or SecurityException
                or InvalidOperationException)
            {
                _settings.StartWithWindows = previous;
                OnPropertyChanged();
                StatusMessage = Strings.AutostartFailed;
                DxLog.Exception("Failed to update autostart.", ex);
            }
        }
    }

    public bool StartMinimized
    {
        get => _settings.StartMinimized;
        set
        {
            if (_settings.StartMinimized == value)
            {
                return;
            }

            _settings.StartMinimized = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool TaskbarAutoHide
    {
        get => _settings.TaskbarAutoHide;
        set
        {
            if (_settings.TaskbarAutoHide == value)
            {
                return;
            }

            _settings.TaskbarAutoHide = value;
            OnPropertyChanged();
            SaveSettings();
            TaskbarSettingsChanged?.Invoke();
        }
    }

    public bool EnableSecondaryTaskbar
    {
        get => _settings.EnableSecondaryTaskbar;
        set
        {
            if (_settings.EnableSecondaryTaskbar == value)
            {
                return;
            }

            _settings.EnableSecondaryTaskbar = value;
            OnPropertyChanged();
            SaveSettings();
            TaskbarSettingsChanged?.Invoke();
        }
    }

    public AppLanguage Language
    {
        get => _settings.Language;
        set
        {
            if (_settings.Language == value)
            {
                return;
            }

            _settings.Language = value;
            OnPropertyChanged();
            RestartRequired = true;
            SaveSettings();
        }
    }

    public AppTheme Theme
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme == value)
            {
                return;
            }

            _settings.Theme = value;
            OnPropertyChanged();
            SaveSettings();
            ThemeChanged?.Invoke(value);
        }
    }

    public bool RestartRequired
    {
        get => _restartRequired;
        private set => SetProperty(ref _restartRequired, value);
    }

    public string BackupStatusMessage
    {
        get => _backupStatusMessage;
        private set => SetProperty(ref _backupStatusMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool NativeAvailable => _native.IsAvailable;

    public bool IsHealthy => !_safeMode && _native.IsHealthy;

    public bool SafeMode => _safeMode;

    public string StatusText => _safeMode
        ? Strings.StatusSafeMode
        : IsHealthy ? Strings.StatusReady : Strings.StatusUnavailable;

    public string NativeStatusText => _safeMode
        ? Strings.SafeMode
        : _native.IsAvailable ? Strings.NativeAvailable : Strings.NativeUnavailable;

    public string ApiVersionText => _native.IsAvailable
        ? _native.ApiVersion.ToString(CultureInfo.InvariantCulture)
        : Strings.NotAvailable;

    public string LibraryVersionText => _native.LibraryVersion ?? Strings.NotAvailable;

    public string PingText => !_native.IsAvailable
        ? Strings.NotAvailable
        : _native.PingOk ? Strings.PingOk : Strings.PingFailed;

    public string NativePath => _native.LibraryPath;

    public string CheckedAtText => _checkedAt == DateTimeOffset.MinValue
        ? Strings.NotAvailable
        : _checkedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private void OnEngineStatusChanged(object? sender, EngineStatus status)
    {
        _native = status.Native;
        _safeMode = status.SafeMode;
        _checkedAt = status.CheckedAt;

        OnPropertyChanged(nameof(NativeAvailable));
        OnPropertyChanged(nameof(IsHealthy));
        OnPropertyChanged(nameof(SafeMode));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(NativeStatusText));
        OnPropertyChanged(nameof(ApiVersionText));
        OnPropertyChanged(nameof(LibraryVersionText));
        OnPropertyChanged(nameof(PingText));
        OnPropertyChanged(nameof(NativePath));
        OnPropertyChanged(nameof(CheckedAtText));
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
            StatusMessage = Strings.SettingsSaved;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            StatusMessage = Strings.SaveFailed;
            DxLog.Exception("Failed to save settings.", ex);
        }
    }

    private void OpenLogs()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            Process.Start(new ProcessStartInfo(AppPaths.LogDirectory)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception)
        {
            StatusMessage = Strings.LogFolderUnavailable;
            DxLog.Exception("Failed to open the log folder.", ex);
        }
    }

    private void OpenRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo(DxToolsInfo.RepositoryUrl)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            DxLog.Exception("Failed to open the repository URL.", ex);
        }
    }

    public void Dispose()
    {
        _engine.StatusChanged -= OnEngineStatusChanged;
        Wallpaper.Dispose();
    }

    private void RefreshPinnedApps()
    {
        PinnedApps.Clear();
        foreach (var app in _settings.PinnedApps)
        {
            PinnedApps.Add(new PinnedAppItemViewModel(app, RemovePinnedApp));
        }
    }

    private void AddPinnedApp()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Strings.PinnedAppsTitle,
            Filter = "Applications|*.exe|All files|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (_settings.PinnedApps.Any(app => string.Equals(app.Path, dialog.FileName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _settings.PinnedApps.Add(new PinnedApp
        {
            Path = dialog.FileName,
            Name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName),
        });

        SaveSettings();
        RefreshPinnedApps();
        PinnedAppsChanged?.Invoke();
    }

    private void RemovePinnedApp(PinnedApp app)
    {
        if (_settings.PinnedApps.Remove(app))
        {
            SaveSettings();
            RefreshPinnedApps();
            PinnedAppsChanged?.Invoke();
        }
    }

    private void RunSelfCheck()
    {
        _engine.Refresh();
        StatusMessage = Strings.SelfCheckDone;
    }

    private void ExportBackup()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = Strings.BackupTitle,
            Filter = "DxTools backup|*.zip|All files|*.*",
            FileName = $"DxTools-backup-{DateTime.Now:yyyyMMdd}.zip",
            AddExtension = true,
            DefaultExt = ".zip",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SettingsBackupService.Export(dialog.FileName);
            BackupStatusMessage = Strings.BackupExported;
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or NotSupportedException)
        {
            BackupStatusMessage = $"{Strings.BackupFailed} {ex.Message}";
            DxLog.Exception("Settings export failed.", ex);
        }
    }

    private void ImportBackup()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Strings.BackupTitle,
            Filter = "DxTools backup|*.zip|All files|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SettingsBackupService.Import(dialog.FileName);
            BackupStatusMessage = Strings.BackupImported;
            BackupImported?.Invoke();
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or NotSupportedException)
        {
            BackupStatusMessage = $"{Strings.BackupFailed} {ex.Message}";
            DxLog.Exception("Settings import failed.", ex);
        }
    }
}
