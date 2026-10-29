using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace CNSysInfo
{
    // Preview only: no encoder, file sink, microphone or recording path is created.
    internal sealed class CameraTestWindow : Window
    {
        private readonly ComboBox devices = new ComboBox { MinWidth = 180, DisplayMemberPath = "Name" };
        private readonly TextBlock status = new TextBlock { Text = "Camera is off", TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock placeholder = new TextBlock { Text = "Camera is off", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.SlateGray };
        private readonly CameraSurface surface = new CameraSurface { Visibility = Visibility.Collapsed };
        private readonly Button start = Action("Start preview"), stop = Action("Stop");
        private readonly List<CameraDevice> cameras = new List<CameraDevice>();
        private CameraPreview preview;
        private readonly DispatcherTimer previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private HwndSource source;
        private bool closing;

        public CameraTestWindow()
        {
            DiagnosticUi.Apply(this);
            Title = "Camera test";
            Width = Math.Min(500, SystemParameters.WorkArea.Width);
            Height = Math.Min(470, SystemParameters.WorkArea.Height);
            MinWidth = Math.Min(380, SystemParameters.WorkArea.Width);
            MinHeight = Math.Min(360, SystemParameters.WorkArea.Height);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;
            Foreground = new SolidColorBrush(Color.FromRgb(11, 31, 58));
            FontFamily = new FontFamily("Segoe UI");
            var body = new Grid { Margin = new Thickness(16) };
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition());
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var selector = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            selector.Children.Add(new TextBlock { Text = "Camera device: ", VerticalAlignment = VerticalAlignment.Center });
            selector.Children.Add(devices); body.Children.Add(selector);
            System.Windows.Automation.AutomationProperties.SetName(devices, "Camera device");
            var view = new Grid { Background = new SolidColorBrush(Color.FromRgb(234, 240, 246)), MinHeight = 130 };
            view.Children.Add(surface); view.Children.Add(placeholder); Grid.SetRow(view, 1); body.Children.Add(view);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
            start.Click += (_, __) => StartPreview();
            stop.Click += (_, __) => StopPreview("Camera is off");
            stop.IsEnabled = false; actions.Children.Add(start); actions.Children.Add(stop); Grid.SetRow(actions, 2); body.Children.Add(actions);
            var notes = new StackPanel();
            notes.Children.Add(status);
            notes.Children.Add(new TextBlock { Text = "Preview only. No recording or saving. Preview stops when you leave this window.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 6, 0, 0) });
            Grid.SetRow(notes, 3); body.Children.Add(notes);
            var bottom = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var refresh = Action("Refresh cameras"); refresh.Click += (_, __) => LoadCameras();
            var close = Action("Close"); close.IsCancel = true; close.Click += (_, __) => Close();
            bottom.Children.Add(refresh); bottom.Children.Add(close); Grid.SetRow(bottom, 4); body.Children.Add(bottom);
            Content = body;
            surface.SizeChanged += (_, __) =>
            {
                try { if (preview != null) preview.Resize(surface.PixelWidth, surface.PixelHeight); }
                catch (COMException) { StopPreview("Camera preview stopped. Check the connection, then retry."); }
            };
            previewTimer.Tick += (_, __) =>
            {
                try { if (preview != null && preview.HasDeviceError()) StopPreview("Camera stopped. Check the connection and Windows camera permissions, then retry."); }
                catch (COMException) { StopPreview("Camera stopped. Check the connection, then retry."); }
            };
            Loaded += (_, __) => LoadCameras();
            SourceInitialized += (_, __) => { source = (HwndSource)PresentationSource.FromVisual(this); source?.AddHook(WindowMessage); };
            Deactivated += (_, __) => StopPreview("Camera stopped. Select Start preview to resume.");
            Closed += (_, __) => { closing = true; StopPreview("Camera is off"); source?.RemoveHook(WindowMessage); ReleaseDevices(); };
        }

        private static Button Action(string text) => new Button { Content = text, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 8, 0), Background = Brushes.White, BorderBrush = Brushes.LightSteelBlue };

        private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Device arrival/removal can invalidate an active capture filter.
            if (message == 0x0219 && (wParam.ToInt64() == 0x8000 || wParam.ToInt64() == 0x8004 || wParam.ToInt64() == 7))
            {
                StopPreview("Camera stopped because connected devices changed.");
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { if (!closing) LoadCameras(); }));
            }
            return IntPtr.Zero;
        }

        private void LoadCameras()
        {
            StopPreview("Camera is off");
            devices.ItemsSource = null; ReleaseDevices();
            try
            {
                cameras.AddRange(CameraPreview.Enumerate());
                devices.ItemsSource = cameras; devices.SelectedIndex = cameras.Count > 0 ? 0 : -1;
                status.Text = cameras.Count > 0 ? "Camera is off. Select Start preview when ready." : "No compatible camera found. Connect a camera, then select Refresh cameras.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Camera enumeration failed: " + ex.GetType().Name);
                status.Text = "Cameras could not be listed. Check the connection and Windows camera permissions, then refresh.";
            }
            start.IsEnabled = cameras.Count > 0;
        }

        private void StartPreview()
        {
            var camera = devices.SelectedItem as CameraDevice;
            if (camera == null) return;
            StopPreview("Starting camera…");
            try
            {
                placeholder.Visibility = Visibility.Collapsed; surface.Visibility = Visibility.Visible; UpdateLayout();
                preview = new CameraPreview();
                preview.Start(camera, surface.Handle, surface.PixelWidth, surface.PixelHeight);
                devices.IsEnabled = false; start.IsEnabled = false; stop.IsEnabled = true;
                status.Text = "Live preview • " + camera.Name;
                previewTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Camera preview failed: " + ex.GetType().Name);
                StopPreview("Unable to preview this camera. It may be in use, disconnected, unsupported, or blocked. Check Windows Settings > Privacy > Camera and allow desktop apps, close other camera apps, then retry.");
            }
        }

        private void StopPreview(string message)
        {
            previewTimer.Stop();
            if (preview != null) { preview.Dispose(); preview = null; }
            surface.Visibility = Visibility.Collapsed; placeholder.Visibility = Visibility.Visible;
            devices.IsEnabled = true; start.IsEnabled = cameras.Count > 0; stop.IsEnabled = false; status.Text = message;
        }
        private void ReleaseDevices() { foreach (var camera in cameras) camera.Dispose(); cameras.Clear(); }
    }

    internal sealed class CameraSurface : HwndHost
    {
        public int PixelWidth => Math.Max(1, (int)(ActualWidth * VisualTreeHelper.GetDpi(this).DpiScaleX));
        public int PixelHeight => Math.Max(1, (int)(ActualHeight * VisualTreeHelper.GetDpi(this).DpiScaleY));
        protected override HandleRef BuildWindowCore(HandleRef parent)
        {
            IntPtr window = CreateWindowEx(0, "STATIC", "", 0x40000000 | 0x10000000 | 0x02000000, 0, 0, 1, 1, parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (window == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            return new HandleRef(this, window);
        }
        protected override void DestroyWindowCore(HandleRef hwnd) { DestroyWindow(hwnd.Handle); }
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(int extendedStyle, string className, string title, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyWindow(IntPtr hwnd);
    }
}
