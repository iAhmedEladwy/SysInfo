using System;
using System.Diagnostics;
using System.Management;

namespace CNSysInfo
{
    /// <summary>
    /// Service to retrieve storage health information including SMART status
    /// </summary>
    internal static class StorageHealthService
    {
        public static StorageHealthInfo GetStorageHealth(string diskIndex = "0")
        {
            var health = new StorageHealthInfo
            {
                DiskIndex = diskIndex,
                HealthStatus = "Unknown",
                Temperature = -1,
                PowerOnHours = -1,
                IsHealthy = false
            };

            try
            {
                // Query SMART data from WMI
                var query = $"SELECT * FROM MSStorageDriver_FailurePredictStatus WHERE InstanceName LIKE '%PhysicalDrive{diskIndex}%'";
                
                using (var searcher = new ManagementObjectSearcher("root\\WMI", query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["PredictFailure"] != null)
                        {
                            bool predictFailure = (bool)obj["PredictFailure"];
                            health.IsHealthy = !predictFailure;
                            health.HealthStatus = predictFailure ? "Warning: Failure Predicted" : "Healthy";
                        }
                    }
                }

                // Try to get temperature
                query = $"SELECT * FROM MSStorageDriver_ATAPISmartData WHERE InstanceName LIKE '%PhysicalDrive{diskIndex}%'";
                
                using (var searcher = new ManagementObjectSearcher("root\\WMI", query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["VendorSpecific"] != null)
                        {
                            byte[] smartData = (byte[])obj["VendorSpecific"];
                            health.Temperature = ExtractTemperatureFromSmart(smartData);
                            health.PowerOnHours = ExtractPowerOnHoursFromSmart(smartData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving storage health: {ex.Message}");
                health.HealthStatus = "Unable to retrieve";
            }

            return health;
        }

        private static int ExtractTemperatureFromSmart(byte[] smartData)
        {
            try
            {
                // SMART attribute 194 (0xC2) is typically temperature
                // This is a simplified extraction - actual SMART parsing is more complex
                if (smartData != null && smartData.Length >= 362)
                {
                    for (int i = 2; i < 362; i += 12)
                    {
                        if (smartData[i] == 0xC2) // Temperature attribute ID
                        {
                            return smartData[i + 5]; // Raw value
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting temperature: {ex.Message}");
            }

            return -1;
        }

        private static int ExtractPowerOnHoursFromSmart(byte[] smartData)
        {
            try
            {
                // SMART attribute 9 (0x09) is Power-On Hours
                if (smartData != null && smartData.Length >= 362)
                {
                    for (int i = 2; i < 362; i += 12)
                    {
                        if (smartData[i] == 0x09) // Power-On Hours attribute ID
                        {
                            // Raw value is typically in bytes 7-10 (little-endian)
                            int hours = smartData[i + 5] | (smartData[i + 6] << 8) | 
                                       (smartData[i + 7] << 16) | (smartData[i + 8] << 24);
                            return hours;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting power-on hours: {ex.Message}");
            }

            return -1;
        }

        public static string FormatStorageHealth(StorageHealthInfo health)
        {
            if (health == null || health.HealthStatus == "Unknown")
                return "Storage health information not available";

            var result = $"Status: {health.HealthStatus}";

            if (health.Temperature > 0)
                result += $" | Temp: {health.Temperature}°C";

            if (health.PowerOnHours > 0)
            {
                int days = health.PowerOnHours / 24;
                result += $" | Power-On Time: {days:N0} days";
            }

            return result;
        }
    }

    internal class StorageHealthInfo
    {
        public string DiskIndex { get; set; }
        public string HealthStatus { get; set; }
        public bool IsHealthy { get; set; }
        public int Temperature { get; set; }
        public int PowerOnHours { get; set; }
    }
}
