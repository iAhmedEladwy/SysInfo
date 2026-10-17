using System;

namespace CNSysInfo
{
    /// <summary>Pure battery mapping rules for Win32_Battery.</summary>
    internal static class BatteryMapper
    {
        /// <summary>
        /// Win32_Battery.BatteryStatus values per Microsoft docs:
        /// 1 Other (discharging), 2 Unknown (usually on AC), 3 Fully Charged, 4 Low, 5 Critical,
        /// 6 Charging, 7 Charging and High, 8 Charging and Low, 9 Charging and Critical, 10 Undefined, 11 Partially Charged.
        /// </summary>
        public static string GetStatusText(int? status)
        {
            switch (status)
            {
                case 1: return "Discharging";
                case 2: return "On AC power";
                case 3: return "Fully charged";
                case 4: return "Low";
                case 5: return "Critical";
                case 6: return "Charging";
                case 7: return "Charging (high)";
                case 8: return "Charging (low)";
                case 9: return "Charging (critical)";
                case 11: return "Partially charged";
                default: return "Unknown";
            }
        }

        public static bool? IsCharging(int? status)
        {
            switch (status)
            {
                case 6:
                case 7:
                case 8:
                case 9:
                    return true;
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 11:
                    return false;
                default:
                    return null;
            }
        }

        /// <summary>True = on AC, false = on battery, null = cannot tell.</summary>
        public static bool? IsOnAcPower(int? status)
        {
            switch (status)
            {
                case 2:
                case 3:
                case 6:
                case 7:
                case 8:
                case 9:
                    return true;
                case 1:
                case 4:
                case 5:
                    return false;
                default:
                    return null; // 11 (partially charged) and 10 (undefined) do not say where power comes from.
            }
        }

        public static string GetPowerText(int? status)
        {
            var ac = IsOnAcPower(status);
            return ac.HasValue ? (ac.Value ? "Plugged in" : "On battery") : "Unknown";
        }

        /// <summary>Positive capacity or null. WMI returns 0 or null when the firmware does not report it.</summary>
        public static int? NormalizeCapacity(ulong? raw)
        {
            if (!raw.HasValue || raw.Value == 0 || raw.Value > int.MaxValue)
            {
                return null;
            }

            // 0xFFFFFFFF is a common "unknown" sentinel and is already excluded by the int.MaxValue check.
            return (int)raw.Value;
        }

        /// <summary>Estimated charge 0..100. Values above 100 (such as 255) mean unknown. 0 is a real value only when reported.</summary>
        public static int? NormalizeChargePercent(ulong? raw)
        {
            if (!raw.HasValue || raw.Value > 100)
            {
                return null;
            }

            return (int)raw.Value;
        }

        /// <summary>Full-charge / design capacity as a percentage. Null when either value is unknown. Capped at 100.</summary>
        public static int? CalculateHealthPercent(int? designCapacity, int? fullChargeCapacity)
        {
            if (!designCapacity.HasValue || !fullChargeCapacity.HasValue ||
                designCapacity.Value <= 0 || fullChargeCapacity.Value <= 0)
            {
                return null;
            }

            var pct = (int)Math.Round(fullChargeCapacity.Value * 100d / designCapacity.Value, MidpointRounding.AwayFromZero);
            return Math.Min(100, pct);
        }
    }
}
