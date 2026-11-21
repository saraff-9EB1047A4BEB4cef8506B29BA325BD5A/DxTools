using System.Globalization;
using System.IO;
using System.Windows.Threading;
using DxTools.App.Properties;
using DxTools.App.Services;
using DxTools.App.ViewModels;
using DxTools.Core;
using DxTools.Engine;

namespace DxTools.App;

public partial class App : System.Windows.Application
{
    private SingleInstance? _singleInstance;
    private SettingsStore? _settingsStore;
    private AppSettings? _settings;
    private EngineHost? _engine;
    private MainWindow? _window;
    private TrayIconHost? _tray;
    private HotkeyService? _hotkeys;
    private DisplayChangeWatcher? _displayWatcher;
    private TaskbarManager? _taskbarManager;
    private TitleBarOverlayManager? _titleBarOverlay;
    private MainViewModel? _viewModel;
    private bool _isExiting;
    private bool _suppressSaveOnExit;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            Bootstrap(e.Args);
        }
        catch (Exception ex)
        {
            DxLog.Exception("Fatal startup error.", ex);
            ShowError(
                Strings.CrashTitle,
                $"{Strings.CrashText}{Environment.NewLine}{Environment.NewLine}{ex.Message}");
            Shutdown(1);
        }
    }

    private void Bootstrap(string[] args)
    {
        var options = CommandLineOptions.Parse(args);

        DxLog.Initialize(AppPaths.LogDirectory);
        DxLog.Info(
            $"Starting {DxToolsInfo.ProductName} {DxToolsInfo.InformationalVersion}. " +
            $"autostart={options.Autostart}, safeMode={options.SafeMode}, " +
            $"settings='{options.SettingsPath ?? AppPaths.SettingsFile}'.");

        foreach (var error in options.Errors)
        {
            DxLog.Warn(error);
        }

        var settingsStore = new SettingsStore(options.SettingsPath);
        var loadResult = settingsStore.Load();
        ApplyLanguage(loadResult.Settings.Language);

        if (loadResult.RecoveredFromCorruption)
        {
            DxLog.Warn($"Settings were recovered from a corrupt file: {loadResult.Error}");
        }

        if (options.ShowHelp)
        {
            ShowInfo(Strings.AppTitle, Strings.HelpText);
            Shutdown(0);
            return;
        }

        if (options.ShowVersion)
        {
            ShowInfo(
                Strings.AppTitle,
                $"{Strings.Version}: {DxToolsInfo.InformationalVersion}{Environment.NewLine}" +
                $"{Strings.License}: {DxToolsInfo.LicenseId}");
            Shutdown(0);
            return;
        }

        _singleInstance = new SingleInstance();
        if (!_singleInstance.IsFirstInstance)
        {
            _singleInstance.TrySignalExisting(message: BuildSingleInstanceMessage(options, args));
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown(0);
            return;
        }

        if (options.RequestExit)
        {
            DxLog.Info("Exit requested, but no other DxTools instance is running.");
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown(0);
            return;
        }

        _singleInstance.ShowRequested += OnShowRequested;
        _singleInstance.ExitRequested += OnExitRequested;
        _singleInstance.CommandRequested += OnCommandRequested;
        _singleInstance.StartServer();

        _settingsStore = settingsStore;
        _settings = loadResult.Settings;

        var autostart = new AutostartService();
        _settings.StartWithWindows = autostart.IsEnabled();

        ThemeManager.Apply(_settings.Theme);

        _engine = new EngineHost(options.SafeMode, _settings);
        _engine.Start();

        if (!options.SafeMode)
        {
            _engine.Triggers.Start();
        }
        ApplyCommandLineActions(options);

        var viewModel = new MainViewModel(settingsStore, _settings, _engine, autostart, DisplayConfirmationWindow.Confirm);
        _viewModel = viewModel;
        viewModel.BackupImported += () => _suppressSaveOnExit = true;
        viewModel.ThemeChanged += theme =>
        {
            ThemeManager.Apply(theme);
            _window?.ApplyTheme(theme);
        };

        _window = new MainWindow(viewModel);
        _window.Closing += OnMainWindowClosing;

        _tray = new TrayIconHost(_window, _engine, ExitApplication);

        var windowHandle = new System.Windows.Interop.WindowInteropHelper(_window).EnsureHandle();
        if (!options.SafeMode)
        {
            _hotkeys = new HotkeyService(windowHandle, _settings);
            _hotkeys.Pressed += OnHotkeyPressed;
            viewModel.SetHotkeys(_hotkeys.Bindings);

            _taskbarManager = new TaskbarManager(
                _engine.Display,
                _settings,
                new TaskbarIconCache(),
                ShowMainWindow,
                safeMode: false,
                uiAccess: _engine.UIAccess);
            viewModel.TaskbarSettingsChanged += () => _taskbarManager?.Rebuild();
            viewModel.HotkeyChangeRequested += OnHotkeyChangeRequested;
            viewModel.HotkeyPrefixChangeRequested += OnHotkeyPrefixChangeRequested;
            viewModel.HotkeyPrefixClearRequested += OnHotkeyPrefixClearRequested;
            viewModel.PinnedAppsChanged += () => _taskbarManager?.Rebuild();

            _titleBarOverlay = new TitleBarOverlayManager(_engine.Windows, _settings);
            viewModel.TitleBarSettingsChanged += () => _titleBarOverlay?.Refresh();

            _displayWatcher = new DisplayChangeWatcher(
                windowHandle,
                () =>
                {
                    viewModel.Display.NotifyTopologyChanged();
                    _taskbarManager?.Rebuild();
                    _engine?.Triggers.Raise(TriggerEventType.DisplayChanged);
                });

            _taskbarManager.Rebuild();
        }

        var showWindow = options.ShowSettings
            || (!HasCommandLineAction(options)
                && (!_settings.FirstRunCompleted
                    || !options.Autostart
                    || !_settings.StartMinimized));

        if (showWindow)
        {
            ShowMainWindow();
        }

        if (!_settings.FirstRunCompleted)
        {
            _settings.FirstRunCompleted = true;
            SaveSettings();
            _tray.ShowFirstRunBalloon();
        }
    }

    private void ApplyCommandLineActions(CommandLineOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApplyDisplayProfile) && _engine is not null)
        {
            var profile = _engine.Profiles.Profiles.FirstOrDefault(item =>
                string.Equals(item.Id, options.ApplyDisplayProfile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Name, options.ApplyDisplayProfile, StringComparison.CurrentCultureIgnoreCase));

            if (profile is null)
            {
                DxLog.Warn($"Display profile '{options.ApplyDisplayProfile}' was not found.");
            }
            else
            {
                var result = _engine.Profiles.Apply(profile);
                DxLog.Info(result.Success
                    ? $"Display profile '{profile.Name}' applied from the command line."
                    : $"Display profile '{profile.Name}' failed: {result.Error}");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ApplyWindowProfile) && _engine is not null)
        {
            var profile = _engine.WindowPlacements.Profiles.FirstOrDefault(item =>
                string.Equals(item.Id, options.ApplyWindowProfile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Name, options.ApplyWindowProfile, StringComparison.CurrentCultureIgnoreCase));

            if (profile is null)
            {
                DxLog.Warn($"Window profile '{options.ApplyWindowProfile}' was not found.");
            }
            else
            {
                var result = _engine.WindowPlacements.Apply(profile);
                DxLog.Info(result.Success
                    ? $"Window profile '{profile.Name}' applied from the command line: {result.Applied}/{result.Total}."
                    : $"Window profile '{profile.Name}' failed: {result.Error}");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.WallpaperPath))
        {
            ApplyWallpaperFile(options.WallpaperPath, options.WallpaperMonitor);
        }
        else if (options.ApplyWallpaper && _settings is not null && _engine is not null)
        {
            var result = _engine.Wallpaper.Apply(_settings.Wallpaper);
            DxLog.Info(result.Success
                ? "Wallpaper applied from the command line."
                : $"Wallpaper command failed: {result.Error}");
        }
    }

    private void ApplyWallpaperFile(string path, string? monitorSelector)
    {
        if (_settings is null || _engine is null)
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            DxLog.Warn($"Wallpaper path '{path}' is invalid: {ex.Message}");
            return;
        }

        if (!File.Exists(fullPath))
        {
            DxLog.Warn($"Wallpaper file '{fullPath}' does not exist.");
            return;
        }

        var monitors = _engine.Display.GetMonitors();
        if (monitors.Count == 0)
        {
            DxLog.Warn("Wallpaper command failed: no monitors were detected.");
            return;
        }

        MonitorInfo? monitor = null;
        if (!string.IsNullOrWhiteSpace(monitorSelector))
        {
            monitor = monitors.FirstOrDefault(item =>
                string.Equals(item.DeviceName, monitorSelector, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.MonitorId, monitorSelector, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.FriendlyName, monitorSelector, StringComparison.OrdinalIgnoreCase));

            if (monitor is null)
            {
                DxLog.Warn($"Monitor '{monitorSelector}' was not found; using the primary monitor.");
            }
        }

        monitor ??= monitors.FirstOrDefault(item => item.IsPrimary) ?? monitors[0];
        var key = string.IsNullOrWhiteSpace(monitor.MonitorId)
            ? monitor.DeviceName
            : monitor.MonitorId;
        if (string.IsNullOrWhiteSpace(key))
        {
            DxLog.Warn("Wallpaper command failed: monitor has no identifier.");
            return;
        }

        var hasConfiguredImage = _settings.Wallpaper.MonitorImages.Values
            .Any(value => !string.IsNullOrWhiteSpace(value));
        if (hasConfiguredImage)
        {
            _settings.Wallpaper.MonitorImages[key] = fullPath;
        }
        else
        {
            foreach (var item in monitors)
            {
                var itemKey = string.IsNullOrWhiteSpace(item.MonitorId)
                    ? item.DeviceName
                    : item.MonitorId;
                if (!string.IsNullOrWhiteSpace(itemKey))
                {
                    _settings.Wallpaper.MonitorImages[itemKey] = fullPath;
                }
            }
        }

        _settings.Wallpaper.Normalize();
        SaveSettings();
        _viewModel?.Wallpaper.RefreshMonitors();

        var result = _engine.Wallpaper.Apply(_settings.Wallpaper);
        var targetText = hasConfiguredImage ? monitor.DisplayName : "all monitors";
        DxLog.Info(result.Success
            ? $"Wallpaper '{fullPath}' applied to {targetText} from the command line."
            : $"Wallpaper file command failed: {result.Error}");
    }

    private static bool HasCommandLineAction(CommandLineOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApplyDisplayProfile)
        || !string.IsNullOrWhiteSpace(options.ApplyWindowProfile)
        || options.ApplyWallpaper
        || !string.IsNullOrWhiteSpace(options.WallpaperPath);

    private static string BuildSingleInstanceMessage(
        CommandLineOptions options,
        IReadOnlyList<string> args)
    {
        if (options.RequestExit)
        {
            return "exit";
        }

        return HasCommandLineAction(options)
            ? SingleInstance.CreateCommandMessage(args)
            : "show";
    }

    private void OnCommandRequested(IReadOnlyList<string> args)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var options = CommandLineOptions.Parse(args);
            ApplyCommandLineActions(options);
        }));
    }

    private static void ApplyLanguage(AppLanguage language)
    {
        CultureInfo? culture = language switch
        {
            AppLanguage.Russian => CultureInfo.GetCultureInfo("ru-RU"),
            AppLanguage.English => CultureInfo.GetCultureInfo("en-US"),
            _ => null,
        };

        if (culture is null)
        {
            return;
        }

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private void OnHotkeyChangeRequested(HotkeyAction action)
    {
        if (_settings is null)
        {
            return;
        }

        var setting = _settings.Hotkeys.FirstOrDefault(hotkey => hotkey.Action == action);
        if (setting is null)
        {
            return;
        }

        var result = HotkeyCaptureWindow.Capture(action, setting.Gesture);
        if (result is null)
        {
            return;
        }

        setting.Modifiers = result.Modifiers;
        setting.VirtualKey = result.VirtualKey;
        setting.Gesture = result.Gesture;

        SaveSettings();
        _hotkeys?.Reload();
        _viewModel?.SetHotkeys(_hotkeys?.Bindings ?? []);
    }

    private void OnHotkeyPrefixChangeRequested(HotkeyAction action)
    {
        if (_settings is null)
        {
            return;
        }

        var setting = _settings.Hotkeys.FirstOrDefault(hotkey => hotkey.Action == action);
        if (setting is null)
        {
            return;
        }

        var result = HotkeyCaptureWindow.CapturePrefix(action, setting.PrefixGesture);
        if (result is null)
        {
            return;
        }

        setting.PrefixModifiers = result.PrefixModifiers;
        setting.PrefixVirtualKey = result.PrefixVirtualKey;
        setting.PrefixGesture = result.PrefixGesture;

        SaveSettings();
        _hotkeys?.Reload();
        _viewModel?.SetHotkeys(_hotkeys?.Bindings ?? []);
    }

    private void OnHotkeyPrefixClearRequested(HotkeyAction action)
    {
        if (_settings is null)
        {
            return;
        }

        var setting = _settings.Hotkeys.FirstOrDefault(hotkey => hotkey.Action == action);
        if (setting is null)
        {
            return;
        }

        setting.PrefixModifiers = 0;
        setting.PrefixVirtualKey = 0;
        setting.PrefixGesture = string.Empty;

        SaveSettings();
        _hotkeys?.Reload();
        _viewModel?.SetHotkeys(_hotkeys?.Bindings ?? []);
    }

    private void OnHotkeyPressed(HotkeyAction action)
    {
        if (_engine is null)
        {
            return;
        }

        switch (action)
        {
            case HotkeyAction.MoveWindowPrevious:
                _ = _engine.Windows.MoveForegroundWindow(-1);
                break;
            case HotkeyAction.MoveWindowNext:
                _ = _engine.Windows.MoveForegroundWindow(1);
                break;
            case HotkeyAction.ToggleTopmost:
                _ = _engine.Windows.ToggleTopmost();
                break;
            case HotkeyAction.SnapToSplit1:
                _ = _engine.Windows.SnapForegroundWindowToSplit(1, _settings?.SplitRows ?? 2, _settings?.SplitColumns ?? 2);
                break;
            case HotkeyAction.SnapToSplit2:
                _ = _engine.Windows.SnapForegroundWindowToSplit(2, _settings?.SplitRows ?? 2, _settings?.SplitColumns ?? 2);
                break;
            case HotkeyAction.SnapToSplit3:
                _ = _engine.Windows.SnapForegroundWindowToSplit(3, _settings?.SplitRows ?? 2, _settings?.SplitColumns ?? 2);
                break;
            case HotkeyAction.SnapToSplit4:
                _ = _engine.Windows.SnapForegroundWindowToSplit(4, _settings?.SplitRows ?? 2, _settings?.SplitColumns ?? 2);
                break;
            case HotkeyAction.DesktopPrevious:
                _ = _engine.VirtualDesktops.Previous();
                break;
            case HotkeyAction.DesktopNext:
                _ = _engine.VirtualDesktops.Next();
                break;
            case HotkeyAction.MoveWindowToPreviousDesktop:
                _ = _engine.VirtualDesktops.MoveWindowPrevious();
                break;
            case HotkeyAction.MoveWindowToNextDesktop:
                _ = _engine.VirtualDesktops.MoveWindowNext();
                break;
        }
    }

    private void OnExitRequested()
    {
        Dispatcher.BeginInvoke(new Action(ExitApplication));
    }

    private void OnShowRequested()
    {
        Dispatcher.BeginInvoke(new Action(ShowMainWindow));
    }

    private void ShowMainWindow()
    {
        _window?.ShowAndActivate();
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        _window?.Hide();
    }

    private void ExitApplication()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(ExitApplication));
            return;
        }

        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        SaveSettings();
        _window?.Close();
        Shutdown(0);
    }

    private void SaveSettings()
    {
        if (_settingsStore is null || _settings is null)
        {
            return;
        }

        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            DxLog.Exception("Failed to save settings.", ex);
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        if (!_suppressSaveOnExit)
        {
            SaveSettings();
        }
        _tray?.Dispose();
        _hotkeys?.Dispose();
        _displayWatcher?.Dispose();
        _engine?.Triggers.Dispose();
        _titleBarOverlay?.Dispose();
        _taskbarManager?.Dispose();
        _viewModel?.Dispose();
        _engine?.Stop();
        _singleInstance?.Dispose();
        DxLog.Info("DxTools stopped.");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        DxLog.Exception("Unhandled dispatcher exception.", e.Exception);
        ShowError(
            Strings.CrashTitle,
            $"{Strings.CrashText}{Environment.NewLine}{Environment.NewLine}{e.Exception.Message}");
        e.Handled = true;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _taskbarManager?.EmergencyRestore();
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _taskbarManager?.EmergencyRestore();

        if (e.ExceptionObject is Exception exception)
        {
            DxLog.Exception("Unhandled domain exception.", exception);
        }
        else
        {
            DxLog.Error($"Unhandled domain exception: {e.ExceptionObject}");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        DxLog.Exception("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }

    private static void ShowInfo(string title, string message)
    {
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private static void ShowError(string title, string message)
    {
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
    }
}
