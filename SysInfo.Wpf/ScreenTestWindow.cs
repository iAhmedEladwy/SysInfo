using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CNSysInfo
{
    internal sealed class ScreenTestWindow : Window
    {
        public ScreenTestWindow()
        {
            DiagnosticUi.Apply(this); Title = "Screen test"; Width = 460; SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize; Background = Brushes.White; FontFamily = new FontFamily("Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var body = new StackPanel { Margin = new Thickness(18) };
            body.Children.Add(new TextBlock { Text = "Inspect pixels, colour and backlighting", FontSize = 16, FontWeight = FontWeights.SemiBold });
            body.Children.Add(new TextBlock { Text = "Select a display, then inspect solid colours yourself. This test does not automatically detect dead pixels or change brightness or resolution.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) });
            var displays = new ComboBox { Margin = new Thickness(0, 0, 0, 12) };
            var screens = System.Windows.Forms.Screen.AllScreens;
            foreach (var screen in screens) displays.Items.Add(screen.DeviceName + " · " + screen.Bounds.Width + " × " + screen.Bounds.Height + (screen.Primary ? " · Primary" : ""));
            displays.SelectedIndex = 0; body.Children.Add(displays);
            var start = new Button { Content = "Start fullscreen test", Padding = new Thickness(12, 8, 12, 8) };
            start.Click += (_, __) =>
            {
                if (displays.SelectedIndex < 0) return;
                var bounds = screens[displays.SelectedIndex].Bounds;
                var area = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                new SolidColourWindow(area) { Owner = this, Icon = Icon }.ShowDialog();
            };
            body.Children.Add(start);
            body.Children.Add(new TextBlock { Text = "Click / Space / →: next colour · ←: previous · H: hide help · Esc: exit", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 12, 0, 0) });
            Content = body;
        }
    }

    internal sealed class SolidColourWindow : Window
    {
        private readonly Brush[] colours = { Brushes.White, Brushes.Black, Brushes.Red, Brushes.Lime, Brushes.Blue, Brushes.Gray };
        private readonly string[] names = { "White", "Black", "Red", "Green", "Blue", "Grey" };
        private readonly TextBlock name = new TextBlock { Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        private readonly Border help;
        private int index;
        private bool closing;
        public SolidColourWindow(Rect bounds)
        {
            Title = "Screen colour test"; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual; Left = bounds.Left; Top = bounds.Top; Width = bounds.Width; Height = bounds.Height;
            ShowInTaskbar = false; Background = colours[0];
            // Screen bounds are physical pixels. Place the HWND after creation, then once more
            // after per-monitor DPI has settled, so mixed-DPI displays fill the selected screen.
            Action place = () =>
            {
                if (!closing) SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero,
                    (int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height, 0x0014);
            };
            Loaded += (_, __) => { place(); Dispatcher.BeginInvoke(place, System.Windows.Threading.DispatcherPriority.Loaded); };
            var body = new Grid(); var line = new DockPanel();
            var exit = new Button { Content = "Exit (Esc)", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(16, 0, 0, 0) };
            exit.Click += (_, __) => Close(); DockPanel.SetDock(exit, Dock.Right); line.Children.Add(exit); line.Children.Add(name);
            help = new Border { Background = new SolidColorBrush(Color.FromArgb(205, 15, 23, 42)), Padding = new Thickness(12), Margin = new Thickness(16),
                CornerRadius = new CornerRadius(6), VerticalAlignment = VerticalAlignment.Top, Child = line };
            body.Children.Add(help); Content = body; UpdateColour();
            MouseLeftButtonDown += (_, args) => { if (!help.IsMouseOver) { Move(1); args.Handled = true; } };
            MouseRightButtonDown += (_, args) => { Move(-1); args.Handled = true; };
            KeyDown += (_, args) =>
            {
                if (args.Key == Key.Escape) Close();
                else if (args.Key == Key.Space || args.Key == Key.Right) Move(1);
                else if (args.Key == Key.Left) Move(-1);
                else if (args.Key == Key.H) help.Visibility = help.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                else return;
                args.Handled = true;
            };
            Closing += (_, __) => closing = true;
            Deactivated += (_, __) => { if (!closing) Close(); };
        }
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        private void Move(int delta) { index = (index + delta + colours.Length) % colours.Length; UpdateColour(); }
        private void UpdateColour() { Background = colours[index]; name.Text = names[index] + " · Click / Space / → next · ← previous · H hide help · Esc exit"; }
    }
}
