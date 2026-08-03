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
        private readonly ContextMenuStrip contextMenu;
        private readonly CancellationTokenSource conversionCancellation = new CancellationTokenSource();
        private readonly object inputSync = new object();
        private KeyboardHook keyboardHook;

        private const int ClipboardAttempts = 12;
        private const int ClipboardRetryDelayMs = 50;
        private int isProcessing;

        internal HotkeyWindow()
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("طريقة الاستخدام (How to use)", null, ShowHelp);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("خروج (Exit)", null, (_, __) => Close());
            trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                       ?? SystemIcons.Application,
                Text = "Switch - محول النص العربي والإنجليزي",
                ContextMenuStrip = contextMenu,
                Visible = true
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                keyboardHook = new KeyboardHook(OnGlobalHotkeyPressed);
                if (!keyboardHook.Start())
                {
                    ErrorLog.Write("keyboard-hook", new InvalidOperationException("Unable to install the low-level keyboard hook."));
                    trayIcon.ShowBalloonTip(5000, "Switch",
                        "تعذر تشغيل مراقبة لوحة المفاتيح. أعد تشغيل البرنامج.",
                        ToolTipIcon.Warning);
                }
            }
            catch (Exception exception)
            {
                ErrorLog.Write("keyboard-hook", exception);
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
            if (!IsDisposed && IsHandleCreated && !conversionCancellation.IsCancellationRequested)
                Task.Run(() => DoConversion(conversionCancellation.Token));
        }

        // ------------------------------------------------------------------ //
        // Runs entirely on a ThreadPool thread.
        // Thread.Sleep calls here do NOT block the message pump, so the
        // keyboard hook callback is always serviced on time by Windows.
        // Clipboard access (which requires the STA thread) is marshalled back
        // to the UI thread with SafeInvoke — a quick, non-sleeping call.
        // ------------------------------------------------------------------ //
        private void DoConversion(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref isProcessing, 1, 0) != 0) return;

            IDataObject originalClipboard = null;
            var clipboardChanged = false;
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                WaitForHotkeyRelease(cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                if (!TryGetClipboardSnapshot(cancellationToken, out originalClipboard)) return;
                if (!TryRunOnUiWithRetry(ClipboardHelper.TryClearOnce, cancellationToken)) return;
                clipboardChanged = true;

                if (WaitWithCancellation(50, cancellationToken)) return;
                lock (inputSync)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    NativeMethods.SendCopyOrPaste(NativeMethods.VkC);
                }

                string selectedText;
                if (!TryReadSelectedText(cancellationToken, out selectedText)) return;

                var convertedText = KeyboardLayoutConverter.Convert(selectedText);
                if (string.Equals(convertedText, selectedText, StringComparison.Ordinal)) return;
                if (cancellationToken.IsCancellationRequested) return;

                if (!TryRunOnUiWithRetry(() => ClipboardHelper.TrySetTextOnce(convertedText), cancellationToken)) return;
                if (WaitWithCancellation(50, cancellationToken)) return;

                lock (inputSync)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    NativeMethods.SendCopyOrPaste(NativeMethods.VkV);
                }
                WaitWithCancellation(100, cancellationToken);
            }
            catch (Exception exception)
            {
                ErrorLog.Write("conversion", exception);
                SafeInvoke(() =>
                    trayIcon.ShowBalloonTip(3000, "Switch",
                        "حدث خطأ غير متوقع أثناء تحويل النص.", ToolTipIcon.Error));
            }
            finally
            {
                if (clipboardChanged)
                {
                    if (!TryRunOnUiWithRetry(() => ClipboardHelper.TryRestoreOnce(originalClipboard), CancellationToken.None))
                        ErrorLog.Write("clipboard-restore", new InvalidOperationException("Clipboard restoration failed."));
                }

                Interlocked.Exchange(ref isProcessing, 0);
            }
        }

        private bool TryGetClipboardSnapshot(CancellationToken cancellationToken, out IDataObject data)
        {
            data = null;

            for (var attempt = 0; attempt < ClipboardAttempts; attempt++)
            {
                ClipboardHelper.DataReadResult result = null;
                SafeInvoke(() => result = ClipboardHelper.ReadDataObjectOnce());

                if (result != null && result.Succeeded)
                {
                    data = result.Data;
                    return true;
                }

                if (WaitWithCancellation(ClipboardRetryDelayMs, cancellationToken)) return false;
            }

            return false;
        }

        private bool TryReadSelectedText(CancellationToken cancellationToken, out string text)
        {
            text = null;
            for (var attempt = 0; attempt < ClipboardAttempts; attempt++)
            {
                ClipboardHelper.TextReadResult result = null;
                SafeInvoke(() => result = ClipboardHelper.ReadTextOnce());

                if (result != null && result.Succeeded && !string.IsNullOrEmpty(result.Text))
                {
                    text = result.Text;
                    return true;
                }

                if (WaitWithCancellation(ClipboardRetryDelayMs, cancellationToken)) return false;
            }

            return false;
        }

        private bool TryRunOnUi(Func<bool> operation)
        {
            var result = false;
            if (!SafeInvoke(() => result = operation())) return false;
            return result;
        }

        private bool TryRunOnUiWithRetry(Func<bool> operation, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < ClipboardAttempts; attempt++)
            {
                if (TryRunOnUi(operation)) return true;
                if (WaitWithCancellation(ClipboardRetryDelayMs, cancellationToken)) return false;
            }

            return false;
        }

        private bool SafeInvoke(MethodInvoker action)
        {
            if (IsDisposed || !IsHandleCreated) return false;

            try
            {
                Invoke(action);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool WaitWithCancellation(int milliseconds, CancellationToken cancellationToken)
        {
            var elapsed = 0;
            const int slice = 10;

            while (elapsed < milliseconds)
            {
                if (cancellationToken.IsCancellationRequested) return true;
                var delay = Math.Min(slice, milliseconds - elapsed);
                Thread.Sleep(delay);
                elapsed += delay;
            }

            return cancellationToken.IsCancellationRequested;
        }

        private static void WaitForHotkeyRelease(CancellationToken cancellationToken)
        {
            const int maximumWaitMs = 1000;
            const int pollIntervalMs = 10;
            var elapsed = 0;

            while (!cancellationToken.IsCancellationRequested && elapsed < maximumWaitMs &&
                   (NativeMethods.IsKeyDown(NativeMethods.VkControl) ||
                    NativeMethods.IsKeyDown(NativeMethods.VkShift) ||
                    NativeMethods.IsKeyDown(NativeMethods.VkSpaceByte)))
            {
                Thread.Sleep(pollIntervalMs);
                elapsed += pollIntervalMs;
            }
        }

        private void ShowHelp(object sender, EventArgs e)
        {
            const string message = "حدد النص المكتوب بتخطيط خاطئ ثم اضغط Ctrl + Shift + Space لتحويله.\n\n" +
                                   "لإيقاف البرنامج، افتح قائمة الأيقونة بجانب الساعة واختر خروج.";

            MessageBox.Show(message, "Switch - EN ↔ AR", MessageBoxButtons.OK, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            lock (inputSync)
            {
                conversionCancellation.Cancel();
            }
            if (keyboardHook != null) keyboardHook.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            contextMenu.Dispose();
            base.OnFormClosed(e);
        }
    }
}
