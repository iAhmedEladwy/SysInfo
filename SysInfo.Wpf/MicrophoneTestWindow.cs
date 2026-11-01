using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CNSysInfo
{
    internal sealed class MicrophoneTestWindow : Window
    {
        private readonly ComboBox devices = new ComboBox { DisplayMemberPath = "Name", Margin = new Thickness(0, 10, 0, 10) };
        private readonly TextBlock status = new TextBlock { Text = "Microphone is off. Record only when ready.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 8) };
        private readonly ProgressBar level = new ProgressBar { Minimum = 0, Maximum = 100, Height = 10, Foreground = Brushes.SeaGreen };
        private readonly Button record = Action("Record (up to 5 sec)"), play = Action("Play recording"), stop = Action("Stop");
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        private MicrophoneCapture capture;
        private SoundPlayer player;
        private MemoryStream playback;
        private byte[] recording;
        private DateTime started;
        private bool playing, releaseFailed;
        public MicrophoneTestWindow()
        {
            DiagnosticUi.Apply(this); Title = "Microphone test"; Width = Math.Min(500, SystemParameters.WorkArea.Width); SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize; Background = Brushes.White; FontFamily = new FontFamily("Segoe UI"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var body = new StackPanel { Margin = new Thickness(18) };
            body.Children.Add(new TextBlock { Text = "Record, watch the input level, then listen back.", FontSize = 15, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(devices); body.Children.Add(level);
            var actions = new WrapPanel(); actions.Children.Add(record); actions.Children.Add(play); actions.Children.Add(stop); body.Children.Add(actions); body.Children.Add(status);
            body.Children.Add(new TextBlock { Text = "Recording starts only when you click Record. Up to five seconds stay in memory; nothing is saved or uploaded. Leaving this window stops recording/playback; closing it clears the sample. Playback uses your default Windows output. Use headphones to avoid feedback.", Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
            Content = body; play.IsEnabled = stop.IsEnabled = false;
            record.Click += (_, __) => StartRecording(); play.Click += (_, __) => PlayRecording(); stop.Click += (_, __) => StopAll();
            timer.Tick += (_, __) => Tick();
            Loaded += (_, __) =>
            {
                try { devices.ItemsSource = MicrophoneCapture.Devices(); devices.SelectedIndex = devices.Items.Count > 0 ? 0 : -1; record.IsEnabled = devices.Items.Count > 0;
                    if (!record.IsEnabled) status.Text = "No microphone input found. Connect a microphone, close and reopen this test."; }
                catch (Exception) { record.IsEnabled = false; status.Text = "Microphones could not be listed. Check Windows microphone permissions and connections."; }
            };
            Deactivated += (_, __) => StopAll(); Closed += (_, __) => { StopAll(); ClearRecording(); };
        }
        private void StartRecording()
        {
            StopAll(); ClearRecording();
            var device = devices.SelectedItem as MicrophoneDevice; if (device == null) return;
            try
            {
                capture = new MicrophoneCapture(device.Id); started = DateTime.UtcNow; devices.IsEnabled = record.IsEnabled = false; stop.IsEnabled = true;
                status.Text = "Recording… Speak into the selected microphone."; timer.Start();
            }
            catch (Exception) { StopAll(); status.Text = "Could not open this microphone. It may be busy, disconnected or blocked. Check Windows Settings > Privacy > Microphone and desktop-app access, then retry."; }
        }
        private void Tick()
        {
            try
            {
                if (capture != null)
                {
                    capture.Poll(); level.Value = capture.Peak;
                    if (capture.Complete || (DateTime.UtcNow - started).TotalSeconds >= 5) StopAll();
                    else status.Text = "Recording · input peak " + capture.Peak + "% · " + (DateTime.UtcNow - started).TotalSeconds.ToString("0.0") + " / 5 sec";
                }
                else if (playing && (DateTime.UtcNow - started).TotalSeconds >= (recording.Length - 44) / (MicrophoneWave.SampleRate * 2d) + 0.1) StopAll();
            }
            catch (Exception) { StopAll(); status.Text = "Microphone input stopped. Check its connection and try again."; }
        }
        private void PlayRecording()
        {
            StopAll(); if (recording == null) return;
            try { playback = new MemoryStream(recording, false); player = new SoundPlayer(playback); player.Load(); player.Play(); playing = true; started = DateTime.UtcNow;
                stop.IsEnabled = true; status.Text = "Playing your recording. Listen for clarity, noise or dropouts."; timer.Start(); }
            catch (Exception) { StopAll(); status.Text = "Could not play the recording. Check the default Windows audio output."; }
        }
        private void StopAll()
        {
            timer.Stop();
            bool attemptedRecording = capture != null;
            if (capture != null)
            {
                try { recording = capture.Finish(); } catch (Exception) { status.Text = "Recording stopped. No usable sample was returned."; }
                finally { capture.Dispose(); releaseFailed |= capture.ReleaseFailed; capture = null; }
            }
            if (player != null) { player.Stop(); player.Dispose(); player = null; }
            playback?.Dispose(); playback = null; playing = false; level.Value = 0;
            devices.IsEnabled = true; record.IsEnabled = !releaseFailed && devices.Items.Count > 0; play.IsEnabled = recording != null; stop.IsEnabled = false;
            if (releaseFailed) { status.Text = "Windows could not release the microphone device. Close SysInfo before testing it again."; return; }
            status.Text = recording == null ? (attemptedRecording ? "No input sample received. Check the selected microphone, connection and Windows permissions." : "Microphone is off. Record only when ready.") : "Stopped. Recording is held in memory until you record again or close this window.";
        }
        private void ClearRecording() { if (recording != null) Array.Clear(recording, 0, recording.Length); recording = null; play.IsEnabled = false; }
        private static Button Action(string label) => new Button { Content = label, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 10, 8, 0) };
    }
}
