using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SwitchSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            const string question = "سيتم تثبيت Switch بصلاحية المسؤول وتشغيله تلقائياً عند تسجيل الدخول إلى ويندوز. هل تريد المتابعة؟";
            if (MessageBox.Show(question, "Switch Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) != DialogResult.Yes)
                return;

            try
            {
                var installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Switch");
                Directory.CreateDirectory(installDirectory);
                var executablePath = Path.Combine(installDirectory, "Switch.exe");
                ExtractApplication(executablePath);
                CreateStartupTask(executablePath);

                MessageBox.Show("تم تثبيت Switch بنجاح. سيعمل الآن، وسيبدأ تلقائياً مع ويندوز بصلاحية المسؤول.",
                    "Switch Setup", MessageBoxButtons.OK, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                Process.Start(executablePath);
            }
            catch (Exception exception)
            {
                MessageBox.Show("تعذر إكمال التثبيت:\n" + exception.Message, "Switch Setup", MessageBoxButtons.OK,
                    MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            }
        }

        private static void ExtractApplication(string destination)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var source = assembly.GetManifestResourceStream("Switch.Payload.exe"))
            {
                if (source == null) throw new InvalidOperationException("ملف التطبيق غير موجود داخل المثبت.");
                using (var output = File.Create(destination))
                {
                    source.CopyTo(output);
                }
            }
        }

        private static void CreateStartupTask(string executablePath)
        {
            var info = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/create /tn \"Switch\" /tr \"\\\"" + executablePath + "\\\"\" /sc onlogon /rl highest /f",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(info))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("تعذر إنشاء مهمة بدء التشغيل: " + process.StandardError.ReadToEnd());
            }
        }
    }
}
