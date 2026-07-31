using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Switch
{
    internal sealed class HotkeyWindow : Form
    {
        private readonly NotifyIcon trayIcon;
        private KeyboardHook keyboardHook;

        // 0 = idle, 1 = busy. Interlocked because the hotkey fires on the hook
        // thread while DoConversion runs on a ThreadPool thread.
        private int _isProcessing;

        internal HotkeyWindow()
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;

            var menu = new ContextMenuStrip();
            menu.Items.Add("خروج (Exit)", null, (_, __) => Close());
            trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                       ?? SystemIcons.Application,
                Text = "Switch - محول النص العربي والإنجليزي",
                ContextMenuStrip = menu,
                Visible = true
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            keyboardHook = new KeyboardHook(OnGlobalHotkeyPressed);
            if (!keyboardHook.Start())
            {
                trayIcon.ShowBalloonTip(5000, "Switch",
                    "تعذر تشغيل مراقبة لوحة المفاتيح. أعد تشغيل البرنامج.",
                    ToolTipIcon.Warning);
            }
            Hide();
        }

        // ------------------------------------------------------------------ //
        // Hotkey entry point — called from the keyboard hook (UI thread).
        // We immediately hand off to the thread pool so the UI thread (and
        // therefore the Windows message pump and the hook itself) stay free.
        // ------------------------------------------------------------------ //
        private void OnGlobalHotkeyPressed()
        {
            if (!IsDisposed && IsHandleCreated)
                Task.Run((Action)DoConversion);
        }

        // ------------------------------------------------------------------ //
        // Runs entirely on a ThreadPool thread.
        // Thread.Sleep calls here do NOT block the message pump, so the
        // keyboard hook callback is always serviced on time by Windows.
        // Clipboard access (which requires the STA thread) is marshalled back
        // to the UI thread with SafeInvoke — a quick, non-sleeping call.
        // ------------------------------------------------------------------ //
        private void DoConversion()
        {
            // Allow only one conversion at a time (atomic swap).
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0) return;

            string originalClipboard = null;
            var clipboardChanged = false;
            try
            {
                // Wait until Ctrl + Shift + Space are fully released.
                // Thread.Sleep here is fine — we are on a background thread.
                WaitForHotkeyRelease();

                // Save and clear the clipboard on the STA (UI) thread.
                SafeInvoke(() =>
                {
                    ClipboardHelper.TryGetText(out originalClipboard);
                    ClipboardHelper.TryClear();
                });
                clipboardChanged = true;

                Thread.Sleep(50);                               // background — safe
                NativeMethods.SendCopyOrPaste(NativeMethods.VkC);

                // Poll for the newly copied text on the background thread.
                // Each clipboard read is a single quick Invoke, while the
                // sleep happens here (not on the UI thread).
                string selectedText = null;
                const int maxAttempts = 12;
                const int retryDelayMs = 50;
                for (var i = 0; i < maxAttempts && string.IsNullOrEmpty(selectedText); i++)
                {
                    Thread.Sleep(retryDelayMs);                 // background — safe
                    SafeInvoke(() => ClipboardHelper.TryGetText(out selectedText));
                }

                if (string.IsNullOrEmpty(selectedText)) return;

                var convertedText = KeyboardLayoutConverter.Convert(selectedText);
                if (convertedText == selectedText) return;

                SafeInvoke(() => ClipboardHelper.TrySetText(convertedText));
                Thread.Sleep(50);                               // background — safe

                NativeMethods.SendCopyOrPaste(NativeMethods.VkV);
                Thread.Sleep(100);                              // background — safe
            }
            catch (Exception)
            {
                SafeInvoke(() =>
                    trayIcon.ShowBalloonTip(3000, "Switch",
                        "حدث خطأ غير متوقع أثناء تحويل النص.", ToolTipIcon.Error));
            }
            finally
            {
                // Always restore the original clipboard and release the lock.
                if (clipboardChanged)
                    SafeInvoke(() => ClipboardHelper.TrySetText(originalClipboard));

                Interlocked.Exchange(ref _isProcessing, 0);
            }
        }

        // ------------------------------------------------------------------ //
        // Marshals an action to the UI (STA) thread, but only when the form
        // is still alive. Silently ignored if the form is already disposed.
        // ------------------------------------------------------------------ //
        private void SafeInvoke(MethodInvoker action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { Invoke(action); }
            catch (ObjectDisposedException) { }
        }

        // ------------------------------------------------------------------ //
        // Spins on a background thread until all hotkey keys are released
        // (or 1 s elapses). The UI thread is NOT involved, so the message
        // pump keeps running and the hook stays responsive throughout.
        // ------------------------------------------------------------------ //
        private static void WaitForHotkeyRelease()
        {
            const int maximumWaitMs = 1000;
            const int pollIntervalMs = 10;
            var elapsed = 0;

            while (elapsed < maximumWaitMs &&
                   (NativeMethods.IsKeyDown(NativeMethods.VkControl) ||
                    NativeMethods.IsKeyDown(NativeMethods.VkShift) ||
                    NativeMethods.IsKeyDown(NativeMethods.VkSpaceByte)))
            {
                Thread.Sleep(pollIntervalMs);
                elapsed += pollIntervalMs;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (keyboardHook != null) keyboardHook.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnFormClosed(e);
        }
    }
}
