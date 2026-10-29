using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CNSysInfo
{
    // UI construction (code-built equivalent of MainWindow.xaml + App.xaml styles,
    // same layout, colors, fonts and templates so the window looks identical).
    public partial class MainWindow : Window
    {
        // Fixed light theme, independent of the Windows appearance setting.
        private static readonly Brush BgBrush = Freeze("#F1F5F9");
        private static readonly Brush InkBrush = Freeze("#0F172A");
        private static readonly Brush MutedBrush = Freeze("#64748B");
        private static readonly Brush AccentBrush = Freeze("#2563EB");
        private static readonly Brush LineBrush = Freeze("#E2E8F0");
        private static readonly Brush BannerBrush = Freeze("#D3D3D3");

        private static Brush Freeze(string hex)
        {
            var b = (Brush)new BrushConverter().ConvertFromString(hex);
            b.Freeze();
            return b;
        }

        // Same ControlTemplates as App.xaml, parsed at runtime (identical rendering).
        private static readonly ControlTemplate ButtonTemplate = TryParseTemplate(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
            "<Border x:Name='b' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' " +
            "BorderThickness='1' CornerRadius='8' Padding='{TemplateBinding Padding}'>" +
            "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border>" +
            "<ControlTemplate.Triggers>" +
            "<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='b' Property='Background' Value='#E0E7FF'/></Trigger>" +
            "<Trigger Property='IsEnabled' Value='False'><Setter TargetName='b' Property='Opacity' Value='0.5'/></Trigger>" +
            "</ControlTemplate.Triggers></ControlTemplate>");

        private static readonly ControlTemplate BarTemplate = TryParseTemplate(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ProgressBar'>" +
            "<Grid><Border Background='{TemplateBinding Background}' CornerRadius='5'/>" +
            "<Border x:Name='PART_Track'/>" +
            "<Border x:Name='PART_Indicator' HorizontalAlignment='Left' Background='{TemplateBinding Foreground}' CornerRadius='5'/>" +
            "</Grid></ControlTemplate>");

        // Templates are parsed at runtime; if that ever fails, fall back to the
        // standard control look instead of taking the whole window down at startup.
        private static ControlTemplate TryParseTemplate(string xaml)
        {
            try { return (ControlTemplate)XamlReader.Parse(xaml); }
            catch (Exception ex) { Debug.WriteLine("Template parse failed, using default look: " + ex); return null; }
        }

        // ---- named elements (same names as the XAML) ----
        private TextBlock lblVersion;
        private TextBlock lblStatus;
        private Button btnStatusDetails;
        private Border toolbar;
        private Button btnRefresh;
        private Button btnExport;
        private Button btnCopy;
        private Button btnTools;
        private Button btnPower;
        private Button btnTests;
        private StackPanel content;
        private Border ratingBanner;
        private TextBlock ratingCaption;
        private TextBlock txtRating;
        private TextBlock txtTimeZone;
        private Grid ratingLayout;
        private Border ratingBadge;
        private Border restartBadge;
        private Border memoryWarningBadge, systemDriveWarningBadge;
        private TextBlock txtPc, txtUser, txtMaker, txtModel, txtSerial, txtAsset;
        private TextBlock txtOs, txtAct, txtUptime, txtBios;
        private TextBlock txtBattery, txtCpu, txtCpuCache, txtMem, txtStorage, txtGpu, txtScreen;
        private ProgressBar pbBattery, pbMem, pbDisk;
        private TextBlock lblBattery, lblMem, lblDisk;
        private Border statusBorder;
        private ScrollViewer contentScroll;
        private bool _initialFitCompleted;
        private bool _userAdjustedWindow;

        private static WrapPanel ValueLine(params UIElement[] values)
        {
            var line = new WrapPanel();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) line.Children.Add(new TextBlock { Text = "  ·  ", Foreground = MutedBrush, FontSize = 12 });
                line.Children.Add(values[i]);
            }
            return line;
        }

        private static Grid MeterRow(ProgressBar bar, TextBlock caption, Border warning = null)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.Margin = new Thickness(0, 0, 10, 0);
            bar.VerticalAlignment = VerticalAlignment.Center;
            caption.Margin = new Thickness(0);
            caption.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(caption, 1);
            row.Children.Add(bar);
            row.Children.Add(caption);
            if (warning != null)
            {
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                warning.Margin = new Thickness(8, 0, 0, 0);
                Grid.SetColumn(warning, 2);
                row.Children.Add(warning);
            }
            return row;
        }

        private static Border SheetSection(string title, params UIElement[] rows)
        {
            var stack = new StackPanel();
            stack.Children.Add(CardTitle(title));
            foreach (var row in rows) stack.Children.Add(row);
            return new Border
            {
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6, 0, 6),
                Child = stack
            };
        }

        private static Border Badge(UIElement child)
        {
            return new Border
            {
                Background = BgBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2, 6, 2),
                Child = child,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static Border ResourceWarningBadge()
        {
            var badge = Badge(new TextBlock { FontSize = 10, FontWeight = FontWeights.SemiBold });
            badge.Padding = new Thickness(5, 0, 5, 0);
            badge.Visibility = Visibility.Collapsed;
            return badge;
        }

        private static void SetResourceWarning(Border badge, ResourcePressure pressure, string label, string explanation)
        {
            badge.Visibility = pressure == ResourcePressure.None ? Visibility.Collapsed : Visibility.Visible;
            var text = (TextBlock)badge.Child;
            text.Text = label;
            text.Foreground = Freeze(pressure == ResourcePressure.Critical ? "#991B1B" : "#92400E");
            badge.Background = Freeze(pressure == ResourcePressure.Critical ? "#FEE2E2" : "#FEF3C7");
            badge.ToolTip = new ToolTip { Content = new TextBlock { Text = explanation, MaxWidth = 340, TextWrapping = TextWrapping.Wrap } };
            System.Windows.Automation.AutomationProperties.SetName(badge, label);
            System.Windows.Automation.AutomationProperties.SetHelpText(badge, explanation);
        }

        private static Grid SectionHeading(string title, UIElement badge)
        {
            var heading = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            heading.ColumnDefinitions.Add(new ColumnDefinition());
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MaxWidth = 240 });
            var label = CardTitle(title);
            label.VerticalAlignment = VerticalAlignment.Center;
            heading.Children.Add(label);
            Grid.SetColumn(badge, 1);
            heading.Children.Add(badge);
            return heading;
        }

        private static Border OsSection(UIElement heading, params UIElement[] rows)
        {
            var stack = new StackPanel();
            stack.Children.Add(heading);
            foreach (var row in rows) stack.Children.Add(row);
            return new Border
            {
                BorderBrush = LineBrush, BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6, 0, 6), Child = stack
            };
        }

        private void ArrangeRating()
        {
            bool narrow = ratingLayout.ActualWidth < 640;
            ratingLayout.ColumnDefinitions[0].Width = new GridLength(narrow ? 0 : 110);
            ratingLayout.ColumnDefinitions[2].Width = new GridLength(narrow ? 0 : 110);
            Grid.SetColumn(txtRating, narrow ? 0 : 1);
            Grid.SetColumnSpan(txtRating, narrow ? 3 : 1);
            Grid.SetRow(txtRating, narrow ? 1 : 0);
            Grid.SetColumn(ratingBadge, narrow ? 0 : 2);
            Grid.SetColumnSpan(ratingBadge, narrow ? 3 : 1);
            txtRating.Margin = new Thickness(0, narrow ? 2 : 0, 0, 0);
        }

        private Rect WorkingArea()
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null) return SystemParameters.WorkArea;
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var area = screen.WorkingArea;
            return new MatrixTransform(source.CompositionTarget.TransformFromDevice).TransformBounds(
                new Rect(area.X, area.Y, area.Width, area.Height));
        }

        private void FitInitialContent()
        {
            if (_initialFitCompleted || _userAdjustedWindow || WindowState != WindowState.Normal) return;
            _initialFitCompleted = true;
            UpdateLayout();
            var work = WorkingArea();
            var availableWidth = Math.Max(1, contentScroll.ViewportWidth);
            // Narrower startup width must never grow the previous 800-wide window's height.
            // If unusually long hardware values wrap, the existing content scroll handles the overflow.
            var previousWidthDifference = Math.Max(0, Math.Min(800, work.Width) - ActualWidth);
            content.Measure(new Size(availableWidth + previousWidthDifference, double.PositiveInfinity));
            var previousContentHeight = content.DesiredSize.Height;
            content.Measure(new Size(availableWidth, double.PositiveInfinity));
            var chrome = Math.Max(0, ActualHeight - ((FrameworkElement)Content).ActualHeight);
            Height = Math.Min(work.Height, Math.Max(MinHeight,
                Math.Min(content.DesiredSize.Height, previousContentHeight) + toolbar.ActualHeight + statusBorder.ActualHeight + chrome + 12));
            Left = Math.Max(work.Left, work.Left + (work.Width - ActualWidth) / 2);
            Top = Math.Max(work.Top, work.Top + (work.Height - Height) / 2);
        }

        // ---- style helpers (same values as the App.xaml styles) ----
        private static Border Card()
        {
            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(4, 3, 4, 3),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1)
            };
        }

        private static TextBlock CardTitle(string text)
        {
            return new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = AccentBrush, Margin = new Thickness(0, 0, 0, 3) };
        }

        private static TextBlock RowValue(string name)
        {
            return new TextBlock { Text = "...", FontSize = 12, Foreground = InkBrush, TextWrapping = TextWrapping.Wrap };
        }

        private static Grid Row(string label, UIElement value)
        {
            var g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = MutedBrush, VerticalAlignment = VerticalAlignment.Top });
            Grid.SetColumn(value, 1);
            g.Children.Add(value);
            return g;
        }

        private static ProgressBar Bar()
        {
            return new ProgressBar
            {
                Height = 7,
                Minimum = 0,
                Maximum = 100,
                Margin = new Thickness(0, 5, 0, 0),
                Background = LineBrush,
                BorderThickness = new Thickness(0),
                Foreground = AccentBrush,
                Template = BarTemplate
            };
        }

        private static TextBlock BarCaption()
        {
            return new TextBlock { Text = "", FontSize = 10, Foreground = MutedBrush, Margin = new Thickness(0, 2, 0, 0) };
        }

        private static Button ToolButton(string text)
        {
            return new Button
            {
                Content = text,
                Padding = new Thickness(7, 4, 7, 4),
                FontSize = 11,
                Margin = new Thickness(0, 0, 6, 2),
                Background = Brushes.White,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                Foreground = InkBrush,
                Cursor = Cursors.Hand,
                Template = ButtonTemplate
            };
        }

        private MenuItem ToolMenuItem(string header, string tag, RoutedEventHandler onClick)
        {
            var item = new MenuItem { Header = header, Tag = tag };
            item.Click += onClick;
            return item;
        }

        private static StackPanel CardStack(params UIElement[] children)
        {
            var sp = new StackPanel();
            foreach (var c in children) sp.Children.Add(c);
            return sp;
        }

        private static Border CardOf(params UIElement[] children)
        {
            var b = Card();
            b.Child = CardStack(children);
            return b;
        }

        private void TryLoadIcon()
        {
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("info.ico"))
                {
                    if (s == null) return;
                    var bmp = BitmapFrame.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    bmp.Freeze();
                    Icon = bmp;
                }
            }
            catch (Exception ex) { Debug.WriteLine("Icon load failed (cosmetic, ignored): " + ex); }
        }

        private void BuildUi()
        {
            Title = "SysInfo";
            var work = SystemParameters.WorkArea;
            Width = Math.Min(760, work.Width);
            Height = Math.Min(720, work.Height);
            MinWidth = Math.Min(520, work.Width);
            MinHeight = Math.Min(400, work.Height);
            SourceInitialized += (_, __) =>
            {
                var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(this);
                source?.AddHook((IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                {
                    if (message == 0x0231) _userAdjustedWindow = true; // WM_ENTERSIZEMOVE
                    return IntPtr.Zero;
                });
            };
            Background = BgBrush;
            Foreground = InkBrush;
            FontFamily = new FontFamily("Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            TryLoadIcon();
            Loaded += Window_Loaded;
            KeyDown += Window_KeyDown;

            var root = new DockPanel();

            // status bar
            statusBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(10, 4, 10, 4)
            };
            DockPanel.SetDock(statusBorder, Dock.Bottom);
            var statusDock = new Grid();
            statusDock.ColumnDefinitions.Add(new ColumnDefinition());
            statusDock.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusDock.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lblVersion = new TextBlock { Foreground = MutedBrush, FontSize = 11 };
            lblVersion.VerticalAlignment = VerticalAlignment.Center;
            lblVersion.Margin = new Thickness(6, 0, 0, 0);
            txtTimeZone = new TextBlock { Foreground = MutedBrush, FontSize = 10,
                TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                Text = "Waiting for system information..." };
            lblStatus = new TextBlock { Foreground = MutedBrush, FontSize = 10, Text = "Ready" };
            btnStatusDetails = new Button { Content = lblStatus, Template = ButtonTemplate,
                Background = Brushes.White, BorderBrush = LineBrush, Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed };
            btnStatusDetails.Click += ShowCollectionDetails;
            System.Windows.Automation.AutomationProperties.SetName(btnStatusDetails, "Collection details");
            Grid.SetColumn(btnStatusDetails, 1);
            var about = new Button { Content = lblVersion, Background = Brushes.White, Foreground = MutedBrush,
                BorderThickness = new Thickness(0), Padding = new Thickness(0), Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center, ToolTip = "About SysInfo",
                Template = TryParseTemplate("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><ContentPresenter VerticalAlignment='Center'/></ControlTemplate>") };
            System.Windows.Automation.AutomationProperties.SetName(about, "About SysInfo");
            about.Click += (_, __) => new AboutWindow { Owner = this, Icon = Icon }.ShowDialog();
            Grid.SetColumn(about, 2);
            statusDock.Children.Add(txtTimeZone);
            statusDock.Children.Add(btnStatusDetails);
            statusDock.Children.Add(about);
            statusBorder.Child = statusDock;
            root.Children.Add(statusBorder);

            // toolbar
            toolbar = new Border { Padding = new Thickness(8, 8, 8, 2) };
            DockPanel.SetDock(toolbar, Dock.Top);
            var bar = new StackPanel { Orientation = Orientation.Horizontal };

            btnRefresh = ToolButton("Refresh (F5)");
            btnRefresh.Click += Refresh_Click;
            btnExport = ToolButton("Export");
            btnExport.Click += Export_Click;
            btnCopy = ToolButton("Copy info");
            btnCopy.Click += Copy_Click;

            btnTools = ToolButton("Tools ▾");
            btnTools.Click += Menu_Click;
            var toolsMenu = new ContextMenu { Background = Brushes.White, Foreground = InkBrush };
            toolsMenu.Items.Add(ToolMenuItem("Disk Management", "diskmgmt.msc", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Device Manager", "devmgmt.msc", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Network Connections", "ncpa.cpl", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Control Panel", "control.exe", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Programs and Features", "appwiz.cpl", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Computer Management", "compmgmt.msc", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Disk Cleanup", "cleanmgr.exe", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Task Manager", "taskmgr.exe", Tool_Click));
            toolsMenu.Items.Add(ToolMenuItem("Services", "services.msc", Tool_Click));
            toolsMenu.Items.Add(new Separator());
            toolsMenu.Items.Add(ToolMenuItem("Check for updates", null, UpdateCheck_Click));
            toolsMenu.Items.Add(ToolMenuItem("Update settings", null, UpdateSettings_Click));
            btnTools.ContextMenu = toolsMenu;

            btnTests = ToolButton("Tests ▾");
            btnTests.Click += Menu_Click;
            btnTests.ToolTip = "Keyboard, stereo speakers and camera checks";
            var testsMenu = new ContextMenu { Background = Brushes.White, Foreground = InkBrush };
            testsMenu.Items.Add(ToolMenuItem("Keyboard test", null, (_, __) => ShowDiagnostic(new KeyboardTestWindow())));
            testsMenu.Items.Add(ToolMenuItem("Stereo speaker test", null, (_, __) => ShowDiagnostic(new StereoSpeakerTestWindow())));
            testsMenu.Items.Add(ToolMenuItem("Camera test", null, (_, __) => ShowDiagnostic(new CameraTestWindow())));
            btnTests.ContextMenu = testsMenu;

            btnPower = ToolButton("Restart...");
            btnPower.Click += Menu_Click;
            var powerMenu = new ContextMenu { Background = Brushes.White, Foreground = InkBrush };
            powerMenu.Items.Add(ToolMenuItem("Restart Outlook (asks first)", null, RestartOutlook_Click));
            powerMenu.Items.Add(ToolMenuItem("Restart Explorer (asks first)", null, RestartExplorer_Click));
            powerMenu.Items.Add(new Separator());
            powerMenu.Items.Add(ToolMenuItem("Restart computer (60 s, cancellable)", null, RestartPc_Click));
            btnPower.ContextMenu = powerMenu;

            bar.Children.Add(btnRefresh);
            bar.Children.Add(btnExport);
            bar.Children.Add(btnCopy);
            bar.Children.Add(btnTools);
            bar.Children.Add(btnTests);
            bar.Children.Add(btnPower);
            toolbar.Child = bar;
            root.Children.Add(toolbar);

            // scrollable content
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(4, 2, 4, 4) };
            contentScroll = scroll;
            content = new StackPanel();

            // rating banner
            ratingBanner = Card();
            ratingBanner.Background = BannerBrush;
            ratingBanner.Padding = new Thickness(12, 6, 12, 6);
            ratingCaption = new TextBlock { Text = "SYSTEM RATING", FontSize = 10, Foreground = InkBrush, FontWeight = FontWeights.SemiBold };
            ratingBadge = Badge(ratingCaption);
            ratingBadge.Background = Brushes.White;
            ratingBadge.HorizontalAlignment = HorizontalAlignment.Right;
            txtRating = new TextBlock { FontSize = 15, FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap, Text = "Evaluating...", TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center };
            ratingLayout = new Grid();
            ratingLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            ratingLayout.ColumnDefinitions.Add(new ColumnDefinition());
            ratingLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            ratingLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ratingLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ratingLayout.Children.Add(txtRating);
            ratingLayout.Children.Add(ratingBadge);
            ratingLayout.SizeChanged += (_, __) => ArrangeRating();
            ratingBanner.Child = ratingLayout;
            content.Children.Add(ratingBanner);

            // One compact sheet: every section uses the same label/value alignment.
            txtPc = RowValue("txtPc"); txtUser = RowValue("txtUser"); txtMaker = RowValue("txtMaker");
            txtModel = RowValue("txtModel"); txtSerial = RowValue("txtSerial"); txtAsset = RowValue("txtAsset");
            txtOs = RowValue("txtOs"); txtAct = RowValue("txtAct"); txtUptime = RowValue("txtUptime"); txtBios = RowValue("txtBios");
            txtBios.FontSize = 10;
            txtBios.MaxWidth = 228;
            var biosBadge = Badge(txtBios);
            biosBadge.MaxWidth = 240;
            biosBadge.HorizontalAlignment = HorizontalAlignment.Left;
            biosBadge.Margin = new Thickness(8, 0, 0, 0);
            var restartText = new TextBlock { Text = "Consider restarting", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Freeze("#92400E") };
            restartBadge = Badge(restartText);
            restartBadge.Background = Freeze("#FEF3C7");
            restartBadge.BorderBrush = Freeze("#FBBF24");
            restartBadge.BorderThickness = new Thickness(1);
            restartBadge.Margin = new Thickness(8, 0, 0, 0);
            restartBadge.Visibility = Visibility.Collapsed;
            restartBadge.ToolTip = "Windows has been running for at least 48 hours. Restart when convenient to refresh Windows and running applications. " +
                "Save your work first. Fast Startup can preserve uptime across shutdowns.";
            System.Windows.Automation.AutomationProperties.SetName(restartBadge, "Consider restarting: Windows uptime is at least 48 hours");
            var uptimeLine = new WrapPanel();
            uptimeLine.Children.Add(txtUptime);
            uptimeLine.Children.Add(restartBadge);
            txtBattery = RowValue("txtBattery");
            pbBattery = Bar();
            lblBattery = BarCaption();
            txtCpu = RowValue("txtCpu");
            txtCpuCache = RowValue("txtCpuCache");
            txtMem = RowValue("txtMem");
            pbMem = Bar();
            lblMem = BarCaption();
            txtStorage = RowValue("txtStorage");
            pbDisk = Bar();
            lblDisk = BarCaption();
            txtGpu = RowValue("txtGpu");
            txtScreen = RowValue("txtScreen");

            memoryWarningBadge = ResourceWarningBadge();
            systemDriveWarningBadge = ResourceWarningBadge();
            var sheet = CardOf(
                SheetSection("COMPUTER",
                    Row("PC / user", ValueLine(txtPc, txtUser)),
                    Row("System", ValueLine(txtMaker, txtModel, biosBadge)),
                    Row("Serial / asset tag", ValueLine(txtSerial, txtAsset))),
                SheetSection("OPERATING SYSTEM",
                    Row("Windows", ValueLine(txtOs, txtAct)),
                    Row("Uptime", uptimeLine)),
                SheetSection("PROCESSOR & MEMORY",
                    Row("Processor", txtCpu), Row("CPU cache", txtCpuCache), Row("Memory", txtMem),
                    Row("Memory in use", MeterRow(pbMem, lblMem, memoryWarningBadge))),
                SheetSection("STORAGE",
                    Row("Drive / health", txtStorage), Row("Space used", MeterRow(pbDisk, lblDisk, systemDriveWarningBadge))),
                SheetSection("BATTERY",
                    Row("Charge / power", txtBattery), Row("Battery health", MeterRow(pbBattery, lblBattery))),
                SheetSection("GRAPHICS & DISPLAYS",
                    Row("Graphics", txtGpu), Row("Displays", txtScreen)));
            content.Children.Add(sheet);

            scroll.Content = content;
            root.Children.Add(scroll);
            Content = root;
        }
    }
}
