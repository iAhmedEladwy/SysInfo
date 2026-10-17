using System;
using System.Collections.Generic;
using System.Text;

namespace CNSysInfo
{
    /// <summary>UI-free presentation rules (colors as hex, texts). Same tiers and wording as the WinForms version; unit-tested.</summary>
    internal static partial class Presentation
    {
        internal struct Tier
        {
            public string Back; public string Fore;
            public Tier(string b, string f) { Back = b; Fore = f; }
        }

        public static readonly Tier Unknown = new Tier("#94A3B8", "#000000");

        public static Tier ForRating(SpecRatingLevel level)
        {
            switch (level)
            {
                case SpecRatingLevel.Flagship: return new Tier("#4B0082", "#FFFFFF");
                case SpecRatingLevel.Enthusiast: return new Tier("#0078D7", "#FFFFFF");
                case SpecRatingLevel.PremiumPerformance: return new Tier("#009688", "#FFFFFF");
                case SpecRatingLevel.HighPerformance: return new Tier("#4CAF50", "#FFFFFF");
                case SpecRatingLevel.ModernStandard: return new Tier("#90EE90", "#006400");
                case SpecRatingLevel.AdequatePerformance: return new Tier("#9CCC65", "#33691E");
                case SpecRatingLevel.BelowStandard: return new Tier("#FFEB3B", "#503C00");
                case SpecRatingLevel.Outdated: return new Tier("#FFC87C", "#8B4513");
                case SpecRatingLevel.Legacy: return new Tier("#FFA54F", "#8B0000");
                case SpecRatingLevel.Obsolete: return new Tier("#FFB6C1", "#8B0000");
                default: return new Tier("#D3D3D3", "#000000");
            }
        }

        public static string RatingText(SpecRatingLevel level, string upgrades)
        {
            string tail(string label) { return string.IsNullOrEmpty(upgrades) ? "" : label + upgrades; }
            switch (level)
            {
                case SpecRatingLevel.Flagship: return "Flagship - Elite workstation for intensive tasks";
                case SpecRatingLevel.Enthusiast: return "Enthusiast - Excellent for prosumer workloads";
                case SpecRatingLevel.PremiumPerformance: return "Premium Performance - Excellent for demanding workloads";
                case SpecRatingLevel.HighPerformance: return "High Performance - Great for multitasking and productivity";
                case SpecRatingLevel.ModernStandard: return "Modern Standard - Ready for everyday tasks";
                case SpecRatingLevel.AdequatePerformance: return "Adequate - Meets minimum requirements for modern use" + tail(" - To improve: ");
                case SpecRatingLevel.BelowStandard: return "Below Standard - Upgrade recommended" + tail(": ");
                case SpecRatingLevel.Outdated: return "Outdated - Significant upgrades needed" + tail(": ");
                case SpecRatingLevel.Legacy: return "Legacy System - Full system replacement strongly advised";
                case SpecRatingLevel.Obsolete: return "Obsolete - Critical: Immediate replacement required";
                default: return "Unknown - not enough data to rate this system";
            }
        }

        // Fills for progress bars (health: higher is better, usage: lower is better).
        public static Tier ForHealth(int pct)
        {
            pct = Math.Max(0, Math.Min(100, pct));
            if (pct >= 90) return ForRating(SpecRatingLevel.Flagship);
            if (pct >= 80) return ForRating(SpecRatingLevel.Enthusiast);
            if (pct >= 70) return ForRating(SpecRatingLevel.PremiumPerformance);
            if (pct >= 60) return ForRating(SpecRatingLevel.HighPerformance);
            if (pct >= 50) return ForRating(SpecRatingLevel.ModernStandard);
            if (pct >= 40) return ForRating(SpecRatingLevel.BelowStandard);
            if (pct >= 30) return ForRating(SpecRatingLevel.Outdated);
            return ForRating(SpecRatingLevel.Obsolete);
        }

        public static Tier ForUsage(int pct)
        {
            pct = Math.Max(0, Math.Min(100, pct));
            if (pct < 40) return ForRating(SpecRatingLevel.Flagship);
            if (pct < 60) return ForRating(SpecRatingLevel.Enthusiast);
            if (pct < 75) return ForRating(SpecRatingLevel.BelowStandard);
            if (pct < 85) return ForRating(SpecRatingLevel.Outdated);
            return ForRating(SpecRatingLevel.Legacy);
        }

        public static string FormatBytes(ulong bytes)
        {
            if (bytes == 0) return "0 B";
            string[] s = { "B", "KB", "MB", "GB", "TB", "PB" };
            double v = bytes; int i = 0;
            while (v >= 1024 && i < s.Length - 1) { v /= 1024; i++; }
            return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " " + s[i];
        }

        public static string CpuText(string name, int cores, int threads)
        {
            var n = string.IsNullOrWhiteSpace(name) ? "" : name.Trim();
            var d = new List<string>();
            if (cores > 0) d.Add("Cores: " + cores);
            if (threads > 0) d.Add("Threads: " + threads);
            if (n.Length == 0 && d.Count == 0) return "Unknown";
            if (d.Count == 0) return n;
            if (n.Length == 0) return string.Join(" | ", d);
            return n + " | " + string.Join(" | ", d);
        }

        public static string MemoryText(string installed, string usable, int slotsUsed, int slotsTotal, int speedMhz)
        {
            var t = installed + " (" + usable + " usable)";
            if (slotsTotal > 0)
                t += " | Slots: " + Math.Max(0, Math.Min(slotsUsed, slotsTotal)) + "/" + slotsTotal;
            if (speedMhz > 0) t += " | Speed: " + speedMhz + " MHz";
            return t;
        }

        public static string CpuCacheText(ulong? l2Kb, ulong? l3Kb)
        {
            string Format(ulong? kb) => !kb.HasValue || kb.Value == 0 || kb.Value > ulong.MaxValue / 1024
                ? "Unknown" : WmiValueParser.FormatBytes(kb.Value * 1024);
            return "L2: " + Format(l2Kb) + " | L3: " + Format(l3Kb);
        }

        public static string BatteryCapacity(int c) { return c > 0 ? string.Format("{0:N0} mWh", c) : "Unknown"; }











        public static string UpgradeRecommendations(CpuSupportLevel cpu, double memoryGb, StorageKind storage)
        {
            var r = new List<string>();
            if (cpu == CpuSupportLevel.Unsupported) r.Add("CPU to Win11-compatible (8th gen Intel/Ryzen 2000+)");
            if (memoryGb > 0)
            {
                if (memoryGb < 8) r.Add("RAM to 16GB (add " + (16 - (int)memoryGb) + "GB)");
                else if (memoryGb < 16) r.Add("RAM to 16GB (add " + (16 - (int)memoryGb) + "GB for better performance)");
                else if (memoryGb < 24 && storage == StorageKind.Nvme) r.Add("RAM to 24GB (add " + (24 - (int)memoryGb) + "GB for enthusiast tier)");
            }
            if (storage == StorageKind.Hdd)
            {
                if (cpu == CpuSupportLevel.Supported && memoryGb >= 16) r.Add("Storage to Gen4 NVMe SSD (5000MB/s+)");
                else if (cpu == CpuSupportLevel.Supported) r.Add("Storage to NVMe SSD");
                else r.Add("Storage to SATA SSD minimum");
            }
            else if (storage == StorageKind.SataSsd && cpu == CpuSupportLevel.Supported && memoryGb >= 16)
                r.Add("Storage to NVMe (3x faster than SATA)");
            return string.Join(", ", r);
        }


    }
}
