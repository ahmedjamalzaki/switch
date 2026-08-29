using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SwitchStartup
{
    internal static class Program
    {
        private const string ApplicationMutexName = @"Local\Switch.KeyboardLayoutConverter";
        private const string LauncherMutexName = @"Local\Switch.StartupLauncher";
        private const string RootTaskName = @"\Switch";
        private const string FallbackTaskName = @"\Microsoft\Windows\Switch\Switch";

        private static int Main()
        {
            bool createdNewLauncher;
            using (var launcherMutex = new Mutex(true, LauncherMutexName, out createdNewLauncher))
            {
                if (!createdNewLauncher)
                    return 0;

                var applicationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Switch.exe");
                if (!File.Exists(applicationPath) || IsApplicationRunning())
                    return 0;

                if (TryRunScheduledTask(RootTaskName) || TryRunScheduledTask(FallbackTaskName))
                    return 0;

                // The logon-triggered task may already be starting in parallel
                // with this Startup-folder entry. Give it time before showing
                // the recovery UAC prompt.
                if (WaitForApplication(3000))
                    return 0;

                // This is only a recovery path for a missing or inaccessible task.
                // The normal startup path is silent because the task was registered
                // by the elevated installer with the highest run level.
                return TryStartElevated(applicationPath) ? 0 : 1;
            }
        }

        private static bool IsApplicationRunning()
        {
            bool createdNewApplicationMutex;
            using (var applicationMutex = new Mutex(false, ApplicationMutexName, out createdNewApplicationMutex))
            {
                return !createdNewApplicationMutex;
            }
        }

        private static bool WaitForApplication(int timeoutMilliseconds)
        {
            const int pollIntervalMilliseconds = 100;
            var elapsed = 0;

            while (elapsed < timeoutMilliseconds)
            {
                if (IsApplicationRunning())
                    return true;

                Thread.Sleep(pollIntervalMilliseconds);
                elapsed += pollIntervalMilliseconds;
            }

            return IsApplicationRunning();
        }

        private static bool TryRunScheduledTask(string taskName)
        {
            var schtasksPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "schtasks.exe");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = schtasksPath,
                    Arguments = string.Format("/Run /TN \"{0}\"", taskName),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null || !process.WaitForExit(5000))
                        return false;

                    return process.ExitCode == 0;
                }
            }
            catch (Win32Exception)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool TryStartElevated(string applicationPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = applicationPath,
                    WorkingDirectory = Path.GetDirectoryName(applicationPath),
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Win32Exception)
            {
                // The user may have declined the recovery UAC prompt.
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
