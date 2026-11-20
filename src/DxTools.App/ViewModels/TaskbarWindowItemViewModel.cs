using System.Windows.Media;
using DxTools.Engine;

namespace DxTools.App.ViewModels;

public sealed class TaskbarWindowItemViewModel : ObservableObject
{
    private string _title;
    private bool _isActive;
    private bool _isMinimized;

    public TaskbarWindowItemViewModel(AppWindow window, ImageSource? icon)
    {
        Handle = window.Handle;
        _title = window.Title;
        _isActive = window.IsForeground;
        _isMinimized = window.IsMinimized;
        Icon = icon;
    }

    public IntPtr Handle { get; }

    public ImageSource? Icon { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

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

    public void Update(AppWindow window)
    {
        Title = window.Title;
        IsActive = window.IsForeground;
        IsMinimized = window.IsMinimized;
    }
}
