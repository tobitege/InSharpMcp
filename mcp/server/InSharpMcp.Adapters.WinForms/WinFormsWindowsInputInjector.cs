using InSharpMcp.Contracts;
using System.Runtime.InteropServices;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsWindowsInputInjector : IWinFormsInputInjector
{
    public ToolResult PointerClick(int screenX, int screenY)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ToolResult.Fail("WinForms pointer input requires Windows SendInput.", "unsupported");
        }

        if (!SetCursorPos(screenX, screenY))
        {
            return Win32Failure("Unable to position the pointer.");
        }

        var inputs = new[]
        {
            MouseInput(MouseEventFlags.LeftDown),
            MouseInput(MouseEventFlags.LeftUp),
        };
        return Send(inputs, "Pointer click sent.");
    }

    public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ToolResult.Fail("WinForms key input requires Windows SendInput.", "unsupported");
        }

        if (!TryGetVirtualKey(key, out var keyCode))
        {
            return ToolResult.Fail("Key is unsupported by the WinForms input injector.", "unsupported_key");
        }

        var modifierKeys = modifiers.Select(GetModifierVirtualKey).ToArray();
        if (modifierKeys.Any(static virtualKey => virtualKey == 0))
        {
            return ToolResult.Fail("One or more key modifiers are unsupported by the WinForms input injector.", "unsupported_modifier");
        }

        var inputs = new List<Input>();
        inputs.AddRange(modifierKeys.Select(static modifierKey => KeyboardInput(modifierKey)));
        inputs.Add(KeyboardInput(keyCode));
        inputs.Add(KeyboardInput(keyCode, KeyEventFlags.KeyUp));

        for (var index = modifierKeys.Length - 1; index >= 0; index--)
        {
            inputs.Add(KeyboardInput(modifierKeys[index], KeyEventFlags.KeyUp));
        }

        return Send(inputs.ToArray(), "Key press sent.");
    }

    public ToolResult TypeText(string text)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ToolResult.Fail("WinForms text input requires Windows SendInput.", "unsupported");
        }

        var inputs = new List<Input>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(UnicodeInput(character));
            inputs.Add(UnicodeInput(character, KeyEventFlags.KeyUp));
        }

        return Send(inputs.ToArray(), "Text input sent.");
    }

    private static Input MouseInput(MouseEventFlags flags) =>
        new()
        {
            Type = InputType.Mouse,
            Data = new InputUnion
            {
                Mouse = new MouseInputData
                {
                    Flags = flags,
                },
            },
        };

    private static Input KeyboardInput(ushort virtualKey, KeyEventFlags flags = 0) =>
        new()
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    VirtualKey = virtualKey,
                    Flags = flags,
                },
            },
        };

    private static Input UnicodeInput(char character, KeyEventFlags flags = 0) =>
        new()
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    ScanCode = character,
                    Flags = flags | KeyEventFlags.Unicode,
                },
            },
        };

    private static ToolResult Send(Input[] inputs, string successMessage)
    {
        if (inputs.Length == 0)
        {
            return ToolResult.Ok(successMessage);
        }

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        return sent == inputs.Length
            ? ToolResult.Ok(successMessage)
            : Win32Failure("Unable to send input.");
    }

    private static ToolResult Win32Failure(string message) =>
        ToolResult.Fail($"{message} Win32 error: {Marshal.GetLastWin32Error().ToString(System.Globalization.CultureInfo.InvariantCulture)}.", "platform_error");

    private static ushort GetModifierVirtualKey(string modifier) =>
        modifier.ToLowerInvariant() switch
        {
            "alt" => VirtualKeys.Menu,
            "control" or "ctrl" => VirtualKeys.Control,
            "shift" => VirtualKeys.Shift,
            "meta" or "win" => VirtualKeys.LWin,
            _ => 0,
        };

    private static bool TryGetVirtualKey(string key, out ushort virtualKey)
    {
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            virtualKey = (ushort)char.ToUpperInvariant(key[0]);
            return true;
        }

        virtualKey = key.ToLowerInvariant() switch
        {
            "enter" => VirtualKeys.Return,
            "escape" => VirtualKeys.Escape,
            "tab" => VirtualKeys.Tab,
            "backspace" => VirtualKeys.Back,
            "delete" => VirtualKeys.Delete,
            "space" => VirtualKeys.Space,
            "arrowup" => VirtualKeys.Up,
            "arrowdown" => VirtualKeys.Down,
            "arrowleft" => VirtualKeys.Left,
            "arrowright" => VirtualKeys.Right,
            "home" => VirtualKeys.Home,
            "end" => VirtualKeys.End,
            "pageup" => VirtualKeys.PageUp,
            "pagedown" => VirtualKeys.PageDown,
            _ => 0,
        };

        if (virtualKey != 0)
        {
            return true;
        }

        if (key.Length is >= 2 and <= 3
            && key[0] == 'F'
            && int.TryParse(key[1..], out var functionKey)
            && functionKey is >= 1 and <= 12)
        {
            virtualKey = (ushort)(VirtualKeys.F1 + functionKey - 1);
            return true;
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    private static class VirtualKeys
    {
        public const ushort Back = 0x08;
        public const ushort Tab = 0x09;
        public const ushort Return = 0x0D;
        public const ushort Shift = 0x10;
        public const ushort Control = 0x11;
        public const ushort Menu = 0x12;
        public const ushort Escape = 0x1B;
        public const ushort Space = 0x20;
        public const ushort PageUp = 0x21;
        public const ushort PageDown = 0x22;
        public const ushort End = 0x23;
        public const ushort Home = 0x24;
        public const ushort Left = 0x25;
        public const ushort Up = 0x26;
        public const ushort Right = 0x27;
        public const ushort Down = 0x28;
        public const ushort Delete = 0x2E;
        public const ushort LWin = 0x5B;
        public const ushort F1 = 0x70;
    }

    private enum InputType : uint
    {
        Mouse = 0,
        Keyboard = 1,
    }

    [Flags]
    private enum MouseEventFlags : uint
    {
        LeftDown = 0x0002,
        LeftUp = 0x0004,
    }

    [Flags]
    private enum KeyEventFlags : uint
    {
        KeyUp = 0x0002,
        Unicode = 0x0004,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public InputType Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInputData Mouse;

        [FieldOffset(0)]
        public KeyboardInputData Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int X;
        public int Y;
        public uint MouseData;
        public MouseEventFlags Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public KeyEventFlags Flags;
        public uint Time;
        public nuint ExtraInfo;
    }
}
