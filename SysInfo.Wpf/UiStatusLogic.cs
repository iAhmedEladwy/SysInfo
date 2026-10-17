using System;
using System.Globalization;

namespace CNSysInfo
{
    internal static class UiStatusLogic
    {
        public static bool ShowRestartReminder(TimeSpan uptime, bool available)
        {
            return available && uptime >= TimeSpan.FromHours(48);
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
