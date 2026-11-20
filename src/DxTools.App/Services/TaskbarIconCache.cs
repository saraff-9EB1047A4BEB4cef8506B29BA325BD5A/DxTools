using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DxTools.Core;

namespace DxTools.App.Services;

public sealed class TaskbarIconCache
{
    private readonly Dictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);
    private ImageSource? _defaultIcon;

    public ImageSource? GetIcon(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && _cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        ImageSource? icon = null;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                using var extracted = Icon.ExtractAssociatedIcon(path);
                if (extracted is not null)
                {
                    icon = Convert(extracted);
                }
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
            {
                DxLog.Warn($"Could not extract taskbar icon for '{path}': {ex.Message}");
            }
        }

        icon ??= GetDefaultIcon();

        if (!string.IsNullOrWhiteSpace(path) && icon is not null)
        {
            _cache[path] = icon;
        }

        return icon;
    }

    private ImageSource? GetDefaultIcon()
    {
        if (_defaultIcon is not null)
        {
            return _defaultIcon;
        }

        try
        {
            var resource = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Resources/DxTools.ico"));
            if (resource?.Stream is not null)
            {
                using var icon = new Icon(resource.Stream);
                _defaultIcon = Convert(icon);
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UriFormatException)
        {
            DxLog.Warn($"Could not load the default taskbar icon: {ex.Message}");
        }

        return _defaultIcon;
    }

    private static ImageSource Convert(Icon icon)
    {
        var source = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
        source.Freeze();
        return source;
    }
}
