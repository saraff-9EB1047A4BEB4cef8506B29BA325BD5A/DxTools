using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DxTools.Core;

namespace DxTools.App.Services;

public static class WindowThemeHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    public static void Apply(Window window, AppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(window);

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var dark = ThemeManager.Resolve(theme) == AppTheme.Dark;
        var value = dark ? 1 : 0;

        try
        {
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref value, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
