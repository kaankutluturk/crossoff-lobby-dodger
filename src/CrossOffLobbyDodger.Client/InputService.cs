using System.Runtime.InteropServices;
using System.Text;

namespace CrossOff.LobbyDodger;

public static class InputService
{
    private const int InputKeyboard = 1;
    private const ushort VirtualKeyEscape = 0x1B;
    private const uint KeyEventKeyUp = 0x0002;

    public static bool ForegroundLooksLikeDeadByDaylight()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return false;
        }

        var title = new StringBuilder(512);
        _ = GetWindowText(foregroundWindow, title, title.Capacity);
        string foregroundTitle = title.ToString();
        return foregroundTitle.Contains("Dead by Daylight", StringComparison.OrdinalIgnoreCase) ||
               foregroundTitle.Contains("DeadByDaylight", StringComparison.OrdinalIgnoreCase);
    }

    public static bool SendEscape()
    {
        Input[] inputs =
        [
            new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = VirtualKeyEscape
                    }
                }
            },
            new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = VirtualKeyEscape,
                        Flags = KeyEventKeyUp
                    }
                }
            }
        ];

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == (uint)inputs.Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public int Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximumCount);
}
