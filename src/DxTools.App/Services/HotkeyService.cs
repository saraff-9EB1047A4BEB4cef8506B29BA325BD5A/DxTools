using System.Runtime.InteropServices;
using System.Windows.Interop;
using DxTools.App.Properties;
using DxTools.Core;

namespace DxTools.App.Services;

public enum HotkeyAction
{
    MoveWindowPrevious,
    MoveWindowNext,
    ToggleTopmost,
}

public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;

    private readonly IntPtr _windowHandle;
    private readonly HwndSource? _source;
    private readonly List<HotkeyBinding> _bindings = [];
    private bool _disposed;

    public HotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle);

        if (_source is null)
        {
            DxLog.Warn("Hotkey service could not attach to the application window.");
            return;
        }

        _source.AddHook(WindowProcedure);

        Register(1, HotkeyAction.MoveWindowPrevious, "Ctrl+Alt+Left", 0x25, Strings.HotkeyMovePrev);
        Register(2, HotkeyAction.MoveWindowNext, "Ctrl+Alt+Right", 0x27, Strings.HotkeyMoveNext);
        Register(3, HotkeyAction.ToggleTopmost, "Ctrl+Alt+Up", 0x26, Strings.HotkeyTopmost);
    }

    public event Action<HotkeyAction>? Pressed;

    public IReadOnlyList<HotkeyInfo> Bindings =>
        _bindings.Select(binding => new HotkeyInfo(binding.Gesture, binding.Description, binding.Registered)).ToList();

    private void Register(int id, HotkeyAction action, string gesture, uint virtualKey, string description)
    {
        var modifiers = ModControl | ModAlt | ModNoRepeat;
        var registered = RegisterHotKey(_windowHandle, id, modifiers, virtualKey);

        if (!registered)
        {
            DxLog.Warn($"Hotkey registration failed: {gesture}.");
        }

        _bindings.Add(new HotkeyBinding(id, action, gesture, description, registered));
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmHotkey)
        {
            return IntPtr.Zero;
        }

        var id = wParam.ToInt32();
        var binding = _bindings.FirstOrDefault(item => item.Id == id && item.Registered);
        if (binding is null)
        {
            return IntPtr.Zero;
        }

        handled = true;

        try
        {
            Pressed?.Invoke(binding.Action);
        }
        catch (Exception ex)
        {
            DxLog.Exception($"Hotkey action '{binding.Action}' failed.", ex);
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var binding in _bindings.Where(item => item.Registered))
        {
            _ = UnregisterHotKey(_windowHandle, binding.Id);
        }

        _source?.RemoveHook(WindowProcedure);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    private sealed record HotkeyBinding(
        int Id,
        HotkeyAction Action,
        string Gesture,
        string Description,
        bool Registered);
}
