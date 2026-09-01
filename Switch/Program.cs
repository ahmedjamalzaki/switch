using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Switch
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Any(argument => string.Equals(argument, "--test", StringComparison.OrdinalIgnoreCase)))
            {
                SelfTests.Run();
                return;
            }

            var startupMode = args.Any(argument =>
                string.Equals(argument, "--startup", StringComparison.OrdinalIgnoreCase));

            if (!StartupTaskLauncher.IsElevated())
            {
                if (startupMode)
                {
                    if (StartupTaskLauncher.IsApplicationRunning())
                        return;

                    StartupTaskLauncher.TryRunScheduledTask();
                    if (StartupTaskLauncher.WaitForApplication(3000))
                        return;
                }

                StartupTaskLauncher.TryStartElevated();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            bool createdNewInstance;
            using (var singleInstance = new Mutex(true, @"Local\Switch.KeyboardLayoutConverter", out createdNewInstance))
            {
                if (!createdNewInstance) return;

                Application.Run(new HotkeyWindow());
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ErrorLog.Write("ui-thread", e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ??
                            new Exception("An unhandled non-Exception object was thrown.");
            ErrorLog.Write("unhandled", exception);
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ErrorLog.Write("unobserved-task", e.Exception);
            e.SetObserved();
        }
    }
}
