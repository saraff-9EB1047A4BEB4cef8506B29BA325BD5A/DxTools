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
    private bool _isExiting;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
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
            _singleInstance.TrySignalExisting();
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown(0);
            return;
        }

        _singleInstance.ShowRequested += OnShowRequested;
        _singleInstance.StartServer();

        _settingsStore = settingsStore;
        _settings = loadResult.Settings;

        var autostart = new AutostartService();
        _settings.StartWithWindows = autostart.IsEnabled();

        ThemeManager.Apply(_settings.Theme);

        _engine = new EngineHost(options.SafeMode);
        _engine.Start();

        var viewModel = new MainViewModel(settingsStore, _settings, _engine, autostart);
        viewModel.ThemeChanged += theme =>
        {
            ThemeManager.Apply(theme);
            _window?.ApplyTheme(theme);
        };

        _window = new MainWindow(viewModel);
        _window.Closing += OnMainWindowClosing;

        _tray = new TrayIconHost(_window, viewModel, _engine, ExitApplication);

        var showWindow = options.ShowSettings
            || !_settings.FirstRunCompleted
            || !options.Autostart
            || !_settings.StartMinimized;

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
        SaveSettings();
        _tray?.Dispose();
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

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
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
