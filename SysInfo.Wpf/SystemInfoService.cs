using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Management;

namespace CNSysInfo
{
    internal static class SystemInfoService
    {
        public static SystemInfoSnapshot Collect()
        {
            // UI responsiveness: deadline and explicit partial-data diagnostics.
            using var collection = new CollectionSession(20000);
            var stopwatch = Stopwatch.StartNew();
            var capturedAt = DateTime.Now;
            var memoryInfo = SafeExecute(GetMemorySummary, nameof(GetMemorySummary), new MemorySummary());
            var installedPhysicalMemory = (memoryInfo != null && !string.IsNullOrWhiteSpace(memoryInfo.InstalledMemory))
                ? memoryInfo.InstalledMemory
                : SafeExecute(GetInstalledPhysicalMemory, nameof(GetInstalledPhysicalMemory), "Unknown");
            var cpuDescriptor = SafeExecute(GetCpuDescriptor, nameof(GetCpuDescriptor), new CpuDescriptor());
            var memoryStatus = SafeExecute(GetMemoryStatus, nameof(GetMemoryStatus), new MemoryStatusInfo());

            // Core system info (fast queries)
            var snapshot = new SystemInfoSnapshot
            {
                CapturedAt = capturedAt,
                TimeZoneDisplayName = SafeExecute(() => TimeZoneInfo.Local.DisplayName, "TimeZone", string.Empty),
                MachineName = SafeExecute(() => Environment.MachineName, "MachineName", string.Empty),
                UserName = SafeExecute(() => Environment.UserName, "UserName", string.Empty),
                Manufacturer = SafeExecute(GetManufacturer, nameof(GetManufacturer), "Unknown"),
                SystemModel = SafeExecute(GetSystemProduct, nameof(GetSystemProduct), "Unknown"),
                SerialNumber = SafeExecute(GetLaptopSerialNumber, nameof(GetLaptopSerialNumber), "Unknown"),
                AssetTag = SafeExecute(GetAssetTag, nameof(GetAssetTag), "Unknown"),
                BiosVersion = SafeExecute(GetBiosVersion, nameof(GetBiosVersion), "Unknown"),
                Cpu = string.IsNullOrWhiteSpace(cpuDescriptor.Name) ? SafeExecute(GetCpuType, nameof(GetCpuType), "Unknown") : cpuDescriptor.Name,
                CpuManufacturer = cpuDescriptor.Manufacturer,
                CpuProcessorId = cpuDescriptor.ProcessorId,
                CpuFamily = cpuDescriptor.Family,
                CpuModel = cpuDescriptor.Model,
                CpuL2CacheKb = cpuDescriptor.L2CacheKb,
                CpuL3CacheKb = cpuDescriptor.L3CacheKb,
                CpuCoreCount = SafeExecute(GetCpuCoreCount, nameof(GetCpuCoreCount), 0),
                CpuLogicalProcessorCount = SafeExecute(GetCpuLogicalProcessorCount, nameof(GetCpuLogicalProcessorCount), 0),
                InstalledPhysicalMemory = installedPhysicalMemory,
                InstalledPhysicalMemoryBytes = memoryInfo != null ? memoryInfo.InstalledBytes : 0UL,
                UsablePhysicalMemory = SafeExecute(GetTotalPhysicalMemory, nameof(GetTotalPhysicalMemory), "Unknown"),
                UsablePhysicalMemoryBytes = memoryStatus.TotalBytes,
                AvailablePhysicalMemoryBytes = memoryStatus.AvailableBytes,
                MemoryLoadPercent = memoryStatus.LoadPercent,
                MemorySlotsUsed = memoryInfo?.SlotsUsed ?? 0,
                MemorySlotsTotal = memoryInfo?.SlotsTotal ?? 0,
                MemorySpeedMhz = memoryInfo?.SpeedMhz ?? 0,
                OsDisplay = SafeExecute(GetOperatingSystemDisplay, nameof(GetOperatingSystemDisplay), "Unknown"),
                OsBuildNumber = SafeExecute(GetOsBuildNumber, nameof(GetOsBuildNumber), 0),
                OsInstallDate = SafeExecute(GetOsInstallDate, nameof(GetOsInstallDate), "Unknown"),
                WindowsUptime = SafeExecute(GetWindowsUptime, nameof(GetWindowsUptime), TimeSpan.FromTicks(-1)),
                Disks = SafeExecute(GetDiskInfo, nameof(GetDiskInfo), new List<DiskInfo>()),
                Gpus = SafeExecute(GetGpuInfo, nameof(GetGpuInfo), new List<GpuInfo>()),
                Monitors = SafeExecute(GetMonitorInfo, nameof(GetMonitorInfo), new List<MonitorInfo>()),

                // Hardware queries
                BatteryInfo = SafeExecute(GetBatteryInfo, nameof(GetBatteryInfo), null)
            };

            // Licensing can start a cold WMI provider; collect it after core hardware
            // so its longer allowance cannot starve those reads.
            var activationClock = Stopwatch.StartNew();
            int activationWarningStart = collection.Warnings.Count;
            snapshot.Activation = collection.Read(() => ActivationLogic.Read(QueryWindowsActivation,
                () => Math.Min(6000 - (int)activationClock.ElapsedMilliseconds, collection.RemainingMilliseconds)),
                "Windows activation", ActivationResult.Unavailable(ActivationUnavailableReason.Error), 6000,
                reason => ActivationResult.Unavailable(reason == "deadline" ? ActivationUnavailableReason.Deadline :
                    reason == "busy" ? ActivationUnavailableReason.Busy : reason == "timeout" ? ActivationUnavailableReason.Timeout : ActivationUnavailableReason.Error));
            if (!snapshot.Activation.IsAvailable)
            {
                // Count the unavailable operation once, retaining its specific reason.
                collection.Warnings.RemoveRange(activationWarningStart, collection.Warnings.Count - activationWarningStart);
                collection.Warnings.Add("Windows activation: " + snapshot.Activation.Reason +
                    " (" + activationClock.ElapsedMilliseconds + " ms)");
            }
            stopwatch.Stop();
            Debug.WriteLine($"[SystemInfoService] Total collection time: {stopwatch.ElapsedMilliseconds}ms");

            snapshot.CollectionWarnings = collection.Warnings.ToArray();
            return snapshot;
        }

