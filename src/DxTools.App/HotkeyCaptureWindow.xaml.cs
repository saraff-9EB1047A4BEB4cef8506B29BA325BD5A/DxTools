using System.Windows;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;

namespace DxTools.App;

public partial class HotkeyCaptureWindow : System.Windows.Window
{
    private readonly HotkeyAction _action;
    private readonly bool _capturePrefix;

    public HotkeyCaptureWindow(HotkeyAction action, string currentGesture, bool capturePrefix = false)
    {
        InitializeComponent();
        _action = action;
        _capturePrefix = capturePrefix;
        GestureText.Text = string.IsNullOrWhiteSpace(currentGesture) ? Strings.HotkeyCaptureHint : currentGesture;

        if (capturePrefix)
        {
            Title = Strings.HotkeyCapturePrefixTitle;
            HintText.Text = Strings.HotkeyCapturePrefixHint;
        }
        Loaded += (_, _) => Focus();
    }

    public HotkeySetting? Result { get; private set; }

    public static HotkeySetting? Capture(HotkeyAction action, string currentGesture)
    {
        var window = new HotkeyCaptureWindow(action, currentGesture);
        _ = window.ShowDialog();
        return window.Result;
    }

    public static HotkeySetting? CapturePrefix(HotkeyAction action, string currentGesture)
    {
        var window = new HotkeyCaptureWindow(action, currentGesture, capturePrefix: true);
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
            Modifiers = _capturePrefix ? 0 : registerModifiers,
            VirtualKey = _capturePrefix ? 0 : (uint)virtualKey,
            Gesture = _capturePrefix ? string.Empty : gesture,
            PrefixModifiers = _capturePrefix ? registerModifiers : 0,
            PrefixVirtualKey = _capturePrefix ? (uint)virtualKey : 0,
            PrefixGesture = _capturePrefix ? gesture : string.Empty,
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
