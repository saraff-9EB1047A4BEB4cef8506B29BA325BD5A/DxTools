using System.Windows.Interop;
using DxTools.App.Properties;
using DxTools.Core;
using DxTools.Interop;

namespace DxTools.App.Services;

public sealed class HotkeyService : IDisposable
{
    public const int HookMessage = 0x8000 + 77;

    private readonly IntPtr _windowHandle;
    private readonly HwndSource? _source;
    private readonly AppSettings _settings;
    private List<HotkeyBinding> _bindings = [];
    private bool _hookActive;
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
        Reload();
    }

    public event Action<HotkeyAction>? Pressed;

    public IReadOnlyList<HotkeyInfo> Bindings =>
        _bindings
            .Select(binding => new HotkeyInfo(
                binding.Setting.Action,
                HotkeySequence.Format(binding.Setting.Steps),
                string.Empty,
                Describe(binding.Setting.Action),
                _hookActive,
                binding.Setting.TargetProcess))
            .ToList();

    public void Reload()
    {
        var settings = _settings.Hotkeys.Count > 0
            ? _settings.Hotkeys
            : HotkeySetting.CreateDefaults();

        var next = settings
            .Select((setting, index) => new HotkeyBinding((uint)(index + 1), setting))
            .ToList();

        var nativeBindings = next
            .Select(binding => CreateNativeBinding(binding.Id, binding.Setting))
            .ToList();

        if (!_hookActive)
        {
            var probe = NativeApi.Probe();
            if (!probe.IsHealthy)
            {
                DxLog.Warn($"Low-level hotkey hook is unavailable: {probe.Error ?? "native API mismatch"}.");
                _bindings = next;
                return;
            }

            _hookActive = NativeHotkey.TryStart(_windowHandle, HookMessage, nativeBindings, out var error);
            if (!_hookActive)
            {
                DxLog.Warn($"Low-level hotkey hook could not be started: {error}");
            }
        }
        else
        {
            var result = NativeHotkey.SetBindings(nativeBindings);
            if (result != 0)
            {
                DxLog.Warn($"Hotkey bindings could not be updated: {result}.");
            }
        }

        _bindings = next;
        DxLog.Info("Hotkey bindings: " + string.Join(", ", next.Select(binding =>
            HotkeySequence.Format(binding.Setting.Steps)
            + (string.IsNullOrWhiteSpace(binding.Setting.TargetProcess)
                ? string.Empty
                : $" [{binding.Setting.TargetProcess}]"))));
    }

    private static NativeHotkeyBinding CreateNativeBinding(uint id, HotkeySetting setting)
    {
        var steps = setting.Steps
            .Where(step => step.VirtualKey != 0 && (step.Modifiers & HotkeySetting.ModifierMask) != 0)
            .Take(NativeHotkeyBinding.MaxSteps)
            .ToList();

        var nativeSteps = new NativeHotkeyStep[NativeHotkeyBinding.MaxSteps];
        for (var index = 0; index < steps.Count; index++)
        {
            nativeSteps[index] = new NativeHotkeyStep
            {
                Modifiers = steps[index].Modifiers,
                VirtualKey = steps[index].VirtualKey,
            };
        }

        return new NativeHotkeyBinding
        {
            Id = id,
            StepCount = (uint)steps.Count,
            Steps = nativeSteps,
            Scope = string.IsNullOrWhiteSpace(setting.TargetProcess)
                ? NativeHotkey.GlobalScope
                : NativeHotkey.ProcessScope,
            ProcessName = setting.TargetProcess ?? string.Empty,
            Reserved = 0,
        };
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != HookMessage)
        {
            return IntPtr.Zero;
        }

        var id = (uint)wParam.ToInt64();
        var binding = _bindings.FirstOrDefault(item => item.Id == id);
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
        HotkeyAction.SnapToSplit1 => Strings.HotkeySnap1,
        HotkeyAction.SnapToSplit2 => Strings.HotkeySnap2,
        HotkeyAction.SnapToSplit3 => Strings.HotkeySnap3,
        HotkeyAction.SnapToSplit4 => Strings.HotkeySnap4,
        HotkeyAction.DesktopPrevious => Strings.HotkeyDesktopPrevious,
        HotkeyAction.DesktopNext => Strings.HotkeyDesktopNext,
        HotkeyAction.MoveWindowToPreviousDesktop => Strings.HotkeyMoveWindowPreviousDesktop,
        HotkeyAction.MoveWindowToNextDesktop => Strings.HotkeyMoveWindowNextDesktop,
        _ => action.ToString(),
    };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        NativeHotkey.Stop();
        _source?.RemoveHook(WindowProcedure);
    }

    private sealed record HotkeyBinding(uint Id, HotkeySetting Setting);
}
