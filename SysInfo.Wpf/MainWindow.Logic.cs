using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CNSysInfo
{
    public partial class MainWindow : Window
    {
        private UiSnapshot _ui;
        private bool _busy;
        private readonly ActivationHistory _activationHistory = new ActivationHistory();
        private string _collectionDetails = string.Empty;
        private static readonly string AppVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.2.2";

        public MainWindow()
        {
            BuildUi();
            StartPowerMonitoring();
        }

        private static Brush B(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblVersion.Text = "SysInfo " + AppVersion + " · About";
            await LoadAsync();
            await CheckForUpdates(true);
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5) { e.Handled = true; await LoadAsync(); }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) { await LoadAsync(); }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            btnRefresh.IsEnabled = !busy;
            btnExport.IsEnabled = !busy && _ui != null;
            btnCopy.IsEnabled = !busy && _ui != null;
            btnTools.IsEnabled = !busy;
            btnPower.IsEnabled = !busy;
            btnTests.IsEnabled = !busy;
            Mouse.OverrideCursor = busy ? Cursors.Wait : null;
        }

        // All blocking reads (WMI, SMART) run in Task.Run, so the window never freezes.
        private async Task LoadAsync()
        {
            if (_busy) return;
            SetBusy(true);
            SetCollectionState(UiStatusLogic.CollectionState(true, false, _ui != null, 0),
                "Refreshing system information. The capture time still identifies the displayed snapshot.", false);
            if (_ui == null)
            {
                txtRating.Text = "Evaluating...";
            }
            try
            {
                var result = await Task.Run(UiSnapshotService.Collect);
                _activationHistory.Observe(result.System.Activation);
                _ui = result;
                Render(false);
                SetCollectionState(UiStatusLogic.CollectionState(false, false, true, result.Warnings.Count),
                    string.Join(Environment.NewLine, result.Warnings), result.Warnings.Count > 0);
                await Dispatcher.InvokeAsync(FitInitialContent, System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Load failed: " + ex);
                if (_ui == null) txtTimeZone.Text = "No system snapshot captured.";
                SetCollectionState(UiStatusLogic.CollectionState(false, true, _ui != null, 0),
                    _ui == null ? "Collection failed. Use Refresh to retry."
                    : "Refresh failed. Previous data is still shown with its original capture time.", true);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetCollectionState(string text, string details, bool actionable)
        {
            lblStatus.Text = text;
            _collectionDetails = details;
            btnStatusDetails.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            btnStatusDetails.IsEnabled = actionable;
            btnStatusDetails.ToolTip = details;
            System.Windows.Automation.AutomationProperties.SetName(btnStatusDetails,
                string.IsNullOrEmpty(text) ? "Collection details" : text + ". Open collection details");
        }

        private void ShowCollectionDetails(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, _collectionDetails, "Collection details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void Bar(ProgressBar bar, TextBlock label, int? pct, Presentation.Tier tier, string text)
        {
            bar.Value = pct.HasValue ? Math.Max(0, Math.Min(100, pct.Value)) : 0;
            bar.Foreground = B(tier.Back);
            label.Text = text;
        }

        private void Render(bool redact)
        {
            if (_ui == null) return;
            var s = _ui.System;
            string R(string v) { return redact ? ReportBuilder.Redacted : v; }

            txtTimeZone.Text = UiStatusLogic.CaptureMetadata(s.TimeZoneDisplayName, s.CapturedAt);
            txtPc.Text = R(s.MachineName);
            txtUser.Text = R(s.UserName);
            txtMaker.Text = s.Manufacturer;
            txtModel.Text = s.SystemModel;
            txtSerial.Text = R(s.SerialNumber);
            txtAsset.Text = R(s.AssetTag);

            txtOs.Text = s.OsDisplay;
            txtAct.Text = _activationHistory.FormatCurrent(s.Activation);
            txtAct.ToolTip = s.Activation == null ? "Activation check unavailable. Use Refresh to retry."
                : s.Activation.IsAvailable ? "Activation checked: " + s.Activation.ObservedAt.ToString("yyyy-MM-dd HH:mm:ss")
                : "Current activation check unavailable: " + s.Activation.Reason + ". Use Refresh to retry.";
            txtUptime.Text = s.WindowsUptimeDisplay;
            txtBios.Text = "BIOS: " + (string.IsNullOrWhiteSpace(s.BiosVersion) ? "Unknown" : s.BiosVersion);
            restartBadge.Visibility = UiStatusLogic.ShowRestartReminder(s.WindowsUptime, s.WindowsUptimeAvailable)
                ? Visibility.Visible : Visibility.Collapsed;

            txtCpu.Text = Presentation.CpuText(s.Cpu, s.CpuCoreCount, s.CpuLogicalProcessorCount);
            txtCpuCache.Text = Presentation.CpuCacheText(s.CpuL2CacheKb, s.CpuL3CacheKb);
            txtMem.Text = Presentation.MemoryText(s.InstalledPhysicalMemory, s.UsablePhysicalMemory, s.MemorySlotsUsed, s.MemorySlotsTotal, s.MemorySpeedMhz);
            var mu = _ui.MemoryUsagePercentage;
            Bar(pbMem, lblMem, mu, mu.HasValue ? Presentation.ForUsage(mu.Value) : Presentation.Unknown,
                mu.HasValue ? mu.Value + "%" : "Unknown");

            var memoryPressure = UiStatusLogic.MemoryPressure(mu);
            SetResourceWarning(memoryWarningBadge, memoryPressure,
                memoryPressure == ResourcePressure.Critical ? "RAM critical" : "High RAM",
                "Memory in use: " + mu + "%. High memory use can slow apps and make browsing feel sluggish. " +
                "Save your work, close unused apps or browser tabs, and check Task Manager > Processes > Memory. " +
                "Use Refresh to check again.");
            var systemUsage = _ui.SystemDriveUsage ?? new DiskUsageInfo();
            var drivePressure = UiStatusLogic.SystemDrivePressure(systemUsage);
            string driveName = (_ui.SystemDriveRoot ?? string.Empty).TrimEnd('\\', '/');
            SetResourceWarning(systemDriveWarningBadge, drivePressure,
                driveName + (drivePressure == ResourcePressure.Critical ? " critical" : " low space"),
                driveName + " has " + Presentation.FormatBytes(systemUsage.FreeBytes) + " free of " + Presentation.FormatBytes(systemUsage.TotalBytes) + ". " +
                "Low Windows drive space can slow apps and interrupt updates or saving files. " +
                "Open Settings > System > Storage to review temporary files or move unneeded files. " +
                "Review files before deleting them. Use Refresh to check again.");

            var sb = new StringBuilder(Presentation.StorageText(s.Disks));
            var du = _ui.DiskUsage;
            if (du.HasData)
                sb.AppendLine().Append("Disk0 usage: " + Presentation.FormatBytes(du.UsedBytes) + " used / " + Presentation.FormatBytes(du.TotalBytes) + " total");
            var sh = _ui.StorageHealth;
            sb.AppendLine().Append("Disk0 health: " + sh.StatusText);
            if (sh.LifeRemainingPercent.HasValue) sb.Append(" | Remaining life: " + sh.LifeRemainingPercent.Value + "%");
            if (sh.TemperatureCelsius.HasValue) sb.Append(" | " + sh.TemperatureCelsius.Value + "°C");
            if (sh.PowerOnHours.HasValue) sb.Append(" | Power-on: " + sh.PowerOnHours.Value.ToString("N0") + " hours");
            txtStorage.Text = sb.ToString();
            txtStorage.ToolTip = string.IsNullOrWhiteSpace(sh.Source) ? "Health data is not exposed by this drive or controller." : "Health source: " + sh.Source;
            Bar(pbDisk, lblDisk, du.HasData ? du.UsagePercentage : (int?)null,
                du.HasData ? Presentation.ForUsage(du.UsagePercentage) : Presentation.Unknown,
                du.HasData ? du.UsagePercentage + "%" : "N/A");

            txtGpu.Text = Presentation.GpuText(s.Gpus);
            txtScreen.Text = Presentation.ScreenText(s.Monitors);

            txtBattery.Text = Presentation.BatteryText(s.BatteryInfo);
            txtBattery.ToolTip = BatteryCapacityDetails(s.BatteryInfo);
            var bi = s.BatteryInfo;
            if (bi == null || bi.Status == "Not Detected")
                Bar(pbBattery, lblBattery, null, Presentation.Unknown, "N/A");
            else if (bi.HealthPercentage.HasValue)
                Bar(pbBattery, lblBattery, bi.HealthPercentage, Presentation.ForHealth(bi.HealthPercentage.Value), bi.HealthPercentage.Value + "%");
            else
                Bar(pbBattery, lblBattery, null, Presentation.Unknown, "Unknown");

            _batteryNeedsRefresh = false;
            _livePower = null; // Refresh establishes a new health snapshot.
            UpdateLivePower();

            string upgrades;
            var level = Presentation.Rate(s, out upgrades);
            var tier = Presentation.ForRating(level);
            txtRating.Text = Presentation.RatingText(level, upgrades);
            ratingBanner.Background = B(tier.Back);
            var fg = B(tier.Fore);
            txtRating.Foreground = fg;
        }

        // ---- actions ----

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b.ContextMenu == null) return;
            b.ContextMenu.PlacementTarget = b;
            b.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            b.ContextMenu.IsOpen = true;
        }

        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            var file = (string)((MenuItem)sender).Tag;
            try { Process.Start(new ProcessStartInfo { FileName = file, UseShellExecute = true }); }
            catch (Exception ex)
            {
                Debug.WriteLine("Tool failed: " + ex);
                MessageBox.Show(this, "Could not open " + file + ".", "Windows tools", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_ui == null)
            {
                MessageBox.Show(this, "System information isn't ready yet. Please wait a moment and try again.", "Copy unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ClipboardHelper.CopySystemInfoToClipboard(_ui.System);
        }

        // SAFETY: ask to close normally first; force-kill only after a second confirmation.
        private async void RestartOutlook_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Close and restart Outlook?\n\nIt will be asked to close normally first so you can save your work.",
                    "Restart Outlook", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK)
                return;
            SetBusy(true);
            RestartResult result;
            try
            {
                // Runs off the UI thread; the force-close question is marshalled back to it.
                result = await Task.Run(() => AppRestarter.Restart("outlook", "outlook.exe", 15000, () =>
                    (bool)Dispatcher.Invoke(() => MessageBox.Show(this,
                        "Outlook did not close within 15 seconds (it may be waiting for you to save something).\n\nForce it to close? Unsaved data will be lost.",
                        "Force close Outlook?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)));
            }
            finally { SetBusy(false); }
            if (result == RestartResult.StartFailed)
                MessageBox.Show(this, "Could not restart Outlook.", "Restart failed", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (result == RestartResult.UserDeclinedForce)
                MessageBox.Show(this, "Outlook was left running.", "Restart cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestartExplorer_Click(object sender, RoutedEventArgs e)
        {
            // Explorer's shell does not exit on a close request, so ending it is the only way to restart it.
            if (MessageBox.Show(this, "Restart Windows Explorer?\n\nThe taskbar and desktop will disappear for a few seconds. Open File Explorer windows will close. Unsaved work in other apps is not affected.",
                    "Restart Explorer", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK)
                return;
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer")) { try { p.Kill(); p.WaitForExit(5000); } catch { } p.Dispose(); }
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Explorer restart failed: " + ex);
                MessageBox.Show(this, "Could not restart Explorer.", "Restart failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // SAFETY: confirmation + 60 s delay that the user can cancel (shutdown /a).
        private void RestartPc_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Restart this computer?\n\nUnsaved work in open applications may be lost. The restart will start after a " + ShutdownPlan.DefaultDelaySeconds + " second countdown, and you will be able to cancel it.",
                    "Restart computer", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK)
                return;
            try { ShutdownPlan.Schedule(ShutdownPlan.DefaultDelaySeconds, "Restart requested from SysInfo"); }
            catch (Exception ex)
            {
                Debug.WriteLine("Reboot schedule failed: " + ex);
                MessageBox.Show(this, "Could not schedule the restart.", "Restart failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show(this, "The computer will restart in " + ShutdownPlan.DefaultDelaySeconds + " seconds.\n\nSave your work now. Click Yes to CANCEL the restart, or No to let it proceed.",
                    "Restart scheduled", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                try { ShutdownPlan.Abort(); }
                catch (Exception ex) { Debug.WriteLine("Abort failed: " + ex); }
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_ui == null)
            {
                MessageBox.Show(this, "System information isn't ready yet. Please wait a moment and try again.", "Export unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var privacy = MessageBox.Show(this,
                "Hide personal identifiers (user name, PC name, serial numbers, asset tag) in the report and screenshot?\n\nYes = redact   No = include   Cancel = don't export",
                "Export privacy", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
            if (privacy == MessageBoxResult.Cancel) return;
            bool redact = privacy == MessageBoxResult.Yes;

            var model = ReportModelMapper.From(_ui.System);
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save system report (a PNG screenshot is saved next to it)",
                Filter = "Text report (*.txt)|*.txt",
                DefaultExt = "txt",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = ReportBuilder.SuggestedFileName(model, redact) + ".txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog(this) != true) return;
            var textPath = dlg.FileName;
            var pngPath = Path.ChangeExtension(textPath, ".png");
            if (!string.Equals(pngPath, textPath, StringComparison.OrdinalIgnoreCase) && File.Exists(pngPath) &&
                MessageBox.Show(this, pngPath + " already exists. Replace it?", "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                var text = ReportBuilder.Build(model, redact, AppVersion);
                await Task.Run(() => File.WriteAllText(textPath, text, new UTF8Encoding(true)));

                // Screenshot = WPF render of the content area (DPI-correct, no window capture tricks needed).
                if (redact) Render(true);
                toolbar.Visibility = Visibility.Collapsed;
                UpdateLayout();
                try { SaveRender(pngPath); }
                finally { toolbar.Visibility = Visibility.Visible; if (redact) Render(false); }

                MessageBox.Show(this, "Report and screenshot saved:\n" + textPath + "\n" + pngPath, "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Export failed: " + ex);
                MessageBox.Show(this, "Export failed: " + ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveRender(string path)
        {
            // Render the whole content (not just the visible part of the scroll viewer).
            content.UpdateLayout();
            var w = Math.Max(1, content.ActualWidth); var h = Math.Max(1, content.ActualHeight);
            var dpi = VisualTreeHelper.GetDpi(this);
            var bmp = new RenderTargetBitmap((int)Math.Ceiling(w * dpi.DpiScaleX), (int)Math.Ceiling(h * dpi.DpiScaleY), 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.DrawRectangle(B("#F1F5F9"), null, new Rect(0, 0, w, h));
                ctx.DrawRectangle(new VisualBrush(content) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }, null, new Rect(0, 0, w, h));
            }
            bmp.Render(dv);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(path)) enc.Save(fs);
        }
    }
}
