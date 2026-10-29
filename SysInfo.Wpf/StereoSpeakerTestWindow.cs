using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CNSysInfo
{
    internal sealed class StereoSpeakerTestWindow : Window
    {
        private SoundPlayer player;
        private MemoryStream audio;
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly TextBlock status = new TextBlock { Text = "Ready. Choose a channel to play a short tone.", TextWrapping = TextWrapping.Wrap };
        private readonly Button stop = Action("Stop");
        public StereoSpeakerTestWindow()
        {
            DiagnosticUi.Apply(this);
            Title = "Stereo speaker test"; Width = Math.Min(470, SystemParameters.WorkArea.Width);
            SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White; Foreground = (Brush)new BrushConverter().ConvertFromString("#0B1F3A"); FontFamily = new FontFamily("Segoe UI"); FontSize = 13;
            var body = new StackPanel { Margin = new Thickness(20) };
            body.Children.Add(new TextBlock { Text = "Output: current default Windows playback device", FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = "Start with low system volume. This test does not change it.", Margin = new Thickness(0, 8, 0, 12), TextWrapping = TextWrapping.Wrap });
            var channels = new Grid(); channels.ColumnDefinitions.Add(new ColumnDefinition()); channels.ColumnDefinitions.Add(new ColumnDefinition());
            var left = Action("▶  Play Left"); left.Click += (_, __) => Play(true);
            var right = Action("▶  Play Right"); right.Click += (_, __) => Play(false); Grid.SetColumn(right, 1);
            channels.Children.Add(left); channels.Children.Add(right); body.Children.Add(channels);
            stop.IsEnabled = false; stop.Click += (_, __) => Stop(); body.Children.Add(stop);
            status.Margin = new Thickness(0, 12, 0, 12); body.Children.Add(status);
            body.Children.Add(new TextBlock { Text = "Listen for each channel yourself. Headphones, HDMI/Bluetooth output, balance and Windows mono-audio settings can affect what you hear. Silence does not automatically mean a failed speaker.", Foreground = (Brush)new BrushConverter().ConvertFromString("#5B6B80"), TextWrapping = TextWrapping.Wrap });
            var close = Action("Close"); close.HorizontalAlignment = HorizontalAlignment.Right; close.IsCancel = true; close.Click += (_, __) => Close(); body.Children.Add(close);
            Content = body;
            timer.Interval = TimeSpan.FromSeconds(StereoTestTone.DurationSeconds + 0.1);
            timer.Tick += (_, __) => Stop();
            Deactivated += (_, __) => Stop(); Closed += (_, __) => Stop();
        }
        private void Play(bool left)
        {
            Stop();
            try
            {
                audio = new MemoryStream(StereoTestTone.CreateWave(left), false);
                player = new SoundPlayer(audio); player.Load(); player.Play();
                stop.IsEnabled = true; status.Text = "Playing " + (left ? "left" : "right") + " channel. Only this channel contains a tone.";
                timer.Start();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is System.ComponentModel.Win32Exception || ex is TimeoutException)
            {
                Stop(); status.Text = "Could not play audio. Check the Windows output device, volume and connection, then try again.";
            }
        }
        private void Stop()
        {
            timer.Stop();
            if (player != null) { player.Stop(); player.Dispose(); player = null; }
            if (audio != null) { audio.Dispose(); audio = null; }
            stop.IsEnabled = false;
            status.Text = "Stopped. Select a channel to test again.";
        }
        private static Button Action(string text) => new Button { Content = text, MinWidth = 95, Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(3, 5, 3, 5) };
    }
}
