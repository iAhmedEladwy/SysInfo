using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
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
        public string Manufacturer { get; set; }
        public string Type { get; set; }
        public string Service { get; set; }
        public string Problem { get; set; }
        public string VendorId { get; set; }
        public string ProductId { get; set; }
        public string UsbVersion { get; set; } = "Unavailable";
        public string LinkSpeed { get; set; } = "Unavailable";
        public string Port { get; set; } = "Unavailable";
        public string Protocols { get; set; } = "Unavailable";
    }
    internal sealed class UsbSnapshot
    {
        public Dictionary<string, UsbTestDevice> Devices;
        public UsbPortScan Ports;
    }
    internal sealed class UsbTestWindow : Window
    {
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        private readonly ListView devices = new ListView { MinHeight = 100 };
        private readonly ListView ports = new ListView();
        private readonly ListBox events = new ListBox { Height = 80 };
        private readonly TextBlock status = new TextBlock { Text = "Reading USB devices and port metadata…", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
        private readonly TextBox details = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Height = 116, Padding = new Thickness(8), Background = Brushes.WhiteSmoke, BorderThickness = new Thickness(0), Margin = new Thickness(0, 8, 0, 0) };
        private readonly TextBlock portNote = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 8, 0, 0) };
        private Dictionary<string, UsbTestDevice> previous;
        private bool reading, closed, updating;
        private DateTime started;
        public UsbTestWindow()
        {
            DiagnosticUi.Apply(this); Title = "USB devices and ports"; Width = Math.Min(720, SystemParameters.WorkArea.Width); Height = Math.Min(580, SystemParameters.WorkArea.Height);
            Background = Brushes.White; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var body = new DockPanel { Margin = new Thickness(16) };
            var header = new DockPanel(); DockPanel.SetDock(header, Dock.Top);
            var refresh = new Button { Content = "Refresh", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(12, 0, 0, 0) }; DockPanel.SetDock(refresh, Dock.Right);
            refresh.Click += (_, __) => Refresh(); header.Children.Add(refresh);
            header.Children.Add(new TextBlock { Text = "Connect a device to identify its hub/port. View device USB version and negotiated link rate separately from port support. No storage writes or speed benchmarks.", TextWrapping = TextWrapping.Wrap }); body.Children.Add(header);
            DockPanel.SetDock(status, Dock.Top); body.Children.Add(status);
            var history = new StackPanel { Margin = new Thickness(0, 10, 0, 0) }; DockPanel.SetDock(history, Dock.Bottom);
            history.Children.Add(new TextBlock { Text = "Connections / removals since opening this window", Margin = new Thickness(0, 0, 0, 4) }); history.Children.Add(events); body.Children.Add(history);
            var tabs = new TabControl();
            var devicePanel = new DockPanel { Margin = new Thickness(6) }; DockPanel.SetDock(details, Dock.Bottom); devicePanel.Children.Add(details); devicePanel.Children.Add(devices);
            var deviceView = new GridView(); Column(deviceView, "Device", "Name", 215); Column(deviceView, "Device USB spec", "UsbVersion", 95); Column(deviceView, "Negotiated link", "LinkSpeed", 160); Column(deviceView, "Port", "Port", 110); devices.View = deviceView;
            devices.SelectionChanged += (_, __) => { if (!updating) ShowDetails(); };
            var portPanel = new DockPanel { Margin = new Thickness(6) }; DockPanel.SetDock(portNote, Dock.Bottom); portPanel.Children.Add(portNote); portPanel.Children.Add(ports);
            var portView = new GridView(); Column(portView, "Port", "Port", 100); Column(portView, "State", "State", 80); Column(portView, "Port protocols", "Protocols", 130); Column(portView, "Negotiated link", "LinkSpeed", 150); Column(portView, "Device", "DeviceName", 130); ports.View = portView;
            tabs.Items.Add(new TabItem { Header = "Devices", Content = devicePanel }); tabs.Items.Add(new TabItem { Header = "Ports", Content = portPanel }); body.Children.Add(tabs); Content = body;
            timer.Tick += (_, __) => Refresh(); Loaded += (_, __) => { Refresh(); timer.Start(); }; Closed += (_, __) => { closed = true; timer.Stop(); };
        }
        private static void Column(GridView view, string title, string property, double width) => view.Columns.Add(new GridViewColumn { Header = title, DisplayMemberBinding = new System.Windows.Data.Binding(property), Width = width });
        private void ShowDetails()
        {
            var item = devices.SelectedItem as UsbTestDevice;
            details.Text = item == null ? "Select a device for manufacturer, type, IDs and link details." :
                item.Name + " · " + item.Manufacturer + "\nType: " + item.Type + " · VID: " + item.VendorId + " · PID: " + item.ProductId +
                "\nWindows status: " + item.Status + " · Problem code: " + item.Problem + " · Driver service: " + item.Service +
                "\nDevice USB spec: " + item.UsbVersion + " · Negotiated link: " + item.LinkSpeed +
                "\n" + item.Port + " · Port protocols: " + item.Protocols +
                "\nSignaling rate is not measured file-transfer speed. Exact lane count/max bandwidth may be unavailable.";
        }
        private async void Refresh()
        {
            if (closed) return;
            if (reading) { if ((DateTime.UtcNow - started).TotalSeconds > 5) status.Text = "Windows is taking longer to read USB metadata. The last successful list is retained."; return; }
            reading = true; started = DateTime.UtcNow;
            try
            {
                var snapshot = await Task.Run(ReadSnapshot); if (closed) return; var current = snapshot.Devices;
                if (previous != null)
                {
                    foreach (var id in UsbDiagnosticLogic.Added(previous.Keys, current.Keys)) AddEvent("Connected: " + current[id].Name);
                    foreach (var id in UsbDiagnosticLogic.Added(current.Keys, previous.Keys)) AddEvent("Disconnected: " + previous[id].Name);
                }
                string selected = (devices.SelectedItem as UsbTestDevice)?.Id;
                previous = current; var rows = current.Values.OrderBy(device => device.Name).ToArray(); updating = true;
                var deviceScroll = Scroller(devices); var portScroll = Scroller(ports);
                double deviceOffset = deviceScroll?.VerticalOffset ?? 0, portOffset = portScroll?.VerticalOffset ?? 0;
                devices.ItemsSource = rows;
                devices.SelectedItem = rows.FirstOrDefault(device => string.Equals(device.Id, selected, StringComparison.OrdinalIgnoreCase)) ?? rows.FirstOrDefault();
                ports.ItemsSource = snapshot.Ports.Ports; portNote.Text = snapshot.Ports.Note; updating = false; ShowDetails();
                _ = Dispatcher.BeginInvoke(new Action(() => { if (!closed) { deviceScroll?.ScrollToVerticalOffset(deviceOffset); portScroll?.ScrollToVerticalOffset(portOffset); } }), DispatcherPriority.Loaded);
                status.Text = current.Count + " USB entries · " + snapshot.Ports.Ports.Count + " hub ports · checked " + DateTime.Now.ToString("HH:mm:ss") + " · refreshes every 3 sec";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("USB list unavailable: " + ex.GetType().Name);
                if (!closed) status.Text = "USB metadata is unavailable. Check the connection and try Refresh. Previous results are retained.";
            }
            finally { reading = false; updating = false; }
        }
        private static ScrollViewer Scroller(DependencyObject element)
        {
            if (element is ScrollViewer scroll) return scroll;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            { var found = Scroller(VisualTreeHelper.GetChild(element, i)); if (found != null) return found; }
            return null;
        }
        private void AddEvent(string message) { events.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " · " + message); while (events.Items.Count > 50) events.Items.RemoveAt(events.Items.Count - 1); }
        private static UsbSnapshot ReadSnapshot()
        {
            var devices = ReadDevices(); var scan = UsbPortReader.Read();
            foreach (var item in devices.Values)
            {
                if (scan.ByDevice.TryGetValue(item.Id, out var port))
                { item.Port = port.Port; item.UsbVersion = port.UsbVersion; item.LinkSpeed = port.LinkSpeed; item.Protocols = port.Protocols; if (string.IsNullOrEmpty(port.DeviceName)) port.DeviceName = item.Name; }
                else if (item.Id.StartsWith("USB\\ROOT_HUB", StringComparison.OrdinalIgnoreCase))
                { item.Port = "Root hub"; item.LinkSpeed = "No parent device link"; }
            }
            return new UsbSnapshot { Devices = devices, Ports = scan };
        }
        private static Dictionary<string, UsbTestDevice> ReadDevices()
        {
            var result = new Dictionary<string, UsbTestDevice>(StringComparer.OrdinalIgnoreCase);
            using (var searcher = new ManagementObjectSearcher("SELECT DeviceID,Name,Status,Manufacturer,PNPClass,Service,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Present = TRUE"))
            {
                searcher.Options.Timeout = TimeSpan.FromSeconds(3);
                using (var collection = searcher.Get()) foreach (ManagementObject item in collection) using (item)
                {
                    string id = Convert.ToString(item["DeviceID"]);
                    if (!id.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) && !id.StartsWith("USBSTOR\\", StringComparison.OrdinalIgnoreCase)) continue;
                    var vid = Regex.Match(id, "VID_([0-9A-F]{4})", RegexOptions.IgnoreCase); var pid = Regex.Match(id, "PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    result[id] = new UsbTestDevice { Id = id, Name = Text(item["Name"]), Status = Text(item["Status"]), Manufacturer = Text(item["Manufacturer"]), Type = Text(item["PNPClass"]), Service = Text(item["Service"]), Problem = Text(item["ConfigManagerErrorCode"]), VendorId = vid.Success ? vid.Groups[1].Value : "Unavailable", ProductId = pid.Success ? pid.Groups[1].Value : "Unavailable" };
                }
            }
            return result;
        }
        private static string Text(object value) => string.IsNullOrWhiteSpace(Convert.ToString(value)) ? "Unavailable" : Convert.ToString(value);
    }
}
