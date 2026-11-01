using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CNSysInfo
{
    internal sealed class MouseTestWindow : Window
    {
        private readonly MouseDiagnosticState state = new MouseDiagnosticState();
        private readonly TextBlock status = new TextBlock { Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap };
        private readonly Canvas surface = new Canvas { Background = Brushes.WhiteSmoke, MinHeight = 220, ClipToBounds = true, Focusable = true };
        private readonly Polyline trail = new Polyline { Stroke = Brushes.DodgerBlue, StrokeThickness = 2, IsHitTestVisible = false };
        public MouseTestWindow()
        {
            DiagnosticUi.Apply(this); Title = "Mouse / touchpad test"; Width = Math.Min(650, SystemParameters.WorkArea.Width); Height = Math.Min(440, SystemParameters.WorkArea.Height);
            Background = Brushes.White; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var body = new DockPanel { Margin = new Thickness(16) };
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) }; DockPanel.SetDock(header, Dock.Top);
            var reset = new Button { Content = "Reset", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(12, 0, 0, 0) };
            reset.Click += (_, __) => { state.Reset(); trail.Points.Clear(); UpdateStatus(); }; DockPanel.SetDock(reset, Dock.Right); header.Children.Add(reset);
            header.Children.Add(new TextBlock { Text = "Move, click, double-click and scroll inside the panel. Touchpad gestures depend on Windows settings.", TextWrapping = TextWrapping.Wrap }); body.Children.Add(header);
            DockPanel.SetDock(status, Dock.Bottom); body.Children.Add(status);
            surface.Children.Add(trail); body.Children.Add(new Border { Child = surface, BorderBrush = Brushes.LightSteelBlue, BorderThickness = new Thickness(1) }); Content = body;
            surface.MouseMove += (_, args) =>
            {
                var point = args.GetPosition(surface);
                if (trail.Points.Count >= 1200) trail.Points.RemoveAt(0);
                trail.Points.Add(point); state.Moves++; UpdateStatus();
            };
            surface.MouseDown += (_, args) => { surface.Focus(); state.Press((int)args.ChangedButton, args.ClickCount == 2); UpdateStatus(); args.Handled = true; };
            surface.MouseUp += (_, args) => { state.Release((int)args.ChangedButton); UpdateStatus(); args.Handled = true; };
            surface.MouseWheel += (_, args) => { state.Scroll(args.Delta); UpdateStatus(); args.Handled = true; };
            surface.MouseLeave += (_, __) => { state.LoseFocus(); UpdateStatus(); };
            Deactivated += (_, __) => { state.LoseFocus(); UpdateStatus(); };
            UpdateStatus();
        }
        private void UpdateStatus()
        {
            status.Text = "Left: " + state.Clicks(0) + "   Right: " + state.Clicks(2) + "   Middle: " + state.Clicks(1) +
                "   Double-clicks: " + state.DoubleClicks + "\nWheel ↑: " + state.WheelUp + " / ↓: " + state.WheelDown + "   Moves: " + state.Moves +
                "   Held: " + (state.AnyHeld ? "Yes" : "None") + "\nThese are received input events, not an automatic hardware-health verdict.";
        }
    }
}
