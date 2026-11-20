using System.Runtime.InteropServices;

namespace DxTools.Engine;

public sealed class VirtualDesktopService
{
    private const ushort VkLeft = 0x25;
    private const ushort VkRight = 0x27;
    private const ushort VkControl = 0x11;
    private const ushort VkShift = 0x10;
    private const ushort VkLWin = 0x5B;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint InputKeyboard = 1;

    public bool Previous() => Send(false, false);

    public bool Next() => Send(true, false);

    public bool MoveWindowPrevious() => Send(false, true);

    public bool MoveWindowNext() => Send(true, true);

    private static bool Send(bool right, bool moveWindow)
    {
        var keys = new List<Input>
        {
            Key(VkLWin),
            Key(VkControl),
        };

        if (moveWindow)
        {
            keys.Add(Key(VkShift));
        }

        keys.Add(Key(right ? VkRight : VkLeft));
        keys.Add(Key(right ? VkRight : VkLeft, up: true));

        if (moveWindow)
        {
            keys.Add(Key(VkShift, up: true));
        }

        keys.Add(Key(VkControl, up: true));
        keys.Add(Key(VkLWin, up: true));

        var inputs = keys.ToArray();
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input Key(ushort virtualKey, bool up = false) => new()
    {
        Type = InputKeyboard,
        Keyboard = new KeyboardInput
        {
            VirtualKey = virtualKey,
            Flags = up ? KeyEventKeyUp : 0,
        },
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);
}
