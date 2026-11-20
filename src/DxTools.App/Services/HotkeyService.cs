using System.Runtime.InteropServices;
using System.Windows.Interop;
using DxTools.App.Properties;
using DxTools.Core;

namespace DxTools.App.Services;

public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;

    private readonly IntPtr _windowHandle;
    private readonly HwndSource? _source;
    private readonly AppSettings _settings;
    private readonly List<HotkeyBinding> _bindings = [];
    private bool _disposed;

    public HotkeyService(IntPtr windowHandle, AppSettings settings)
    {
        _windowHandle = windowHandle;
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _source = HwndSource.FromHwnd(windowHandle);

        if (_source is null)
        {
            DxLog.Warn("Hotkey service could not attach to the application window.");
            return;
        }

        _source.AddHook(WindowProcedure);
        RegisterAll();
    }

    public event Action<HotkeyAction>? Pressed;

    public IReadOnlyList<HotkeyInfo> Bindings =>
        _bindings
            .Select(binding => new HotkeyInfo(
                binding.Setting.Action,
                binding.Setting.Gesture,
                Describe(binding.Setting.Action),
                binding.Registered))
            .ToList();

    public void Reload()
    {
        foreach (var binding in _bindings.Where(item => item.Registered))
        {
            _ = UnregisterHotKey(_windowHandle, binding.Id);
        }

        _bindings.Clear();
        RegisterAll();
    }

    private void RegisterAll()
    {
        var settings = _settings.Hotkeys.Count > 0
            ? _settings.Hotkeys
            : HotkeySetting.CreateDefaults();

        var id = 1;
        foreach (var setting in settings)
        {
            var registered = RegisterHotKey(
                _windowHandle,
                id,
                setting.Modifiers,
                setting.VirtualKey);

            if (!registered)
            {
                DxLog.Warn($"Hotkey registration failed: {setting.Gesture}.");
            }

            _bindings.Add(new HotkeyBinding(id, setting, registered));
            id++;
        }
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
            Pressed?.Invoke(binding.Setting.Action);
        }
        catch (Exception ex)
        {
            DxLog.Exception($"Hotkey action '{binding.Setting.Action}' failed.", ex);
        }

        return IntPtr.Zero;
    }

    private static string Describe(HotkeyAction action) => action switch
    {
        HotkeyAction.MoveWindowPrevious => Strings.HotkeyMovePrev,
        HotkeyAction.MoveWindowNext => Strings.HotkeyMoveNext,
        HotkeyAction.ToggleTopmost => Strings.HotkeyTopmost,
        _ => action.ToString(),
    };

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

    private sealed record HotkeyBinding(int Id, HotkeySetting Setting, bool Registered);
}
