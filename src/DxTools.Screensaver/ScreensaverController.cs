using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DxTools.Core;
using Forms = System.Windows.Forms;

namespace DxTools.Screensaver;

internal sealed class ScreensaverController
{
    private readonly System.Windows.Application _application;
    private readonly List<Window> _windows = [];
    private readonly List<string> _images = [];
    private readonly DispatcherTimer _timer;
    private DateTimeOffset _startedAt;
    private int _imageIndex;

    public ScreensaverController(System.Windows.Application application)
    {
        _application = application;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10),
        };
        _timer.Tick += (_, _) => ShowNextImage();
    }

    public void Start(string[] args)
    {
        var mode = args.FirstOrDefault()?.ToLowerInvariant();
        if (mode is "/c" or "-c" or "/config")
        {
            OpenConfiguration();
            return;
        }

        if (mode is "/p" or "-p" or "/preview")
        {
            _application.Shutdown();
            return;
        }

        LoadImages();
        CreateWindows();
        _startedAt = DateTimeOffset.Now;
        ShowNextImage();
        _timer.Start();
    }

    private void LoadImages()
    {
        var settings = new SettingsStore().Load().Settings;
        var folder = settings.Wallpaper.SlideshowFolder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        try
        {
            _images.AddRange(Directory
                .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.CurrentCultureIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DxLog.Warn($"Screensaver image enumeration failed: {ex.Message}");
        }
    }

    private void CreateWindows()
    {
        var screens = Forms.Screen.AllScreens;
        foreach (var screen in screens)
        {
            var image = new System.Windows.Controls.Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            };

            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                Background = System.Windows.Media.Brushes.Black,
                Cursor = System.Windows.Input.Cursors.None,
                Content = image,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Width = 100,
                Height = 100,
            };

            window.MouseMove += (_, _) => DismissAfterGrace();
            window.MouseDown += (_, _) => DismissAfterGrace();
            window.KeyDown += (_, _) => DismissAfterGrace();
            window.Loaded += (_, _) => PositionWindow(window, screen.Bounds);

            window.Show();
            _windows.Add(window);
        }

        if (_windows.Count > 0)
        {
            _windows[0].Activate();
            _windows[0].Focus();
        }
    }

    private static void PositionWindow(Window window, System.Drawing.Rectangle bounds)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            _ = SetWindowPos(
                handle,
                new IntPtr(-1),
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                0x0040);
        }
    }

    private void ShowNextImage()
    {
        if (_windows.Count == 0)
        {
            return;
        }

        ImageSource? source = null;
        if (_images.Count > 0)
        {
            var path = _images[_imageIndex % _images.Count];
            _imageIndex++;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                source = bitmap;
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or UriFormatException)
            {
                DxLog.Warn($"Screensaver image load failed: {ex.Message}");
            }
        }

        foreach (var window in _windows)
        {
            if (window.Content is System.Windows.Controls.Image image && source is not null)
            {
                image.Source = source;
            }
        }
    }

    private void DismissAfterGrace()
    {
        if (DateTimeOffset.Now - _startedAt < TimeSpan.FromSeconds(1.2))
        {
            return;
        }

        _timer.Stop();
        foreach (var window in _windows)
        {
            window.Close();
        }

        _application.Shutdown();
    }

    private static void OpenConfiguration()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "DxTools.exe");
        if (File.Exists(exe))
        {
            try
            {
                Process.Start(new ProcessStartInfo(exe, "--show") { UseShellExecute = true });
                return;
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                DxLog.Exception("Failed to open DxTools settings from the screensaver.", ex);
            }
        }

        System.Windows.MessageBox.Show(
            "Open DxTools and configure the wallpaper slideshow folder to use it as the screensaver image source.",
            "DxTools screensaver",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
