using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using DxTools.App.Properties;

namespace DxTools.App;

public partial class DisplayConfirmationWindow : System.Windows.Window
{
    private readonly DispatcherTimer _timer;
    private readonly string _profileName;
    private int _seconds = 15;

    public DisplayConfirmationWindow(string profileName)
    {
        InitializeComponent();
        _profileName = profileName;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTimerTick;

        UpdateText();
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    public bool Keep { get; private set; }

    public static bool Confirm(string profileName)
    {
        var window = new DisplayConfirmationWindow(profileName);
        _ = window.ShowDialog();
        return window.Keep;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _seconds--;
        if (_seconds <= 0)
        {
            Keep = false;
            Close();
            return;
        }

        UpdateText();
    }

    private void UpdateText()
    {
        var message = string.Format(
            CultureInfo.CurrentCulture,
            Strings.DisplayApplyConfirmText,
            _seconds);

        MessageText.Text = string.IsNullOrWhiteSpace(_profileName)
            ? message
            : $"{message}{Environment.NewLine}{_profileName}";
    }

    private void Keep_Click(object sender, RoutedEventArgs e)
    {
        Keep = true;
        Close();
    }

    private void Revert_Click(object sender, RoutedEventArgs e)
    {
        Keep = false;
        Close();
    }
}
