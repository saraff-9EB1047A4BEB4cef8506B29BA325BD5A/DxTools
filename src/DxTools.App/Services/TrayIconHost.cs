using System.Drawing;
using System.IO;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;
using Forms = System.Windows.Forms;

namespace DxTools.App.Services;

public sealed class TrayIconHost : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _statusItem;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Icon? _icon;

    public TrayIconHost(MainWindow window, EngineHost engine, Action exit)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(exit);

        _icon = LoadIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon ?? SystemIcons.Application,
            Text = Strings.AppTitle,
            Visible = true,
        };

        _menu = new Forms.ContextMenuStrip();

        var openItem = new Forms.ToolStripMenuItem(Strings.Open);
        openItem.Click += (_, _) => window.ShowAndActivate();
        _menu.Items.Add(openItem);

        _statusItem = new Forms.ToolStripMenuItem { Enabled = false };
        _menu.Items.Add(_statusItem);

        _menu.Items.Add(new Forms.ToolStripSeparator());

        var aboutItem = new Forms.ToolStripMenuItem(Strings.AboutTitle);
        aboutItem.Click += (_, _) => ShowAbout();
        _menu.Items.Add(aboutItem);

        _menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem(Strings.Exit);
        exitItem.Click += (_, _) => exit();
        _menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = _menu;
        _notifyIcon.DoubleClick += (_, _) => window.ShowAndActivate();

        engine.StatusChanged += OnStatusChanged;
        UpdateStatus(engine.Status);
    }

    public void ShowFirstRunBalloon() =>
        _notifyIcon.ShowBalloonTip(5000, Strings.FirstRunTitle, Strings.FirstRunText, Forms.ToolTipIcon.Info);

    private void OnStatusChanged(object? sender, EngineStatus status)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => UpdateStatus(status));
            return;
        }

        UpdateStatus(status);
    }

    private void UpdateStatus(EngineStatus status)
    {
        var text = status.SafeMode
            ? Strings.StatusSafeMode
            : status.IsHealthy ? Strings.StatusReady : Strings.StatusUnavailable;

        _statusItem.Text = $"{Strings.TrayStatus}: {text}";

        var tooltip = $"{Strings.AppTitle} — {text}";
        _notifyIcon.Text = tooltip.Length <= 63 ? tooltip : tooltip[..63];
    }

    private static void ShowAbout()
    {
        var text = $"{Strings.AboutTitle}{Environment.NewLine}{Environment.NewLine}" +
                   $"{Strings.Version}: {DxToolsInfo.InformationalVersion}{Environment.NewLine}" +
                   $"{Strings.License}: {DxToolsInfo.LicenseId}{Environment.NewLine}{Environment.NewLine}" +
                   DxToolsInfo.Copyright;

        System.Windows.MessageBox.Show(
            text,
            Strings.AppTitle,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private static Icon? LoadIcon()
    {
        try
        {
            var resource = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Resources/DxTools.ico"));
            if (resource?.Stream is not null)
            {
                return new Icon(resource.Stream, new Size(32, 32));
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UriFormatException)
        {
            DxLog.Warn($"Could not load the tray icon: {ex.Message}");
        }

        return null;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon?.Dispose();
    }
}
