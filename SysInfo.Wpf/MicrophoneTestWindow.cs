using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CNSysInfo
{
    internal sealed class MicrophoneTestWindow : Window
    {
        private readonly ComboBox devices = new ComboBox { DisplayMemberPath = "Name", Margin = new Thickness(0, 10, 8, 10) };
        private readonly TextBlock status = new TextBlock { Text = "Opening live mic activity…", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 8) };
        private readonly ProgressBar level = new ProgressBar { Minimum = 0, Maximum = 100, Height = 10, Foreground = Brushes.SeaGreen };
        private readonly Canvas waveform = new Canvas { Height = 90, Background = Brushes.WhiteSmoke, ClipToBounds = true, Margin = new Thickness(0, 10, 0, 0) };
        private readonly Polyline trace = new Polyline { Stroke = Brushes.DodgerBlue, StrokeThickness = 2 };
        private readonly Queue<int> history = new Queue<int>(); // Levels only, never retained audio samples.
        private readonly Button pause = new Button { Content = "Pause", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 10, 0, 10) };
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        private readonly bool enableLiveInput;
        private MicrophoneActivityInput input;
        private bool loaded, closed, paused, releaseFailed;
        public MicrophoneTestWindow() : this(true) { }
        internal MicrophoneTestWindow(bool enableLiveInput)
        {
            this.enableLiveInput = enableLiveInput;
            DiagnosticUi.Apply(this); Title = "Mic test"; Width = Math.Min(500, SystemParameters.WorkArea.Width); SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize; Background = Brushes.White; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var body = new StackPanel { Margin = new Thickness(18) };
            body.Children.Add(new TextBlock { Text = "Speak and watch the live activity", FontSize = 15, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            var controls = new DockPanel(); DockPanel.SetDock(pause, Dock.Right); controls.Children.Add(pause); controls.Children.Add(devices); body.Children.Add(controls);
            body.Children.Add(level); waveform.Children.Add(trace); body.Children.Add(waveform); body.Children.Add(status);
            body.Children.Add(new TextBlock { Text = "The mic listens automatically while this window is active. Audio is used only to calculate activity and is immediately discarded—no recording, playback, files or uploads. Leaving this window pauses the mic. A flat line can also mean a muted, quiet or unavailable input.", Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
            Content = body;
            pause.Click += (_, __) => { paused = !paused; if (paused) StopInput("Paused · mic is off"); else StartInput(); pause.Content = paused ? "Resume" : "Pause"; };
            devices.SelectionChanged += (_, __) => { if (loaded) { StopInput("Switching input…"); StartInput(); } };
            waveform.SizeChanged += (_, __) => Draw(); timer.Tick += (_, __) => Tick();
            Loaded += (_, __) =>
            {
                try { devices.ItemsSource = MicrophoneActivityInput.Devices(); devices.SelectedIndex = devices.Items.Count > 0 ? 0 : -1; loaded = true;
                    if (devices.Items.Count == 0) { pause.IsEnabled = false; status.Text = "No microphone found. Connect one, then reopen this test."; }
                    else if (!enableLiveInput) status.Text = "Live input disabled in this UI verification fixture.";
                    else StartInput(); }
                catch (Exception) { pause.IsEnabled = false; status.Text = "Microphone inputs could not be listed. Check the connection and Windows microphone permissions."; }
            };
            Activated += (_, __) => StartInput();
            Deactivated += (_, __) => StopInput("Paused · mic is off outside this window");
            Closed += (_, __) => { closed = true; StopInput("Mic is off"); history.Clear(); trace.Points.Clear(); };
        }
        private void StartInput()
        {
            if (!loaded || closed || paused || releaseFailed || !enableLiveInput || !IsActive || input != null) return;
            var device = devices.SelectedItem as MicrophoneDevice; if (device == null) return;
            try { input = new MicrophoneActivityInput(device.Id); status.Text = "Listening · speak into the selected mic"; timer.Start(); }
            catch (Exception) { StopInput("Mic unavailable. It may be muted, busy, disconnected or blocked. Check Windows microphone permissions and desktop-app access."); }
        }
        private void Tick()
        {
            if (input == null) return;
            try
            {
                input.Poll(); int peak = input.Peak; level.Value = peak; history.Enqueue(peak); while (history.Count > 64) history.Dequeue(); Draw();
                status.Text = "Listening · live activity " + peak + "%";
            }
            catch (Exception) { StopInput("Mic input stopped. Check its connection and Windows permissions, then reopen this test."); }
        }
        private void Draw()
        {
            trace.Points.Clear(); var points = history.ToArray(); double middle = waveform.ActualHeight / 2;
            if (points.Length == 0) { trace.Points.Add(new Point(0, middle)); trace.Points.Add(new Point(waveform.ActualWidth, middle)); return; }
            for (int i = 0; i < points.Length; i++)
            {
                double x = i * waveform.ActualWidth / Math.Max(1, points.Length - 1), magnitude = points[i] * Math.Max(0, middle - 3) / 100;
                trace.Points.Add(new Point(x, middle - magnitude)); trace.Points.Add(new Point(x, middle + magnitude));
            }
        }
        private void StopInput(string message)
        {
            timer.Stop();
            if (input != null) { input.Dispose(); releaseFailed |= input.ReleaseFailed; input = null; }
            level.Value = 0; history.Clear(); Draw();
            status.Text = releaseFailed ? "Windows could not release the mic. Close SysInfo before testing it again." : message;
            if (releaseFailed) pause.IsEnabled = false;
        }
    }
}
