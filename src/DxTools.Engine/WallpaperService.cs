using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DxTools.Core;
using Microsoft.Win32;

namespace DxTools.Engine;

public sealed record WallpaperOperationResult(bool Success, string? Error, string? AppliedPath);

public sealed class WallpaperService
{
    private const uint SpiSetDeskWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x0001;
    private const uint SpifSendChange = 0x0002;

    private readonly DisplayService _display;

    public WallpaperService(DisplayService display)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
    }

    public WallpaperOperationResult Apply(WallpaperSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Normalize();

        var monitors = _display.GetMonitors();
        if (monitors.Count == 0)
        {
            return new WallpaperOperationResult(false, "No monitors were detected.", null);
        }

        var minX = monitors.Min(monitor => monitor.X);
        var minY = monitors.Min(monitor => monitor.Y);
        var maxX = monitors.Max(monitor => monitor.Right);
        var maxY = monitors.Max(monitor => monitor.Bottom);
        var width = Math.Max(1, maxX - minX);
        var height = Math.Max(1, maxY - minY);

        try
        {
            using var canvas = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(canvas);
            graphics.Clear(Color.Black);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var imagesUsed = 0;
            foreach (var monitor in monitors)
            {
                var imagePath = ResolveImage(settings, monitor);
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    continue;
                }

                try
                {
                    using var image = Image.FromFile(imagePath);
                    var target = new Rectangle(
                        monitor.X - minX,
                        monitor.Y - minY,
                        monitor.Width,
                        monitor.Height);

                    DrawImage(graphics, image, target, settings.FitMode);
                    imagesUsed++;
                }
                catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException)
                {
                    DxLog.Warn($"Wallpaper image '{imagePath}' could not be used: {ex.Message}");
                }
            }

            if (imagesUsed == 0)
            {
                return new WallpaperOperationResult(false, "No wallpaper images are configured.", null);
            }

            var directory = Path.Combine(AppPaths.LocalAppDataDirectory, "wallpaper");
            Directory.CreateDirectory(directory);
            var fileName = $"wallpaper-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}.png";
            var outputPath = Path.Combine(directory, fileName);
            canvas.Save(outputPath, ImageFormat.Png);

            SetWallpaperStyle();

            if (!SystemParametersInfo(
                SpiSetDeskWallpaper,
                0,
                outputPath,
                SpifUpdateIniFile | SpifSendChange))
            {
                var error = Marshal.GetLastWin32Error();
                return new WallpaperOperationResult(
                    false,
                    $"SystemParametersInfo failed with Win32 error {error}.",
                    outputPath);
            }

            CleanupOldWallpapers(directory, outputPath);
            DxLog.Info($"Wallpaper applied: {outputPath}.");

            return new WallpaperOperationResult(true, null, outputPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException)
        {
            DxLog.Exception("Wallpaper composition failed.", ex);
            return new WallpaperOperationResult(false, ex.Message, null);
        }
    }

    private static string? ResolveImage(WallpaperSettings settings, MonitorInfo monitor)
    {
        if (!string.IsNullOrWhiteSpace(monitor.MonitorId)
            && settings.MonitorImages.TryGetValue(monitor.MonitorId, out var image))
        {
            return image;
        }

        return settings.MonitorImages.TryGetValue(monitor.DeviceName, out var deviceImage)
            ? deviceImage
            : null;
    }

    private static void SetWallpaperStyle()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop")
            ?? throw new InvalidOperationException("Unable to open the desktop registry key.");

        key.SetValue("WallpaperStyle", "22", RegistryValueKind.String);
        key.SetValue("TileWallpaper", "0", RegistryValueKind.String);
    }

    private static void DrawImage(Graphics graphics, Image image, Rectangle target, WallpaperFitMode fitMode)
    {
        var state = graphics.Save();
        graphics.SetClip(target);

        try
        {
            switch (fitMode)
            {
                case WallpaperFitMode.Stretch:
                    graphics.DrawImage(image, target);
                    break;

                case WallpaperFitMode.Center:
                    graphics.DrawImage(
                        image,
                        target.X + ((target.Width - image.Width) / 2f),
                        target.Y + ((target.Height - image.Height) / 2f),
                        image.Width,
                        image.Height);
                    break;

                case WallpaperFitMode.Tile:
                    using (var brush = new TextureBrush(image, WrapMode.Tile))
                    {
                        graphics.FillRectangle(brush, target);
                    }

                    break;

                case WallpaperFitMode.Fit:
                {
                    var scale = Math.Min((double)target.Width / image.Width, (double)target.Height / image.Height);
                    var width = (int)Math.Round(image.Width * scale);
                    var height = (int)Math.Round(image.Height * scale);
                    var x = target.X + ((target.Width - width) / 2);
                    var y = target.Y + ((target.Height - height) / 2);
                    graphics.DrawImage(image, new Rectangle(x, y, width, height));
                    break;
                }

                default:
                {
                    var scale = Math.Max((double)target.Width / image.Width, (double)target.Height / image.Height);
                    var width = (int)Math.Round(image.Width * scale);
                    var height = (int)Math.Round(image.Height * scale);
                    var x = target.X + ((target.Width - width) / 2);
                    var y = target.Y + ((target.Height - height) / 2);
                    graphics.DrawImage(image, new Rectangle(x, y, width, height));
                    break;
                }
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static void CleanupOldWallpapers(string directory, string current)
    {
        try
        {
            var files = new DirectoryInfo(directory)
                .GetFiles("wallpaper-*.png")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(5)
                .ToList();

            foreach (var file in files)
            {
                if (!string.Equals(file.FullName, current, StringComparison.OrdinalIgnoreCase))
                {
                    file.Delete();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DxLog.Warn($"Old wallpaper cleanup failed: {ex.Message}");
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint action,
        uint parameter,
        string value,
        uint flags);
}
