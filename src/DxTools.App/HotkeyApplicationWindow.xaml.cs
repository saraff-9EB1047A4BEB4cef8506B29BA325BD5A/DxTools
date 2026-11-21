using System.Diagnostics;
using System.Windows;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.App;

public partial class HotkeyApplicationWindow : System.Windows.Window
{
    private readonly string? _previousForegroundProcess;

    public HotkeyApplicationWindow(string? currentProcess)
    {
        InitializeComponent();
        _previousForegroundProcess = GetForegroundProcessName();
        ProcessText.Text = currentProcess ?? string.Empty;
        Loaded += (_, _) =>
        {
            ProcessText.Focus();
            ProcessText.SelectAll();
            LoadProcesses();
        };
    }

    public string? Result { get; private set; }

    public static bool TryChoose(System.Windows.Window owner, string? currentProcess, out string? process)
    {
        var window = new HotkeyApplicationWindow(currentProcess)
        {
            Owner = owner,
        };
        var accepted = window.ShowDialog() == true;
        process = accepted ? window.Result : null;
        return accepted;
    }

    private void LoadProcesses()
    {
        var names = new List<string>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero
                    && !string.IsNullOrWhiteSpace(process.ProcessName))
                {
                    names.Add(process.ProcessName + ".exe");
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        ProcessList.ItemsSource = names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private void ProcessList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProcessList.SelectedItem is string name)
        {
            ProcessText.Text = name;
        }
    }

    private void UseActiveWindow_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_previousForegroundProcess))
        {
            ProcessText.Text = _previousForegroundProcess;
            return;
        }

        var foreground = Win32Window.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            ShowError(Strings.HotkeyApplicationInvalid);
            return;
        }

        _ = Win32Window.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == 0)
        {
            ShowError(Strings.HotkeyApplicationInvalid);
            return;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            ProcessText.Text = process.ProcessName + ".exe";
        }
        catch (Exception ex) when (ex is ArgumentException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            ShowError(Strings.HotkeyApplicationInvalid);
        }
    }

    private static string? GetForegroundProcessName()
    {
        var foreground = Win32Window.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return null;
        }

        _ = Win32Window.GetWindowThreadProcessId(foreground, out var processId);
        if (processId == 0 || processId == Environment.ProcessId)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName + ".exe";
        }
        catch (Exception ex) when (ex is ArgumentException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private void AnyApplication_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = true;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProcessText.Text))
        {
            Result = null;
            DialogResult = true;
            Close();
            return;
        }

        var normalized = HotkeySetting.NormalizeProcessName(ProcessText.Text);
        if (normalized is null)
        {
            ShowError(Strings.HotkeyApplicationInvalid);
            return;
        }

        Result = normalized;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
