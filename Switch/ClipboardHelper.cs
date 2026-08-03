using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Switch
{
    internal static class ClipboardHelper
    {
        internal sealed class DataReadResult
        {
            internal readonly bool Succeeded;
            internal readonly IDataObject Data;

            internal DataReadResult(bool succeeded, IDataObject data)
            {
                Succeeded = succeeded;
                Data = data;
            }
        }

        internal sealed class TextReadResult
        {
            internal readonly bool Succeeded;
            internal readonly string Text;

            internal TextReadResult(bool succeeded, string text)
            {
                Succeeded = succeeded;
                Text = text;
            }
        }

        internal static DataReadResult ReadDataObjectOnce()
        {
            try
            {
                return new DataReadResult(true, Clipboard.GetDataObject());
            }
            catch (ExternalException)
            {
                return new DataReadResult(false, null);
            }
        }

        internal static TextReadResult ReadTextOnce()
        {
            try
            {
                var text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                return new TextReadResult(true, text);
            }
            catch (ExternalException)
            {
                return new TextReadResult(false, null);
            }
        }

        internal static bool TryClearOnce()
        {
            try
            {
                Clipboard.Clear();
                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        internal static bool TrySetTextOnce(string text)
        {
            try
            {
                Clipboard.SetText(text ?? string.Empty);
                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        internal static bool TryRestoreOnce(IDataObject data)
        {
            try
            {
                if (data == null) Clipboard.Clear();
                else Clipboard.SetDataObject(data, true);
                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
        }
    }
}
