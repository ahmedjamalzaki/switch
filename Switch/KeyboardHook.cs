using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Switch
{
    // Equivalent to Python's keyboard.add_hotkey: a global low-level keyboard hook.
    internal sealed class KeyboardHook : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private readonly Action hotkeyPressed;
        private readonly LowLevelKeyboardProc callback;
        private IntPtr hookHandle;
        private bool spaceIsDown;

        internal KeyboardHook(Action hotkeyPressed)
        {
            this.hotkeyPressed = hotkeyPressed;
            callback = HookCallback;
        }

        internal bool Start()
        {
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                hookHandle = SetWindowsHookEx(WhKeyboardLl, callback, GetModuleHandle(module.ModuleName), 0);
            }
            return hookHandle != IntPtr.Zero;
        }

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (code >= 0)
                {
                    var message = wParam.ToInt32();
                    var key = Marshal.ReadInt32(lParam);

                    if ((message == WmKeyUp || message == WmSysKeyUp) && key == NativeMethods.VkSpace)
                    {
                        spaceIsDown = false;
                    }
                    else if ((message == WmKeyDown || message == WmSysKeyDown) &&
                             key == NativeMethods.VkSpace && !spaceIsDown)
                    {
                        spaceIsDown = true;
                        if (NativeMethods.IsKeyDown(NativeMethods.VkControl) &&
                            NativeMethods.IsKeyDown(NativeMethods.VkShift))
                        {
                            hotkeyPressed();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // Exceptions must never cross the unmanaged hook callback.
                ErrorLog.Write("keyboard-hook-callback", exception);
            }

            try
            {
                return CallNextHookEx(hookHandle, code, wParam, lParam);
            }
            catch (Exception exception)
            {
                ErrorLog.Write("keyboard-hook-next", exception);
                return IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (hookHandle == IntPtr.Zero) return;
            UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
        }

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc procedure, IntPtr moduleHandle, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);
    }
}
