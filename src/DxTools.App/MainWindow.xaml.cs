using System.Windows;
using System.Windows.Interop;
using DxTools.App.Services;
using DxTools.App.ViewModels;
using DxTools.Core;
using DxTools.Interop;

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

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            Activate();
            Focus();
            return;
        }

        if (Win32Taskbar.IsIconic(handle))
        {
            _ = Win32Window.ShowWindow(handle, Win32Window.SwRestore);
        }

        if (!Win32Taskbar.SetForegroundWindow(handle))
        {
            AttachToForegroundThread(handle);
        }

        Activate();
        Focus();

        if (Win32Window.GetForegroundWindow() != handle && !Topmost)
        {
            Topmost = true;
            Topmost = false;
            Activate();
        }
    }

    private static void AttachToForegroundThread(IntPtr handle)
    {
        var foreground = Win32Window.GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == handle)
        {
            return;
        }

        var foregroundThread = Win32Window.GetWindowThreadProcessId(foreground, out _);
        var targetThread = Win32Window.GetWindowThreadProcessId(handle, out _);
        if (foregroundThread == 0 || targetThread == 0 || foregroundThread == targetThread)
        {
            return;
        }

        var attached = false;
        try
        {
            attached = Win32Taskbar.AttachThreadInput(foregroundThread, targetThread, true);
            _ = Win32Taskbar.SetForegroundWindow(handle);
        }
        finally
        {
            if (attached)
            {
                _ = Win32Taskbar.AttachThreadInput(foregroundThread, targetThread, false);
            }
        }
    }
}
