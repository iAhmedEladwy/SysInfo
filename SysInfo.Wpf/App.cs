using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CNSysInfo
{
    // Code-only App (identical behavior to the XAML version; the .NET Framework 4.8
    // build has no XAML markup-compile step, so the tree is built in code instead).
    public class App : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--apply-update")
            {
                MessageBox.Show("SysInfo now uses manual downloads. Open https://github.com/iAhmedEladwy/SysInfo/releases to download the latest EXE. Close SysInfo before replacing your old copy.", "SysInfo updates");
                return;
            }
            // Catch EVERY failure path, not just dispatcher ones: an exception thrown
            // while the window is constructed happens before the message loop exists,
            // so DispatcherUnhandledException never sees it and the app just vanishes.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                CrashLog.Show(e.ExceptionObject as Exception, "background thread");
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                CrashLog.Write(e.Exception, "unobserved task");
                e.SetObserved();
            };

            try
            {
                var app = new App();
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                CrashLog.Show(ex, "startup");
            }
        }

        public App()
        {
            // Guard for errors on the UI thread after the window is up.
            DispatcherUnhandledException += (s, e) =>
            {
                CrashLog.Show(e.Exception, "window");
                e.Handled = true;
            };
        }
    }

    // Writes a readable crash log next to the exe (falls back to %LOCALAPPDATA%) and
    // shows a dialog pointing at it, so a broken start is diagnosable instead of silent.
    internal static class CrashLog
    {
        public static void Show(Exception ex, string where)
        {
            if (ex == null) ex = new Exception("Unknown error");
            string path = Write(ex, where);
            var sb = new StringBuilder();
            sb.Append("SysInfo hit an unexpected problem").Append(where == "startup" ? " while starting" : "").Append(".\n\n");
            sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            if (ex.InnerException != null)
                sb.Append("\n").Append(ex.InnerException.GetType().Name).Append(": ").Append(ex.InnerException.Message);
            if (path != null)
                sb.Append("\n\nFull details were saved to:\n").Append(path);
            sb.Append("\n\nPlease send a screenshot of this message (or the log file) so it can be fixed.");
            try { MessageBox.Show(sb.ToString(), "SysInfo error", MessageBoxButton.OK, MessageBoxImage.Error); }
            catch { /* if even the dialog fails, the log file is still there */ }
        }

        public static string Write(Exception ex, string where)
        {
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string text =
                    "SysInfo crash report\n" +
                    "Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                    "Where: " + where + "\n" +
                    "OS: " + Environment.OSVersion + " (64-bit: " + Environment.Is64BitOperatingSystem + ")\n" +
                    "CLR: " + Environment.Version + "\n" +
                    "App: " + typeof(CrashLog).Assembly.GetName().Version + "\n\n" +
                    ex + "\n";
                string name = "SysInfo-crash-" + stamp + ".log";

                string exeDir = null;
                try { exeDir = Path.GetDirectoryName(typeof(CrashLog).Assembly.Location); } catch { }
                foreach (var dir in new[]
                {
                    exeDir,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ELADWYSOFT\\SysInfo")
                })
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    try
                    {
                        Directory.CreateDirectory(dir);
                        string p = Path.Combine(dir, name);
                        File.WriteAllText(p, text, new UTF8Encoding(true));
                        return p;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
    }
}
