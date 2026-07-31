using System;
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
        internal static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int virtualKey);

        internal static bool IsKeyDown(byte virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        internal static void KeyUp(byte virtualKey)
        {
            keybd_event(virtualKey, 0, KeyEventKeyUp, UIntPtr.Zero);
        }

        internal static void KeyPress(byte virtualKey)
        {
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, KeyEventKeyUp, UIntPtr.Zero);
        }

        internal static void SendCopyOrPaste(byte key)
        {
            // Mirror the original Python keybd_event sequence exactly.
            KeyUp(VkShift);
            KeyUp(VkSpaceByte);
            System.Threading.Thread.Sleep(10);
            keybd_event(VkControl, 0, 0, UIntPtr.Zero);
            KeyPress(key);
            System.Threading.Thread.Sleep(10);
            KeyUp(VkControl);
        }
    }
}
