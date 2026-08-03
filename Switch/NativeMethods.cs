using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Switch
{
    internal static class NativeMethods
    {
        internal const int VkSpace = 0x20;
        internal const byte VkControl = 0x11;
        internal const byte VkShift = 0x10;
        internal const byte VkC = 0x43;
        internal const byte VkV = 0x56;
        internal const byte VkSpaceByte = 0x20;
        internal const uint KeyEventKeyUp = 0x0002;

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInput);

        private const uint InputKeyboard = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            internal uint Type;
            internal InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] internal MOUSEINPUT Mouse;
            [FieldOffset(0)] internal KEYBDINPUT Keyboard;
            [FieldOffset(0)] internal HARDWAREINPUT Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            internal int X;
            internal int Y;
            internal uint MouseData;
            internal uint Flags;
            internal uint Time;
            internal UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            internal ushort VirtualKey;
            internal ushort ScanCode;
            internal uint Flags;
            internal uint Time;
            internal UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            internal uint Message;
            internal ushort ParameterLow;
            internal ushort ParameterHigh;
        }

        internal static bool IsKeyDown(byte virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        internal static void KeyUp(byte virtualKey)
        {
            SendInputs(CreateKeyboardInput(virtualKey, KeyEventKeyUp));
        }

        internal static void SendCopyOrPaste(byte key)
        {
            KeyUp(VkShift);
            KeyUp(VkSpaceByte);
            System.Threading.Thread.Sleep(10);
            SendInputs(
                CreateKeyboardInput(VkControl, 0),
                CreateKeyboardInput(key, 0),
                CreateKeyboardInput(key, KeyEventKeyUp),
                CreateKeyboardInput(VkControl, KeyEventKeyUp));
        }

        private static INPUT CreateKeyboardInput(byte virtualKey, uint flags)
        {
            return new INPUT
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KEYBDINPUT
                    {
                        VirtualKey = virtualKey,
                        ScanCode = 0,
                        Flags = flags,
                        Time = 0,
                        ExtraInfo = UIntPtr.Zero
                    }
                }
            };
        }

        private static void SendInputs(params INPUT[] inputs)
        {
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent != inputs.Length)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput failed.");
        }
    }
}
