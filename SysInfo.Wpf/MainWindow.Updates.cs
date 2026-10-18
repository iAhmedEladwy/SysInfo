using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CNSysInfo
{
    public partial class MainWindow
    {
        private bool _checkingUpdates;

        private async void UpdateCheck_Click(object sender, RoutedEventArgs e) => await CheckForUpdates(false);

        private async Task CheckForUpdates(bool startup)
        {
            if (_checkingUpdates) return;
            _checkingUpdates = true;
            bool installing = false;
            const string checking = "Checking for updates...";
            string previousStatus = lblStatus.Text;
            string previousDetails = _collectionDetails;
            bool previousActionable = btnStatusDetails.IsEnabled;
            try
            {
                var preferences = UpdatePreferences.Load();
                if (startup && !preferences.CheckOnStartup) return;
                SetCollectionState(checking, "Checking GitHub for a newer SysInfo release.", false);
                var update = await new UpdateClient().CheckAsync(typeof(App).Assembly.GetName().Version,
                    startup ? preferences.SkippedVersion : string.Empty);
                if (lblStatus.Text == checking) SetCollectionState(previousStatus, previousDetails, previousActionable);
                if (update == null || !IsVisible) return;
                var offer = new UpdateOfferWindow(update, preferences) { Owner = this };
                if (offer.ShowDialog() != true) return;
                installing = true;
                SetBusy(true);
                SetCollectionState("Downloading update...", "Downloading and verifying the selected GitHub release.", false);
                string directory = Path.Combine(UpdateInstaller.CacheRoot, Guid.NewGuid().ToString("N"));
                var package = await new UpdateClient().DownloadAsync(update, directory);
                string stage = await Task.Run(() => UpdateInstaller.StagePackage(package, directory, update.VersionText));
                UpdateInstaller.LaunchHelper(stage, update.VersionText);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Update check/download failed: " + ex.GetType().Name);
                // Startup network/offline/rate-limit failures stay silent. A selected
                // installation failure is shown by the download state below.
                if (installing)
                {
                    SetCollectionState("Update failed", "The update was not installed. " + ex.Message, true);
                    MessageBox.Show(this, "The update was not installed. The current app remains available.\n\n" + ex.Message,
                        "SysInfo update", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                _checkingUpdates = false;
                if (lblStatus.Text == checking) SetCollectionState(previousStatus, previousDetails, previousActionable);
                if (installing && IsVisible) SetBusy(false);
            }
        }

        private void UpdateSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = UpdatePreferences.Load();
                var dialog = new Window { Title = "Update settings", Owner = this, Width = 380, SizeToContent = SizeToContent.Height,
                    ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Brushes.White, Foreground = InkBrush };
                var panel = new StackPanel { Margin = new Thickness(18) };
                var enabled = new CheckBox { Content = "Check for updates on startup", IsChecked = settings.CheckOnStartup, Margin = new Thickness(0, 0, 0, 12) };
                panel.Children.Add(enabled);
                var clear = new CheckBox { Content = "Clear skipped release", Margin = new Thickness(0, 0, 0, 12) };
                panel.Children.Add(clear);
                var save = ToolButton("Save");
                save.Click += (_, __) =>
                {
                    try { settings.CheckOnStartup = enabled.IsChecked == true; if (clear.IsChecked == true) settings.SkippedVersion = string.Empty; settings.Save(); dialog.Close(); }
                    catch (Exception) { MessageBox.Show(dialog, "Update preferences could not be saved for this Windows user.", "Update settings"); }
                };
                panel.Children.Add(save); dialog.Content = panel; dialog.ShowDialog();
            }
            catch (Exception) { MessageBox.Show(this, "Update preferences could not be saved for this Windows user.", "Update settings"); }
        }
    }

    internal sealed class UpdateOfferWindow : Window
    {
        public UpdateOfferWindow(AppUpdate update, UpdatePreferences settings)
        {
            Title = "SysInfo update available"; Width = 430; SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White; Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "SysInfo " + update.VersionText + " is available", FontSize = 18, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock { Text = "Installed: " + typeof(App).Assembly.GetName().Version.ToString(3) +
                "\nDownload, verify and restart SysInfo to update. Your preferences are retained.", Margin = new Thickness(0, 10, 0, 14), TextWrapping = TextWrapping.Wrap });
            var skip = new CheckBox { Content = "Skip this release", Margin = new Thickness(0, 0, 0, 8) };
            var disable = new CheckBox { Content = "Don't check for updates automatically", Margin = new Thickness(0, 0, 0, 18) };
            panel.Children.Add(skip); panel.Children.Add(disable);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var later = new Button { Content = "Not now", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
            var install = new Button { Content = "Update now", Padding = new Thickness(12, 6, 12, 6) };
            install.Click += (_, __) => { skip.IsChecked = false; DialogResult = true; };
            buttons.Children.Add(later); buttons.Children.Add(install); panel.Children.Add(buttons); Content = panel;
            Closing += (_, args) =>
            {
                settings.CheckOnStartup = disable.IsChecked != true;
                if (skip.IsChecked == true) settings.SkippedVersion = update.VersionText;
                try { settings.Save(); }
                catch (Exception) { args.Cancel = true; MessageBox.Show(this, "Update preferences could not be saved. Please try again.", "SysInfo update"); }
            };
        }
    }
}
