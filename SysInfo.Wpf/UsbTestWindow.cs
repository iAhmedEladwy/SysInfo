using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CNSysInfo
{
    internal sealed class UsbTestDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
    }
    internal sealed class UsbTestWindow : Window
    {
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        private readonly ListView devices = new ListView { MinHeight = 130 };
        private readonly ListBox events = new ListBox { Height = 100 };
        private readonly TextBlock status = new TextBlock { Text = "Reading connected USB devices…", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
        private Dictionary<string, UsbTestDevice> previous;
        private bool reading, closed;
        private DateTime started;
        public UsbTestWindow()
        {
            DiagnosticUi.Apply(this); Title = "USB detection test"; Width = Math.Min(650, SystemParameters.WorkArea.Width); Height = Math.Min(460, SystemParameters.WorkArea.Height);
            Background = Brushes.White; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var body = new DockPanel { Margin = new Thickness(16) };
            var header = new DockPanel(); DockPanel.SetDock(header, Dock.Top);
            var refresh = new Button { Content = "Refresh", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(12, 0, 0, 0) }; DockPanel.SetDock(refresh, Dock.Right);
            refresh.Click += (_, __) => Refresh(); header.Children.Add(refresh);
            header.Children.Add(new TextBlock { Text = "Connect or unplug a USB device and watch for detection. This does not measure port speed, power delivery or storage health.", TextWrapping = TextWrapping.Wrap }); body.Children.Add(header);
            DockPanel.SetDock(status, Dock.Top); body.Children.Add(status);
            var history = new StackPanel { Margin = new Thickness(0, 10, 0, 0) }; DockPanel.SetDock(history, Dock.Bottom);
            history.Children.Add(new TextBlock { Text = "Changes since this window opened", Margin = new Thickness(0, 0, 0, 4) }); history.Children.Add(events); body.Children.Add(history);
            var view = new GridView(); view.Columns.Add(new GridViewColumn { Header = "Device", DisplayMemberBinding = new System.Windows.Data.Binding("Name"), Width = 430 });
            view.Columns.Add(new GridViewColumn { Header = "Windows status", DisplayMemberBinding = new System.Windows.Data.Binding("Status"), Width = 110 }); devices.View = view;
            body.Children.Add(devices); Content = body;
            timer.Tick += (_, __) => Refresh(); Loaded += (_, __) => { Refresh(); timer.Start(); };
            Closed += (_, __) => { closed = true; timer.Stop(); };
        }
        private async void Refresh()
        {
            if (closed) return;
            if (reading) { if ((DateTime.UtcNow - started).TotalSeconds > 5) status.Text = "Windows is taking longer to list USB devices. The last successful list is retained."; return; }
            reading = true; started = DateTime.UtcNow;
            try
            {
                var current = await Task.Run(ReadDevices);
                if (closed) return;
                if (previous != null)
                {
                    foreach (var id in UsbDiagnosticLogic.Added(previous.Keys, current.Keys)) AddEvent("Connected: " + current[id].Name);
                    foreach (var id in UsbDiagnosticLogic.Added(current.Keys, previous.Keys)) AddEvent("Disconnected: " + previous[id].Name);
                }
                previous = current; devices.ItemsSource = current.Values.OrderBy(device => device.Name).ToArray();
                status.Text = current.Count + " detected USB entries · checked " + DateTime.Now.ToString("HH:mm:ss") + " · refreshes every 3 seconds";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("USB list unavailable: " + ex.GetType().Name);
                if (!closed) status.Text = "USB detection is unavailable. Check the device connection and try Refresh. Previous results are retained.";
            }
            finally { reading = false; }
        }
        private void AddEvent(string message) { events.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " · " + message); while (events.Items.Count > 50) events.Items.RemoveAt(events.Items.Count - 1); }
        private static Dictionary<string, UsbTestDevice> ReadDevices()
        {
            var result = new Dictionary<string, UsbTestDevice>(StringComparer.OrdinalIgnoreCase);
            using (var searcher = new ManagementObjectSearcher("SELECT DeviceID,Name,Status FROM Win32_PnPEntity WHERE Present = TRUE"))
            {
                searcher.Options.Timeout = TimeSpan.FromSeconds(3);
                using (var collection = searcher.Get()) foreach (ManagementObject item in collection)
                using (item)
                {
                    string id = Convert.ToString(item["DeviceID"]);
                    if (!id.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) && !id.StartsWith("USBSTOR\\", StringComparison.OrdinalIgnoreCase)) continue;
                    result[id] = new UsbTestDevice { Id = id, Name = string.IsNullOrWhiteSpace(Convert.ToString(item["Name"])) ? "USB device" : Convert.ToString(item["Name"]), Status = string.IsNullOrWhiteSpace(Convert.ToString(item["Status"])) ? "Unknown" : Convert.ToString(item["Status"]) };
                }
            }
            return result;
        }
    }
}
