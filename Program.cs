using System;
using System.Reflection;
using System.Windows.Forms;

namespace CNSysInfo
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            TryConfigureHighDpiMode();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmSysInfo());
        }

        private static void TryConfigureHighDpiMode()
        {
            try
            {
                var assembly = typeof(Application).Assembly;
                var highDpiModeType = assembly.GetType("System.Windows.Forms.HighDpiMode");
                if (highDpiModeType == null)
                {
                    return;
                }

                var perMonitorV2Value = Enum.Parse(highDpiModeType, "PerMonitorV2");

                var setHighDpiMode = typeof(Application).GetMethod(
                    "SetHighDpiMode",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { highDpiModeType },
                    modifiers: null);

                setHighDpiMode?.Invoke(null, new[] { perMonitorV2Value });
            }
            catch
            {
                // Ignore - API not available on current framework/runtime.
            }
        }
    }
}
