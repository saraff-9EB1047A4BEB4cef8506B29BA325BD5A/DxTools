using System.Windows;
using DxTools.App.Services;
using DxTools.App.ViewModels;
using DxTools.Core;

namespace DxTools.App;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        SourceInitialized += (_, _) => ApplyTheme(ViewModel.Theme);
    }

    public MainViewModel ViewModel { get; }

    public void ApplyTheme(AppTheme theme)
    {
        WindowThemeHelper.Apply(this, theme);
    }

    public void ShowAndActivate()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
    }
}
