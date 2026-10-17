using System;
using System.Collections.Generic;
using System.Globalization;

namespace CNSysInfo
{
    internal enum StorageHealthState
    {
        Unknown,
        Good,
        Warning,
        Critical
    }

    /// <summary>
    /// Storage health. Health (is the drive failing?) is kept strictly apart from usage (how full is it?).
    /// A drive that exposes no SMART data is Unknown. It is never reported as 100% healthy.
    /// </summary>
    internal class StorageHealthInfo
    {
        public StorageHealthInfo()
        {
            DiskIndex = string.Empty;
            State = StorageHealthState.Unknown;
            StatusText = "Unknown";
            Source = string.Empty;
        }

        public string DiskIndex { get; set; }
        public StorageHealthState State { get; set; }
        public string StatusText { get; set; }
        /// <summary>Where the verdict came from: "SMART", "Windows disk status", or empty when nothing was readable.</summary>
        public string Source { get; set; }
        /// <summary>SSD life remaining from the drive's own normalised SMART value. Null when the drive does not report one (all HDDs, most NVMe through this API).</summary>
        public int? LifeRemainingPercent { get; set; }
        public int? TemperatureCelsius { get; set; }
        public int? PowerOnHours { get; set; }
        public bool? IsSsd { get; set; }

        // Compatibility with existing UI code.
        public string HealthStatus { get { return StatusText; } }
        public bool IsHealthy { get { return State == StorageHealthState.Good; } }
    }

    /// <summary>Disk usage (capacity), independent from health.</summary>
    internal class DiskUsageInfo
    {
        public bool HasData { get; set; }
        public ulong TotalBytes { get; set; }
        public ulong UsedBytes { get; set; }
        public ulong FreeBytes { get; set; }
        public int UsagePercentage { get; set; }

        public static DiskUsageInfo FromTotals(ulong totalBytes, ulong freeBytes)
        {
            var usage = new DiskUsageInfo();
            if (totalBytes == 0)
            {
                return usage;
            }

            if (freeBytes > totalBytes)
            {
                freeBytes = totalBytes;
            }

            usage.TotalBytes = totalBytes;
            usage.FreeBytes = freeBytes;
            usage.UsedBytes = totalBytes - freeBytes;
            usage.UsagePercentage = WmiValueParser.Percent(usage.UsedBytes, totalBytes) ?? 0;
            usage.HasData = true;
            return usage;
        }
    }

    internal readonly struct SmartAttribute
    {
        public SmartAttribute(int id, int currentValue, ulong rawValue)
        {
            Id = id;
            CurrentValue = currentValue;
            RawValue = rawValue;
        }

        public int Id { get; }
        public int CurrentValue { get; }
        public ulong RawValue { get; }
    }

    internal static class StorageHealthLogic
    {
        // Normalised SMART attributes that behave as "life remaining" on SSDs.
        private static readonly int[] SsdLifeAttributeIds = { 177, 231, 233, 169, 202 };

        // Raw counters where any non-zero value means the drive has already remapped or lost sectors.
        private static readonly int[] BadSectorAttributeIds = { 5, 196, 197, 198 };

        public const int WarnBelowPercent = 50;
        public const int CriticalAtOrBelowPercent = 10;

