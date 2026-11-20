using System.IO;
using System.Windows.Media;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed class TaskbarGroupItemViewModel : ObservableObject
{
    private readonly List<AppWindow> _windows;
    private string _title;
    private bool _isActive;
    private bool _isMinimized;

    public TaskbarGroupItemViewModel(string key, IReadOnlyList<AppWindow> windows, ImageSource? icon)
    {
        Key = key;
        _windows = windows.ToList();
        Icon = icon;
        _title = BuildTitle();
        _isActive = _windows.Any(window => window.IsForeground);
        _isMinimized = _windows.Count > 0 && _windows.All(window => window.IsMinimized);
    }

    public string Key { get; }

    public string? PinnedPath { get; private set; }

    public bool IsPinnedOnly => !string.IsNullOrWhiteSpace(PinnedPath);

    public ImageSource? Icon { get; }

    public IReadOnlyList<AppWindow> Windows => _windows;

    public int Count => _windows.Count;

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string DisplayTitle => Count > 1 ? $"{Title} ({Count})" : Title;

    public string ToolTipText => string.Join(Environment.NewLine, _windows.Select(window => window.Title));

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public bool IsMinimized
    {
        get => _isMinimized;
        private set => SetProperty(ref _isMinimized, value);
    }

    public TaskbarGroupItemViewModel(string title, string pinnedPath, ImageSource? icon)
    {
        Key = pinnedPath;
        PinnedPath = pinnedPath;
        _windows = [];
        Icon = icon;
        _title = title;
    }

    public void Update(IReadOnlyList<AppWindow> windows)
    {
        _windows.Clear();
        _windows.AddRange(windows);
        Title = BuildTitle();
        IsActive = _windows.Any(window => window.IsForeground);
        IsMinimized = _windows.Count > 0 && _windows.All(window => window.IsMinimized);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(ToolTipText));
    }

    private string BuildTitle()
    {
        if (_windows.Count == 0)
        {
            return string.Empty;
        }

        if (_windows.Count == 1)
        {
            return _windows[0].Title;
        }

        var path = _windows[0].ExecutablePath;
        var name = string.IsNullOrWhiteSpace(path) ? null : Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(name)
            ? _windows[0].Title
            : name;
    }
}
