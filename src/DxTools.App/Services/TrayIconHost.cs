using System.Drawing;
using System.IO;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Engine;
using DxTools.Interop;
using Forms = System.Windows.Forms;

namespace DxTools.App.Services;

public sealed class TrayIconHost : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _statusItem;
    private readonly Forms.ToolStripMenuItem _moveActiveItem;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly EngineHost _engine;
    private readonly Action? _windowsChanged;
    private readonly Icon? _icon;

    public TrayIconHost(MainWindow window, EngineHost engine, Action exit, Action? windowsChanged = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(exit);

        _engine = engine;
        _windowsChanged = windowsChanged;
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

        _moveActiveItem = new Forms.ToolStripMenuItem(Strings.MoveActiveWindowToMonitor);
        _moveActiveItem.DropDownOpening += (_, _) => RebuildMoveMenu(_moveActiveItem);
        _menu.Items.Add(_moveActiveItem);

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

    private void RebuildMoveMenu(Forms.ToolStripMenuItem item)
    {
        item.DropDownItems.Clear();

        var monitors = _engine.Display.GetMonitors();
        if (monitors.Count == 0)
        {
            item.DropDownItems.Add(new Forms.ToolStripMenuItem(Strings.NoMonitorsAvailable) { Enabled = false });
            return;
        }

        var foreground = Win32Window.GetForegroundWindow();
        var hasSource = _engine.Windows.TryGetMonitorIndex(foreground, out var sourceIndex);

        for (var index = 0; index < monitors.Count; index++)
        {
            var monitor = monitors[index];
            var markers = new List<string>();

            if (monitor.IsPrimary)
            {
                markers.Add(Strings.PrimaryMonitor);
            }

            if (hasSource && index == sourceIndex)
            {
                markers.Add(Strings.CurrentMonitor);
            }

            var label = markers.Count == 0
                ? monitor.DisplayName
                : $"{monitor.DisplayName} ({string.Join(", ", markers)})";

            var entry = new Forms.ToolStripMenuItem(label)
            {
                Enabled = hasSource && index != sourceIndex,
                Checked = hasSource && index == sourceIndex,
            };

            var targetIndex = index;
            entry.Click += (_, _) =>
            {
                if (_engine.Windows.MoveWindowToMonitor(foreground, targetIndex))
                {
                    _windowsChanged?.Invoke();
                }
            };

            item.DropDownItems.Add(entry);
        }
    }

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
