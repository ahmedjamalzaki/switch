using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace Switch
{
    internal static class StartupTaskLauncher
    {
        private const string ApplicationMutexName = @"Local\Switch.KeyboardLayoutConverter";
        private const string RootTaskName = @"\Switch";
        private const string FallbackTaskName = @"\Microsoft\Windows\Switch\Switch";

        internal static bool IsElevated()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception exception)
            {
                ErrorLog.Write("elevation-check", exception);
                return false;
            }
        }

        internal static bool IsApplicationRunning()
        {
            try
            {
                bool createdNewApplicationMutex;
                using (var applicationMutex = new Mutex(false, ApplicationMutexName, out createdNewApplicationMutex))
                {
                    return !createdNewApplicationMutex;
                }
            }
            catch (Exception exception)
            {
                ErrorLog.Write("instance-check", exception);
                return false;
            }
        }

        internal static bool TryRunScheduledTask()
        {
            return TryRunTask(RootTaskName) || TryRunTask(FallbackTaskName);
        }

        internal static bool WaitForApplication(int timeoutMilliseconds)
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

        internal static bool TryStartElevated()
        {
            try
            {
                var applicationPath = Process.GetCurrentProcess().MainModule.FileName;
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
                // The user may have declined the UAC prompt.
                return false;
            }
            catch (InvalidOperationException exception)
            {
                ErrorLog.Write("elevation-start", exception);
                return false;
            }
            catch (Exception exception)
            {
                ErrorLog.Write("elevation-start", exception);
                return false;
            }
        }

        private static bool TryRunTask(string taskName)
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
            catch (Exception exception)
            {
                ErrorLog.Write("startup-task", exception);
                return false;
            }
        }
    }
}
