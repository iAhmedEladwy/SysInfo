using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CNSysInfo
{
    public partial class frmSysInfo : Form
    {
        private SystemInfoSnapshot? _currentSnapshot;

        // DPI Awareness P/Invoke declarations
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        private enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }

        private enum SpecRatingLevel
        {
            Unknown,
            Flagship,           // 🏆 Elite tier
            Enthusiast,         // 💎 Prosumer tier
            PremiumPerformance, // ⭐ Premium tier
            HighPerformance,    // 🚀 High tier
            ModernStandard,     // ✅ Modern tier
            AdequatePerformance,// ⚡ Adequate tier
            BelowStandard,      // ⚠️ Below standard
            Outdated,           // 📉 Outdated
            Legacy,             // 🔻 Legacy
            Obsolete            // ❌ Obsolete
        }

        private enum StorageKind
        {
            Unknown,
            Nvme,
            SataSsd,
            Hdd
        }

        // P/Invoke for screenshot functionality
        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public frmSysInfo()
        {
            // Setup DPI awareness BEFORE InitializeComponent
            SetupDpiAwareness();
            
            InitializeComponent();
        }

        private void SetupDpiAwareness()
        {
            try
            {
                // Try Windows 8.1 and later method first
                SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_System_DPI_Aware);
            }
            catch
            {
                try
                {
                    // Fallback for Windows Vista through Windows 8
                    SetProcessDPIAware();
                }
                catch
                {
                    // If both fail, continue without explicit DPI awareness
                    Debug.WriteLine("[frmSysInfo] Could not set DPI awareness");
                }
            }
        }

        #region Event Handlers

        private void forceRestartOutlookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("TASKKILL /F /IM outlook.exe");
            RunCmdCommand("START Outlook");
        }

        private void forceRestartExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("TASKKILL /F /IM explorer.exe");
            RunCmdCommand("START Explorer");
        }

        private void forceRebootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("Shutdown -r -t 00");
        }

        private void diskManagementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("START diskmgmt.msc");
        }

        private void deviceManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("START devmgmt.msc");
        }

        private void networkConnectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("START ncpa.cpl");
        }

        private void controlPanelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("START control");
        }

        private void programsAndFeaturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("control.exe /name Microsoft.ProgramsAndFeatures");
        }

        private void computerManagementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("compmgmt.msc");
        }

        private void diskCleanupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("cleanmgr.exe");
        }

        private void taskManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("taskmgr.exe");
        }

        private void servicesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunCmdCommand("services.msc");
        }

        private void copyToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentSnapshot == null)
            {
                MessageBox.Show("System information isn't ready yet. Please wait a moment and try again.", "Copy unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ClipboardHelper.CopySystemInfoToClipboard(_currentSnapshot);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(frmSysInfo)}] Copy to clipboard failed: {ex}");
                MessageBox.Show("Failed to copy to clipboard. Please try again.", "Copy failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label9_Click(object sender, EventArgs e)
        {
            // Empty event handler
        }

        private async void frmSysInfo_Load(object sender, EventArgs e)
        {
            lblversion.Text = "ELADWYSOFT SysInfo " + Application.ProductVersion;
            
            // Show loading message
            textBox1.Text = "Loading system information, please wait...";
            txtOS.Text = "Loading...";
            txtBatteryHealth.Text = "Loading...";
            lblSpecRatingValue.Text = "Evaluating...";
            lblSpecRatingValue.BackColor = UnifiedColors.Unknown;
            lblSpecRatingValue.ForeColor = UnifiedColors.DefaultText;

            // Set MaximumSize to the current screen size for better adaptation
            this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;

            await LoadSystemInfoAsync();
        }

        private async void btnExport_Click(object sender, EventArgs e)
        {
            if (_currentSnapshot == null)
            {
                MessageBox.Show("System information isn't ready yet. Please wait a moment and try again.", "Export unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            menuStrip1.Visible = false;

            try
            {
                var exportDirectory = EnsureExportDirectory();
                var baseFileName = $"SysInfo_{SanitizeFileName(_currentSnapshot.SystemModel)}_{SanitizeFileName(_currentSnapshot.SerialNumber)}_{_currentSnapshot.CapturedAt:yyyyMMdd_HHmmss}";
                var textFilePath = Path.Combine(exportDirectory, baseFileName + ".txt");
                var screenshotPath = Path.Combine(exportDirectory, baseFileName + ".png");

                await Task.Run(() => SaveSnapshotToFile(_currentSnapshot, textFilePath));
                TakeAndSaveScreenshot(screenshotPath);

                MessageBox.Show($"Report and screenshot saved:\n{textFilePath}\n{screenshotPath}", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(frmSysInfo)}] Export failed: {ex}");
                MessageBox.Show("Export failed. Please check the logs for more information.", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                menuStrip1.Visible = true;
            }
        }

        #endregion

        #region System Info Loading

        private async Task LoadSystemInfoAsync()
        {
            Cursor = Cursors.WaitCursor;
            menuStrip1.Enabled = false;
            btnExport.Enabled = false;

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var snapshot = await Task.Run(SystemInfoService.Collect);
                stopwatch.Stop();
                
                Debug.WriteLine($"[{nameof(frmSysInfo)}] Data collection completed in {stopwatch.ElapsedMilliseconds}ms");
                
                _currentSnapshot = snapshot;
                UpdateUi(snapshot);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(frmSysInfo)}] Failed to load system info: {ex}");
                MessageBox.Show("Unable to load system information. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                menuStrip1.Enabled = true;
                btnExport.Enabled = true;
            }
        }

        #endregion

        #region UI Update

        private void UpdateUi(SystemInfoSnapshot snapshot)
        {
            // Basic Info
            txtTimeZone.Text = $"Timezone: {snapshot.TimeZoneDisplayName} | Captured: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss}";
            txtPCName.Text = $"PC Name: {snapshot.MachineName}";
            txtUserName.Text = $"Current user: {snapshot.UserName}";

            // Hardware Info
            txtSystemModel.Text = snapshot.SystemModel;
            txtSerialNumber.Text = snapshot.SerialNumber;
            txtManufacturer.Text = snapshot.Manufacturer;

            // Memory with progress bar
            var memoryText = $"{snapshot.InstalledPhysicalMemory} ({snapshot.UsablePhysicalMemory} usable)";
            if (snapshot.MemorySlotsTotal > 0)
            {
                var usedSlots = snapshot.MemorySlotsUsed;
                if (usedSlots < 0)
                    usedSlots = 0;
                if (usedSlots > snapshot.MemorySlotsTotal)
                    usedSlots = snapshot.MemorySlotsTotal;

                memoryText += $" | Slots: {usedSlots}/{snapshot.MemorySlotsTotal}";
            }
            if (snapshot.MemorySpeedMhz > 0)
            {
                memoryText += $" | Speed: {snapshot.MemorySpeedMhz} MHz";
            }
            txtMemory.Text = memoryText;

            // Update memory progress bar - calculate usage percentage
            int memoryUsagePercent = CalculateMemoryUsagePercentage(snapshot);
            pbMemory.Value = Math.Max(0, Math.Min(100, memoryUsagePercent));
            pbMemory.Text = $"{memoryUsagePercent}%";

            var memoryColor = UnifiedColors.GetMemoryUsageTier(memoryUsagePercent);
            pbMemory.ProgressColor = memoryColor;
            pbMemory.ForeColor = UnifiedColors.GetTextForTier(memoryColor);

            // Storage with progress bar
            var storageDetails = BuildStorageText(snapshot.Disks);
            txtStorage.Text = string.IsNullOrWhiteSpace(storageDetails)
                ? "No disks detected."
                : storageDetails;

            var diskUsage = GetDisk0Usage();

            if (diskUsage.HasData)
            {
                var usageSummary = $"Disk0 Usage: {FormatBytes(diskUsage.UsedBytes)} used / {FormatBytes(diskUsage.TotalBytes)} total ({diskUsage.UsagePercentage}% used)";
                txtStorage.Text = $"{txtStorage.Text}{Environment.NewLine}{usageSummary}";
            }

            var storageHealth = StorageHealthService.GetStorageHealth("0");
            UpdateStorageProgress(storageHealth, diskUsage);

            // Battery with progress bar
            txtBatteryHealth.Text = BuildBatteryText(snapshot.BatteryInfo);

            // Update battery progress bar
            if (snapshot.BatteryInfo != null)
            {
                int health = snapshot.BatteryInfo.HealthPercentage;
                pbBattery.Value = Math.Max(0, Math.Min(100, health));
                pbBattery.Text = $"{health}%";

                var healthColor = UnifiedColors.GetHealthTier(health);
                pbBattery.ProgressColor = healthColor;
                pbBattery.ForeColor = UnifiedColors.GetTextForTier(healthColor);
            }
            else
            {
                pbBattery.Value = 0;
                pbBattery.Text = "N/A";
                pbBattery.ProgressColor = UnifiedColors.Unknown;
                pbBattery.ForeColor = UnifiedColors.DefaultText;
            }

            txtCPU.Text = BuildCpuText(snapshot);
            txtAssetTag.Text = snapshot.AssetTag;
            txtGPU.Text = BuildGpuText(snapshot.Gpus);
            txtScreenInfo.Text = BuildScreenText(snapshot.Monitors);
            txtUptime.Text = $"Windows Uptime: {snapshot.WindowsUptimeDisplay}";

            // Software Info
            txtOS.Text = snapshot.OsDisplay;
            txtBiosVersion.Text = snapshot.BiosVersion;
            txtWinActivationStatus.Text = snapshot.WindowsActivationStatus;

            UpdateSpecRating(snapshot);
        }

        private int CalculateMemoryUsagePercentage(SystemInfoSnapshot snapshot)
        {
            try
            {
                // Extract total memory in GB
                var match = System.Text.RegularExpressions.Regex.Match(
                    snapshot.UsablePhysicalMemory,
                    @"([\d.]+)\s*GB");

                if (match.Success && double.TryParse(match.Groups[1].Value, out double totalGB))
                {
                    // Get current memory usage from Performance Counter
                    using (var memCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes"))
                    {
                        float availableMB = memCounter.NextValue();
                        float totalMB = (float)(totalGB * 1024);
                        float usedMB = totalMB - availableMB;
                        int usagePercent = (int)((usedMB / totalMB) * 100);

                        return Math.Max(0, Math.Min(100, usagePercent));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating memory usage: {ex.Message}");
            }

            return 0; // Default if calculation fails
        }

        private void UpdateStorageProgress(StorageHealthInfo storageHealth, DiskUsageInfo usageInfo)
        {
            var diskUsage = GetDisk0Usage();

            if (diskUsage.HasData)
            {
                int usagePercent = Math.Max(0, Math.Min(100, diskUsage.UsagePercentage));
                pbStorage.Value = usagePercent;
                pbStorage.Text = $"{usagePercent}% Used";

                var usageColor = UnifiedColors.GetMemoryUsageTier(usagePercent);
                pbStorage.ProgressColor = usageColor;
                pbStorage.ForeColor = UnifiedColors.GetTextForTier(usageColor);

                Debug.WriteLine($"[Storage] Showing usage: {usagePercent}% ({FormatBytes(diskUsage.UsedBytes)} / {FormatBytes(diskUsage.TotalBytes)})");
            }
            else
            {
                pbStorage.Value = 0;
                pbStorage.Text = "N/A";
                pbStorage.ProgressColor = UnifiedColors.Unknown;
                pbStorage.ForeColor = UnifiedColors.DefaultText;

                Debug.WriteLine("[Storage] No usage data available");
            }
        }

        private static string BuildCpuText(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
                return "Unknown";

            var cpuName = string.IsNullOrWhiteSpace(snapshot.Cpu) ? string.Empty : snapshot.Cpu.Trim();
            var details = new List<string>();

            if (snapshot.CpuCoreCount > 0)
                details.Add($"Cores: {snapshot.CpuCoreCount}");

            if (snapshot.CpuLogicalProcessorCount > 0)
                details.Add($"Threads: {snapshot.CpuLogicalProcessorCount}");

            if (string.IsNullOrEmpty(cpuName) && details.Count == 0)
                return "Unknown";

            if (details.Count == 0)
                return cpuName;

            if (string.IsNullOrEmpty(cpuName))
                return string.Join(" | ", details);

            return $"{cpuName} | {string.Join(" | ", details)}";
        }

        private static string BuildBatteryText(BatteryInfo battery)
        {
            if (battery == null || battery.Status == "Not Detected")
                return "No battery detected (Desktop PC).";

            var healthText = battery.HealthPercentage > 0 ? $"{battery.HealthPercentage}%" : "Unknown";
            var statusSuffix = string.IsNullOrWhiteSpace(battery.Status) ? string.Empty : $" - {battery.Status}";

            var capacityDetails = new List<string>();
            if (battery.DesignCapacity > 0)
                capacityDetails.Add($"Design {FormatBatteryCapacity(battery.DesignCapacity)}");
            if (battery.FullChargeCapacity > 0)
                capacityDetails.Add($"Full {FormatBatteryCapacity(battery.FullChargeCapacity)}");

            var capacitySuffix = capacityDetails.Count > 0 ? $" ({string.Join(", ", capacityDetails)})" : string.Empty;

            return $"{healthText}{statusSuffix}{capacitySuffix}";
        }

        private static string BuildStorageText(IReadOnlyList<DiskInfo> disks)
        {
            if (disks == null || disks.Count == 0)
                return "No disks detected.";

            var builder = new StringBuilder();
            foreach (var disk in disks)
            {
                builder.AppendLine($"Disk{disk.Index}: {disk.Model} | {disk.Size} | {disk.MediaType}");
            }

            return builder.ToString().TrimEnd();
        }

        private class DiskUsageInfo
        {
            public bool HasData { get; set; }
            public ulong TotalBytes { get; set; }
            public ulong UsedBytes { get; set; }
            public ulong FreeBytes { get; set; }
            public int UsagePercentage { get; set; }
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown";

            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = fileName;

            foreach (var c in invalid)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            sanitized = sanitized.Replace(' ', '_');

            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);

            return sanitized;
        }

        private static string FormatBytes(ulong bytes)
        {
            if (bytes == 0)
                return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            double value = bytes;
            int suffixIndex = 0;

            while (value >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                value /= 1024;
                suffixIndex++;
            }

            return $"{value:0.##} {suffixes[suffixIndex]}";
        }

        private DiskUsageInfo GetDisk0Usage()
        {
            var usage = new DiskUsageInfo();

            try
            {
                ulong totalBytes = 0;
                ulong freeBytes = 0;

                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, Size, FreeSpace, DriveType FROM Win32_LogicalDisk WHERE DriveType = 3"))
                {
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var deviceId = disk["DeviceID"]?.ToString();
                        if (string.IsNullOrWhiteSpace(deviceId))
                            continue;

                        var physicalDiskIndex = GetPhysicalDiskIndexForLogicalDrive(deviceId);

                        if (physicalDiskIndex == 0)
                        {
                            if (ulong.TryParse(disk["Size"]?.ToString(), out var sizeBytes) && sizeBytes > 0)
                            {
                                ulong free = 0;
                                ulong.TryParse(disk["FreeSpace"]?.ToString(), out free);

                                totalBytes += sizeBytes;
                                freeBytes += free;

                                Debug.WriteLine($"[Storage] Found {deviceId}: {FormatBytes(sizeBytes)} total, {FormatBytes(free)} free (Disk {physicalDiskIndex})");
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
                    usage.UsagePercentage = (int)Math.Round((double)usedBytes * 100.0 / totalBytes);
                    usage.HasData = true;

                    Debug.WriteLine($"[Storage] Disk 0 Total: {FormatBytes(totalBytes)}, Used: {FormatBytes(usedBytes)}, Usage: {usage.UsagePercentage}%");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Storage] Error calculating disk usage: {ex.Message}");
            }

            return usage;
        }

        private int GetPhysicalDiskIndexForLogicalDrive(string logicalDriveId)
        {
            try
            {
                var partitionQuery = $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{logicalDriveId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition";
                using (var partitionSearcher = new ManagementObjectSearcher(partitionQuery))
                {
                    foreach (ManagementObject partition in partitionSearcher.Get())
                    {
                        var partitionId = partition["DeviceID"]?.ToString();
                        if (string.IsNullOrWhiteSpace(partitionId))
                            continue;

                        var diskQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                        using (var diskSearcher = new ManagementObjectSearcher(diskQuery))
                        {
                            foreach (ManagementObject disk in diskSearcher.Get())
                            {
                                if (int.TryParse(disk["Index"]?.ToString(), out var diskIndex))
                                    return diskIndex;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Storage] Error resolving physical disk for {logicalDriveId}: {ex.Message}");
            }

            return -1;
        }

        private static string BuildGpuText(IReadOnlyList<GpuInfo> gpus)
        {
            if (gpus == null || gpus.Count == 0)
                return "No GPUs detected.";

            var builder = new StringBuilder();
            for (var index = 0; index < gpus.Count; index++)
            {
                var gpu = gpus[index];
                builder.AppendLine($"GPU{index + 1}: {gpu.Name} | VRAM: {gpu.AdapterRam}");
            }

            return builder.ToString().TrimEnd();
        }

        private static string BuildScreenText(IReadOnlyList<MonitorInfo> monitors)
        {
            if (monitors == null || monitors.Count == 0)
                return "No monitors detected.";

            var builder = new StringBuilder();
            for (var index = 0; index < monitors.Count; index++)
            {
                var monitor = monitors[index];
                var parts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(monitor.ScreenSize))
                    parts.Add(monitor.ScreenSize);
                
                if (!string.IsNullOrWhiteSpace(monitor.RefreshRate))
                    parts.Add($"Refresh: {monitor.RefreshRate}");
                
                if (!string.IsNullOrWhiteSpace(monitor.Resolution))
                    parts.Add($"Resolution: {monitor.Resolution}");
                
                parts.Add($"Touch: {(monitor.IsTouchScreen ? "Yes" : "No")}");
                
                if (parts.Count > 0)
                    builder.AppendLine($"Monitor{index + 1}: {string.Join(" | ", parts)}");
                else
                    builder.AppendLine($"Monitor{index + 1}: {monitor.Name}");
            }

            return builder.ToString().TrimEnd();
        }

        private static string FormatBatteryCapacity(int capacity)
        {
            return capacity > 0 ? string.Format("{0:N0} mWh", capacity) : "Unknown";
        }

        #endregion

        #region Spec Rating

        private void UpdateSpecRating(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                ApplySpecRating(SpecRatingLevel.AdequatePerformance);
                return;
            }

            var cpuSupport = CpuSupportEvaluator.Evaluate(snapshot);
            var memoryGb = ExtractMemoryInGigabytes(snapshot.InstalledPhysicalMemory);
            var primaryDisk = ResolvePrimaryDisk(snapshot.Disks);
            var storageKind = ClassifyStorageKind(primaryDisk);

            var hasCpuData = cpuSupport != CpuSupportLevel.Unknown;
            var hasMemoryData = memoryGb > 0;
            var hasDiskData = storageKind != StorageKind.Unknown;

            var level = EvaluateSpecRating(cpuSupport, hasCpuData, memoryGb, hasMemoryData, storageKind, hasDiskData);
            ApplySpecRating(level);
        }

        private static double ExtractMemoryInGigabytes(string memoryDisplay)
        {
            return ExtractCapacityInGigabytes(memoryDisplay);
        }

        private static DiskInfo ResolvePrimaryDisk(IReadOnlyList<DiskInfo> disks)
        {
            if (disks == null || disks.Count == 0)
                return null;

            DiskInfo primary = null;
            foreach (var disk in disks)
            {
                if (disk == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(disk.Index) &&
                    string.Equals(disk.Index.Trim(), "0", StringComparison.OrdinalIgnoreCase))
                {
                    return disk;
                }

                if (primary == null)
                    primary = disk;
            }

            return primary;
        }

        private static double ExtractCapacityInGigabytes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return -1;

            var match = Regex.Match(value, @"([0-9]+(?:\.[0-9]+)?)\s*(TB|GB|MB)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return -1;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                return -1;

            var unit = match.Groups[2].Value.ToUpperInvariant();
            switch (unit)
            {
                case "TB":
                    return numeric * 1024;
                case "GB":
                    return numeric;
                case "MB":
                    return numeric / 1024;
                default:
                    return -1;
            }
        }

        private static StorageKind ClassifyStorageKind(DiskInfo disk)
        {
            if (disk == null)
                return StorageKind.Unknown;

            var mediaType = (disk.MediaType ?? string.Empty).Trim();
            var model = (disk.Model ?? string.Empty).Trim();
            var interfaceType = (disk.InterfaceType ?? string.Empty).Trim();
            var combined = $"{mediaType} {model} {interfaceType}".ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(mediaType) &&
                string.IsNullOrWhiteSpace(model) &&
                string.IsNullOrWhiteSpace(interfaceType))
            {
                return StorageKind.Unknown;
            }

            if (mediaType.IndexOf("nvme", StringComparison.OrdinalIgnoreCase) >= 0)
                return StorageKind.Nvme;

            if (!string.IsNullOrWhiteSpace(interfaceType) &&
                interfaceType.IndexOf("pci", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (combined.Contains("ssd") || combined.Contains("solid state") || combined.Contains("non-volatile"))
                    return StorageKind.Nvme;
            }

            if (combined.Contains("nvme") || combined.Contains("non-volatile memory express") ||
                combined.Contains("pci express") || combined.Contains("pcie") || combined.Contains("pc i e"))
            {
                return StorageKind.Nvme;
            }

            if ((combined.Contains("m.2") || combined.Contains("m2")) && !combined.Contains("sata"))
                return StorageKind.Nvme;

            if (combined.Contains("sata") && combined.Contains("ssd"))
                return StorageKind.SataSsd;

            if (mediaType.IndexOf("sata", StringComparison.OrdinalIgnoreCase) >= 0 &&
                mediaType.IndexOf("ssd", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StorageKind.SataSsd;
            }

            if ((mediaType.Equals("SSD", StringComparison.OrdinalIgnoreCase) ||
                mediaType.Equals("Solid State Disk", StringComparison.OrdinalIgnoreCase)))
            {
                if (interfaceType.IndexOf("pci", StringComparison.OrdinalIgnoreCase) >= 0)
                    return StorageKind.Nvme;

                if (combined.Contains("sata"))
                    return StorageKind.SataSsd;
            }

            if (combined.Contains("ssd"))
                return StorageKind.SataSsd;

            if (combined.Contains("hdd") || combined.Contains("hard disk") || combined.Contains("hard-disk"))
                return StorageKind.Hdd;

            if (mediaType.IndexOf("hdd", StringComparison.OrdinalIgnoreCase) >= 0)
                return StorageKind.Hdd;

            return StorageKind.Unknown;
        }

        private static SpecRatingLevel EvaluateSpecRating(
            CpuSupportLevel cpuSupport,
            bool hasCpuData,
            double memoryGb,
            bool hasMemoryData,
            StorageKind storageKind,
            bool hasDiskData)
        {
            bool hasWin11Cpu = cpuSupport == CpuSupportLevel.Supported;
            bool hasOldCpu = cpuSupport == CpuSupportLevel.Unsupported;
            bool hasNvme = storageKind == StorageKind.Nvme;
            bool hasSataSsd = storageKind == StorageKind.SataSsd;
            bool hasAnySsd = hasNvme || hasSataSsd;
            bool hasHdd = storageKind == StorageKind.Hdd;

            // Tier 1: FLAGSHIP (Top 1%)
            // Requirements: Win11 CPU + 32GB+ RAM + NVMe
            if (hasWin11Cpu && memoryGb >= 32 && hasNvme)
            {
                return SpecRatingLevel.Flagship;
            }

            // Tier 2: ENTHUSIAST (Top 5%)
            // Requirements: Win11 CPU + 24GB+ RAM + NVMe, OR Win11 CPU + 32GB+ + SATA SSD
            if (hasWin11Cpu && ((memoryGb >= 24 && hasNvme) || (memoryGb >= 32 && hasSataSsd)))
            {
                return SpecRatingLevel.Enthusiast;
            }

            // Tier 3: PREMIUM PERFORMANCE (Top 15%)
            // Requirements: Win11 CPU + 16-23GB RAM + NVMe
            if (hasWin11Cpu && memoryGb >= 16 && memoryGb < 24 && hasNvme)
            {
                return SpecRatingLevel.PremiumPerformance;
            }

            // Tier 4: HIGH PERFORMANCE (Top 30%)
            // Requirements: Win11 CPU + 16GB+ RAM + SATA SSD, OR Win11 CPU + 24GB+ + Any SSD
            if (hasWin11Cpu && ((memoryGb >= 16 && hasSataSsd) || (memoryGb >= 24 && hasAnySsd)))
            {
                return SpecRatingLevel.HighPerformance;
            }

            // Tier 5: MODERN STANDARD (Top 50%)
            // Requirements: Win11 CPU + 12-16GB RAM + Any SSD
            if (hasWin11Cpu && memoryGb >= 12 && memoryGb < 16 && hasAnySsd)
            {
                return SpecRatingLevel.ModernStandard;
            }

            // Tier 6: ADEQUATE PERFORMANCE (Average)
            // Requirements: Win11 CPU + 8-12GB RAM + SSD, OR Old CPU + 16GB+ + NVMe
            if ((hasWin11Cpu && memoryGb >= 8 && memoryGb < 12 && hasAnySsd) ||
                (hasOldCpu && memoryGb >= 16 && hasNvme))
            {
                return SpecRatingLevel.AdequatePerformance;
            }

            // Tier 7: BELOW STANDARD (Below Average)
            // Requirements: Win11 CPU + <8GB + SSD, OR Win11 CPU + Any RAM + HDD, OR Old CPU + 12GB+ + SSD
            if ((hasWin11Cpu && memoryGb > 0 && memoryGb < 8 && hasAnySsd) ||
                (hasWin11Cpu && hasHdd) ||
                (hasOldCpu && memoryGb >= 12 && hasAnySsd))
            {
                return SpecRatingLevel.BelowStandard;
            }

            // Tier 8: OUTDATED (Old but usable)
            // Requirements: Old CPU + 8-12GB RAM + Any storage, OR Old CPU + <8GB + SSD
            if (hasOldCpu && ((memoryGb >= 8 && memoryGb < 12) || (memoryGb < 8 && hasAnySsd)))
            {
                return SpecRatingLevel.Outdated;
            }

            // Tier 9: LEGACY (Very old)
            // Requirements: Old CPU + 6-8GB RAM + HDD, OR Old CPU + 4-6GB + SSD
            if (hasOldCpu && ((memoryGb >= 6 && memoryGb < 8 && hasHdd) || (memoryGb >= 4 && memoryGb < 6 && hasAnySsd)))
            {
                return SpecRatingLevel.Legacy;
            }

            // Tier 10: OBSOLETE (Critical - needs immediate replacement)
            // Requirements: Old CPU + ≤4GB RAM, OR Old CPU + <6GB + HDD
            if (hasOldCpu && (memoryGb <= 4 || (memoryGb < 6 && hasHdd)))
            {
                return SpecRatingLevel.Obsolete;
            }

            // Default to Adequate if we can't determine
            return SpecRatingLevel.AdequatePerformance;
        }

        private void ApplySpecRating(SpecRatingLevel level)
        {
            var upgradeRecommendations = BuildUpgradeRecommendations();

            switch (level)
            {
                case SpecRatingLevel.Flagship:
                    lblSpecRatingValue.Text = "🏆 Flagship - Elite workstation for intensive tasks";
                    lblSpecRatingValue.BackColor = UnifiedColors.Flagship;
                    lblSpecRatingValue.ForeColor = UnifiedColors.FlagshipText;
                    break;

                case SpecRatingLevel.Enthusiast:
                    lblSpecRatingValue.Text = "💎 Enthusiast - Excellent for prosumer workloads";
                    lblSpecRatingValue.BackColor = UnifiedColors.Enthusiast;
                    lblSpecRatingValue.ForeColor = UnifiedColors.EnthusiastText;
                    break;

                case SpecRatingLevel.PremiumPerformance:
                    lblSpecRatingValue.Text = "⭐ Premium Performance - Excellent for demanding workloads";
                    lblSpecRatingValue.BackColor = UnifiedColors.Premium;
                    lblSpecRatingValue.ForeColor = UnifiedColors.PremiumText;
                    break;

                case SpecRatingLevel.HighPerformance:
                    lblSpecRatingValue.Text = "🚀 High Performance - Great for multitasking & productivity";
                    lblSpecRatingValue.BackColor = UnifiedColors.HighPerformance;
                    lblSpecRatingValue.ForeColor = UnifiedColors.HighPerformanceText;
                    break;

                case SpecRatingLevel.ModernStandard:
                    lblSpecRatingValue.Text = "✅ Modern Standard - Ready for everyday tasks";
                    lblSpecRatingValue.BackColor = UnifiedColors.ModernStandard;
                    lblSpecRatingValue.ForeColor = UnifiedColors.ModernStandardText;
                    break;

                case SpecRatingLevel.AdequatePerformance:
                    lblSpecRatingValue.Text = "✓ Adequate - Meets minimum requirements for modern use" +
                        (string.IsNullOrEmpty(upgradeRecommendations) ? "" : " - To improve: " + upgradeRecommendations);
                    lblSpecRatingValue.BackColor = UnifiedColors.Adequate;
                    lblSpecRatingValue.ForeColor = UnifiedColors.AdequateText;
                    break;

                case SpecRatingLevel.BelowStandard:
                    lblSpecRatingValue.Text = "⚠️ Below Standard - Upgrade recommended: " +
                        (string.IsNullOrEmpty(upgradeRecommendations) ? "Multiple components" : upgradeRecommendations);
                    lblSpecRatingValue.BackColor = UnifiedColors.BelowStandard;
                    lblSpecRatingValue.ForeColor = UnifiedColors.BelowStandardText;
                    break;

                case SpecRatingLevel.Outdated:
                    lblSpecRatingValue.Text = "📉 Outdated - Significant upgrades needed: " +
                        (string.IsNullOrEmpty(upgradeRecommendations) ? "CPU, RAM, Storage" : upgradeRecommendations);
                    lblSpecRatingValue.BackColor = UnifiedColors.Outdated;
                    lblSpecRatingValue.ForeColor = UnifiedColors.OutdatedText;
                    break;

                case SpecRatingLevel.Legacy:
                    lblSpecRatingValue.Text = "🔻 Legacy System - Full system replacement strongly advised";
                    lblSpecRatingValue.BackColor = UnifiedColors.Legacy;
                    lblSpecRatingValue.ForeColor = UnifiedColors.LegacyText;
                    break;

                case SpecRatingLevel.Obsolete:
                    lblSpecRatingValue.Text = "❌ Obsolete - Critical: Immediate replacement required";
                    lblSpecRatingValue.BackColor = UnifiedColors.Obsolete;
                    lblSpecRatingValue.ForeColor = UnifiedColors.ObsoleteText;
                    break;

                default:
                    lblSpecRatingValue.Text = "❓ Unable to determine system rating";
                    lblSpecRatingValue.BackColor = Color.LightGray;
                    lblSpecRatingValue.ForeColor = Color.Black;
                    break;
            }
        }

        private string BuildUpgradeRecommendations()
{
    if (_currentSnapshot == null)
        return string.Empty;

    var recommendations = new List<string>();
    var cpuSupport = CpuSupportEvaluator.Evaluate(_currentSnapshot);
    var memoryGb = ExtractMemoryInGigabytes(_currentSnapshot.InstalledPhysicalMemory);
    var primaryDisk = ResolvePrimaryDisk(_currentSnapshot.Disks);
    var storageKind = ClassifyStorageKind(primaryDisk);

    // CPU recommendations
    if (cpuSupport == CpuSupportLevel.Unsupported)
    {
        recommendations.Add("CPU to Win11-compatible (8th gen Intel/Ryzen 2000+)");
    }

    // RAM recommendations (more granular)
    if (memoryGb > 0)
    {
        if (memoryGb < 8)
            recommendations.Add($"RAM to 16GB (add {16 - (int)memoryGb}GB)");
        else if (memoryGb < 16)
            recommendations.Add($"RAM to 16GB (add {16 - (int)memoryGb}GB for better performance)");
        else if (memoryGb < 24 && storageKind == StorageKind.Nvme)
            recommendations.Add($"RAM to 24GB (add {24 - (int)memoryGb}GB for enthusiast tier)");
    }

    // Storage recommendations (more specific)
    if (storageKind == StorageKind.Hdd)
    {
        if (cpuSupport == CpuSupportLevel.Supported && memoryGb >= 16)
            recommendations.Add("Storage to Gen4 NVMe SSD (5000MB/s+)");
        else if (cpuSupport == CpuSupportLevel.Supported)
            recommendations.Add("Storage to NVMe SSD");
        else
            recommendations.Add("Storage to SATA SSD minimum");
    }
    else if (storageKind == StorageKind.SataSsd && cpuSupport == CpuSupportLevel.Supported && memoryGb >= 16)
    {
        recommendations.Add("Storage to NVMe (3x faster than SATA)");
    }

    return string.Join(", ", recommendations);
}

        #endregion

        #region Export and Screenshot

        private static void SaveSnapshotToFile(SystemInfoSnapshot snapshot, string filePath)
        {
            var builder = new StringBuilder();
            
            // Basic Information
            builder.AppendLine("=== SYSTEM INFORMATION ===");
            builder.AppendLine();
            builder.AppendLine($"Date: {snapshot.CapturedAt}");
            builder.AppendLine($"Timezone: {snapshot.TimeZoneDisplayName}");
            builder.AppendLine($"PC Name: {snapshot.MachineName}");
            builder.AppendLine($"Current user: {snapshot.UserName}");
            builder.AppendLine();
            
            // Operating System
            builder.AppendLine("=== OPERATING SYSTEM ===");
            builder.AppendLine($"OS: {snapshot.OsDisplay}");
            builder.AppendLine($"OS Installed On: {snapshot.OsInstallDate}");
            builder.AppendLine($"Windows Activation Status: {snapshot.WindowsActivationStatus}");
            builder.AppendLine($"Uptime: {snapshot.WindowsUptimeDisplay}");
            builder.AppendLine();
            
            // Hardware
            builder.AppendLine("=== HARDWARE ===");
            builder.AppendLine($"Manufacturer: {snapshot.Manufacturer}");
            builder.AppendLine($"Model: {snapshot.SystemModel}");
            builder.AppendLine($"Serial Number: {snapshot.SerialNumber}");
            builder.AppendLine($"Asset Tag: {snapshot.AssetTag}");
            builder.AppendLine($"BIOS Version: {snapshot.BiosVersion}");
            builder.AppendLine($"CPU: {BuildCpuText(snapshot)}");
            
            var memoryLine = $"{snapshot.InstalledPhysicalMemory} ({snapshot.UsablePhysicalMemory} usable)";
            if (snapshot.MemorySlotsTotal > 0)
            {
                var usedSlots = snapshot.MemorySlotsUsed;
                if (usedSlots < 0)
                    usedSlots = 0;
                if (usedSlots > snapshot.MemorySlotsTotal)
                    usedSlots = snapshot.MemorySlotsTotal;
                memoryLine += $" | Slots: {usedSlots}/{snapshot.MemorySlotsTotal}";
            }
            if (snapshot.MemorySpeedMhz > 0)
                memoryLine += $" | Speed: {snapshot.MemorySpeedMhz} MHz";
            
            builder.AppendLine($"Memory: {memoryLine}");
            builder.AppendLine();
            
            // Storage
            builder.AppendLine("=== STORAGE ===");
            foreach (var disk in snapshot.Disks)
            {
                builder.AppendLine($"Disk{disk.Index}: {disk.Model} | {disk.Size} | {disk.MediaType}");
            }
            builder.AppendLine();
            
            // Graphics
            builder.AppendLine("=== GRAPHICS ===");
            for (var index = 0; index < snapshot.Gpus.Count; index++)
            {
                var gpu = snapshot.Gpus[index];
                builder.AppendLine($"GPU{index + 1}: {gpu.Name} | VRAM: {gpu.AdapterRam}");
            }
            builder.AppendLine();
            
            // Battery
            if (snapshot.BatteryInfo != null && snapshot.BatteryInfo.Status != "Not Detected")
            {
                builder.AppendLine("=== BATTERY ===");
                var healthText = snapshot.BatteryInfo.HealthPercentage > 0
                    ? $"{snapshot.BatteryInfo.HealthPercentage}%"
                    : "Unknown";
                builder.AppendLine($"Health: {healthText}");
                if (snapshot.BatteryInfo.DesignCapacity > 0)
                    builder.AppendLine($"Design Capacity: {FormatBatteryCapacity(snapshot.BatteryInfo.DesignCapacity)}");
                if (snapshot.BatteryInfo.FullChargeCapacity > 0)
                    builder.AppendLine($"Full Charge Capacity: {FormatBatteryCapacity(snapshot.BatteryInfo.FullChargeCapacity)}");
                builder.AppendLine($"Status: {snapshot.BatteryInfo.Status}");
                builder.AppendLine($"Power: {snapshot.BatteryInfo.PowerStatus}");
                builder.AppendLine();
            }
            
            builder.AppendLine($"App Version: v{Application.ProductVersion}");

            File.WriteAllText(filePath, builder.ToString());
        }

        private static string EnsureExportDirectory()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.CreateDirectory(directory);
            return directory;
        }

        private void TakeAndSaveScreenshot(string filePath)
        {
            Bitmap? bitmap = null;
            try
            {
                if (!TryCaptureUsingPrintWindow(Handle, out bitmap))
                {
                    bitmap?.Dispose();
                    bitmap = CaptureUsingDpiAwareCopy();
                }

                if (bitmap == null)
                    throw new InvalidOperationException("Unable to capture window image.");

                bitmap.Save(filePath, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(frmSysInfo)}] Screenshot failed: {ex}");
                throw;
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        private Bitmap CaptureUsingDpiAwareCopy()
        {
            using (var dpiGraphics = CreateGraphics())
            {
                var dpiX = dpiGraphics.DpiX;
                var dpiY = dpiGraphics.DpiY;
                var scaleX = dpiX / 96f;
                var scaleY = dpiY / 96f;
                var captureWidth = Math.Max(1, (int)Math.Round(Width * scaleX));
                var captureHeight = Math.Max(1, (int)Math.Round(Height * scaleY));
                var captureSize = new Size(captureWidth, captureHeight);

                var bitmap = new Bitmap(captureWidth, captureHeight, PixelFormat.Format32bppArgb);
                bitmap.SetResolution(dpiX, dpiY);

                using (var graphics = Graphics.FromImage(bitmap))
                {
                    var location = PointToScreen(Point.Empty);
                    graphics.CopyFromScreen(location, Point.Empty, captureSize);
                }

                return bitmap;
            }
        }

        private bool TryCaptureUsingPrintWindow(IntPtr hwnd, out Bitmap? bitmap)
        {
            bitmap = null;
            if (hwnd == IntPtr.Zero)
                return false;

            if (!GetWindowRect(hwnd, out var rect))
                return false;

            rect = NormalizeWindowRect(hwnd, rect);

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return false;

            bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var hdc = graphics.GetHdc();
                try
                {
                    if (PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                        return true;

                    if (PrintWindow(hwnd, hdc, 0))
                        return true;
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }

            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }
            return false;
        }

        private static RECT NormalizeWindowRect(IntPtr hwnd, RECT rect)
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out var dwmRect, Marshal.SizeOf(typeof(RECT))) == 0)
                        return dwmRect;
                }
            }
            catch (DllNotFoundException)
            {
                // DWM not available on this OS
            }
            catch (EntryPointNotFoundException)
            {
                // Attribute unavailable
            }

            return rect;
        }

        #endregion

        #region Command Execution

        private void RunCmdCommand(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C " + command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        MessageBox.Show("Unable to start the command process.", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                        Debug.WriteLine($"Command '{command}' output:{Environment.NewLine}{output}");

                    if (!string.IsNullOrWhiteSpace(error))
                        Debug.WriteLine($"Command '{command}' error:{Environment.NewLine}{error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(frmSysInfo)}] Command '{command}' failed: {ex}");
                MessageBox.Show("Error executing command. Check logs for details.", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }

    #region Unified Color Palette (10-Tier System)

    public static class UnifiedColors
    {
        // === SPEC RATING COLORS (10 Tiers) ===

        // Tier 1: Flagship (Top 1%)
        public static readonly Color Flagship = Color.FromArgb(75, 0, 130);        // Deep Indigo
        public static readonly Color FlagshipText = Color.White;

        // Tier 2: Enthusiast (Top 5%)
        public static readonly Color Enthusiast = Color.FromArgb(0, 120, 215);     // Windows Blue #0078D7
        public static readonly Color EnthusiastText = Color.White;

        // Tier 3: Premium Performance (Top 15%)
        public static readonly Color Premium = Color.FromArgb(0, 150, 136);        // Teal
        public static readonly Color PremiumText = Color.White;

        // Tier 4: High Performance (Top 30%)
        public static readonly Color HighPerformance = Color.FromArgb(76, 175, 80); // Material Green
        public static readonly Color HighPerformanceText = Color.White;

        // Tier 5: Modern Standard (Top 50%)
        public static readonly Color ModernStandard = Color.FromArgb(144, 238, 144); // Light Green
        public static readonly Color ModernStandardText = Color.DarkGreen;

        // Tier 6: Adequate Performance (Average) - Entry Good
        public static readonly Color Adequate = Color.FromArgb(156, 204, 101);     // Light Lime (positive green)
        public static readonly Color AdequateText = Color.FromArgb(51, 105, 30);   // Dark Green

        // === BELOW THIS LINE = NOT RECOMMENDED ===

        // Tier 7: Below Standard (Below Average)
        public static readonly Color BelowStandard = Color.FromArgb(255, 235, 59); // Yellow (Warning)
        public static readonly Color BelowStandardText = Color.FromArgb(80, 60, 0);

        // Tier 8: Outdated (Old)
        public static readonly Color Outdated = Color.FromArgb(255, 200, 124);     // Light Orange
        public static readonly Color OutdatedText = Color.FromArgb(139, 69, 19);

        // Tier 9: Legacy (Very Old)
        public static readonly Color Legacy = Color.FromArgb(255, 165, 79);        // Coral
        public static readonly Color LegacyText = Color.DarkRed;

        // Tier 10: Obsolete (Critical)
        public static readonly Color Obsolete = Color.FromArgb(255, 182, 193);     // Light Pink
        public static readonly Color ObsoleteText = Color.DarkRed;

        // === PROGRESS BAR COLORS ===

        public static readonly Color HealthExcellent = Flagship;
        public static readonly Color HealthVeryGood = Enthusiast;
        public static readonly Color HealthGood = Premium;
        public static readonly Color HealthFair = HighPerformance;
        public static readonly Color HealthAdequate = ModernStandard;
        public static readonly Color HealthPoor = BelowStandard;
        public static readonly Color HealthCritical = Outdated;
        public static readonly Color HealthSevere = Obsolete;

        public static readonly Color UsageLow = Flagship;
        public static readonly Color UsageModerate = Enthusiast;
        public static readonly Color UsageHigh = BelowStandard;
        public static readonly Color UsageVeryHigh = Outdated;
        public static readonly Color UsageCritical = Legacy;

        public static readonly Color Unknown = Color.FromArgb(148, 163, 184);
        public static readonly Color DefaultText = Color.Black;
        public static readonly Color Background = Color.FromArgb(240, 240, 240);
        public static readonly Color Surface = Color.White;
        public static readonly Color ProgressBackground = Color.FromArgb(240, 240, 240);

        // === HELPER METHODS ===

        public static Color GetHealthTier(int percentage)
        {
            percentage = Math.Max(0, Math.Min(100, percentage));

            if (percentage >= 95) return HealthExcellent;
            if (percentage >= 85) return HealthVeryGood;
            if (percentage >= 75) return HealthGood;
            if (percentage >= 65) return HealthFair;
            if (percentage >= 55) return HealthAdequate;
            if (percentage >= 45) return HealthPoor;
            if (percentage >= 35) return HealthCritical;
            return HealthSevere;
        }

        public static Color GetMemoryUsageTier(int percentage)
        {
            percentage = Math.Max(0, Math.Min(100, percentage));

            if (percentage < 30) return UsageLow;
            if (percentage < 50) return UsageModerate;
            if (percentage < 70) return UsageHigh;
            if (percentage < 85) return UsageVeryHigh;
            return UsageCritical;
        }

        public static Color GetTextForTier(Color backgroundColor)
        {
            if (backgroundColor == Flagship || backgroundColor == Enthusiast ||
                backgroundColor == Premium || backgroundColor == HighPerformance)
            {
                return Color.White;
            }

            if (backgroundColor == ModernStandard || backgroundColor == Adequate)
                return Color.FromArgb(0, 100, 0);

            if (backgroundColor == BelowStandard)
                return BelowStandardText;

            if (backgroundColor == Outdated)
                return OutdatedText;

            if (backgroundColor == Legacy || backgroundColor == Obsolete)
                return Color.DarkRed;

            return DefaultText;
        }

        public static Color GetBackgroundForTier(Color tierColor)
        {
            return tierColor;
        }
    }

    #endregion
}
