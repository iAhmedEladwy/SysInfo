using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
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

        private enum SpecRatingLevel
        {
            Unknown,
            Approved,
            Low,
            Obsolete
        }

        private enum StorageKind
        {
            Unknown,
            Nvme,
            SataSsd,
            Hdd
        }

        public frmSysInfo()
        {
            InitializeComponent();
        }
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

        }
        private async void frmSysInfo_Load(object sender, EventArgs e)
        {
            lblversion.Text = "ELADWYSOFT SysInfo " + Application.ProductVersion;
            // Show loading message
            textBox1.Text = "Loading system information, please wait...";
            txtOS.Text = "Loading...";
            txtBatteryHealth.Text = "Loading...";
            lblSpecRatingValue.Text = "Evaluating...";
            lblSpecRatingValue.BackColor = Color.LightGray;
            lblSpecRatingValue.ForeColor = Color.Black;

            // Set MaximumSize to the current screen size for better adaptation
            this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;

            await LoadSystemInfoAsync();
        }

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
                menuStrip1.Enabled = true;
                btnExport.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

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
            var memoryText = $"{snapshot.InstalledPhysicalMemory} ({snapshot.UsablePhysicalMemory} usable)";
            if (snapshot.MemorySlotsTotal > 0)
            {
                var usedSlots = snapshot.MemorySlotsUsed;
                if (usedSlots < 0)
                {
                    usedSlots = 0;
                }
                if (usedSlots > snapshot.MemorySlotsTotal)
                {
                    usedSlots = snapshot.MemorySlotsTotal;
                }

                memoryText += $" | Slots: {usedSlots}/{snapshot.MemorySlotsTotal}";
            }
            if (snapshot.MemorySpeedMhz > 0)
            {
                memoryText += $" | Speed: {snapshot.MemorySpeedMhz} MHz";
            }
            txtMemory.Text = memoryText;
            txtStorage.Text = BuildStorageText(snapshot.Disks);
            txtCPU.Text = BuildCpuText(snapshot);
            txtAssetTag.Text = snapshot.AssetTag;
            txtGPU.Text = BuildGpuText(snapshot.Gpus);
            txtScreenInfo.Text = BuildScreenText(snapshot.Monitors);
            txtUptime.Text = $"Windows Uptime: {snapshot.WindowsUptimeDisplay}";

            // Software Info
            txtOS.Text = snapshot.OsDisplay;
            txtBiosVersion.Text = snapshot.BiosVersion;
            txtWinActivationStatus.Text = snapshot.WindowsActivationStatus;
            txtBatteryHealth.Text = BuildBatteryText(snapshot.BatteryInfo);

            UpdateSpecRating(snapshot);
        }
        
        private static string BuildCpuText(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "Unknown";
            }

            var cpuName = string.IsNullOrWhiteSpace(snapshot.Cpu) ? string.Empty : snapshot.Cpu.Trim();
            var details = new List<string>();

            if (snapshot.CpuCoreCount > 0)
            {
                details.Add($"Cores: {snapshot.CpuCoreCount}");
            }

            if (snapshot.CpuLogicalProcessorCount > 0)
            {
                details.Add($"Threads: {snapshot.CpuLogicalProcessorCount}");
            }

            if (string.IsNullOrEmpty(cpuName) && details.Count == 0)
            {
                return "Unknown";
            }

            if (details.Count == 0)
            {
                return cpuName;
            }

            if (string.IsNullOrEmpty(cpuName))
            {
                return string.Join(" | ", details);
            }

            return $"{cpuName} | {string.Join(" | ", details)}";
        }
        
        private static string BuildBatteryText(BatteryInfo battery)
        {
            if (battery == null || battery.Status == "Not Detected")
            {
                return "No battery detected (Desktop PC).";
            }

            var healthText = battery.HealthPercentage > 0 ? $"{battery.HealthPercentage}%" : "Unknown";
            var statusSuffix = string.IsNullOrWhiteSpace(battery.Status) ? string.Empty : $" - {battery.Status}";

            var capacityDetails = new List<string>();
            if (battery.DesignCapacity > 0)
            {
                capacityDetails.Add($"Design {FormatBatteryCapacity(battery.DesignCapacity)}");
            }
            if (battery.FullChargeCapacity > 0)
            {
                capacityDetails.Add($"Full {FormatBatteryCapacity(battery.FullChargeCapacity)}");
            }

            var capacitySuffix = capacityDetails.Count > 0 ? $" ({string.Join(", ", capacityDetails)})" : string.Empty;

            return $"{healthText}{statusSuffix}{capacitySuffix}";
        }

        private static string BuildStorageText(IReadOnlyList<DiskInfo> disks)
        {
            if (disks == null || disks.Count == 0)
            {
                return "No disks detected.";
            }

            var builder = new StringBuilder();
            foreach (var disk in disks)
            {
                builder.AppendLine($"Disk{disk.Index}: {disk.Model} | {disk.Size} | {disk.MediaType}");
            }

            return builder.ToString().TrimEnd();
        }

        private void UpdateSpecRating(SystemInfoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                ApplySpecRating(SpecRatingLevel.Low);
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
            {
                return null;
            }

            DiskInfo primary = null;
            foreach (var disk in disks)
            {
                if (disk == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(disk.Index) &&
                    string.Equals(disk.Index.Trim(), "0", StringComparison.OrdinalIgnoreCase))
                {
                    return disk;
                }

                if (primary == null)
                {
                    primary = disk;
                }
            }

            return primary;
        }

        private static double ExtractCapacityInGigabytes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return -1;
            }

            var match = Regex.Match(value, @"([0-9]+(?:\.[0-9]+)?)\s*(TB|GB|MB)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return -1;
            }

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
            {
                return -1;
            }

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
            {
                return StorageKind.Unknown;
            }

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
            {
                return StorageKind.Nvme;
            }

            if (!string.IsNullOrWhiteSpace(interfaceType) &&
                interfaceType.IndexOf("pci", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (combined.Contains("ssd") ||
                    combined.Contains("solid state") ||
                    combined.Contains("non-volatile"))
                {
                    return StorageKind.Nvme;
                }
            }

            if (combined.Contains("nvme") ||
                combined.Contains("non-volatile memory express") ||
                combined.Contains("pci express") ||
                combined.Contains("pcie") ||
                combined.Contains("pc i e"))
            {
                return StorageKind.Nvme;
            }

            if ((combined.Contains("m.2") || combined.Contains("m2")) &&
                !combined.Contains("sata"))
            {
                return StorageKind.Nvme;
            }

            if (combined.Contains("sata") && combined.Contains("ssd"))
            {
                return StorageKind.SataSsd;
            }

            if (mediaType.IndexOf("sata", StringComparison.OrdinalIgnoreCase) >= 0 &&
                mediaType.IndexOf("ssd", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StorageKind.SataSsd;
            }

            if (mediaType.Equals("SSD", StringComparison.OrdinalIgnoreCase) ||
                mediaType.Equals("Solid State Disk", StringComparison.OrdinalIgnoreCase))
            {
                if (interfaceType.IndexOf("pci", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return StorageKind.Nvme;
                }

                if (combined.Contains("sata"))
                {
                    return StorageKind.SataSsd;
                }
            }

            if (combined.Contains("ssd"))
            {
                return StorageKind.SataSsd;
            }

            if (combined.Contains("hdd") ||
                combined.Contains("hard disk") ||
                combined.Contains("hard-disk"))
            {
                return StorageKind.Hdd;
            }

            if (mediaType.IndexOf("hdd", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return StorageKind.Hdd;
            }

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
            if ((hasCpuData && cpuSupport == CpuSupportLevel.Unsupported) ||
                (hasMemoryData && memoryGb > 0 && memoryGb <= 8) ||
                (hasDiskData && storageKind == StorageKind.Hdd))
            {
                return SpecRatingLevel.Obsolete;
            }

            if (cpuSupport == CpuSupportLevel.Supported &&
                hasMemoryData && hasDiskData &&
                memoryGb >= 16 &&
                storageKind == StorageKind.Nvme)
            {
                return SpecRatingLevel.Approved;
            }

            return SpecRatingLevel.Low;
        }

        private void ApplySpecRating(SpecRatingLevel level)
        {
            var excellentBackground = Color.FromArgb(209, 250, 229);
            var excellentText = Color.FromArgb(6, 95, 70);

            var moderateBackground = Color.FromArgb(254, 243, 199);
            var moderateText = Color.FromArgb(146, 64, 14);

            var poorBackground = Color.FromArgb(254, 226, 226);
            var poorText = Color.FromArgb(153, 27, 27);

            switch (level)
            {
                case SpecRatingLevel.Approved:
                    lblSpecRatingValue.Text = "✅ High Performance";
                    lblSpecRatingValue.BackColor = excellentBackground;
                    lblSpecRatingValue.ForeColor = excellentText;
                    break;
                case SpecRatingLevel.Low:
                    lblSpecRatingValue.Text = "⚠️ Standard Performance";
                    lblSpecRatingValue.BackColor = moderateBackground;
                    lblSpecRatingValue.ForeColor = moderateText;
                    break;
                case SpecRatingLevel.Obsolete:
                    lblSpecRatingValue.Text = "❌ Low Performance";
                    lblSpecRatingValue.BackColor = poorBackground;
                    lblSpecRatingValue.ForeColor = poorText;
                    break;
                default:
                    lblSpecRatingValue.Text = "ℹ️ Unknown";
                    lblSpecRatingValue.BackColor = Color.LightGray;
                    lblSpecRatingValue.ForeColor = Color.Black;
                    break;
            }
        }

        private static string BuildGpuText(IReadOnlyList<GpuInfo> gpus)
        {
            if (gpus == null || gpus.Count == 0)
            {
                return "No GPUs detected.";
            }

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
            {
                return "No monitors detected.";
            }

            var builder = new StringBuilder();
            for (var index = 0; index < monitors.Count; index++)
            {
                var monitor = monitors[index];
                var parts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(monitor.ScreenSize))
                {
                    parts.Add(monitor.ScreenSize);
                }
                
                if (!string.IsNullOrWhiteSpace(monitor.RefreshRate))
                {
                    parts.Add($"Refresh: {monitor.RefreshRate}");
                }
                
                if (!string.IsNullOrWhiteSpace(monitor.Resolution))
                {
                    parts.Add($"Resolution: {monitor.Resolution}");
                }
                
                parts.Add($"Touch: {(monitor.IsTouchScreen ? "Yes" : "No")}");
                
                if (parts.Count > 0)
                {
                    builder.AppendLine($"Monitor{index + 1}: {string.Join(" | ", parts)}");
                }
                else
                {
                    builder.AppendLine($"Monitor{index + 1}: {monitor.Name}");
                }
            }

            return builder.ToString().TrimEnd();
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
                var baseFileName = $"SysInfo_{_currentSnapshot.MachineName}_{_currentSnapshot.CapturedAt:yyyyMMdd_HHmmss}";
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
            {
                memoryLine += $" | Speed: {snapshot.MemorySpeedMhz} MHz";
            }
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
                {
                    builder.AppendLine($"Design Capacity: {FormatBatteryCapacity(snapshot.BatteryInfo.DesignCapacity)}");
                }
                if (snapshot.BatteryInfo.FullChargeCapacity > 0)
                {
                    builder.AppendLine($"Full Charge Capacity: {FormatBatteryCapacity(snapshot.BatteryInfo.FullChargeCapacity)}");
                }
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
                {
                    throw new InvalidOperationException("Unable to capture window image.");
                }

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
            {
                return false;
            }

            if (!GetWindowRect(hwnd, out var rect))
            {
                return false;
            }

            rect = NormalizeWindowRect(hwnd, rect);

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
            {
                return false;
            }

            bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var hdc = graphics.GetHdc();
                try
                {
                    if (PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                    {
                        return true;
                    }

                    if (PrintWindow(hwnd, hdc, 0))
                    {
                        return true;
                    }
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
                    {
                        return dwmRect;
                    }
                }
            }
            catch (DllNotFoundException)
            {
                // DWM not available on this OS (e.g., Server Core); fall back to standard rect.
            }
            catch (EntryPointNotFoundException)
            {
                // Attribute unavailable; ignore and return original rect.
            }

            return rect;
        }

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
                    {
                        Debug.WriteLine($"Command '{command}' output:{Environment.NewLine}{output}");
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Debug.WriteLine($"Command '{command}' error:{Environment.NewLine}{error}");
                    }
                }
            }
            catch (Exception ex)
            {
            Debug.WriteLine($"[{nameof(frmSysInfo)}] Command '{command}' failed: {ex}");
            MessageBox.Show("Error executing command. Check logs for details.", "Command Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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

        private static string FormatBatteryCapacity(int capacity)
        {
            return capacity > 0 ? string.Format("{0:N0} mWh", capacity) : "Unknown";
        }

    }
}

