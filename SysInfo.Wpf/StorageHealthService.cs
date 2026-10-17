using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace CNSysInfo
{
    /// <summary>WMI access for storage health (SMART) and usage. Decision logic lives in StorageHealthLogic.</summary>
    internal static class StorageHealthService
    {
        public static StorageHealthInfo GetStorageHealth(string diskIndex = "0")
        {
            var windowsHealth = new StorageHealthInfo { DiskIndex = diskIndex };
            ReadWindowsStorageHealth(diskIndex, windowsHealth);
            Dictionary<int, SmartAttribute> attributes = null;
            bool? failurePredicted = null;
            string windowsStatus = null;
            string pnpId = null;

            try
            {
                if (int.TryParse(diskIndex, out var index))
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT PNPDeviceID, Status FROM Win32_DiskDrive WHERE Index = " + index))
                    {
                        searcher.Options.Timeout = TimeSpan.FromSeconds(1);
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            pnpId = obj["PNPDeviceID"]?.ToString();
                            windowsStatus = obj["Status"]?.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is ManagementException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"[StorageHealthService] Win32_DiskDrive query failed: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(pnpId))
            {
                // root\WMI classes are not available on every machine (NVMe and some USB drives expose nothing); that is Unknown, not an error.
                try
                {
                    using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus"))
                    {
                        searcher.Options.Timeout = TimeSpan.FromSeconds(1);
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (StorageHealthLogic.InstanceMatchesPnpId(obj["InstanceName"]?.ToString(), pnpId) && obj["PredictFailure"] is bool predict)
                            {
                                failurePredicted = predict;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is ManagementException || ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[StorageHealthService] Failure-predict query unavailable: {ex.Message}");
                }

                try
                {
                    using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT InstanceName, VendorSpecific FROM MSStorageDriver_FailurePredictData"))
                    {
                        searcher.Options.Timeout = TimeSpan.FromSeconds(1);
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (StorageHealthLogic.InstanceMatchesPnpId(obj["InstanceName"]?.ToString(), pnpId) && obj["VendorSpecific"] is byte[] data)
                            {
                                attributes = StorageHealthLogic.ParseSmartAttributes(data);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is ManagementException || ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[StorageHealthService] SMART data query unavailable: {ex.Message}");
                }
            }

            var health = StorageHealthLogic.Evaluate(diskIndex, attributes, failurePredicted, windowsStatus);
            if (windowsHealth.State > health.State)
            {
                health.State = windowsHealth.State;
                health.StatusText = windowsHealth.StatusText;
                health.Source = windowsHealth.Source;
            }
            health.LifeRemainingPercent = windowsHealth.LifeRemainingPercent;
            health.TemperatureCelsius = windowsHealth.TemperatureCelsius ?? health.TemperatureCelsius;
            health.PowerOnHours = windowsHealth.PowerOnHours ?? health.PowerOnHours;
            if (int.TryParse(diskIndex, out var nvmeIndex))
            {
                var nvme = NvmeHealthReader.Read(nvmeIndex);
                if (nvme != null)
                {
                    if (nvme.State >= health.State)
                    {
                        health.State = nvme.State;
                        health.StatusText = nvme.StatusText;
                        health.Source = nvme.Source;
                    }
                    health.IsSsd = true;
                    health.LifeRemainingPercent = nvme.LifeRemainingPercent;
                    health.TemperatureCelsius = nvme.TemperatureCelsius ?? health.TemperatureCelsius;
                    health.PowerOnHours = nvme.PowerOnHours ?? health.PowerOnHours;
                }
            }
            return health;
        }

        private static void ReadWindowsStorageHealth(string diskIndex, StorageHealthInfo health)
        {
            if (!int.TryParse(diskIndex, out var index) || index < 0) return;
            const string storageNamespace = @"root\Microsoft\Windows\Storage";
            try
            {
                using (var searcher = new ManagementObjectSearcher(storageNamespace,
                    "SELECT * FROM MSFT_Disk WHERE Number = " + index))
                {
                    searcher.Options.Timeout = TimeSpan.FromSeconds(2);
                    using (var disks = searcher.Get())
                    foreach (ManagementObject disk in disks)
                    using (disk)
                    {
                        StorageHealthLogic.MergeWindowsHealth(health,
                            (int?)WmiValueParser.ToUInt64(disk["HealthStatus"]), null, null, null, null, null);
                        // Follow the association to this OS disk, never assume that a
                        // PhysicalDisk DeviceId is the same as a Win32 disk index.
                        using (var counterSearcher = new ManagementObjectSearcher(storageNamespace,
                            "ASSOCIATORS OF {" + disk.Path.RelativePath + "} WHERE AssocClass = MSFT_DiskToStorageReliabilityCounter"))
                        {
                            counterSearcher.Options.Timeout = TimeSpan.FromSeconds(2);
                            using (var counters = counterSearcher.Get())
                            foreach (ManagementObject counter in counters)
                            using (counter)
                            {
                                StorageHealthLogic.MergeWindowsHealth(health, null,
                                    (int?)WmiValueParser.ToUInt64(counter["Wear"]),
                                    (int?)WmiValueParser.ToUInt64(counter["Temperature"]),
                                    (int?)WmiValueParser.ToUInt64(counter["PowerOnHours"]),
                                    WmiValueParser.ToUInt64(counter["ReadErrorsUncorrected"]),
                                    WmiValueParser.ToUInt64(counter["WriteErrorsUncorrected"]));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is ManagementException || ex is UnauthorizedAccessException || ex is System.Runtime.InteropServices.COMException)
            {
                Debug.WriteLine("[StorageHealthService] Windows storage health unavailable: " + ex.Message);
            }
        }

        public static string FormatStorageHealth(StorageHealthInfo health)
        {
            return StorageHealthLogic.Format(health);
        }

        public static DiskUsageInfo CalculateDiskUsage(string diskIndex = "0")
        {
            var usage = new DiskUsageInfo();

            if (!int.TryParse(diskIndex, out var index))
                return usage;

            var modernUsage = CalculateWindowsVolumeUsage(index);
            if (modernUsage.HasData) return modernUsage;

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

                                        // The association already returns Win32_LogicalDisk,
                                        // including capacity. Do not query it a second time.
                                        if (!ulong.TryParse(logicalDisk["Size"]?.ToString(), out var sizeBytes) || sizeBytes == 0)
                                            continue;

                                        if (!ulong.TryParse(logicalDisk["FreeSpace"]?.ToString(), out var free))
                                            return usage; // unreadable is not zero free space
                                        if (free > sizeBytes)
                                            free = sizeBytes;

                                        totalBytes += sizeBytes;
                                        freeBytes += free;
                            }
                        }
                    }
                }

                return DiskUsageInfo.FromTotals(totalBytes, freeBytes);
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

        private static DiskUsageInfo CalculateWindowsVolumeUsage(int diskIndex)
        {
            if (diskIndex < 0) return new DiskUsageInfo();
            try
            {
                ulong total = 0, free = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage",
                    "SELECT DriveLetter FROM MSFT_Partition WHERE DiskNumber = " + diskIndex))
                {
                    searcher.Options.Timeout = TimeSpan.FromSeconds(2);
                    using (var partitions = searcher.Get())
                    foreach (ManagementObject partition in partitions)
                    using (partition)
                    {
                        var letter = partition["DriveLetter"]?.ToString();
                        if (letter == null || letter.Length != 1 || !char.IsLetter(letter[0])) continue;
                        var path = letter + @":\";
                        if (!seen.Add(path)) continue;
                        var drive = new DriveInfo(path);
                        if (!drive.IsReady) return new DiskUsageInfo();
                        // TotalFreeSpace measures physical free space rather than
                        // the current user's quota (AvailableFreeSpace).
                        total = checked(total + (ulong)drive.TotalSize);
                        free = checked(free + (ulong)drive.TotalFreeSpace);
                    }
                }
                return DiskUsageInfo.FromTotals(total, free);
            }
            catch (Exception ex) when (ex is ManagementException || ex is UnauthorizedAccessException ||
                ex is IOException || ex is System.Runtime.InteropServices.COMException)
            {
                Debug.WriteLine("[StorageHealthService] Windows volume mapping unavailable: " + ex.Message);
                return new DiskUsageInfo();
            }
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
    }
}
