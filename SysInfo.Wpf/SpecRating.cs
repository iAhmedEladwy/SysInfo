using System;
using System.Globalization;

namespace CNSysInfo
{
    public enum CpuSupportLevel
    {
        Unknown,
        Supported,
        Unsupported
    }

    public enum SpecRatingLevel
    {
        Unknown,
        Flagship,
        Enthusiast,
        PremiumPerformance,
        HighPerformance,
        ModernStandard,
        AdequatePerformance,
        BelowStandard,
        Outdated,
        Legacy,
        Obsolete
    }

    public enum StorageKind
    {
        Unknown,
        Nvme,
        SataSsd,
        Hdd
    }

    /// <summary>
    /// Deterministic, non-overlapping rating. Score = CPU points + RAM points + storage points.
    /// Every score maps to exactly one tier (half-open ranges). If any input is unknown the result
    /// is Unknown - it is never silently promoted to Adequate.
    /// CPU:      Win11-supported 4, not supported 0.
    /// RAM (GB): >=32 6 | [24,32) 5 | [16,24) 4 | [12,16) 3 | [8,12) 2 | [4,8) 1 | [0.1,4) 0
    /// Storage:  NVMe 3 | SATA SSD 2 | HDD 0
    /// Tiers:    13 Flagship | 12 Enthusiast | 11 Premium | 10 High | 9 Modern | 7-8 Adequate |
    ///           5-6 Below | 4 Outdated | 2-3 Legacy | 0-1 Obsolete
    /// </summary>
    public static class SpecRatingEngine
    {
        public const int MaxScore = 13;

        public static SpecRatingLevel Evaluate(CpuSupportLevel cpu, double memoryGb, StorageKind storage)
        {
            if (cpu == CpuSupportLevel.Unknown || storage == StorageKind.Unknown ||
                double.IsNaN(memoryGb) || memoryGb <= 0)
            {
                return SpecRatingLevel.Unknown;
            }

            return LevelForScore(CpuPoints(cpu) + RamPoints(memoryGb) + StoragePoints(storage));
        }

        public static int CpuPoints(CpuSupportLevel cpu)
        {
            return cpu == CpuSupportLevel.Supported ? 4 : 0;
        }

        public static int RamPoints(double gb)
        {
            if (gb >= 32) return 6;
            if (gb >= 24) return 5;
            if (gb >= 16) return 4;
            if (gb >= 12) return 3;
            if (gb >= 8) return 2;
            if (gb >= 4) return 1;
            return 0;
        }

        public static int StoragePoints(StorageKind kind)
        {
            switch (kind)
            {
                case StorageKind.Nvme: return 3;
                case StorageKind.SataSsd: return 2;
                default: return 0;
            }
        }

        public static SpecRatingLevel LevelForScore(int score)
        {
            if (score >= 13) return SpecRatingLevel.Flagship;
            if (score == 12) return SpecRatingLevel.Enthusiast;
            if (score == 11) return SpecRatingLevel.PremiumPerformance;
            if (score == 10) return SpecRatingLevel.HighPerformance;
            if (score == 9) return SpecRatingLevel.ModernStandard;
            if (score >= 7) return SpecRatingLevel.AdequatePerformance;
            if (score >= 5) return SpecRatingLevel.BelowStandard;
            if (score == 4) return SpecRatingLevel.Outdated;
            if (score >= 2) return SpecRatingLevel.Legacy;
            return SpecRatingLevel.Obsolete;
        }

        /// <summary>Plain-text label, used by both the UI and the exported report.</summary>
        public static string Label(SpecRatingLevel level)
        {
            switch (level)
            {
                case SpecRatingLevel.Flagship: return "Flagship";
                case SpecRatingLevel.Enthusiast: return "Enthusiast";
                case SpecRatingLevel.PremiumPerformance: return "Premium Performance";
                case SpecRatingLevel.HighPerformance: return "High Performance";
                case SpecRatingLevel.ModernStandard: return "Modern Standard";
                case SpecRatingLevel.AdequatePerformance: return "Adequate";
                case SpecRatingLevel.BelowStandard: return "Below Standard";
                case SpecRatingLevel.Outdated: return "Outdated";
                case SpecRatingLevel.Legacy: return "Legacy";
                case SpecRatingLevel.Obsolete: return "Obsolete";
                default: return "Unknown (not enough data to rate)";
            }
        }

        /// <summary>Single-SSD-branch, ordered classification; no shadowed branches.</summary>
        public static StorageKind ClassifyStorage(string mediaType, string model, string interfaceType)
        {
            mediaType = (mediaType ?? string.Empty).Trim();
            model = (model ?? string.Empty).Trim();
            interfaceType = (interfaceType ?? string.Empty).Trim();
            if (mediaType.Length == 0 && model.Length == 0 && interfaceType.Length == 0)
                return StorageKind.Unknown;

            var all = (mediaType + " " + model + " " + interfaceType).ToLowerInvariant();
            bool Has(string s) { return all.Contains(s); }

            bool nvme = Has("nvme") || Has("non-volatile memory express") || Has("pcie") || Has("pci express");
            bool sata = Has("sata");
            bool ssd = Has("ssd") || Has("solid state") || Has("solid-state");
            bool hdd = Has("hdd") || Has("hard disk") || Has("hard-disk");

            if (nvme) return StorageKind.Nvme;
            if (interfaceType.ToLowerInvariant().Contains("pci") && ssd) return StorageKind.Nvme;
            if ((Has("m.2") || Has("m2 ") || all.EndsWith("m2")) && !sata) return StorageKind.Nvme;
            if (ssd) return StorageKind.SataSsd;   // SATA SSD, or SSD with unspecified bus
            if (hdd) return StorageKind.Hdd;
            return StorageKind.Unknown;
        }

        /// <summary>Installed RAM in GB from the exact byte count when known, else from display text; -1 when unknown.</summary>
        public static double MemoryGb(ulong installedBytes, string displayText)
        {
            if (installedBytes > 0) return installedBytes / (1024.0 * 1024.0 * 1024.0);
            return ParseGigabytes(displayText);
        }

        public static double ParseGigabytes(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1;
            var t = text.Trim();
            int i = 0;
            while (i < t.Length && (char.IsDigit(t[i]) || t[i] == '.' || t[i] == ',')) i++;
            if (i == 0) return -1;
            double value;
            if (!double.TryParse(t.Substring(0, i).Replace(",", string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return -1;
            var unit = t.Substring(i).Trim().ToUpperInvariant();
            if (unit.StartsWith("TB")) return value * 1024;
            if (unit.StartsWith("GB")) return value;
            if (unit.StartsWith("MB")) return value / 1024;
            return -1;
        }
    }
}
