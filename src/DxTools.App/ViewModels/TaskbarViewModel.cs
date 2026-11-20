using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using DxTools.App.Services;
using DxTools.Core;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed class TaskbarViewModel : ObservableObject, IDisposable
{
    private readonly WindowCatalog _catalog;
    private readonly IReadOnlyList<MonitorInfo> _monitors;
    private readonly int _monitorIndex;
    private readonly TaskbarIconCache _icons;
    private readonly Action _showSettings;
    private readonly DispatcherTimer _timer;
    private string _signature = string.Empty;
    private string _clockText = string.Empty;
    private bool _disposed;

    public TaskbarViewModel(
        WindowCatalog catalog,
        IReadOnlyList<MonitorInfo> monitors,
        int monitorIndex,
        TaskbarIconCache icons,
        Action showSettings)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
        _monitorIndex = monitorIndex;
        _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        _showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));

        ActivateCommand = new RelayCommand(parameter =>
        {
            if (parameter is TaskbarWindowItemViewModel item)
            {
                _ = _catalog.Activate(item.Handle);
                Refresh();
            }
        });
        OpenSettingsCommand = new RelayCommand(_showSettings);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTimerTick;

        Monitor = _monitors[_monitorIndex];
        ClockText = DateTime.Now.ToString("HH:mm");
        Refresh();
    }

    public ObservableCollection<TaskbarWindowItemViewModel> Windows { get; } = [];

    public ICommand ActivateCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public MonitorInfo Monitor { get; }

    public string MonitorLabel => Monitor.DisplayName;

    public string ClockText
    {
        get => _clockText;
        private set => SetProperty(ref _clockText, value);
    }

    public void Start() => _timer.Start();

    public void Refresh()
    {
        var windows = _catalog.GetWindows()
            .Where(window => window.MonitorIndex == _monitorIndex)
            .ToList();

        var signature = string.Join(
            "|",
            windows.Select(window => $"{window.Handle}:{window.Title}:{window.IsForeground}:{window.IsMinimized}"));

        if (string.Equals(signature, _signature, StringComparison.Ordinal))
        {
            return;
        }

        var existing = Windows.ToDictionary(item => item.Handle);
        Windows.Clear();

        foreach (var window in windows)
        {
            if (existing.TryGetValue(window.Handle, out var item))
            {
                item.Update(window);
            }
            else
            {
                item = new TaskbarWindowItemViewModel(window, _icons.GetIcon(window.ExecutablePath));
            }

            Windows.Add(item);
        }

        _signature = signature;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        ClockText = DateTime.Now.ToString("HH:mm");
        Refresh();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
