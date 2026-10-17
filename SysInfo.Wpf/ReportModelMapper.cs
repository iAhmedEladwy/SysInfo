using System.Linq;

namespace CNSysInfo
{
    internal static class ReportModelMapper
    {
        public static ReportModel From(SystemInfoSnapshot s)
        {
            var m = new ReportModel
            {
                CapturedAt = s.CapturedAt, TimeZoneDisplayName = s.TimeZoneDisplayName, MachineName = s.MachineName,
                UserName = s.UserName, Manufacturer = s.Manufacturer, SystemModel = s.SystemModel,
                SerialNumber = s.SerialNumber, AssetTag = s.AssetTag, BiosVersion = s.BiosVersion, Cpu = s.Cpu,
                OsDisplay = s.OsDisplay, OsInstallDate = s.OsInstallDate, WindowsActivationStatus = s.WindowsActivationStatus,
                UptimeDisplay = s.WindowsUptimeDisplay, InstalledPhysicalMemory = s.InstalledPhysicalMemory, InstalledPhysicalMemoryBytes = s.InstalledPhysicalMemoryBytes,
                UsablePhysicalMemory = s.UsablePhysicalMemory, CpuCoreCount = s.CpuCoreCount,
                CpuLogicalProcessorCount = s.CpuLogicalProcessorCount, MemorySlotsUsed = s.MemorySlotsUsed,
                CpuL2CacheKb = s.CpuL2CacheKb, CpuL3CacheKb = s.CpuL3CacheKb,
                MemorySlotsTotal = s.MemorySlotsTotal, MemorySpeedMhz = s.MemorySpeedMhz,
                CpuSupport = CpuSupportEvaluator.Evaluate(s)
            };
            foreach (var d in s.Disks)
                m.Disks.Add(new ReportDisk { Index = d.Index, Model = d.Model, Size = d.Size, MediaType = d.MediaType, InterfaceType = d.InterfaceType });
            foreach (var g in s.Gpus)
                m.Gpus.Add(new ReportGpu { Name = g.Name, AdapterRam = g.AdapterRam });
            if (s.BatteryInfo != null && s.BatteryInfo.Status != "Not Detected")
                m.Battery = new ReportBattery { HealthPercentage = s.BatteryInfo.HealthPercentage.GetValueOrDefault(), DesignCapacity = s.BatteryInfo.DesignCapacity.GetValueOrDefault(),
                    FullChargeCapacity = s.BatteryInfo.FullChargeCapacity.GetValueOrDefault(), Status = s.BatteryInfo.Status, PowerStatus = s.BatteryInfo.PowerStatus };
            return m;
        }
    }
}
