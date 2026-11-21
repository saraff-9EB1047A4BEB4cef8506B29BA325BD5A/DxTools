using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DxTools.Core;
using Forms = System.Windows.Forms;

namespace DxTools.Screensaver;

internal sealed class ScreensaverController
{
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    private static readonly TimeSpan ImageInterval = TimeSpan.FromSeconds(10);

    private readonly System.Windows.Application _application;
    private readonly List<Window> _windows = [];
    private readonly List<ScreensaverMediaPresenter> _presenters = [];
    private readonly List<ScreensaverMediaItem> _items = [];
    private readonly HashSet<string> _failedVideos = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _imageTimer;
    private readonly DispatcherTimer _videoTimer;
    private readonly DispatcherTimer _previewTimer;
    private HwndSource? _previewSource;
    private IntPtr _previewParent;
    private int _previewWidth;
    private int _previewHeight;
    private DateTimeOffset _startedAt;
    private int _imageIndex;
    private ScreensaverMediaItem? _currentItem;
    private bool _advancing;
    private bool _shutdown;
    private bool _isPreview;

    public ScreensaverController(System.Windows.Application application)
    {
        _application = application;
        _imageTimer = new DispatcherTimer { Interval = ImageInterval };
        _imageTimer.Tick += (_, _) => Advance();

        _videoTimer = new DispatcherTimer();
        _videoTimer.Tick += (_, _) => Advance();

        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _previewTimer.Tick += (_, _) => UpdatePreviewBounds();
    }

    public void Start(string[] args)
    {
        var options = ScreensaverCommandLine.Parse(args);
        if (!options.IsValid)
        {
            DxLog.Warn(options.Error ?? "Invalid screensaver arguments.");
            _application.Shutdown();
            return;
        }

        switch (options.Mode)
        {
            case ScreensaverMode.Configure:
                OpenConfiguration();
                _application.Shutdown();
                break;
            case ScreensaverMode.Preview:
                StartPreview(options.PreviewWindow);
                break;
            default:
                StartFullScreen();
                break;
        }
    }

    private void StartFullScreen()
    {
        LoadMedia();
        CreateFullScreenWindows();
        _startedAt = DateTimeOffset.Now;
        _application.Dispatcher.BeginInvoke(
            new Action(Advance),
            DispatcherPriority.Loaded);
    }

    private void StartPreview(IntPtr parent)
    {
        if (parent == IntPtr.Zero || !IsWindow(parent))
        {
            DxLog.Warn("Screensaver preview window handle is not valid.");
            _application.Shutdown();
            return;
        }

        _isPreview = true;
        _previewParent = parent;
        LoadMedia();

        if (!GetClientRect(parent, out var client))
        {
            _application.Shutdown();
            return;
        }

        _previewWidth = Math.Max(1, client.Right - client.Left);
        _previewHeight = Math.Max(1, client.Bottom - client.Top);

        var parameters = new HwndSourceParameters("DxTools screensaver preview")
        {
            ParentWindow = parent,
            WindowStyle = WsChild | WsVisible,
            PositionX = 0,
            PositionY = 0,
            Width = _previewWidth,
            Height = _previewHeight,
        };

        try
        {
            _previewSource = new HwndSource(parameters);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            DxLog.Exception("Screensaver preview window could not be created.", ex);
            _application.Shutdown();
            return;
        }

        var presenter = CreatePresenter();
        _previewSource.RootVisual = presenter.Root;
        _previewTimer.Start();
        _application.Dispatcher.BeginInvoke(
            new Action(Advance),
            DispatcherPriority.Loaded);
    }

    private void LoadMedia()
    {
        var settings = new SettingsStore().Load().Settings;
        var folder = settings.Wallpaper.SlideshowFolder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        _items.Clear();
        _items.AddRange(ScreensaverMediaCatalog.Enumerate(
            folder,
            settings.Wallpaper.ScreensaverIncludeVideos));
    }

    private void CreateFullScreenWindows()
    {
        foreach (var screen in Forms.Screen.AllScreens)
        {
            var presenter = CreatePresenter();
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                Background = System.Windows.Media.Brushes.Black,
                Cursor = System.Windows.Input.Cursors.None,
                Content = presenter.Root,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Width = 100,
                Height = 100,
            };

            window.MouseMove += (_, _) => DismissAfterGrace();
            window.MouseDown += (_, _) => DismissAfterGrace();
            window.KeyDown += (_, _) => DismissAfterGrace();
            window.Loaded += (_, _) => PositionFullScreenWindow(window, screen.Bounds);

            _windows.Add(window);
            window.Show();
        }

