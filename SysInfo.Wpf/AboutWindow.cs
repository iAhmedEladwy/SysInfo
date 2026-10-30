using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CNSysInfo
{
    internal sealed class AboutWindow : Window
    {
        private const string Email = "eladwy.ahmed@gmail.com";
        private static readonly Brush Navy = ColorBrush("#0B1F3A");
        private static readonly Brush Slate = ColorBrush("#5B6B80");
        private static readonly Brush Ice = ColorBrush("#EAF6FB");

        public AboutWindow()
        {
            Title = "About SysInfo";
            Width = 440;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;
            Foreground = Navy;
            FontFamily = new FontFamily("Segoe UI");
            var body = new StackPanel { Margin = new Thickness(24) };
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("branding-logo.png"))
            {
                if (stream != null)
                {
                    var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    bitmap.Freeze();
                    var logo = new Image { Source = bitmap, Width = 320, Height = 60, Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 24) };
                    System.Windows.Automation.AutomationProperties.SetName(logo, "ELADWYSOFT IT SOLUTIONS logo");
                    body.Children.Add(logo);
                }
            }
            body.Children.Add(new TextBlock { Text = "SysInfo", FontSize = 23, FontWeight = FontWeights.SemiBold });
            body.Children.Add(new TextBlock { Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3),
                Foreground = Slate, FontSize = 12, Margin = new Thickness(0, 2, 0, 12) });
            body.Children.Add(new TextBlock { Text = "Windows system information and hardware health.",
                FontSize = 12, Foreground = Slate, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 18) });
            body.Children.Add(Detail("Developed by", new TextBlock { Text = "Ahmed Eladwy" }));
            body.Children.Add(Detail("Company", new TextBlock { Text = "ELADWYSOFT" }));
            var email = new TextBlock();
            email.Inlines.Add(Link(Email, "mailto:" + Email + "?subject=SysInfo"));
            body.Children.Add(Detail("Email", email));
            var github = new TextBlock { TextWrapping = TextWrapping.Wrap };
            github.Inlines.Add(Link("github.com/iAhmedEladwy/SysInfo", "https://github.com/iAhmedEladwy/SysInfo"));
            body.Children.Add(Detail("GitHub", github));
            var project = new TextBlock();
            project.Inlines.Add(Link("GitHub releases", "https://github.com/iAhmedEladwy/SysInfo/releases"));
            body.Children.Add(Detail("Project", project));
            body.Children.Add(new Border
            {
                Background = Ice, CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 16, 0, 0),
                Child = new TextBlock { Text = "صدقة جارية لوالدي\nنسألكم الدعاء له بالرحمة والمغفرة.", FlowDirection = FlowDirection.RightToLeft,
                    TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = 14, Foreground = Navy }
            });
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0) };
            var copy = ActionButton("Copy email");
            copy.Margin = new Thickness(0, 0, 8, 0);
            copy.Click += (_, __) =>
            {
                try { Clipboard.SetText(Email); }
                catch (Exception) { MessageBox.Show(this, "The clipboard is busy. Please try again.", "Copy email"); }
            };
            var close = ActionButton("Close");
            close.IsCancel = true;
            close.IsDefault = true;
            close.Click += (_, __) => Close();
            actions.Children.Add(copy); actions.Children.Add(close); body.Children.Add(actions);
            Content = body;
        }

        private static Brush ColorBrush(string color)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(color);
            brush.Freeze();
            return brush;
        }

        private static Grid Detail(string label, UIElement value)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new TextBlock { Text = label, Foreground = Slate, FontSize = 12 });
            Grid.SetColumn(value, 1); row.Children.Add(value); return row;
        }

        private Hyperlink Link(string label, string address)
        {
            var link = new Hyperlink(new Run(label)) { NavigateUri = new Uri(address), Foreground = Navy, ToolTip = address };
            link.RequestNavigate += (_, args) =>
            {
                args.Handled = true;
                try { Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); }
                catch (Exception) { MessageBox.Show(this, "Windows could not open this link. Please try opening it in your browser or email app.", "Open link"); }
            };
            return link;
        }

        private static Button ActionButton(string label) => new Button
        {
            Content = label, Padding = new Thickness(12, 6, 12, 6), MinWidth = 76,
            Background = Ice, Foreground = Navy, BorderBrush = Ice
        };
    }
}