        /// <summary>
        /// MSStorageDriver_* instance names look like "SCSI\Disk&amp;Ven_X&amp;Prod_Y\4&amp;1a2b&amp;0&amp;000000_0".
        /// They do not contain "PhysicalDriveN", so match on the Win32_DiskDrive.PNPDeviceID prefix and an optional "_N" suffix.
        /// </summary>
        public static bool InstanceMatchesPnpId(string instanceName, string pnpDeviceId)
        {
            if (string.IsNullOrWhiteSpace(instanceName) || string.IsNullOrWhiteSpace(pnpDeviceId))
            {
                return false;
            }

            var instance = instanceName.Trim();
            var pnp = pnpDeviceId.Trim();
            if (!instance.StartsWith(pnp, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var rest = instance.Substring(pnp.Length);
            if (rest.Length == 0)
            {
                return true;
            }

            return rest.Length >= 2 && rest[0] == '_' && AllDigits(rest, 1);
        }

        private static bool AllDigits(string text, int start)
        {
            for (var i = start; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return false;
                }
            }

            return text.Length > start;
        }

        /// <summary>Parses the 512 byte ATA SMART VendorSpecific block (2 byte revision, then 30 entries of 12 bytes).</summary>
        public static Dictionary<int, SmartAttribute> ParseSmartAttributes(byte[] data)
        {
            var result = new Dictionary<int, SmartAttribute>();
            if (data == null)
            {
                return result;
            }

            for (var i = 2; i + 12 <= data.Length && i < 2 + 30 * 12; i += 12)
            {
                int id = data[i];
                if (id == 0)
                {
                    continue;
                }

                int current = data[i + 3];
                ulong raw = 0;
                for (var b = 0; b < 6; b++)
                {
                    raw |= (ulong)data[i + 5 + b] << (8 * b);
                }

                result[id] = new SmartAttribute(id, current, raw);
            }

            return result;
        }

        public static int? ExtractTemperature(IDictionary<int, SmartAttribute> attributes)
        {
            if (attributes != null && attributes.TryGetValue(0xC2, out var attr))
            {
                var temp = (int)(attr.RawValue & 0xFF);
                if (temp > 0 && temp < 120)
                {
                    return temp;
                }
            }

            return null;
        }

        public static int? ExtractPowerOnHours(IDictionary<int, SmartAttribute> attributes)
        {
            if (attributes != null && attributes.TryGetValue(0x09, out var attr))
            {
                // Some vendors pack milliseconds or minutes into the upper raw bytes; only the low 32 bits are the hour counter.
                var hours = attr.RawValue & 0xFFFFFFFFUL;
                if (hours > 0 && hours <= int.MaxValue)
                {
                    return (int)hours;
                }
            }

            return null;
        }

        /// <summary>Lowest normalised "life remaining" value (1..100) from the known SSD attributes, or null when none is present.</summary>
        public static int? ExtractSsdLifeRemaining(IDictionary<int, SmartAttribute> attributes)
        {
            if (attributes == null)
            {
                return null;
            }

            int? lowest = null;
            foreach (var id in SsdLifeAttributeIds)
            {
                if (attributes.TryGetValue(id, out var attr) && attr.CurrentValue >= 1 && attr.CurrentValue <= 100)
                {
                    if (!lowest.HasValue || attr.CurrentValue < lowest.Value)
                    {
                        lowest = attr.CurrentValue;
                    }
                }
            }

            return lowest;
        }

        public static bool HasBadSectors(IDictionary<int, SmartAttribute> attributes)
        {
            if (attributes == null)
            {
                return false;
            }

            foreach (var id in BadSectorAttributeIds)
            {
                if (attributes.TryGetValue(id, out var attr) && (attr.RawValue & 0xFFFFFFFFUL) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Combines the signals in order of trust: drive failure prediction, Windows "Pred Fail" status,
        /// SMART attributes, then Windows basic status. No data means Unknown.
        /// </summary>
        public static StorageHealthInfo Evaluate(string diskIndex, IDictionary<int, SmartAttribute> attributes, bool? failurePredicted, string windowsDiskStatus, bool knownSsdLifeEncoding = false)
        {
            var info = new StorageHealthInfo { DiskIndex = diskIndex ?? string.Empty };
            var hasAttributes = attributes != null && attributes.Count > 0;

            if (hasAttributes)
            {
                info.TemperatureCelsius = ExtractTemperature(attributes);
                info.PowerOnHours = ExtractPowerOnHours(attributes);
                // ATA attribute IDs have vendor-specific meanings. Only use this
                // decoder when the caller has confirmed the device's encoding.
                info.LifeRemainingPercent = knownSsdLifeEncoding ? ExtractSsdLifeRemaining(attributes) : null;
                info.IsSsd = info.LifeRemainingPercent.HasValue ? (bool?)true : null;
            }

            var windowsStatus = (windowsDiskStatus ?? string.Empty).Trim();

            if (failurePredicted == true)
            {
                info.State = StorageHealthState.Critical;
                info.StatusText = "Failure predicted by the drive (SMART)";
                info.Source = "SMART";
                return info;
            }

            if (windowsStatus.IndexOf("pred", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info.State = StorageHealthState.Critical;
                info.StatusText = "Windows reports the drive is predicted to fail";
                info.Source = "Windows disk status";
                return info;
            }

            if (hasAttributes)
            {
                info.Source = "SMART";
                if (info.LifeRemainingPercent.HasValue && info.LifeRemainingPercent.Value <= CriticalAtOrBelowPercent)
                {
                    info.State = StorageHealthState.Critical;
                    info.StatusText = "SSD life nearly used up";
                    return info;
                }

                if (HasBadSectors(attributes))
                {
                    info.State = StorageHealthState.Warning;
                    info.StatusText = "Reallocated or pending sectors detected";
                    return info;
                }

                if (info.LifeRemainingPercent.HasValue && info.LifeRemainingPercent.Value < WarnBelowPercent)
                {
                    info.State = StorageHealthState.Warning;
                    info.StatusText = "SSD wear is high";
                    return info;
                }

                // Temperature / power counters alone cannot establish health.
                if (attributes.ContainsKey(5) && attributes.ContainsKey(197) && attributes.ContainsKey(198))
                {
                    info.State = StorageHealthState.Good;
                    info.StatusText = "No sector problems reported by SMART";
                    return info;
                }
            }

            if (failurePredicted == false)
            {
                info.State = StorageHealthState.Good;
                info.StatusText = "No failure predicted (SMART overall status)";
                info.Source = "SMART";
                return info;
            }

            if (windowsStatus.Length > 0 &&
                !windowsStatus.Equals("OK", StringComparison.OrdinalIgnoreCase) &&
                !windowsStatus.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                info.State = StorageHealthState.Warning;
                info.StatusText = "Windows disk status: " + windowsStatus;
                info.Source = "Windows disk status";
                return info;
            }

            // Windows "OK" only means the driver answers. It is not a health measurement.
            info.State = StorageHealthState.Unknown;
            info.StatusText = windowsStatus.Equals("OK", StringComparison.OrdinalIgnoreCase)
                ? "Unknown (no SMART data; Windows basic status is OK)"
                : "Unknown";
            info.Source = windowsStatus.Equals("OK", StringComparison.OrdinalIgnoreCase) ? "Windows disk status" : string.Empty;
            return info;
        }

        public static void MergeWindowsHealth(StorageHealthInfo info, int? healthStatus,
            int? wear, int? temperature, int? powerOnHours, ulong? readErrors, ulong? writeErrors)
        {
            if (temperature > 0 && temperature < 120) info.TemperatureCelsius = temperature;
            if (powerOnHours >= 0 && powerOnHours < int.MaxValue) info.PowerOnHours = powerOnHours;
            // Wear is percentage consumed, not remaining. 100+ means exhausted.
            if (wear >= 0 && wear <= 255)
                info.LifeRemainingPercent = Math.Max(0, 100 - wear.Value);

            StorageHealthState state = StorageHealthState.Unknown;
            string text = null;
            if (healthStatus == 2)
            {
                state = StorageHealthState.Critical;
                text = "Windows storage provider reports unhealthy";
            }
            else if (info.LifeRemainingPercent <= CriticalAtOrBelowPercent)
            {
                state = StorageHealthState.Critical;
                text = "Drive endurance nearly used up";
            }
            else if (healthStatus == 1 || readErrors > 0 || writeErrors > 0)
            {
                state = StorageHealthState.Warning;
                text = readErrors > 0 || writeErrors > 0
                    ? "Uncorrected read or write errors reported"
                    : "Windows storage provider reports a health warning";
            }
            else if (info.LifeRemainingPercent < WarnBelowPercent)
            {
                state = StorageHealthState.Warning;
                text = "Drive wear is high";
            }
            else if (healthStatus == 0)
            {
                state = StorageHealthState.Good;
                text = "Windows storage provider reports healthy";
            }
            // Never allow a healthy provider to erase an ATA failure prediction.
            if (state > info.State || (state == info.State && state == StorageHealthState.Unknown))
            {
                if (text != null)
                {
                    info.State = state;
                    info.StatusText = text;
                    info.Source = "Windows storage health";
                }
            }
        }

        public static string Format(StorageHealthInfo health)
        {
            if (health == null)
            {
                return "Storage health: Unknown";
            }

            var parts = new List<string> { "Health: " + health.StatusText };
            if (health.LifeRemainingPercent.HasValue)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "Drive life remaining: {0}%", health.LifeRemainingPercent.Value));
            }

            if (health.TemperatureCelsius.HasValue)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "Temp: {0}\u00B0C", health.TemperatureCelsius.Value));
            }

            if (health.PowerOnHours.HasValue)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "Power-on: {0:N0} days", health.PowerOnHours.Value / 24));
            }

            return string.Join(" | ", parts);
        }
    }
}
