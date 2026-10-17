using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CNSysInfo
{

    public sealed class ReportDisk { public string Index = ""; public string Model = ""; public string Size = ""; public string MediaType = ""; public string InterfaceType = ""; }
    public sealed class ReportGpu { public string Name = ""; public string AdapterRam = ""; }
    public sealed class ReportBattery { public int HealthPercentage; public int DesignCapacity; public int FullChargeCapacity; public string Status = ""; public string PowerStatus = ""; }

    /// <summary>UI-independent snapshot for reports (so it can be unit tested without WinForms/WMI).</summary>
    public sealed class ReportModel
    {
        public DateTime CapturedAt;
        public string TimeZoneDisplayName = "", MachineName = "", UserName = "", Manufacturer = "", SystemModel = "",
            SerialNumber = "", AssetTag = "", BiosVersion = "", Cpu = "", OsDisplay = "", OsInstallDate = "",
            WindowsActivationStatus = "", UptimeDisplay = "", InstalledPhysicalMemory = "", UsablePhysicalMemory = "";
        public ulong InstalledPhysicalMemoryBytes;
        public int CpuCoreCount, CpuLogicalProcessorCount, MemorySlotsUsed, MemorySlotsTotal, MemorySpeedMhz;
        public ulong? CpuL2CacheKb, CpuL3CacheKb;
        public CpuSupportLevel CpuSupport = CpuSupportLevel.Unknown;
        public List<ReportDisk> Disks = new List<ReportDisk>();
        public List<ReportGpu> Gpus = new List<ReportGpu>();
        public ReportBattery Battery;
    }

    /// <summary>Builds the text report. Same rating computation as the UI (SpecRatingEngine).</summary>
    public static class ReportBuilder
    {
        public const string Redacted = "[redacted]";

        public static SpecRatingLevel ComputeRating(ReportModel s)
        {
            if (s == null) return SpecRatingLevel.Unknown;
            var cpu = s.CpuSupport;
            var gb = SpecRatingEngine.MemoryGb(s.InstalledPhysicalMemoryBytes, s.InstalledPhysicalMemory);
            var disk = ResolvePrimaryDisk(s.Disks);
            var kind = disk == null ? StorageKind.Unknown : SpecRatingEngine.ClassifyStorage(disk.MediaType, disk.Model, disk.InterfaceType);
            return SpecRatingEngine.Evaluate(cpu, gb, kind);
        }

        public static ReportDisk ResolvePrimaryDisk(IReadOnlyList<ReportDisk> disks)
        {
            if (disks == null || disks.Count == 0) return null;
            ReportDisk first = null;
            foreach (var d in disks)
            {
                if (d == null) continue;
                if (string.Equals((d.Index ?? string.Empty).Trim(), "0", StringComparison.Ordinal)) return d;
                if (first == null) first = d;
            }
            return first;
        }

        public static string SuggestedFileName(ReportModel s, bool redact)
        {
            var model = Sanitize(s.SystemModel);
            var stamp = s.CapturedAt.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            return redact ? "SysInfo_" + model + "_" + stamp
                          : "SysInfo_" + model + "_" + Sanitize(s.SerialNumber) + "_" + stamp;
        }

        public static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            var sb = new StringBuilder();
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in name.Trim()) sb.Append(invalid.Contains(c) || c == ' ' ? '_' : c);
            return sb.ToString();
        }

        public static string Build(ReportModel s, bool redact, string appVersion)
        {
            var b = new StringBuilder();
            string Pc(string v) { return redact ? Redacted : v; }

            b.AppendLine("=== SYSTEM INFORMATION ===");
            if (redact) b.AppendLine("(Personal identifiers redacted: user name, PC name, serial numbers, asset tag)");
            b.AppendLine();
            b.AppendLine("Date: " + s.CapturedAt);
            b.AppendLine("Timezone: " + s.TimeZoneDisplayName);
            b.AppendLine("PC Name: " + Pc(s.MachineName));
            b.AppendLine("Current user: " + Pc(s.UserName));
            b.AppendLine();
            b.AppendLine("=== OPERATING SYSTEM ===");
            b.AppendLine("OS: " + s.OsDisplay);
            b.AppendLine("OS Installed On: " + (string.IsNullOrWhiteSpace(s.OsInstallDate) ? "Unknown" : s.OsInstallDate));
            b.AppendLine("Windows Activation Status: " + s.WindowsActivationStatus);
            b.AppendLine("Uptime: " + s.UptimeDisplay);
            b.AppendLine();
            b.AppendLine("=== HARDWARE ===");
            b.AppendLine("Manufacturer: " + s.Manufacturer);
            b.AppendLine("Model: " + s.SystemModel);
            b.AppendLine("Serial Number: " + Pc(s.SerialNumber));
            b.AppendLine("Asset Tag: " + Pc(s.AssetTag));
            b.AppendLine("BIOS Version: " + s.BiosVersion);
            b.AppendLine("CPU: " + CpuText(s));
            b.AppendLine("CPU cache: " + Presentation.CpuCacheText(s.CpuL2CacheKb, s.CpuL3CacheKb));

            var mem = s.InstalledPhysicalMemory + " (" + s.UsablePhysicalMemory + " usable)";
            if (s.MemorySlotsTotal > 0)
            {
                var used = Math.Max(0, Math.Min(s.MemorySlotsUsed, s.MemorySlotsTotal));
                mem += " | Slots: " + used + "/" + s.MemorySlotsTotal;
            }
            if (s.MemorySpeedMhz > 0) mem += " | Speed: " + s.MemorySpeedMhz + " MHz";
            b.AppendLine("Memory: " + mem);
            b.AppendLine();

            b.AppendLine("=== STORAGE ===");
            foreach (var d in s.Disks) b.AppendLine("Disk" + d.Index + ": " + d.Model + " | " + d.Size + " | " + d.MediaType);
            b.AppendLine();

            b.AppendLine("=== GRAPHICS ===");
            for (var i = 0; i < s.Gpus.Count; i++) b.AppendLine("GPU" + (i + 1) + ": " + s.Gpus[i].Name + " | VRAM: " + s.Gpus[i].AdapterRam);
            b.AppendLine();

            if (s.Battery != null)
            {
                var bat = s.Battery;
                b.AppendLine("=== BATTERY ===");
                b.AppendLine("Health: " + (bat.HealthPercentage > 0 ? bat.HealthPercentage + "%" : "Unknown"));
                if (bat.DesignCapacity > 0) b.AppendLine("Design Capacity: " + bat.DesignCapacity.ToString("N0", CultureInfo.InvariantCulture) + " mWh");
                if (bat.FullChargeCapacity > 0) b.AppendLine("Full Charge Capacity: " + bat.FullChargeCapacity.ToString("N0", CultureInfo.InvariantCulture) + " mWh");
                b.AppendLine("Status: " + bat.Status);
                b.AppendLine("Power: " + bat.PowerStatus);
                b.AppendLine();
            }

            b.AppendLine("=== SPEC RATING ===");
            b.AppendLine("Rating: " + SpecRatingEngine.Label(ComputeRating(s)));
            b.AppendLine();
            b.AppendLine("App Version: v" + appVersion);

            var text = b.ToString();
            return redact ? Scrub(text, s) : text;
        }

        /// <summary>Removes any remaining occurrence of the user/machine name or serials in free text.</summary>
        public static string Scrub(string text, ReportModel s)
        {
            foreach (var secret in new[] { s.UserName, s.MachineName, s.SerialNumber, s.AssetTag })
            {
                if (string.IsNullOrWhiteSpace(secret) || secret.Trim().Length < 3) continue;
                if (secret.Trim().Equals("Unknown", StringComparison.OrdinalIgnoreCase)) continue;
                text = Replace(text, secret.Trim(), Redacted);
            }
            return text;
        }

        private static string Replace(string text, string find, string with)
        {
            // .NET Framework 4.8 has no string.Replace(string, string, StringComparison);
            // Regex with culture-invariant IgnoreCase keeps the same ignore-case semantics.
            return System.Text.RegularExpressions.Regex.Replace(
                text,
                System.Text.RegularExpressions.Regex.Escape(find),
                with.Replace("$", "$$"),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        }

        private static string CpuText(ReportModel s)
        {
            var name = (s.Cpu ?? string.Empty).Trim();
            var d = new List<string>();
            if (s.CpuCoreCount > 0) d.Add("Cores: " + s.CpuCoreCount);
            if (s.CpuLogicalProcessorCount > 0) d.Add("Threads: " + s.CpuLogicalProcessorCount);
            if (name.Length == 0 && d.Count == 0) return "Unknown";
            if (d.Count == 0) return name;
            if (name.Length == 0) return string.Join(" | ", d);
            return name + " | " + string.Join(" | ", d);
        }
    }
}
