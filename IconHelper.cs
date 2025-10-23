using System.Drawing;

namespace CNSysInfo
{
    /// <summary>
    /// Helper class for managing Unicode icons (emoji/symbols)
    /// No external dependencies required - uses built-in Unicode characters
    /// </summary>
    public static class IconHelper
    {
        // Hardware Icons
        public const string CPU = "🖥️";
        public const string Memory = "💾";
        public const string Storage = "💿";
        public const string GPU = "🎮";
        public const string Battery = "🔋";
        public const string Screen = "🖥️";
        public const string Manufacturer = "🏭";
        public const string Model = "📦";
        public const string SerialNumber = "🔢";
        public const string AssetTag = "🏷️";
        public const string BIOS = "⚙️";

        // Software Icons
        public const string OS = "💻";
        public const string User = "👤";
        public const string Computer = "🖥️";
        public const string Time = "🕒";
        public const string Timezone = "🌍";
        public const string Uptime = "⏱️";

        // Status Icons
        public const string Success = "✅";
        public const string Warning = "⚠️";
        public const string Error = "❌";
        public const string Info = "ℹ️";
        public const string Refresh = "🔄";
        public const string Export = "📤";

        // Performance Icons
        public const string HighPerformance = "⚡";
        public const string MediumPerformance = "📊";
        public const string LowPerformance = "🐌";

        /// <summary>
        /// Get a color for a given icon category
        /// </summary>
        public static Color GetIconColor(string iconCategory)
        {
            switch (iconCategory)
            {
                case "cpu":
                case "processor":
                    return Color.FromArgb(59, 130, 246); // Blue
                case "memory":
                case "ram":
                    return Color.FromArgb(139, 92, 246); // Purple
                case "storage":
                case "disk":
                    return Color.FromArgb(16, 185, 129); // Green
                case "gpu":
                case "graphics":
                    return Color.FromArgb(239, 68, 68); // Red
                case "battery":
                    return Color.FromArgb(251, 191, 36); // Amber
                case "screen":
                case "monitor":
                    return Color.FromArgb(6, 182, 212); // Cyan
                default:
                    return Color.FromArgb(107, 114, 128); // Gray
            }
        }

        /// <summary>
        /// Get health status color based on percentage
        /// </summary>
        public static Color GetHealthColor(int percentage)
        {
            if (percentage >= 80)
                return Color.FromArgb(34, 197, 94); // Green
            else if (percentage >= 60)
                return Color.FromArgb(234, 179, 8); // Yellow
            else if (percentage >= 40)
                return Color.FromArgb(249, 115, 22); // Orange
            else
                return Color.FromArgb(239, 68, 68); // Red
        }

        /// <summary>
        /// Get battery icon based on level
        /// </summary>
        public static string GetBatteryIcon(int percentage)
        {
            if (percentage >= 90)
                return "🔋"; // Full
            else if (percentage >= 60)
                return "🔋"; // High
            else if (percentage >= 30)
                return "🪫"; // Medium
            else
                return "🪫"; // Low
        }
    }
}
