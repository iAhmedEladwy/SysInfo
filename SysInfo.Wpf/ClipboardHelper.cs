using System;
using System.Text;
using System.Windows;

namespace CNSysInfo
{
    internal static class ClipboardHelper
    {
        public static void CopySystemInfoToClipboard(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                System.Windows.MessageBox.Show("No system information available to copy.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var text = BuildClipboardText(snapshot);
                Clipboard.SetText(text);
                System.Windows.MessageBox.Show("System information copied to clipboard!", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to copy to clipboard: " + ex.Message, "Clipboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal static string BuildClipboardText(SystemInfoSnapshot snapshot)
        {
            var builder = new StringBuilder();

            builder.AppendLine("=== SYSTEM INFORMATION REPORT ===");
            builder.AppendLine("Generated: " + snapshot.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine();

            // Computer Info
            builder.AppendLine("--- COMPUTER ---");
            builder.AppendLine("PC Name: " + snapshot.MachineName);
            builder.AppendLine("User: " + snapshot.UserName);
            builder.AppendLine("Manufacturer: " + snapshot.Manufacturer);
            builder.AppendLine("Model: " + snapshot.SystemModel);
            builder.AppendLine("Serial Number: " + snapshot.SerialNumber);
            builder.AppendLine("Asset Tag: " + snapshot.AssetTag);
            builder.AppendLine();

            // Operating System
            builder.AppendLine("--- OPERATING SYSTEM ---");
            builder.AppendLine("OS: " + snapshot.OsDisplay);
            builder.AppendLine("Activation: " + snapshot.WindowsActivationStatus);
            builder.AppendLine("Installed On: " + snapshot.OsInstallDate);
            builder.AppendLine("Uptime: " + snapshot.WindowsUptimeDisplay);
            builder.AppendLine("BIOS: " + snapshot.BiosVersion);
            builder.AppendLine("Timezone: " + snapshot.TimeZoneDisplayName);
            builder.AppendLine();

            // Hardware
            builder.AppendLine("--- HARDWARE ---");
            builder.AppendLine("CPU: " + snapshot.Cpu);
            var memoryLine = snapshot.InstalledPhysicalMemory + " (" + snapshot.UsablePhysicalMemory + " usable)";
            if (snapshot.MemorySlotsTotal > 0)
            {
                var usedSlots = snapshot.MemorySlotsUsed;
                if (usedSlots < 0)
                    usedSlots = 0;
                if (usedSlots > snapshot.MemorySlotsTotal)
                    usedSlots = snapshot.MemorySlotsTotal;
                memoryLine += " | Slots: " + usedSlots + "/" + snapshot.MemorySlotsTotal;
            }
            if (snapshot.MemorySpeedMhz > 0)
            {
                memoryLine += " | Speed: " + snapshot.MemorySpeedMhz + " MHz";
            }
            builder.AppendLine("Memory: " + memoryLine);
            builder.AppendLine();

            // Storage
            builder.AppendLine("Storage:");
            foreach (var disk in snapshot.Disks)
            {
                builder.AppendLine("  Disk" + disk.Index + ": " + disk.Model + " | " + disk.Size + " | " + disk.MediaType);
            }
            builder.AppendLine();

            // Graphics
            builder.AppendLine("GPU(s):");
            for (var i = 0; i < snapshot.Gpus.Count; i++)
            {
                var gpu = snapshot.Gpus[i];
                builder.AppendLine("  GPU" + (i + 1) + ": " + gpu.Name + " | VRAM: " + gpu.AdapterRam);
            }
            builder.AppendLine();

            // Battery
            if (snapshot.BatteryInfo != null)
            {
                builder.AppendLine("--- BATTERY ---");
                builder.AppendLine("Name: " + snapshot.BatteryInfo.Name);
                var healthText = snapshot.BatteryInfo.HealthPercentage > 0
                    ? snapshot.BatteryInfo.HealthPercentage + "%"
                    : "Unknown";
                builder.AppendLine("Health: " + healthText);
                if (snapshot.BatteryInfo.DesignCapacity > 0)
                {
                    builder.AppendLine("Design Capacity: " + FormatBatteryCapacity(snapshot.BatteryInfo.DesignCapacity));
                }
                if (snapshot.BatteryInfo.FullChargeCapacity > 0)
                {
                    builder.AppendLine("Full Charge Capacity: " + FormatBatteryCapacity(snapshot.BatteryInfo.FullChargeCapacity));
                }
                builder.AppendLine("Status: " + snapshot.BatteryInfo.Status);
                builder.AppendLine("Power: " + snapshot.BatteryInfo.PowerStatus);
                builder.AppendLine();
            }

            builder.AppendLine("=== END OF REPORT ===");

            return builder.ToString();
        }

        private static string FormatBatteryCapacity(int? capacity)
        {
            return capacity > 0 ? string.Format("{0:N0} mWh", capacity.Value) : "Unknown";
        }
    }
}
