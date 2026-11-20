using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.App.Services;

public sealed record TrayItem(
    string Name,
    System.Windows.Media.ImageSource? Icon,
    Rect Bounds,
    AutomationElement Element);

public sealed class TrayMirrorService
{
    private const int MaxIconSize = 96;

    public IReadOnlyList<TrayItem> Capture()
    {
        var shell = FindWindowW("Shell_TrayWnd", null);
        if (shell == IntPtr.Zero || !Win32Window.GetWindowRect(shell, out var shellRect))
        {
            return Array.Empty<TrayItem>();
        }

        try
        {
            var root = AutomationElement.FromHandle(shell);
            var condition = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Button);

            var buttons = root.FindAll(TreeScope.Descendants, condition);
            var items = new List<TrayItem>();

            foreach (AutomationElement button in buttons)
            {
                var bounds = button.Current.BoundingRectangle;
                if (bounds.Width <= 0
                    || bounds.Height <= 0
                    || bounds.Left < shellRect.Right - 430)
                {
                    continue;
                }

                var name = button.Current.Name;
                if (string.IsNullOrWhiteSpace(name)
                    || name.Contains("Показать рабочий стол", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Show desktop", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                items.Add(new TrayItem(name, CaptureIcon(bounds), bounds, button));
            }

            return items;
        }
        catch (Exception ex) when (ex is ElementNotAvailableException
            or InvalidOperationException
            or COMException)
        {
            DxLog.Warn($"Tray capture failed: {ex.Message}");
            return Array.Empty<TrayItem>();
        }
    }

    public void Click(TrayItem item)
    {
        try
        {
            if (item.Element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern)
                && pattern is InvokePattern invoke)
            {
                invoke.Invoke();
                return;
            }
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException)
        {
            DxLog.Warn($"Tray UIA invoke failed: {ex.Message}");
        }

        try
        {
            var x = (int)(item.Bounds.Left + (item.Bounds.Width / 2));
            var y = (int)(item.Bounds.Top + (item.Bounds.Height / 2));
            _ = SetCursorPos(x, y);
            mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        }
        catch (Exception ex)
        {
            DxLog.Exception("Tray click failed.", ex);
        }
    }

    private static System.Windows.Media.ImageSource? CaptureIcon(Rect bounds)
    {
        try
        {
            var width = Math.Clamp((int)bounds.Width, 1, MaxIconSize);
            var height = Math.Clamp((int)bounds.Height, 1, MaxIconSize);

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                (int)bounds.Left,
                (int)bounds.Top,
                0,
                0,
                new System.Drawing.Size(width, height));

            var handle = bitmap.GetHbitmap();
            try
            {
                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                _ = DeleteObject(handle);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or Win32Exception)
        {
            DxLog.Warn($"Tray icon capture failed: {ex.Message}");
            return null;
        }
    }

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string className, string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);
}
