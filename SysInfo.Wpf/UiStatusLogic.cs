using System;
using System.Globalization;

namespace CNSysInfo
{
    internal enum ResourcePressure { None, Warning, Critical }

    internal static class UiStatusLogic
    {
        public static bool ShowRestartReminder(TimeSpan uptime, bool available)
        {
            return available && uptime >= TimeSpan.FromHours(48);
        }

        public static ResourcePressure MemoryPressure(int? percent)
        {
            if (!percent.HasValue || percent < 0 || percent > 100) return ResourcePressure.None;
            return percent >= 95 ? ResourcePressure.Critical : percent >= 80 ? ResourcePressure.Warning : ResourcePressure.None;
        }

        public static ResourcePressure SystemDrivePressure(DiskUsageInfo usage)
        {
            if (usage == null || !usage.HasData || usage.TotalBytes == 0 || usage.FreeBytes > usage.TotalBytes)
                return ResourcePressure.None;
            const ulong gib = 1024UL * 1024 * 1024;
            decimal freePercent = (decimal)usage.FreeBytes * 100 / usage.TotalBytes;
            // Combine absolute and relative free space so a large drive with ample free GB is not called critical.
            if (usage.FreeBytes <= 2 * gib || (freePercent <= 5 && usage.FreeBytes <= 5 * gib))
                return ResourcePressure.Critical;
            if (usage.FreeBytes <= 10 * gib || (freePercent <= 10 && usage.FreeBytes <= 20 * gib))
                return ResourcePressure.Warning;
            return ResourcePressure.None;
        }

        public static string CaptureMetadata(string timeZone, DateTime capturedAt)
        {
            return "Timezone: " + (string.IsNullOrWhiteSpace(timeZone) ? "Unknown" : timeZone) +
                "  |  Captured: " + capturedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static string CollectionState(bool refreshing, bool failed, bool hasPrevious, int warnings)
        {
            if (refreshing) return "Refreshing...";
            if (failed) return hasPrevious ? "Refresh failed" : "Collection failed";
            return warnings > 0 ? warnings + " unavailable " + (warnings == 1 ? "read" : "reads") : string.Empty;
        }
    }
}
