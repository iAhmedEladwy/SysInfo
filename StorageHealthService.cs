using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace CNSysInfo
{
    /// <summary>
    /// Service to retrieve storage health information including SMART status.
    /// </summary>
    internal static class StorageHealthService
    {
        private static readonly Dictionary<int, (double Weight, double Limit)> CriticalHddAttributes = new Dictionary<int, (double Weight, double Limit)>
        {
            { 5,   (1.0, 70.0) },  // Reallocated Sectors Count
            { 196, (1.0, 70.0) },  // Reallocation Event Count
            { 197, (0.6, 70.0) },  // Current Pending Sectors
            { 198, (1.0, 70.0) },  // Offline Uncorrectable Sectors
            { 10,  (2.0, 60.0) },  // Spin Retry Count
            { 187, (1.0, 70.0) },  // Reported Uncorrectable Errors
            { 188, (0.5, 50.0) }   // Command Timeout
        };

        private static readonly int[] SsdHealthAttributeIds = { 177, 231, 233, 169, 202, 170 };

        public static StorageHealthInfo GetStorageHealth(string diskIndex = "0")
        {
            var health = new StorageHealthInfo
            {
                DiskIndex = diskIndex,
                HealthStatus = "Unknown",
                Temperature = -1,
                PowerOnHours = -1,
                IsHealthy = false,
                HealthPercentage = -1,
                IsSsd = false
            };

            try
            {
                bool? failurePredicted = null;

                var statusQuery = $"SELECT * FROM MSStorageDriver_FailurePredictStatus WHERE InstanceName LIKE '%PhysicalDrive{diskIndex}%'";
                using (var statusSearcher = new ManagementObjectSearcher("root\\WMI", statusQuery))
                {
                    foreach (ManagementObject obj in statusSearcher.Get())
                    {
                        if (obj["PredictFailure"] is bool predictFailure)
                        {
                            failurePredicted = predictFailure;
                        }
                    }
                }

                var smartQuery = $"SELECT * FROM MSStorageDriver_ATAPISmartData WHERE InstanceName LIKE '%PhysicalDrive{diskIndex}%'";
                using (var smartSearcher = new ManagementObjectSearcher("root\\WMI", smartQuery))
                {
                    foreach (ManagementObject obj in smartSearcher.Get())
                    {
                        if (obj["VendorSpecific"] is byte[] smartData)
                        {
                            var attributes = ParseSmartAttributes(smartData);

                            health.Temperature = ExtractTemperature(attributes);
                            health.PowerOnHours = ExtractPowerOnHours(attributes);
                            health.HealthPercentage = CalculateHealthPercentage(attributes, out bool isSsd);
                            health.IsSsd = isSsd;
                            break;
                        }
                    }
                }

                if (health.HealthPercentage >= 0)
                {
                    health.HealthStatus = GetHealthStatusText(health.HealthPercentage);
                    health.IsHealthy = health.HealthPercentage >= 80;
                }

                if (failurePredicted.HasValue)
                {
                    if (failurePredicted.Value)
                    {
                        health.IsHealthy = false;
                        health.HealthStatus = "Warning: Failure Predicted";
                        health.HealthPercentage = health.HealthPercentage < 0
                            ? 5
                            : Math.Min(health.HealthPercentage, 5);
                    }
                    else if (health.HealthPercentage < 0)
                    {
                        health.IsHealthy = true;
                        health.HealthStatus = "Healthy";
                        health.HealthPercentage = 100;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving storage health: {ex.Message}");
                health.HealthStatus = "Unable to retrieve";
            }

            if (health.HealthPercentage < 0)
            {
                TryPopulateHealthFromDiskStatus(diskIndex, health);
            }

            return health;
        }

        public static DiskUsageInfo CalculateDiskUsage(string diskIndex = "0")
        {
            var usage = new DiskUsageInfo();

            if (!int.TryParse(diskIndex, out var index))
                return usage;

            try
            {
                var deviceId = ResolveDiskDeviceId(index);
                if (string.IsNullOrWhiteSpace(deviceId))
                    return usage;

                var escapedDeviceId = EscapeWmiValue(deviceId);
                ulong totalBytes = 0;
                ulong freeBytes = 0;
                var seenLogicalDisks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var partitionQuery = $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{escapedDeviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                using (var partitionSearcher = new ManagementObjectSearcher(partitionQuery))
                {
                    foreach (ManagementObject partition in partitionSearcher.Get())
                    {
                        var partitionId = partition["DeviceID"]?.ToString();
                        if (string.IsNullOrWhiteSpace(partitionId))
                            continue;

                        var escapedPartitionId = EscapeWmiValue(partitionId);
                        var logicalQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escapedPartitionId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition";
                        using (var logicalSearcher = new ManagementObjectSearcher(logicalQuery))
                        {
                            foreach (ManagementObject logicalDisk in logicalSearcher.Get())
                            {
                                var logicalDeviceId = logicalDisk["DeviceID"]?.ToString();
                                if (string.IsNullOrWhiteSpace(logicalDeviceId))
                                    continue;

                                if (!seenLogicalDisks.Add(logicalDeviceId))
                                    continue;

                                var logicalInfoQuery = $"SELECT DeviceID, Size, FreeSpace FROM Win32_LogicalDisk WHERE DeviceID='{logicalDeviceId}' AND DriveType=3";
                                using (var logicalInfoSearcher = new ManagementObjectSearcher(logicalInfoQuery))
                                {
                                    foreach (ManagementObject logicalInfo in logicalInfoSearcher.Get())
                                    {
                                        if (!ulong.TryParse(logicalInfo["Size"]?.ToString(), out var sizeBytes) || sizeBytes == 0)
                                            continue;

                                        ulong free = 0;
                                        ulong.TryParse(logicalInfo["FreeSpace"]?.ToString(), out free);
                                        if (free > sizeBytes)
                                            free = sizeBytes;

                                        totalBytes += sizeBytes;
                                        freeBytes += free;
                                    }
                                }
                            }
                        }
                    }
                }

                if (totalBytes > 0)
                {
                    var usedBytes = totalBytes > freeBytes ? totalBytes - freeBytes : 0;
                    usage.TotalBytes = totalBytes;
                    usage.FreeBytes = freeBytes;
                    usage.UsedBytes = usedBytes;
                    usage.UsagePercentage = (int)Math.Round((double)usedBytes * 100d / totalBytes);
                    if (usage.UsagePercentage < 0)
                    {
                        usage.UsagePercentage = 0;
                    }
                    else if (usage.UsagePercentage > 100)
                    {
                        usage.UsagePercentage = 100;
                    }
                    usage.HasData = true;
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[StorageHealthService] Failed to calculate disk usage: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[StorageHealthService] Access denied while calculating disk usage: {ex.Message}");
            }

            return usage;
        }

        private static Dictionary<int, SmartAttribute> ParseSmartAttributes(byte[] smartData)
        {
            var attributes = new Dictionary<int, SmartAttribute>();

            if (smartData == null || smartData.Length < 12)
                return attributes;

            try
            {
                for (int i = 2; i + 10 < smartData.Length; i += 12)
                {
                    int id = smartData[i];
                    if (id == 0)
                        continue;

                    int currentValue = smartData[i + 3];

                    ulong rawValue = 0;
                    for (int offset = 0; offset < 6; offset++)
                    {
                        rawValue |= (ulong)smartData[i + 5 + offset] << (8 * offset);
                    }

                    attributes[id] = new SmartAttribute(id, currentValue, rawValue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing SMART attributes: {ex.Message}");
            }

            return attributes;
        }

        private static int CalculateHealthPercentage(Dictionary<int, SmartAttribute> attributes, out bool isSsd)
        {
            isSsd = false;

            if (attributes == null || attributes.Count == 0)
                return -1;

            var ssdHealth = CalculateSsdHealth(attributes);
            if (ssdHealth.HasValue)
            {
                isSsd = true;
                return ClampPercentage(ssdHealth.Value);
            }

            isSsd = false;
            return CalculateHddHealth(attributes);
        }

        private static int CalculateHddHealth(Dictionary<int, SmartAttribute> attributes)
        {
            double healthFraction = 1.0;
            bool hadDegradation = false;

            foreach (var kvp in CriticalHddAttributes)
            {
                if (!attributes.TryGetValue(kvp.Key, out var attribute))
                    continue;

                int rawMetric = attribute.RawLow16;
                if (rawMetric <= 0)
                    continue;

                hadDegradation = true;

                var (weight, limit) = kvp.Value;
                double degradation = rawMetric * weight;
                double capped = Math.Min(degradation, limit);
                double remainingFraction = Math.Max(0.0, 100.0 - capped) / 100.0;

                healthFraction *= remainingFraction;
            }

            if (!hadDegradation)
                return 100;

            int percentage = (int)Math.Round(healthFraction * 100.0);
            return ClampPercentage(percentage);
        }

        private static int? CalculateSsdHealth(Dictionary<int, SmartAttribute> attributes)
        {
            var values = new List<int>();

            foreach (var id in SsdHealthAttributeIds)
            {
                if (!attributes.TryGetValue(id, out var attribute))
                    continue;

                var interpretedValue = InterpretSmartPercentage(attribute);
                if (interpretedValue.HasValue)
                    values.Add(interpretedValue.Value);
            }

            if (values.Count == 0)
                return null;

            return values.Min();
        }

        private static int? InterpretSmartPercentage(SmartAttribute attribute)
        {
            if (attribute.CurrentValue > 0 && attribute.CurrentValue <= 100 && attribute.CurrentValue != 0xFF)
                return ClampPercentage(attribute.CurrentValue);

            int rawLowByte = attribute.RawLow16 & 0xFF;
            if (rawLowByte > 0 && rawLowByte <= 100)
                return ClampPercentage(rawLowByte);

            int rawLow16 = attribute.RawLow16;
            if (rawLow16 > 100 && rawLow16 <= 10000)
            {
                double scaled = rawLow16 / 100.0;
                if (scaled > 0 && scaled <= 100)
                    return ClampPercentage((int)Math.Round(scaled));
            }

            var rawBytes = BitConverter.GetBytes(attribute.RawValue);
            foreach (var b in rawBytes)
            {
                if (b > 0 && b <= 100)
                    return ClampPercentage(b);
            }

            return null;
        }

        private static int ExtractTemperature(Dictionary<int, SmartAttribute> attributes)
        {
            try
            {
                if (attributes.TryGetValue(0xC2, out var temperatureAttr))
                {
                    int temp = temperatureAttr.RawLow16 & 0xFF;
                    if (temp > 0)
                        return temp;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting temperature: {ex.Message}");
            }

            return -1;
        }

        private static int ExtractPowerOnHours(Dictionary<int, SmartAttribute> attributes)
        {
            try
            {
                if (attributes.TryGetValue(0x09, out var powerAttr))
                {
                    var hours = powerAttr.RawValue;
                    if (hours > int.MaxValue)
                        return int.MaxValue;

                    return (int)hours;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting power-on hours: {ex.Message}");
            }

            return -1;
        }

        private static string ResolveDiskDeviceId(int diskIndex)
        {
            try
            {
                var query = $"SELECT DeviceID FROM Win32_DiskDrive WHERE Index = {diskIndex}";
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var id = disk["DeviceID"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(id))
                            return id;
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[StorageHealthService] Failed to resolve disk DeviceID: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[StorageHealthService] Access denied resolving disk DeviceID: {ex.Message}");
            }

            return string.Empty;
        }

        private static string EscapeWmiValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        private static string GetHealthStatusText(int healthPercentage)
        {
            if (healthPercentage < 0)
                return "Unknown";

            if (healthPercentage >= 80)
                return "Healthy";

            if (healthPercentage >= 50)
                return "Degraded";

            return "Critical";
        }

        private static int ClampPercentage(int value)
        {
            if (value < 0)
                return 0;

            if (value > 100)
                return 100;

            return value;
        }

        public static string FormatStorageHealth(StorageHealthInfo health)
        {
            if (health == null)
                return "Storage health information not available";

            var parts = new List<string>();

            if (health.HealthPercentage >= 0)
                parts.Add($"Health: {health.HealthPercentage}%");

            if (!string.IsNullOrWhiteSpace(health.HealthStatus) &&
                !health.HealthStatus.Equals("Unknown", StringComparison.OrdinalIgnoreCase) &&
                !health.HealthStatus.Equals("Unable to retrieve", StringComparison.OrdinalIgnoreCase))
                parts.Add($"Status: {health.HealthStatus}");

            if (health.Temperature > 0)
                parts.Add($"Temp: {health.Temperature}\u00B0C");

            if (health.PowerOnHours > 0)
            {
                int days = health.PowerOnHours / 24;
                parts.Add($"Power-On Time: {days:N0} days");
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : "Storage health information not available";
        }

        private static void TryPopulateHealthFromDiskStatus(string diskIndex, StorageHealthInfo health)
        {
            if (health == null)
                return;

            if (!int.TryParse(diskIndex, out var index))
                return;

            try
            {
                var query = $"SELECT Status FROM Win32_DiskDrive WHERE Index = {index}";
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var status = obj["Status"]?.ToString();
                        if (string.IsNullOrWhiteSpace(status))
                            continue;

                        if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
                        {
                            health.HealthPercentage = 100;
                            health.HealthStatus = "Healthy";
                            health.IsHealthy = true;
                        }
                        else if (status.IndexOf("pred", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            health.HealthPercentage = 5;
                            health.HealthStatus = "Warning: Failure Predicted";
                            health.IsHealthy = false;
                        }
                        else
                        {
                            health.HealthPercentage = 50;
                            health.HealthStatus = status;
                            health.IsHealthy = false;
                        }

                        break;
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"Error retrieving fallback disk status: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Access denied retrieving fallback disk status: {ex.Message}");
            }
        }
    }

    internal class StorageHealthInfo
    {
        public string DiskIndex { get; set; }
        public string HealthStatus { get; set; }
        public bool IsHealthy { get; set; }
        public int Temperature { get; set; }
        public int PowerOnHours { get; set; }
        public int HealthPercentage { get; set; }
        public bool IsSsd { get; set; }
    }

    internal class DiskUsageInfo
    {
        public bool HasData { get; set; }
        public ulong TotalBytes { get; set; }
        public ulong UsedBytes { get; set; }
        public ulong FreeBytes { get; set; }
        public int UsagePercentage { get; set; }
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
        public int RawLow16 => (int)(RawValue & 0xFFFF);
    }
}
