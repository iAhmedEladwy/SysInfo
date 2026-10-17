using System;
using System.Globalization;

namespace CNSysInfo
{
    /// <summary>Pure helpers for converting raw WMI/registry values. No System.Management dependency, so they are unit-testable.</summary>
    internal static class WmiValueParser
    {
        /// <summary>
        /// Parses a CIM datetime ("yyyymmddHHMMSS.mmmmmmsUUU", UUU = minutes offset from UTC) into local time.
        /// Returns null for empty, malformed or placeholder values (all zeros).
        /// </summary>
        public static DateTime? ParseCimDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var text = value.Trim();
            if (text.Length < 14)
            {
                return null;
            }

            if (!int.TryParse(text.Substring(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
                !int.TryParse(text.Substring(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month) ||
                !int.TryParse(text.Substring(6, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var day) ||
                !int.TryParse(text.Substring(8, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var hour) ||
                !int.TryParse(text.Substring(10, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var minute) ||
                !int.TryParse(text.Substring(12, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var second))
            {
                return null;
            }

            if (year < 1980 || month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59 || second > 59)
            {
                return null;
            }

            DateTime stamp;
            try
            {
                stamp = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            // Optional UTC offset in minutes, e.g. "+120" or "-300".
            var signIndex = text.LastIndexOfAny(new[] { '+', '-' });
            if (signIndex >= 14 && signIndex < text.Length - 1 &&
                int.TryParse(text.Substring(signIndex + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var offsetMinutes))
            {
                if (text[signIndex] == '-')
                {
                    offsetMinutes = -offsetMinutes;
                }

                var utc = DateTime.SpecifyKind(stamp.AddMinutes(-offsetMinutes), DateTimeKind.Utc);
                return utc.ToLocalTime();
            }

            return stamp;
        }

        /// <summary>Registry HKLM\...\CurrentVersion\InstallDate is seconds since 1970-01-01 UTC.</summary>
        public static DateTime? FromUnixSeconds(long seconds)
        {
            if (seconds <= 0 || seconds > 4102444800L)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
        }

        public static string FormatInstallDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "Unknown";
        }

        /// <summary>Converts a WMI numeric object (ushort/uint/ulong/string...) to ulong, or null when absent or invalid.</summary>
        public static ulong? ToUInt64(object value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                switch (value)
                {
                    case ulong u: return u;
                    case uint ui: return ui;
                    case ushort us: return us;
                    case byte b: return b;
                    case long l: return l >= 0 ? (ulong?)l : null;
                    case int i: return i >= 0 ? (ulong?)i : null;
                    case short s: return s >= 0 ? (ulong?)s : null;
                }

                return ulong.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    ? (ulong?)parsed
                    : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>Binary units (1 KB = 1024 B), matching what Windows shows in Settings.</summary>
        public static string FormatBytes(ulong bytes)
        {
            if (bytes == 0)
            {
                return "0 B";
            }

            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            double value = bytes;
            var index = 0;
            while (value >= 1024 && index < suffixes.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", value, suffixes[index]);
        }

        /// <summary>Whole-number percentage of used/total, clamped to 0..100. Null when total is 0.</summary>
        public static int? Percent(ulong used, ulong total)
        {
            if (total == 0)
            {
                return null;
            }

            var pct = (int)Math.Round(used * 100d / total, MidpointRounding.AwayFromZero);
            return Math.Max(0, Math.Min(100, pct));
        }
    }
}
