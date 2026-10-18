using System;
using System.Collections.Generic;
using System.IO;

namespace CNSysInfo
{
    internal sealed class UiSnapshot
    {
        public SystemInfoSnapshot System { get; set; } = new SystemInfoSnapshot();
        /// <summary>RAM load 0-100 from the system snapshot (GlobalMemoryStatusEx), null when unreadable.</summary>
        public int? MemoryUsagePercentage { get { return System.MemoryLoadPercent; } }
        public DiskUsageInfo DiskUsage { get; set; } = new DiskUsageInfo();
        public string SystemDriveRoot { get; set; } = string.Empty;
        public DiskUsageInfo SystemDriveUsage { get; set; } = new DiskUsageInfo();
        public StorageHealthInfo StorageHealth { get; set; } = new StorageHealthInfo();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Worker-side collection envelope. Everything that can block (WMI, SMART) runs here inside Task.Run,
    /// never on the UI thread. Query logic lives in SystemInfoService and StorageHealthService (single implementation).
    /// </summary>
    internal static class UiSnapshotService
    {
        public static UiSnapshot Collect()
        {
            var result = new UiSnapshot { System = SystemInfoService.Collect() };
            using var collection = new CollectionSession(12000);
            result.SystemDriveRoot = Path.GetPathRoot(Environment.SystemDirectory);
            result.SystemDriveUsage = collection.Read(() =>
            {
                var drive = new DriveInfo(result.SystemDriveRoot);
                if (!drive.IsReady) throw new IOException("Windows system volume is not ready.");
                return DiskUsageInfo.FromTotals((ulong)drive.TotalSize, (ulong)drive.AvailableFreeSpace);
            }, "System drive free space", new DiskUsageInfo(), 1000);
            result.DiskUsage = collection.Read(() => StorageHealthService.CalculateDiskUsage("0"),
                "Disk0 usage", new DiskUsageInfo(), 4000);
            result.StorageHealth = collection.Read(() => StorageHealthService.GetStorageHealth("0"),
                "Disk0 health", result.StorageHealth, 8000);
            if (!result.DiskUsage.HasData && collection.Warnings.Count == 0)
                collection.Warnings.Add("Disk0 usage: no readable volumes on this disk");
            if (result.System.MemoryLoadPercent == null) collection.Warnings.Add("Memory usage: data unavailable");
            // Unknown health is a normal answer (NVMe / no SMART), so it is shown as "Unknown" but not counted as a failed read.
            var warnings = new List<string>(result.System.CollectionWarnings);
            warnings.AddRange(collection.Warnings);
            result.Warnings = warnings.ToArray();
            return result;
        }
    }
}
