using Microsoft.Win32;

namespace CNSysInfo
{
    internal sealed class UpdatePreferences
    {
        public const string RegistryPath = @"Software\ELADWYSOFT\SysInfo\Updates";
        public bool CheckOnStartup = true;
        public string SkippedVersion = string.Empty;

        public static UpdatePreferences Load()
        {
            var settings = new UpdatePreferences();
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                if (key == null) return settings;
                settings.CheckOnStartup = !(key.GetValue("CheckOnStartup") is int value) || value != 0;
                settings.SkippedVersion = key.GetValue("SkippedVersion") as string ?? string.Empty;
            }
            return settings;
        }

        public void Save()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                if (key == null) throw new System.IO.IOException("Could not open update preferences.");
                key.SetValue("CheckOnStartup", CheckOnStartup ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("SkippedVersion", SkippedVersion ?? string.Empty, RegistryValueKind.String);
            }
        }
    }
}
