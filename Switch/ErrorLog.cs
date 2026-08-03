using System;
using System.IO;
using System.Text;

namespace Switch
{
    internal static class ErrorLog
    {
        private const long MaximumLogBytes = 1024 * 1024;
        private static readonly object Sync = new object();

        internal static void Write(string operation, Exception exception)
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Switch");
                Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, "errors.log");
                var line = string.Format(
                    "[{0:O}] {1}: {2}{3}",
                    DateTime.UtcNow,
                    operation,
                    exception.GetType().FullName,
                    Environment.NewLine + exception.Message + Environment.NewLine);

                lock (Sync)
                {
                    if (File.Exists(path) && new FileInfo(path).Length > MaximumLogBytes)
                        File.Delete(path);

                    File.AppendAllText(path, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never break the conversion path.
            }
        }
    }
}
