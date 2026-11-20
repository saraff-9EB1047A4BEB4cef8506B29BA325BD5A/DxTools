using System.Windows;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;

namespace DxTools.App;

public partial class HotkeyCaptureWindow : System.Windows.Window
{
    private readonly HotkeyAction _action;

    public HotkeyCaptureWindow(HotkeyAction action, string currentGesture)
    {
        InitializeComponent();
        _action = action;
        GestureText.Text = currentGesture;
        Loaded += (_, _) => Focus();
    }

    public HotkeySetting? Result { get; private set; }

    public static HotkeySetting? Capture(HotkeyAction action, string currentGesture)
    {
        var window = new HotkeyCaptureWindow(action, currentGesture);
        _ = window.ShowDialog();
        return window.Result;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            Close();
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            ShowError();
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey == 0)
        {
            ShowError();
            return;
        }

        var registerModifiers = 0x4000u;
        if ((modifiers & ModifierKeys.Control) != 0)
        {
            registerModifiers |= 0x0002;
        }

        if ((modifiers & ModifierKeys.Alt) != 0)
        {
            registerModifiers |= 0x0001;
        }

        if ((modifiers & ModifierKeys.Shift) != 0)
        {
            registerModifiers |= 0x0004;
        }

        if ((modifiers & ModifierKeys.Windows) != 0)
        {
            registerModifiers |= 0x0008;
        }

        var gesture = BuildGesture(modifiers, key);
        Result = new HotkeySetting
        {
            Action = _action,
            Modifiers = registerModifiers,
            VirtualKey = (uint)virtualKey,
            Gesture = gesture,
        };

        DialogResult = true;
        Close();
    }

    private static string BuildGesture(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if ((modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
        if ((modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");
        if ((modifiers & ModifierKeys.Windows) != 0) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private void ShowError()
    {
        ErrorText.Text = Strings.HotkeyCaptureInvalid;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}
