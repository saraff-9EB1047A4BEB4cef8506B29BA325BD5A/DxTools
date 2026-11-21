using System.Windows;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Core;

namespace DxTools.App;

public partial class HotkeyCaptureWindow : System.Windows.Window
{
    private readonly List<HotkeyStep> _steps = [];

    public HotkeyCaptureWindow(HotkeyAction action, IEnumerable<HotkeyStep> currentSteps)
    {
        InitializeComponent();
        _ = action;
        _steps.AddRange(currentSteps
            .Where(step => step.VirtualKey != 0)
            .Take(HotkeySetting.MaxSteps)
            .Select(step => step.Clone()));

        UpdateDisplay();
        Loaded += (_, _) =>
        {
            Focus();
            Keyboard.Focus(this);
        };
    }

    public List<HotkeyStep>? Result { get; private set; }

    public static List<HotkeyStep>? CaptureSequence(
        System.Windows.Window? owner,
        HotkeyAction action,
        IEnumerable<HotkeyStep> currentSteps)
    {
        var window = new HotkeyCaptureWindow(action, currentSteps)
        {
            Owner = owner,
        };
        _ = window.ShowDialog();
        return window.Result;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            Result = null;
            Close();
            return;
        }

        if (key == Key.Enter)
        {
            Finish();
            return;
        }

        if (key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (_steps.Count > 0)
            {
                _steps.RemoveAt(_steps.Count - 1);
                HideError();
                UpdateDisplay();
            }

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
            ShowError(Strings.HotkeyCaptureInvalid);
            return;
        }

        if (_steps.Count >= HotkeySetting.MaxSteps)
        {
            ShowError(string.Format(Strings.HotkeyCaptureTooManySteps, HotkeySetting.MaxSteps));
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey == 0)
        {
            ShowError(Strings.HotkeyCaptureInvalid);
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

        _steps.Add(new HotkeyStep
        {
            Modifiers = registerModifiers,
            VirtualKey = (uint)virtualKey,
            Gesture = BuildGesture(modifiers, key),
        });

        HideError();
        UpdateDisplay();
    }

    private void Finish_Click(object sender, RoutedEventArgs e) => Finish();

    private void Finish()
    {
        if (_steps.Count == 0)
        {
            ShowError(Strings.HotkeyCaptureInvalid);
            return;
        }

        Result = _steps.Select(step => step.Clone()).ToList();
        DialogResult = true;
        Close();
    }

    private void UpdateDisplay()
    {
        GestureText.Text = _steps.Count == 0
            ? Strings.HotkeyCaptureHint
            : HotkeySequence.Format(_steps);
        StepCountText.Text = string.Format(
            Strings.HotkeyCaptureStepCount,
            _steps.Count,
            HotkeySetting.MaxSteps);
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

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }
}
