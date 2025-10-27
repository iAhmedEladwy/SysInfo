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
            if (string.IsNullOrWhiteSpace(iconCategory))
            {
                return UnifiedColors.Unknown;
            }

            switch (iconCategory.Trim().ToLowerInvariant())
            {
                case "cpu":
                case "processor":
                    return UnifiedColors.Premium;
                case "memory":
                case "ram":
                    return UnifiedColors.ModernStandard;
                case "storage":
                case "disk":
                    return UnifiedColors.Adequate;
                case "gpu":
                case "graphics":
                    return UnifiedColors.Outdated;
                case "battery":
                    return UnifiedColors.ModernStandard;
                case "screen":
                case "monitor":
                    return UnifiedColors.Premium;
                default:
                    return UnifiedColors.Unknown;
            }
        }

        /// <summary>
        /// Get health status color based on percentage
        /// </summary>
        public static Color GetHealthColor(int percentage)
        {
            return UnifiedColors.GetHealthTier(percentage);
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