        internal static string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Days} days, {timeSpan.Hours} hours, {timeSpan.Minutes} minutes, {timeSpan.Seconds} seconds";
        }

        private static int TryParseUnsigned(object value)
        {
            if (value == null)
            {
                return 0;
            }

            switch (value)
            {
                case ushort ushortValue:
                    return ushortValue;
                case uint uintValue:
                    return (int)uintValue;
                case int intValue when intValue >= 0:
                    return intValue;
            }

            if (int.TryParse(value.ToString(), out var parsed) && parsed >= 0)
            {
                return parsed;
            }

            return 0;
        }

        private static string GetLaptopSerialNumber()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var serialNumber = obj["SerialNumber"] != null ? obj["SerialNumber"].ToString() : null;
                    if (!string.IsNullOrWhiteSpace(serialNumber))
                    {
                        return serialNumber;
                    }
                }
            }

            return "Unknown";
        }

        private static string GetSystemProduct()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_ComputerSystemProduct"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"] != null ? obj["Name"].ToString() : null;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
                }
            }

            return "Unknown";
        }

        private static string GetManufacturer()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var manufacturer = obj["Manufacturer"] != null ? obj["Manufacturer"].ToString() : null;
                    if (!string.IsNullOrWhiteSpace(manufacturer))
                    {
                        return manufacturer;
                    }
                }
            }

            return "Unknown";
        }

        private static string GetTotalPhysicalMemory()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["TotalPhysicalMemory"] == null)
                    {
                        continue;
                    }

                    var totalPhysicalMemoryBytes = WmiValueParser.ToUInt64(obj["TotalPhysicalMemory"]);
                    if (!totalPhysicalMemoryBytes.HasValue || totalPhysicalMemoryBytes.Value == 0)
                    {
                        continue;
                    }

                    return FormatBytes(totalPhysicalMemoryBytes.Value);
                }
            }

            return "Unknown";
        }

        private static string GetInstalledPhysicalMemory()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
            {
                ulong installedMemoryBytes = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["Capacity"] == null)
                    {
                        continue;
                    }

                    installedMemoryBytes += WmiValueParser.ToUInt64(obj["Capacity"]) ?? 0UL;
                }

                return installedMemoryBytes == 0 ? "Unknown" : FormatBytes(installedMemoryBytes);
            }
        }

        private static MemorySummary GetMemorySummary()
        {
            var summary = new MemorySummary();
            ulong installedBytes = 0;

            try
            {
                using (var moduleSearcher = new ManagementObjectSearcher("SELECT Capacity, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in moduleSearcher.Get())
                    {
                        if (obj["Capacity"] == null)
                        {
                            continue;
                        }

                        var capacity = WmiValueParser.ToUInt64(obj["Capacity"]) ?? 0UL;
                        if (capacity == 0)
                        {
                            continue;
                        }

                        installedBytes += capacity;
                        summary.SlotsUsed++;

                        // ConfiguredClockSpeed is what the module actually runs at; Speed is the rated maximum.
                        if (summary.SpeedMhz == 0)
                        {
                            var configured = WmiValueParser.ToUInt64(obj["ConfiguredClockSpeed"]) ?? 0UL;
                            var rated = WmiValueParser.ToUInt64(obj["Speed"]) ?? 0UL;
                            var speed = configured > 0 ? configured : rated;
                            if (speed > 0 && speed < int.MaxValue)
                            {
                                summary.SpeedMhz = (int)speed;
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Unable to read physical memory modules: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Access denied reading physical memory modules: {ex.Message}");
            }

            try
            {
                using (var arraySearcher = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray"))
                {
                    foreach (ManagementObject obj in arraySearcher.Get())
                    {
                        if (obj["MemoryDevices"] == null)
                        {
                            continue;
                        }

                        var slots = Convert.ToInt32(obj["MemoryDevices"]);
                        if (slots > 0)
                        {
                            summary.SlotsTotal += slots;
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Unable to read memory slot count: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Access denied reading memory slot count: {ex.Message}");
            }

            if (summary.SlotsTotal == 0 && summary.SlotsUsed > 0)
            {
                summary.SlotsTotal = summary.SlotsUsed;
            }

            if (installedBytes > 0)
            {
                summary.InstalledBytes = installedBytes;
                summary.InstalledMemory = FormatBytes(installedBytes);
            }

            return summary;
        }

        private static ActivationResult QueryWindowsActivation(bool fallback, int timeoutMs)
        {
            const string windowsApplicationId = "55c92734-d682-4d71-983e-d6ec3f16059f";
            string filter = fallback ? "Description LIKE 'Windows%'" : $"ApplicationID = '{windowsApplicationId}'";
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT LicenseStatus, PartialProductKey FROM SoftwareLicensingProduct WHERE " + filter))
                {
                    searcher.Options.Timeout = TimeSpan.FromMilliseconds(Math.Max(1, timeoutMs));
                    WindowsLicenseStatus? found = null;
                    using (var rows = searcher.Get())
                    foreach (ManagementObject obj in rows)
                    using (obj)
                    {
                        if (obj == null || obj["LicenseStatus"] == null || string.IsNullOrWhiteSpace(obj["PartialProductKey"]?.ToString())) continue;
                        int code = Convert.ToInt32(obj["LicenseStatus"]);
                        if (code < 0 || code > 6) continue;
                        var status = (WindowsLicenseStatus)code;
                        if (status == WindowsLicenseStatus.Licensed) return new ActivationResult(status, ActivationUnavailableReason.None, DateTime.Now);
                        if (!found.HasValue) found = status;
                    }
                    return found.HasValue ? new ActivationResult(found, ActivationUnavailableReason.None, DateTime.Now) : ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
                }
            }
            catch (ManagementException ex)
            {
                return ActivationResult.Unavailable(ex.ErrorCode == ManagementStatus.Timedout ? ActivationUnavailableReason.Timeout : ActivationUnavailableReason.Error);
            }
            catch (Exception) { return ActivationResult.Unavailable(ActivationUnavailableReason.Error); }
        }
        private static CpuDescriptor GetCpuDescriptor()
        {
            // Win32_Processor has no "Model" property (selecting it makes the whole query fail), and its "Family"
            // is the SMBIOS processor family enum, not the CPUID family. CPUID family/model come from ProcessorId.
            using (var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, ProcessorId, L2CacheSize, L3CacheSize FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var processorId = obj["ProcessorId"] != null ? obj["ProcessorId"].ToString().Trim() : string.Empty;
                    var signature = CpuId.Parse(processorId);

                    return new CpuDescriptor
                    {
                        Name = obj["Name"] != null ? obj["Name"].ToString().Trim() : string.Empty,
                        Manufacturer = obj["Manufacturer"] != null ? obj["Manufacturer"].ToString().Trim() : string.Empty,
                        ProcessorId = processorId,
                        L2CacheKb = WmiValueParser.ToUInt64(obj["L2CacheSize"]),
                        L3CacheKb = WmiValueParser.ToUInt64(obj["L3CacheSize"]),
                        Family = signature.IsValid ? signature.Family : 0,
                        Model = signature.IsValid ? signature.Model : 0
                    };
                }
            }

            return new CpuDescriptor();
        }

        private static string GetCpuType()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var cpuName = obj["Name"] != null ? obj["Name"].ToString() : null;
                    if (!string.IsNullOrWhiteSpace(cpuName))
                    {
                        return cpuName;
                    }
                }
            }

            return "Unknown";
        }

        private static int GetCpuCoreCount()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor"))
            {
                var totalCores = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj?["NumberOfCores"] == null)
                    {
                        continue;
                    }

                    if (int.TryParse(obj["NumberOfCores"].ToString(), out var cores) && cores > 0)
                    {
                        totalCores += cores;
                    }
                }

                if (totalCores > 0)
                {
                    return totalCores;
                }
            }

            return 0;
        }

        private static int GetCpuLogicalProcessorCount()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT NumberOfLogicalProcessors FROM Win32_Processor"))
            {
                var totalLogical = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj?["NumberOfLogicalProcessors"] == null)
                    {
                        continue;
                    }

                    if (int.TryParse(obj["NumberOfLogicalProcessors"].ToString(), out var logical) && logical > 0)
                    {
                        totalLogical += logical;
                    }
                }

                if (totalLogical > 0)
                {
                    return totalLogical;
                }
            }

            return Environment.ProcessorCount;
        }

        private static string GetAssetTag()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT SMBIOSAssetTag FROM Win32_SystemEnclosure"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var assetTag = obj["SMBIOSAssetTag"] != null ? obj["SMBIOSAssetTag"].ToString() : null;
                    if (!string.IsNullOrWhiteSpace(assetTag) &&
                        !assetTag.Trim().Equals("To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase) &&
                        !assetTag.Trim().Equals("Default string", StringComparison.OrdinalIgnoreCase) &&
                        !assetTag.Trim().Equals("None", StringComparison.OrdinalIgnoreCase) &&
                        !assetTag.Trim().Equals("No Asset Tag", StringComparison.OrdinalIgnoreCase))
                    {
                        return assetTag;
                    }
                }
            }

            return "Not assigned";
        }

        private static string GetOperatingSystemDisplay()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Caption, BuildNumber, OSArchitecture, Version FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var caption = obj["Caption"] != null ? obj["Caption"].ToString() : string.Empty;
                    var architecture = obj["OSArchitecture"] != null ? obj["OSArchitecture"].ToString() : string.Empty;
                    WindowsVersionInfo.TryParseBuild(obj["BuildNumber"] != null ? obj["BuildNumber"].ToString() : string.Empty, out var build);

                    var major = 0;
                    var versionText = obj["Version"] != null ? obj["Version"].ToString() : string.Empty;
                    var dot = versionText.IndexOf('.');
                    if (dot > 0)
                    {
                        int.TryParse(versionText.Substring(0, dot), out major);
                    }

                    // Windows 10 and 11 both report 10.0. Decide by build number (>= 22000 is Windows 11), not by Caption.
                    var displayVersion = GetWindowsDisplayVersion();
                    return WindowsVersionInfo.BuildDisplay(caption, major, build, architecture, displayVersion == "Unknown" ? string.Empty : displayVersion);
                }
            }

            return "Unknown";
        }

        private static int GetOsBuildNumber()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT BuildNumber FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (WindowsVersionInfo.TryParseBuild(obj["BuildNumber"] != null ? obj["BuildNumber"].ToString() : string.Empty, out var build))
                    {
                        return build;
                    }
                }
            }

            return 0;
        }

        private static string GetBiosVersion()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT SoftwareElementID FROM Win32_BIOS"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var biosVersion = obj["SoftwareElementID"] != null ? obj["SoftwareElementID"].ToString() : null;
                    if (!string.IsNullOrWhiteSpace(biosVersion))
                    {
                        return biosVersion;
                    }
                }
            }

            return "Unknown";
        }

        private static TimeSpan GetWindowsUptime()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var lastBootValue = obj["LastBootUpTime"] != null ? obj["LastBootUpTime"].ToString() : null;
                    if (string.IsNullOrWhiteSpace(lastBootValue))
                    {
                        continue;
                    }

                    var lastBoot = ManagementDateTimeConverter.ToDateTime(lastBootValue);
                    return DateTime.Now - lastBoot;
                }
            }

            return TimeSpan.FromTicks(-1);
        }

        private static List<DiskInfo> GetDiskInfo()
        {
            var disks = new List<DiskInfo>();

            var physicalDiskInfo = GetPhysicalDiskMediaInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT Index, Model, Size, MediaType, InterfaceType, SerialNumber, PNPDeviceID FROM Win32_DiskDrive"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var sizeBytes = WmiValueParser.ToUInt64(obj["Size"]) ?? 0UL;
                    var mediaType = obj["MediaType"] != null ? obj["MediaType"].ToString() : string.Empty;
                    var interfaceType = obj["InterfaceType"] != null ? obj["InterfaceType"].ToString() : string.Empty;
                    var model = obj["Model"] != null ? obj["Model"].ToString() : string.Empty;
                    var serialNumber = obj["SerialNumber"] != null ? obj["SerialNumber"].ToString() : string.Empty;
                    var pnpDeviceId = obj["PNPDeviceID"] != null ? obj["PNPDeviceID"].ToString() : string.Empty;

                    disks.Add(new DiskInfo
                    {
                        Index = obj["Index"] != null ? obj["Index"].ToString() : string.Empty,
                        Model = model,
                        SizeBytes = sizeBytes,
                        Size = sizeBytes == 0 ? "Unknown" : FormatBytes(sizeBytes),
                        MediaType = DetermineDiskType(mediaType, model, interfaceType, pnpDeviceId, serialNumber, physicalDiskInfo),
                        InterfaceType = interfaceType ?? string.Empty
                    });
                }
            }

            return disks;
        }

        private static string DetermineDiskType(string mediaType, string model, string interfaceType, string pnpDeviceId, string serialNumber, IReadOnlyList<PhysicalDiskMediaInfo> physicalDiskInfo)
        {
            var resolved = ResolvePhysicalDiskMediaType(model, serialNumber, physicalDiskInfo);
            if (!string.Equals(resolved, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return resolved;
            }

            var candidate = $"{mediaType} {model} {interfaceType} {pnpDeviceId}".ToLowerInvariant();
            if (candidate.Contains("nvme") ||
                candidate.Contains("non-volatile memory express") ||
                candidate.Contains("pcie") ||
                candidate.Contains("pci express"))
            {
                return "NVMe SSD";
            }

            if (candidate.Contains("ssd") || candidate.Contains("solid-state") || candidate.Contains("solid state"))
            {
                if (candidate.Contains("sata"))
                {
                    return "SATA SSD";
                }

                return "SSD";
            }

            if (candidate.Contains("hdd") ||
                candidate.Contains("hard disk") ||
                candidate.Contains("hard-disk") ||
                (candidate.Contains("sata") && !candidate.Contains("ssd")))
            {
                return "HDD";
            }

            return string.IsNullOrWhiteSpace(mediaType) ? "Unknown" : mediaType;
        }

        private static string ResolvePhysicalDiskMediaType(string model, string serialNumber, IReadOnlyList<PhysicalDiskMediaInfo> physicalDiskInfo)
        {
            if (physicalDiskInfo == null || physicalDiskInfo.Count == 0)
            {
                return "Unknown";
            }

            var serialTrimmed = string.IsNullOrWhiteSpace(serialNumber) ? string.Empty : serialNumber.Trim();
            if (!string.IsNullOrWhiteSpace(serialTrimmed))
            {
                foreach (var disk in physicalDiskInfo)
                {
                    if (!string.IsNullOrWhiteSpace(disk.SerialNumber) &&
                        string.Equals(disk.SerialNumber, serialTrimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        return MapPhysicalMediaType(disk.MediaType, disk.BusType);
                    }
                }
            }

            var modelTrimmed = string.IsNullOrWhiteSpace(model) ? string.Empty : model.Trim();
            if (!string.IsNullOrWhiteSpace(modelTrimmed))
            {
                foreach (var disk in physicalDiskInfo)
                {
                    if (string.IsNullOrWhiteSpace(disk.FriendlyName))
                    {
                        continue;
                    }

                    if (string.Equals(disk.FriendlyName, modelTrimmed, StringComparison.OrdinalIgnoreCase) ||
                        disk.FriendlyName.IndexOf(modelTrimmed, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        modelTrimmed.IndexOf(disk.FriendlyName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return MapPhysicalMediaType(disk.MediaType, disk.BusType);
                    }
                }
            }

            return "Unknown";
        }

        private static string MapPhysicalMediaType(int mediaType, int busType)
        {
            const int StorageBusTypeSata = 11;
            const int StorageBusTypeNvme = 17;

            if (busType == StorageBusTypeNvme) // BusType 8 is RAID, not NVMe.
            {
                return "NVMe SSD";
            }

            if (busType == StorageBusTypeSata && mediaType == 4)
            {
                return "SATA SSD";
            }

            switch (mediaType)
            {
                case 3:
                    return "HDD";
                case 4:
                    return busType == StorageBusTypeSata ? "SATA SSD" : "SSD";
                case 5:
                    return "SCM";
            }

            return "Unknown";
        }

        private static List<PhysicalDiskMediaInfo> GetPhysicalDiskMediaInfo()
        {
            var disks = new List<PhysicalDiskMediaInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\Microsoft\\Windows\\Storage", "SELECT FriendlyName, SerialNumber, MediaType, BusType FROM MSFT_PhysicalDisk"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        disks.Add(new PhysicalDiskMediaInfo
                        {
                            FriendlyName = obj["FriendlyName"] != null ? obj["FriendlyName"].ToString().Trim() : string.Empty,
                            SerialNumber = obj["SerialNumber"] != null ? obj["SerialNumber"].ToString().Trim() : string.Empty,
                            MediaType = obj["MediaType"] != null ? Convert.ToInt32(obj["MediaType"]) : 0,
                            BusType = obj["BusType"] != null ? Convert.ToInt32(obj["BusType"]) : 0
                        });
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Unable to query physical disk media types: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Access denied retrieving physical disk media types: {ex.Message}");
            }

            return disks;
        }

        private sealed class PhysicalDiskMediaInfo
        {
            public PhysicalDiskMediaInfo()
            {
                FriendlyName = string.Empty;
                SerialNumber = string.Empty;
            }

            public string FriendlyName { get; set; }
            public string SerialNumber { get; set; }
            public int MediaType { get; set; }
            public int BusType { get; set; }
        }

        private sealed class MemorySummary
        {
            public MemorySummary()
            {
                InstalledMemory = string.Empty;
                SlotsUsed = 0;
                SlotsTotal = 0;
                SpeedMhz = 0;
            }

            public string InstalledMemory { get; set; }
            public ulong InstalledBytes { get; set; }
            public int SlotsUsed { get; set; }
            public int SlotsTotal { get; set; }
            public int SpeedMhz { get; set; }
        }

        private static List<GpuInfo> GetGpuInfo()
        {
            var gpus = new List<GpuInfo>();

            using (var searcher = new ManagementObjectSearcher("SELECT Name, Description, DriverVersion, VideoProcessor, AdapterRAM FROM Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    gpus.Add(new GpuInfo
                    {
                        Name = obj["Name"] != null ? obj["Name"].ToString() : "Unknown",
                        Description = obj["Description"] != null ? obj["Description"].ToString() : "Unknown",
                        DriverVersion = obj["DriverVersion"] != null ? obj["DriverVersion"].ToString() : "Unknown",
                        VideoProcessor = obj["VideoProcessor"] != null ? obj["VideoProcessor"].ToString() : "Unknown",
                        AdapterRam = FormatAdapterRam(WmiValueParser.ToUInt64(obj["AdapterRAM"]))
                    });
                }
            }

            return gpus;
        }

        private static string GetOsInstallDate()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT InstallDate FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var parsed = WmiValueParser.ParseCimDateTime(obj["InstallDate"] != null ? obj["InstallDate"].ToString() : null);
                        if (parsed.HasValue)
                        {
                            return WmiValueParser.FormatInstallDate(parsed);
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Win32_OperatingSystem.InstallDate failed: {ex.Message}");
            }

            // Fallback: registry value is seconds since 1970 (UTC).
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    var raw = key != null ? key.GetValue("InstallDate") : null;
                    if (raw != null && long.TryParse(raw.ToString(), out var seconds))
                    {
                        return WmiValueParser.FormatInstallDate(WmiValueParser.FromUnixSeconds(seconds));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Registry InstallDate failed: {ex.Message}");
            }

            return "Unknown";
        }

        private static BatteryInfo GetBatteryInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, BatteryStatus, EstimatedChargeRemaining, DesignCapacity, FullChargeCapacity FROM Win32_Battery"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var design = BatteryMapper.NormalizeCapacity(WmiValueParser.ToUInt64(obj["DesignCapacity"]));
                        var full = BatteryMapper.NormalizeCapacity(WmiValueParser.ToUInt64(obj["FullChargeCapacity"]));
                        var charge = BatteryMapper.NormalizeChargePercent(WmiValueParser.ToUInt64(obj["EstimatedChargeRemaining"]));
                        var statusRaw = WmiValueParser.ToUInt64(obj["BatteryStatus"]);
                        int? status = statusRaw.HasValue && statusRaw.Value < 1000 ? (int?)statusRaw.Value : null;

                        // Win32_Battery usually leaves capacities empty; root\WMI has them in mWh.
                        if (!design.HasValue || !full.HasValue)
                        {
                            var fallback = GetBatteryCapacitiesFallback();
                            design = design ?? fallback.DesignCapacity;
                            full = full ?? fallback.FullChargeCapacity;
                        }

                        return new BatteryInfo
                        {
                            Name = obj["Name"] != null ? obj["Name"].ToString() : "Battery",
                            ChargeLevel = charge,
                            DesignCapacity = design,
                            FullChargeCapacity = full,
                            HealthPercentage = BatteryMapper.CalculateHealthPercent(design, full),
                            Status = BatteryMapper.GetStatusText(status),
                            PowerStatus = BatteryMapper.GetPowerText(status),
                            IsCharging = BatteryMapper.IsCharging(status)
                        };
                    }
                }
            }
            catch (ManagementException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Battery query failed: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[{nameof(SystemInfoService)}] Battery query denied: {ex.Message}");
            }

            // No Win32_Battery instance: desktop PC (or the query failed; the log line above says which).
            return null;
        }

        private static (int? DesignCapacity, int? FullChargeCapacity) GetBatteryCapacitiesFallback()
        {
            int? designCapacity = null;
            int? fullChargeCapacity = null;

            try
            {
                using (var designSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT DesignedCapacity FROM BatteryStaticData"))
                {
                    foreach (ManagementObject obj in designSearcher.Get())
                    {
                        if (!designCapacity.HasValue)
                        {
                            designCapacity = BatteryMapper.NormalizeCapacity(WmiValueParser.ToUInt64(obj["DesignedCapacity"]));
                        }
                    }
                }
            }
            catch (ManagementException)
            {
                // Ignore and try alternative sources.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore and try alternative sources.
            }

            try
            {
                using (var fullSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity"))
                {
                    foreach (ManagementObject obj in fullSearcher.Get())
                    {
                        if (!fullChargeCapacity.HasValue)
                        {
                            fullChargeCapacity = BatteryMapper.NormalizeCapacity(WmiValueParser.ToUInt64(obj["FullChargedCapacity"]));
                        }
                    }
                }
            }
            catch (ManagementException)
            {
                // Ignore; we return whatever data we already gathered.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore; we return whatever data we already gathered.
            }

            return (designCapacity, fullChargeCapacity);
        }

        private static OfficeInfo GetOfficeInfo()
        {
            var officeInfo = new OfficeInfo();

            // Check common Office versions (2016, 2019, 2021, 365)
            string[] versions = { "16.0", "15.0", "14.0" };
            string[] productNames = { "Office 365/2021/2019/2016", "Office 2013", "Office 2010" };

            for (int i = 0; i < versions.Length; i++)
            {
                // Check 64-bit
                using (var key = Registry.LocalMachine.OpenSubKey(string.Format(@"SOFTWARE\Microsoft\Office\{0}\Common\InstallRoot", versions[i])))
                {
                    if (key != null)
                    {
                        var path = key.GetValue("Path");
                        if (path != null)
                        {
                            officeInfo.IsInstalled = true;
                            officeInfo.Version = versions[i];
                            officeInfo.ProductName = productNames[i];
                            break;
                        }
                    }
                }

                // Check 32-bit on 64-bit OS
                using (var key = Registry.LocalMachine.OpenSubKey(string.Format(@"SOFTWARE\WOW6432Node\Microsoft\Office\{0}\Common\InstallRoot", versions[i])))
                {
                    if (key != null)
                    {
                        var path = key.GetValue("Path");
                        if (path != null)
                        {
                            officeInfo.IsInstalled = true;
                            officeInfo.Version = versions[i];
                            officeInfo.ProductName = productNames[i] + " (32-bit)";
                            break;
                        }
                    }
                }
            }

            if (officeInfo.IsInstalled)
            {
                // Try to get activation status (with timeout to avoid hanging)
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT LicenseStatus FROM OfficeSoftwareProtectionProduct WHERE PartialProductKey IS NOT NULL"))
                    {
                        searcher.Options.Timeout = new TimeSpan(0, 0, 3); // 3 second timeout

                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var licenseStatus = obj["LicenseStatus"] != null ? Convert.ToInt32(obj["LicenseStatus"]) : 0;
                            officeInfo.ActivationStatus = licenseStatus == 1 ? "Activated" : "Not Activated";
                            break; // Only check first result
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GetOfficeInfo] Activation check failed: {ex.Message}");
                    officeInfo.ActivationStatus = "Unknown";
                }

                if (string.IsNullOrEmpty(officeInfo.ActivationStatus))
                {
                    officeInfo.ActivationStatus = "Unknown";
                }
            }

            return officeInfo.IsInstalled ? officeInfo : null;
        }

        private static string GetWindowsProductKey()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var serialNumber = obj["SerialNumber"] != null ? obj["SerialNumber"].ToString() : null;
                        if (!string.IsNullOrWhiteSpace(serialNumber))
                        {
                            return serialNumber;
                        }
                    }
                }
            }
            catch
            {
                // Fall back to registry method
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            var productId = key.GetValue("ProductId");
                            if (productId != null)
                            {
                                return productId.ToString();
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore
                }
            }

            return "Unknown";
        }

        private static List<string> GetAntivirusProducts()
        {
            var antivirusProducts = new List<string>();

            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT displayName FROM AntiVirusProduct"))
                {
                    searcher.Options.Timeout = new TimeSpan(0, 0, 3); // 3 second timeout

                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var displayName = obj["displayName"] != null ? obj["displayName"].ToString() : null;
                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            antivirusProducts.Add(displayName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetAntivirusProducts] Error: {ex.Message}");
                // SecurityCenter2 might not be available on servers
                antivirusProducts.Add("Unable to detect (Server OS or restricted access)");
            }

            if (antivirusProducts.Count == 0)
            {
                antivirusProducts.Add("Windows Defender (Default)");
            }

            return antivirusProducts;
        }

        private static List<MonitorInfo> GetMonitorInfo()
        {
            var monitors = new List<MonitorInfo>();
            bool isTouchEnabled = false;

            try
            {
                // Check for touchscreen support
                try
                {
                    var touchValue = Microsoft.Win32.Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata",
                        "EnableDeviceSupportForTouch",
                        null);

                    if (touchValue != null && touchValue.ToString() == "1")
                    {
                        isTouchEnabled = true;
                    }

                    // Alternative check using GetSystemMetrics
                    const int SM_DIGITIZER = 94;
                    const int NID_INTEGRATED_TOUCH = 0x01;
                    int digitizer = GetSystemMetrics(SM_DIGITIZER);
                    if ((digitizer & NID_INTEGRATED_TOUCH) != 0)
                    {
                        isTouchEnabled = true;
                    }
                }
                catch
                {
                    // Touchscreen detection failed, not critical
                }

                // Get native resolutions using EnumDisplaySettings
                var nativeResolutions = new Dictionary<string, string>();
                var screens = System.Windows.Forms.Screen.AllScreens;

                foreach (var screen in screens)
                {
                    try
                    {
                        DEVMODE devMode = new DEVMODE();
                        devMode.dmSize = (short)Marshal.SizeOf(devMode);

                        // Get current display settings (native resolution)
                        if (EnumDisplaySettings(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                        {
                            nativeResolutions[screen.DeviceName] = $"{devMode.dmPelsWidth} x {devMode.dmPelsHeight}";
                        }
                    }
                    catch
                    {
                        // Fallback to screen bounds if EnumDisplaySettings fails
                        nativeResolutions[screen.DeviceName] = $"{screen.Bounds.Width} x {screen.Bounds.Height}";
                    }
                }

                // Try WMI first for detailed monitor info
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBasicDisplayParams"))
                {
                    int screenIndex = 0;

                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            // Get physical size in centimeters
                            var maxHorizontalSize = obj["MaxHorizontalImageSize"];
                            var maxVerticalSize = obj["MaxVerticalImageSize"];

                            double sizeInInches = 0;
                            if (maxHorizontalSize != null && maxVerticalSize != null)
                            {
                                double widthCm = Convert.ToDouble(maxHorizontalSize);
                                double heightCm = Convert.ToDouble(maxVerticalSize);
                                double diagonalCm = Math.Sqrt(widthCm * widthCm + heightCm * heightCm);
                                sizeInInches = diagonalCm / 2.54;
                            }

                            var screen = screenIndex < screens.Length ? screens[screenIndex] : screens[0];
                            int refreshRate = 60; // Default
                            string panelType = "Unknown";

                            // Try to get panel type from EDID
                            try
                            {
                                using (var edidSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorID"))
                                {
                                    int edidIndex = 0;
                                    foreach (ManagementObject edidObj in edidSearcher.Get())
                                    {
                                        if (edidIndex == screenIndex)
                                        {
                                            // Try to get manufacturer product code which might indicate panel type
                                            var userFriendlyName = edidObj["UserFriendlyName"] as ushort[];
                                            if (userFriendlyName != null && userFriendlyName.Length > 0)
                                            {
                                                string name = System.Text.Encoding.ASCII.GetString(
                                                    Array.ConvertAll(userFriendlyName, b => (byte)b)).Trim('\0', ' ');

                                                // Common panel type indicators
                                                if (name.Contains("IPS") || name.Contains("In-Plane"))
                                                {
                                                    panelType = "IPS";
                                                }
                                                else if (name.Contains("TN") || name.Contains("Twisted"))
                                                {
                                                    panelType = "TN";
                                                }
                                                else if (name.Contains("VA") || name.Contains("Vertical"))
                                                {
                                                    panelType = "VA";
                                                }
                                                else if (name.Contains("OLED"))
                                                {
                                                    panelType = "OLED";
                                                }
                                            }
                                            break;
                                        }
                                        edidIndex++;
                                    }
                                }
                            }
                            catch
                            {
                                // Panel type detection failed, use default
                            }

                            try
                            {
                                // Try to get refresh rate from WMI
                                using (var modeSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorListedSupportedSourceModes"))
                                {
                                    int modeIndex = 0;
                                    foreach (ManagementObject mode in modeSearcher.Get())
                                    {
                                        if (modeIndex == screenIndex)
                                        {
                                            var modes = mode["MonitorSourceModes"] as ManagementBaseObject[];
                                            if (modes != null && modes.Length > 0)
                                            {
                                                var lastMode = modes[modes.Length - 1];
                                                if (lastMode["VerticalRefreshRate"] != null)
                                                {
                                                    refreshRate = Convert.ToInt32(lastMode["VerticalRefreshRate"]);
                                                }
                                            }
                                            break;
                                        }
                                        modeIndex++;
                                    }
                                }
                            }
                            catch
                            {
                                // Use default refresh rate
                            }

                            // Get native resolution for this screen
                            string resolution = nativeResolutions.ContainsKey(screen.DeviceName)
                                ? nativeResolutions[screen.DeviceName]
                                : $"{screen.Bounds.Width} x {screen.Bounds.Height}";

                            monitors.Add(new MonitorInfo
                            {
                                Name = screen.DeviceName,
                                Resolution = resolution,
                                RefreshRate = $"{refreshRate} Hz",
                                ScreenSize = sizeInInches > 0 ? $"{sizeInInches:F1}\"" : "Unknown",
                                PanelType = panelType,
                                IsTouchScreen = isTouchEnabled
                            });

                            screenIndex++;
                        }
                        catch
                        {
                            // Skip this monitor if we can't get info
                        }
                    }
                }

                // Fallback: Use System.Windows.Forms.Screen if WMI failed
                if (monitors.Count == 0)
                {
                    foreach (var screen in screens)
                    {
                        string resolution = nativeResolutions.ContainsKey(screen.DeviceName)
                            ? nativeResolutions[screen.DeviceName]
                            : $"{screen.Bounds.Width} x {screen.Bounds.Height}";

                        monitors.Add(new MonitorInfo
                        {
                            Name = screen.DeviceName,
                            Resolution = resolution,
                            RefreshRate = "60 Hz",
                            ScreenSize = "Unknown",
                            PanelType = "Unknown",
                            IsTouchScreen = isTouchEnabled
                        });
                    }
                }
            }
            catch
            {
                // Last resort fallback
                try
                {
                    foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                    {
                        monitors.Add(new MonitorInfo
                        {
                            Name = screen.DeviceName,
                            Resolution = $"{screen.Bounds.Width} x {screen.Bounds.Height}",
                            RefreshRate = "Unknown",
                            ScreenSize = "Unknown",
                            PanelType = "Unknown",
                            IsTouchScreen = false
                        });
                    }
                }
                catch
                {
                    // Ignore
                }
            }

            return monitors;
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        private const int ENUM_CURRENT_SETTINGS = -1;

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        private static string GetLastWindowsUpdate()
        {
            try
            {
                // Optimized: Only process updates, limit iteration
                using (var searcher = new ManagementObjectSearcher("SELECT HotFixID, InstalledOn FROM Win32_QuickFixEngineering"))
                {
                    searcher.Options.Timeout = new TimeSpan(0, 0, 5); // 5 second timeout

                    DateTime latestDate = DateTime.MinValue;
                    string latestUpdate = "Unknown";
                    int count = 0;
                    const int maxIterations = 50; // Limit iterations for performance

                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (++count > maxIterations) break; // Don't process too many updates

                        var installedOnStr = obj["InstalledOn"] != null ? obj["InstalledOn"].ToString() : null;
                        if (!string.IsNullOrWhiteSpace(installedOnStr))
                        {
                            DateTime installedOn;
                            if (DateTime.TryParse(installedOnStr, out installedOn))
                            {
                                if (installedOn > latestDate)
                                {
                                    latestDate = installedOn;
                                    var hotFixId = obj["HotFixID"] != null ? obj["HotFixID"].ToString() : "";
                                    latestUpdate = latestDate.ToString("yyyy-MM-dd") + " (" + hotFixId + ")";
                                }
                            }
                        }
                    }

                    if (latestDate != DateTime.MinValue)
                    {
                        return latestUpdate;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetLastWindowsUpdate] Error: {ex.Message}");
            }

            return "Unknown";
        }

        private static string GetWindowsDisplayVersion()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
            {
                var displayVersionValue = key != null ? key.GetValue("DisplayVersion") : null;
                return displayVersionValue == null ? "Unknown" : displayVersionValue.ToString();
            }
        }

        // Win32_VideoController.AdapterRAM is a 32-bit field: it saturates at 4 GB, so larger cards cannot be reported exactly through it.
        private static string FormatAdapterRam(ulong? bytes)
        {
            if (!bytes.HasValue || bytes.Value == 0)
            {
                return "Unknown";
            }

            return bytes.Value >= 0xFFFFFFF0UL ? "4 GB or more (WMI limit)" : FormatBytes(bytes.Value);
        }

        internal static string FormatBytes(ulong bytes)
        {
            return WmiValueParser.FormatBytes(bytes);
        }

        // GlobalMemoryStatusEx: authoritative physical memory totals and the system memory load (0-100).
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private sealed class MemoryStatusInfo
        {
            public ulong TotalBytes { get; set; }
            public ulong AvailableBytes { get; set; }
            public int? LoadPercent { get; set; }
        }

        private static MemoryStatusInfo GetMemoryStatus()
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
            if (!GlobalMemoryStatusEx(ref status) || status.ullTotalPhys == 0)
            {
                return new MemoryStatusInfo();
            }

            return new MemoryStatusInfo
            {
                TotalBytes = status.ullTotalPhys,
                AvailableBytes = status.ullAvailPhys,
                LoadPercent = (int)Math.Min(100u, status.dwMemoryLoad)
            };
        }

        private static T SafeExecute<T>(Func<T> func, string context)
        {
            return SafeExecute(func, context, default(T));
        }

        private static T SafeExecute<T>(Func<T> func, string context, T fallback)
        {
            if (CollectionSession.Current != null)
                return CollectionSession.Current.Read(func, context, fallback);
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                LogException(ex, context);
                return fallback;
            }
        }

        private static T SafeExecuteWithTimeout<T>(Func<T> func, string context, T fallback, int timeoutMs)
        {
            try
            {
                var task = System.Threading.Tasks.Task.Run(func);
                if (task.Wait(timeoutMs))
                {
                    return task.Result;
                }
                else
                {
                    Debug.WriteLine($"[{nameof(SystemInfoService)}] Timeout in {context} after {timeoutMs}ms");
                    return fallback;
                }
            }
            catch (Exception ex)
            {
                LogException(ex, context);
                return fallback;
            }
        }

        private static void LogException(Exception ex, string context)
        {
            Debug.WriteLine($"[{nameof(SystemInfoService)}] Error in {context}: {ex}");
        }
    }

    internal sealed class SystemInfoSnapshot
    {
        public SystemInfoSnapshot()
        {
            TimeZoneDisplayName = string.Empty;
            MachineName = string.Empty;
            UserName = string.Empty;
            Manufacturer = string.Empty;
            SystemModel = string.Empty;
            SerialNumber = string.Empty;
            AssetTag = string.Empty;
            BiosVersion = string.Empty;
            Cpu = string.Empty;
            CpuManufacturer = string.Empty;
            CpuProcessorId = string.Empty;
            CpuFamily = 0;
            CpuModel = 0;
            CpuCoreCount = 0;
            CpuLogicalProcessorCount = 0;
            InstalledPhysicalMemory = string.Empty;
            UsablePhysicalMemory = string.Empty;
            MemorySlotsUsed = 0;
            MemorySlotsTotal = 0;
            MemorySpeedMhz = 0;
            OsDisplay = string.Empty;
            OsInstallDate = string.Empty;
            Activation = ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
            Disks = new List<DiskInfo>();
            Gpus = new List<GpuInfo>();
            Monitors = new List<MonitorInfo>();
        }

        public IReadOnlyList<string> CollectionWarnings { get; set; } = Array.Empty<string>();
        public DateTime CapturedAt { get; set; }
        public string TimeZoneDisplayName { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string Manufacturer { get; set; }
        public string SystemModel { get; set; }
        public string SerialNumber { get; set; }
        public string AssetTag { get; set; }
        public string BiosVersion { get; set; }
        public string Cpu { get; set; }
        public string CpuManufacturer { get; set; }
        public string CpuProcessorId { get; set; }
        public int CpuFamily { get; set; }
        public int CpuModel { get; set; }
        public ulong? CpuL2CacheKb { get; set; }
        public ulong? CpuL3CacheKb { get; set; }
        public int CpuCoreCount { get; set; }
        public int CpuLogicalProcessorCount { get; set; }
        public string InstalledPhysicalMemory { get; set; }
        public string UsablePhysicalMemory { get; set; }
        /// <summary>Sum of installed module capacities in bytes (0 = unknown).</summary>
        public ulong InstalledPhysicalMemoryBytes { get; set; }
        /// <summary>Total physical memory usable by Windows in bytes (0 = unknown).</summary>
        public ulong UsablePhysicalMemoryBytes { get; set; }
        public ulong AvailablePhysicalMemoryBytes { get; set; }
        /// <summary>System memory load 0-100 captured at collection time; null if it could not be read.</summary>
        public int? MemoryLoadPercent { get; set; }
        public int OsBuildNumber { get; set; }
        public int MemorySlotsUsed { get; set; }
        public int MemorySlotsTotal { get; set; }
        public int MemorySpeedMhz { get; set; }
        public string OsDisplay { get; set; }
        public string OsInstallDate { get; set; }
        public ActivationResult Activation { get; set; }
        public string WindowsActivationStatus => Activation?.DisplayText ?? "Unknown";
        public TimeSpan WindowsUptime { get; set; } = TimeSpan.FromTicks(-1);
        public bool WindowsUptimeAvailable => WindowsUptime >= TimeSpan.Zero;
        public IReadOnlyList<DiskInfo> Disks { get; set; }
        public IReadOnlyList<GpuInfo> Gpus { get; set; }
        public IReadOnlyList<MonitorInfo> Monitors { get; set; }
        public BatteryInfo BatteryInfo { get; set; }

        public string WindowsUptimeDisplay
        {
            get { return WindowsUptimeAvailable ? SystemInfoService.FormatTimeSpan(WindowsUptime) : "Unknown"; }
        }
    }

    internal sealed class CpuDescriptor
    {
        public CpuDescriptor()
        {
            Name = string.Empty;
            Manufacturer = string.Empty;
            ProcessorId = string.Empty;
            Family = 0;
            Model = 0;
        }

        public string Name { get; set; }
        public ulong? L2CacheKb { get; set; }
        public ulong? L3CacheKb { get; set; }
        public string Manufacturer { get; set; }
        public string ProcessorId { get; set; }
        public int Family { get; set; }
        public int Model { get; set; }
    }

    internal sealed class DiskInfo
    {
        public DiskInfo()
        {
            Index = string.Empty;
            Model = string.Empty;
            Size = string.Empty;
            MediaType = string.Empty;
            InterfaceType = string.Empty;
        }

        public string Index { get; set; }
        public string Model { get; set; }
        public string Size { get; set; }
        /// <summary>Raw disk size in bytes (0 = unknown). Use this for any calculation; Size is display text only.</summary>
        public ulong SizeBytes { get; set; }
        public string MediaType { get; set; }
        public string InterfaceType { get; set; }
    }

    internal sealed class GpuInfo
    {
        public GpuInfo()
        {
            Name = string.Empty;
            Description = string.Empty;
            DriverVersion = string.Empty;
            VideoProcessor = string.Empty;
            AdapterRam = string.Empty;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string DriverVersion { get; set; }
        public string VideoProcessor { get; set; }
        public string AdapterRam { get; set; }
    }

    internal sealed class BatteryInfo
    {
        public BatteryInfo()
        {
            Name = string.Empty;
            Status = string.Empty;
            PowerStatus = string.Empty;
        }

        public string Name { get; set; }
        /// <summary>Charge 0-100, or null when the battery did not report it (do not show as 0%).</summary>
        public int? ChargeLevel { get; set; }
        /// <summary>Capacities in mWh; null when unknown.</summary>
        public int? DesignCapacity { get; set; }
        public int? FullChargeCapacity { get; set; }
        /// <summary>Full-charge / design capacity in percent; null when either capacity is unknown.</summary>
        public int? HealthPercentage { get; set; }
        public string Status { get; set; }
        public string PowerStatus { get; set; }
        /// <summary>Null when the status code does not say.</summary>
        public bool? IsCharging { get; set; }
    }

    internal sealed class OfficeInfo
    {
        public OfficeInfo()
        {
            Version = string.Empty;
            ProductName = string.Empty;
            ActivationStatus = string.Empty;
        }

        public bool IsInstalled { get; set; }
        public string Version { get; set; }
        public string ProductName { get; set; }
        public string ActivationStatus { get; set; }
    }

    internal sealed class MonitorInfo
    {
        public MonitorInfo()
        {
            Name = string.Empty;
            Resolution = string.Empty;
            RefreshRate = string.Empty;
            ScreenSize = string.Empty;
            PanelType = string.Empty;
        }

        public string Name { get; set; }
        public string Resolution { get; set; }
        public string RefreshRate { get; set; }
        public string ScreenSize { get; set; }
        public string PanelType { get; set; }
        public bool IsTouchScreen { get; set; }
    }
}
