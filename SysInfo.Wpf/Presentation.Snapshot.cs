using System;
using System.Collections.Generic;
using System.Text;

namespace CNSysInfo
{
    internal static partial class Presentation
    {
        public static string BatteryText(BatteryInfo b)
        {
            if (b == null || b.Status == "Not Detected") return "No battery detected (Desktop PC).";
            var health = b.HealthPercentage.HasValue ? b.HealthPercentage.Value + "%" : "Unknown";
            var status = string.IsNullOrWhiteSpace(b.Status) ? "" : " - " + b.Status;
            var cap = new List<string>();
            if (b.DesignCapacity.HasValue) cap.Add("Design " + BatteryCapacity(b.DesignCapacity.Value));
            if (b.FullChargeCapacity.HasValue) cap.Add("Full " + BatteryCapacity(b.FullChargeCapacity.Value));
            return health + status + (cap.Count > 0 ? " (" + string.Join(", ", cap) + ")" : "");
        }

        public static string StorageText(IReadOnlyList<DiskInfo> disks)
        {
            if (disks == null || disks.Count == 0) return "No disks detected.";
            var sb = new StringBuilder();
            foreach (var d in disks) sb.AppendLine("Disk" + d.Index + ": " + d.Model + " | " + d.Size + " | " + d.MediaType);
            return sb.ToString().TrimEnd();
        }

        public static string GpuText(IReadOnlyList<GpuInfo> gpus)
        {
            if (gpus == null || gpus.Count == 0) return "No GPUs detected.";
            var sb = new StringBuilder();
            for (int i = 0; i < gpus.Count; i++) sb.AppendLine("GPU" + (i + 1) + ": " + gpus[i].Name + " | VRAM: " + gpus[i].AdapterRam);
            return sb.ToString().TrimEnd();
        }

        public static string ScreenText(IReadOnlyList<MonitorInfo> mons)
        {
            if (mons == null || mons.Count == 0) return "No monitors detected.";
            var sb = new StringBuilder();
            for (int i = 0; i < mons.Count; i++)
            {
                var m = mons[i]; var p = new List<string>();
                if (!string.IsNullOrWhiteSpace(m.ScreenSize)) p.Add(m.ScreenSize);
                if (!string.IsNullOrWhiteSpace(m.RefreshRate)) p.Add("Refresh: " + m.RefreshRate);
                if (!string.IsNullOrWhiteSpace(m.Resolution)) p.Add("Resolution: " + m.Resolution);
                p.Add("Touch: " + (m.IsTouchScreen ? "Yes" : "No"));
                sb.AppendLine("Monitor" + (i + 1) + ": " + string.Join(" | ", p));
            }
            return sb.ToString().TrimEnd();
        }

        public static ReportDisk PrimaryDiskOf(IReadOnlyList<DiskInfo> disks)
        {
            if (disks == null || disks.Count == 0) return null;
            DiskInfo primary = null;
            foreach (var d in disks)
            {
                if (d == null) continue;
                if (!string.IsNullOrWhiteSpace(d.Index) && d.Index.Trim() == "0") { primary = d; break; }
                if (primary == null) primary = d;
            }
            return primary == null ? null : new ReportDisk { Index = primary.Index, Model = primary.Model, Size = primary.Size, MediaType = primary.MediaType, InterfaceType = primary.InterfaceType };
        }

        /// <summary>Single place that turns a snapshot into level + upgrade text (used by the UI).</summary>
        public static SpecRatingLevel Rate(SystemInfoSnapshot s, out string upgrades)
        {
            upgrades = "";
            if (s == null) return SpecRatingLevel.Unknown;
            var cpu = CpuSupportEvaluator.Evaluate(s);
            var gb = SpecRatingEngine.MemoryGb(s.InstalledPhysicalMemoryBytes, s.InstalledPhysicalMemory);
            var disk = PrimaryDiskOf(s.Disks);
            var kind = disk == null ? StorageKind.Unknown : SpecRatingEngine.ClassifyStorage(disk.MediaType, disk.Model, disk.InterfaceType);
            upgrades = UpgradeRecommendations(cpu, gb, kind);
            return SpecRatingEngine.Evaluate(cpu, gb, kind);
        }
    }
}
