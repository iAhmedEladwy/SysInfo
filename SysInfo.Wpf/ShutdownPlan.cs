using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CNSysInfo
{
    /// <summary>Pure helpers for reboot and graceful application restart (no UI).</summary>
    public static class ShutdownPlan
    {
        public const int DefaultDelaySeconds = 60;

        public static string RebootArguments(int delaySeconds, string comment)
        {
            if (delaySeconds < 5) delaySeconds = 5;      // never an instant, uncancelable reboot
            if (delaySeconds > 600) delaySeconds = 600;
            var c = (comment ?? string.Empty).Replace("\"", "'");
            return "/r /t " + delaySeconds + " /c \"" + c + "\"";
        }

        public const string AbortArguments = "/a";

        public static void Schedule(int delaySeconds, string comment)
        {
            Run("shutdown.exe", RebootArguments(delaySeconds, comment));
        }

        public static void Abort()
        {
            Run("shutdown.exe", AbortArguments);
        }

        private static void Run(string file, string args)
        {
            using (var p = Process.Start(new ProcessStartInfo { FileName = file, Arguments = args, UseShellExecute = false, CreateNoWindow = true }))
            {
                if (p != null) p.WaitForExit(10000);
            }
        }
    }

    public enum RestartResult { NotRunningStarted, ClosedGracefully, ForcedAfterConfirmation, UserDeclinedForce, StartFailed }

    public static class AppRestarter
    {
        /// <summary>
        /// Asks the app to close (CloseMainWindow), waits, and only kills if the caller's
        /// confirmForce callback returns true. Then starts it again.
        /// </summary>
        public static RestartResult Restart(string processName, string startTarget, int gracefulWaitMs, Func<bool> confirmForce)
        {
            var procs = Process.GetProcessesByName(processName).ToList();
            var result = RestartResult.NotRunningStarted;
            try
            {
                if (procs.Count > 0)
                {
                    foreach (var p in procs) { try { p.CloseMainWindow(); } catch { } }
                    var deadline = DateTime.UtcNow.AddMilliseconds(gracefulWaitMs);
                    while (DateTime.UtcNow < deadline && procs.Any(SafeRunning))
                        Thread.Sleep(250);

                    result = RestartResult.ClosedGracefully;
                    if (procs.Any(SafeRunning))
                    {
                        if (confirmForce == null || !confirmForce())
                            return RestartResult.UserDeclinedForce;
                        foreach (var p in procs.Where(SafeRunning)) { try { p.Kill(); p.WaitForExit(5000); } catch { } }
                        result = RestartResult.ForcedAfterConfirmation;
                    }
                }
                Process.Start(new ProcessStartInfo { FileName = startTarget, UseShellExecute = true });
                return result;
            }
            catch
            {
                return RestartResult.StartFailed;
            }
            finally
            {
                foreach (var p in procs) p.Dispose();
            }
        }

        private static bool SafeRunning(Process p)
        {
            try { return !p.HasExited; } catch { return false; }
        }
    }
}
