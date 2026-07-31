using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Switch
{
    internal static class ClipboardHelper
    {
        private const int Attempts = 12;
        private const int RetryDelayMilliseconds = 50;

        internal static bool TryGetText(out string text)
        {
            text = null;
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                try
                {
                    if (Clipboard.ContainsText()) text = Clipboard.GetText();
                    return true;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }
            return false;
        }

        internal static bool TryClear()
        {
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                try
                {
                    Clipboard.Clear();
                    return true;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }
            return false;
        }

        internal static bool TrySetText(string text)
        {
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                try
                {
                    Clipboard.SetText(text ?? string.Empty);
                    return true;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }
            return false;
        }

        internal static bool TryWaitForCopiedText(out string text)
        {
            text = null;
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                if (TryGetText(out text) && !string.IsNullOrEmpty(text)) return true;
                Thread.Sleep(RetryDelayMilliseconds);
            }
            return false;
        }
    }
}