        if (_windows.Count > 0)
        {
            _windows[0].Activate();
            _windows[0].Focus();
        }
    }

    private ScreensaverMediaPresenter CreatePresenter()
    {
        var presenter = new ScreensaverMediaPresenter();
        presenter.VideoEnded += OnVideoEnded;
        presenter.VideoFailed += OnVideoFailed;
        presenter.MediaOpened += OnMediaOpened;
        _presenters.Add(presenter);
        return presenter;
    }

    private ScreensaverMediaPresenter? PrimaryPresenter =>
        _presenters.Count == 0 ? null : _presenters[0];

    private void OnVideoEnded(ScreensaverMediaPresenter presenter)
    {
        if (!ReferenceEquals(presenter, PrimaryPresenter)
            || _shutdown
            || _currentItem is not { IsVideo: true })
        {
            return;
        }

        _application.Dispatcher.BeginInvoke(new Action(Advance));
    }

    private void OnVideoFailed(ScreensaverMediaPresenter presenter, string error)
    {
        if (!ReferenceEquals(presenter, PrimaryPresenter)
            || _shutdown
            || _currentItem is not { IsVideo: true })
        {
            return;
        }

        DxLog.Warn($"Screensaver video failed: {error}");
        _failedVideos.Add(_currentItem.Path);
        _application.Dispatcher.BeginInvoke(new Action(Advance));
    }

    private void OnMediaOpened(ScreensaverMediaPresenter presenter, TimeSpan duration)
    {
        if (!ReferenceEquals(presenter, PrimaryPresenter)
            || _shutdown
            || _currentItem is not { IsVideo: true }
            || duration <= TimeSpan.Zero)
        {
            return;
        }

        _videoTimer.Stop();
        _videoTimer.Interval = duration + TimeSpan.FromSeconds(2);
        _videoTimer.Start();
    }

    private void Advance()
    {
        if (_advancing || _shutdown)
        {
            return;
        }

        _advancing = true;
        _imageTimer.Stop();
        _videoTimer.Stop();

        try
        {
            if (_items.Count == 0 || _presenters.Count == 0)
            {
                return;
            }

            for (var attempt = 0; attempt < _items.Count; attempt++)
            {
                var item = _items[_imageIndex % _items.Count];
                _imageIndex++;

                if (item.IsVideo && _failedVideos.Contains(item.Path))
                {
                    continue;
                }

                _currentItem = item;
                var video = false;
                foreach (var presenter in _presenters)
                {
                    video |= presenter.Show(item);
                }

                if (item.IsVideo && video)
                {
                    return;
                }

                if (!item.IsVideo)
                {
                    _imageTimer.Start();
                    return;
                }
            }
        }
        finally
        {
            _advancing = false;
        }
    }

    private void DismissAfterGrace()
    {
        if (_isPreview || DateTimeOffset.Now - _startedAt < TimeSpan.FromSeconds(1.2))
        {
            return;
        }

        Shutdown();
    }

    private void UpdatePreviewBounds()
    {
        if (_shutdown || _previewSource is null)
        {
            return;
        }

        if (_previewParent == IntPtr.Zero
            || !IsWindow(_previewParent)
            || !GetClientRect(_previewParent, out var client))
        {
            Shutdown();
            return;
        }

        var width = Math.Max(1, client.Right - client.Left);
        var height = Math.Max(1, client.Bottom - client.Top);
        if (width == _previewWidth && height == _previewHeight)
        {
            return;
        }

        _previewWidth = width;
        _previewHeight = height;
        _ = SetWindowPos(
            _previewSource.Handle,
            IntPtr.Zero,
            0,
            0,
            width,
            height,
            SwpNoZOrder | SwpNoActivate | SwpShowWindow);
    }

    private void Shutdown()
    {
        if (_shutdown)
        {
            return;
        }

        _shutdown = true;
        _imageTimer.Stop();
        _videoTimer.Stop();
        _previewTimer.Stop();

        foreach (var presenter in _presenters)
        {
            presenter.Dispose();
        }

        _presenters.Clear();

        _previewSource?.Dispose();
        _previewSource = null;

        foreach (var window in _windows)
        {
            window.Close();
        }

        _windows.Clear();
        _application.Shutdown();
    }

    private static void PositionFullScreenWindow(Window window, System.Drawing.Rectangle bounds)
    {
        var handle = new WindowInteropHelper(window).Handle;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);
}

internal sealed class ScreensaverMediaPresenter : IDisposable
{
    private readonly System.Windows.Controls.Image _image;
    private readonly System.Windows.Controls.MediaElement _video;
    private bool _disposed;

    public ScreensaverMediaPresenter()
    {
        _image = new System.Windows.Controls.Image
        {
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
        };

        _video = new System.Windows.Controls.MediaElement
        {
            Stretch = Stretch.UniformToFill,
            IsMuted = true,
            LoadedBehavior = System.Windows.Controls.MediaState.Manual,
            UnloadedBehavior = System.Windows.Controls.MediaState.Manual,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
        };

        _video.MediaEnded += (_, _) => VideoEnded?.Invoke(this);
        _video.MediaOpened += (_, _) =>
        {
            if (_video.NaturalDuration.HasTimeSpan)
            {
                MediaOpened?.Invoke(this, _video.NaturalDuration.TimeSpan);
            }
        };
        _video.MediaFailed += (_, e) =>
            VideoFailed?.Invoke(this, e.ErrorException?.Message ?? "Media element failed.");

        Root = new System.Windows.Controls.Grid
        {
            Background = System.Windows.Media.Brushes.Black,
        };
        Root.Children.Add(_image);
        Root.Children.Add(_video);
    }

    public System.Windows.Controls.Grid Root { get; }

    public event Action<ScreensaverMediaPresenter>? VideoEnded;

    public event Action<ScreensaverMediaPresenter, string>? VideoFailed;

    public event Action<ScreensaverMediaPresenter, TimeSpan>? MediaOpened;

    public bool Show(ScreensaverMediaItem item)
    {
        if (item.IsVideo)
        {
            _image.Visibility = Visibility.Collapsed;
            _video.Visibility = Visibility.Visible;
            _video.Stop();
            _video.Source = new Uri(item.Path, UriKind.Absolute);
            _video.Play();
            return true;
        }

        _video.Stop();
        _video.Source = null;
        _video.Visibility = Visibility.Collapsed;

        ImageSource? source = null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(item.Path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            source = bitmap;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UriFormatException)
        {
            DxLog.Warn($"Screensaver image load failed: {ex.Message}");
        }

        _image.Source = source;
        _image.Visibility = Visibility.Visible;
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _video.Stop();
        _video.Close();
        _video.Source = null;
    }
}
