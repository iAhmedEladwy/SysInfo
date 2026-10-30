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
        private readonly ComboBox sample = new ComboBox { SelectedIndex = 0, MinWidth = 230, Margin = new Thickness(0, 8, 0, 8) };
        private readonly TextBlock status = new TextBlock { Text = "Ready. Compare the same sample on left and right.", TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock guidance = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 0, 0, 8) };
        private readonly Button stop = Action("■  Stop");
        public StereoSpeakerTestWindow()
        {
            DiagnosticUi.Apply(this);
            Title = "Speaker quality test"; Width = Math.Min(530, SystemParameters.WorkArea.Width);
            SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White; Foreground = (Brush)new BrushConverter().ConvertFromString("#0B1F3A"); FontFamily = new FontFamily("Segoe UI"); FontSize = 12;
            var body = new StackPanel { Margin = new Thickness(18) };
            body.Children.Add(new TextBlock { Text = "Output: current default Windows playback device", FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = "Start at low Windows volume, then increase gradually to a comfortable level. This test does not change system volume.", Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap });
            sample.Items.Add("Frequency sweep · 180 Hz–6 kHz · 5 sec");
            sample.Items.Add("Music-like sample · melody / harmonics · 6 sec");
            sample.Items.Add("Channel check · 440 Hz tone · 1.2 sec");
            sample.SelectedIndex = 0;
            System.Windows.Automation.AutomationProperties.SetName(sample, "Speaker test sample");
            sample.SelectionChanged += (_, __) => { Stop(); UpdateGuidance(); };
            body.Children.Add(sample); body.Children.Add(guidance);
            var channels = new Grid(); channels.ColumnDefinitions.Add(new ColumnDefinition()); channels.ColumnDefinitions.Add(new ColumnDefinition()); channels.ColumnDefinitions.Add(new ColumnDefinition());
            var left = Action("▶  Left"); left.Click += (_, __) => Play(SpeakerChannel.Left);
            var right = Action("▶  Right"); right.Click += (_, __) => Play(SpeakerChannel.Right); Grid.SetColumn(right, 1);
            var both = Action("▶  Both"); both.Click += (_, __) => Play(SpeakerChannel.Both); Grid.SetColumn(both, 2);
            channels.Children.Add(left); channels.Children.Add(right); channels.Children.Add(both); body.Children.Add(channels);
            stop.IsEnabled = false; stop.Click += (_, __) => Stop(); body.Children.Add(stop);
            status.Margin = new Thickness(0, 8, 0, 8); body.Children.Add(status);
            body.Children.Add(new TextBlock { Text = "Compare clarity, balance and any buzzing, crackling or rattling between speakers. This is a listening check, not an automatic quality score. Built-in speakers may reproduce low/high frequencies weakly. Windows mono audio, balance, enhancements or HDMI/headphone routing can affect results.", Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
            Content = body;
            timer.Tick += (_, __) => Stop();
            Deactivated += (_, __) => Stop(); Closed += (_, __) => Stop();
            UpdateGuidance();
        }
        private SpeakerSample SelectedSample => sample.SelectedIndex == 1 ? SpeakerSample.Music : sample.SelectedIndex == 2 ? SpeakerSample.ChannelTone : SpeakerSample.FrequencySweep;
        private void UpdateGuidance()
        {
            guidance.Text = SelectedSample == SpeakerSample.FrequencySweep ? "Listen for extra buzzing or rattling as pitch rises. A clean sweep changes pitch smoothly."
                : SelectedSample == SpeakerSample.Music ? "Compare the melody on each speaker. Listen for crackling, roughness or distortion on individual notes."
                : "Check that the selected speaker is audible. Use the sweep and music-like sample to assess sound quality.";
        }
        private void Play(SpeakerChannel channel)
        {
            Stop();
            try
            {
                audio = new MemoryStream(StereoTestTone.CreateWave(SelectedSample, channel), false);
                player = new SoundPlayer(audio); player.Load(); player.Play();
                stop.IsEnabled = true; status.Text = "Playing " + channel.ToString().ToLowerInvariant() + " · " + sample.SelectedItem;
                timer.Interval = TimeSpan.FromSeconds(StereoTestTone.Duration(SelectedSample) + 0.1); timer.Start();
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
            stop.IsEnabled = false; status.Text = "Stopped. Select a channel to compare the same sample.";
        }
        private static Button Action(string text) => new Button { Content = text, MinWidth = 85, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(3, 4, 3, 4) };
    }
}
