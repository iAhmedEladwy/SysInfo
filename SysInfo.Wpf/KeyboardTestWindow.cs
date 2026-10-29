using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CNSysInfo
{
    internal sealed class KeyboardTestWindow : Window
    {
        private readonly DiagnosticKeyState state = new DiagnosticKeyState();
        private readonly Dictionary<Key, Border> keyTiles = new Dictionary<Key, Border>();
        private readonly StackPanel numpad = new StackPanel();
        private readonly TextBlock status = new TextBlock();
        private static readonly Brush Ink = Brush("#0B1F3A"), Muted = Brush("#5B6B80"), Blue = Brush("#0078D4"), Tested = Brush("#D7EDFF");

        public KeyboardTestWindow()
        {
            DiagnosticUi.Apply(this);
            Title = "Keyboard test";
            SizeToContent = SizeToContent.WidthAndHeight;
            MaxWidth = SystemParameters.WorkArea.Width; MaxHeight = SystemParameters.WorkArea.Height;
            ResizeMode = ResizeMode.CanMinimize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White; Foreground = Ink; FontFamily = new FontFamily("Segoe UI"); FontSize = 12;
            var body = new DockPanel { Margin = new Thickness(16) };
            var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
            DockPanel.SetDock(footer, Dock.Bottom);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var reset = Button("Reset"); reset.Click += (_, __) => { state.Reset(); Paint(); status.Text = "Ready. Press a key to begin."; };
            var close = Button("Close"); close.Click += (_, __) => Close();
            actions.Children.Add(reset); actions.Children.Add(close);
            footer.Children.Add(new TextBlock { Text = "Blue: pressed    Light blue: tested    White: not tested", Foreground = Muted, Margin = new Thickness(0, 0, 0, 8) });
            footer.Children.Add(status); footer.Children.Add(actions); body.Children.Add(footer);
            var help = new StackPanel(); DockPanel.SetDock(help, Dock.Top);
            help.Children.Add(new TextBlock { Text = "Press keys while this window is active. Tab still moves between controls.", TextWrapping = TextWrapping.Wrap });
            help.Children.Add(new TextBlock { Text = "Fn and Windows-reserved shortcuts may not be reported. An untested key is not a failed key.", Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8) });
            var show = new CheckBox { Content = "Show numpad", IsChecked = false, Margin = new Thickness(0, 0, 0, 8) };
            show.Checked += (_, __) => numpad.Visibility = Visibility.Visible; show.Unchecked += (_, __) => numpad.Visibility = Visibility.Collapsed;
            help.Children.Add(show); body.Children.Add(help);
            var keyboard = new StackPanel { Orientation = Orientation.Horizontal };
            var main = new StackPanel(); keyboard.Children.Add(main);
            AddRow(main, new[]{Key.Escape,Key.F1,Key.F2,Key.F3,Key.F4,Key.F5,Key.F6,Key.F7,Key.F8,Key.F9,Key.F10,Key.F11,Key.F12,Key.PrintScreen,Key.Scroll,Key.Pause}, new[]{"Esc","F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12","PrtSc","ScrLk","Pause"});
            AddRow(main, new[]{Key.Oem3,Key.D1,Key.D2,Key.D3,Key.D4,Key.D5,Key.D6,Key.D7,Key.D8,Key.D9,Key.D0,Key.OemMinus,Key.OemPlus,Key.Back}, new[]{"`","1","2","3","4","5","6","7","8","9","0","-","=","Back"});
            AddRow(main, new[]{Key.Tab,Key.Q,Key.W,Key.E,Key.R,Key.T,Key.Y,Key.U,Key.I,Key.O,Key.P,Key.OemOpenBrackets,Key.OemCloseBrackets,Key.Oem5}, new[]{"Tab","Q","W","E","R","T","Y","U","I","O","P","[","]","\\"});
            AddRow(main, new[]{Key.Capital,Key.A,Key.S,Key.D,Key.F,Key.G,Key.H,Key.J,Key.K,Key.L,Key.Oem1,Key.OemQuotes,Key.Return}, new[]{"Caps","A","S","D","F","G","H","J","K","L",";","'","Enter"});
            AddRow(main, new[]{Key.LeftShift,Key.Oem102,Key.Z,Key.X,Key.C,Key.V,Key.B,Key.N,Key.M,Key.OemComma,Key.OemPeriod,Key.OemQuestion,Key.RightShift}, new[]{"LShift","ISO","Z","X","C","V","B","N","M",",",".","/","RShift"});
            AddRow(main, new[]{Key.LeftCtrl,Key.LWin,Key.LeftAlt,Key.Space,Key.RightAlt,Key.RWin,Key.Apps,Key.RightCtrl}, new[]{"LCtrl","LWin","LAlt","Space","RAlt","RWin","Menu","RCtrl"});
            AddRow(main, new[]{Key.Insert,Key.Delete,Key.Home,Key.End,Key.Prior,Key.Next,Key.Left,Key.Down,Key.Up,Key.Right}, new[]{"Ins","Del","Home","End","PgUp","PgDn","←","↓","↑","→"});
            numpad.Margin = new Thickness(12, 0, 0, 0); numpad.Visibility = Visibility.Collapsed; keyboard.Children.Add(numpad);
            AddRow(numpad,new[]{Key.NumLock,Key.Divide,Key.Multiply,Key.Subtract},new[]{"Num","/","*","-"});
            AddRow(numpad,new[]{Key.NumPad7,Key.NumPad8,Key.NumPad9,Key.Add},new[]{"7","8","9","+"});
            AddRow(numpad,new[]{Key.NumPad4,Key.NumPad5,Key.NumPad6},new[]{"4","5","6"});
            AddRow(numpad,new[]{Key.NumPad1,Key.NumPad2,Key.NumPad3},new[]{"1","2","3"});
            AddRow(numpad,new[]{Key.NumPad0,Key.Decimal},new[]{"0","."});
            numpad.Children.Add(new TextBlock { Text = "Enter shares the main\nEnter indication.", Foreground = Muted, Margin = new Thickness(2, 6, 0, 0) });
            var viewer = new ScrollViewer { Content = keyboard, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            body.Children.Add(viewer); Content = body;
            status.Text = "Ready. Press a key to begin.";
            PreviewKeyDown += OnDown; PreviewKeyUp += OnUp;
            Deactivated += (_, __) => { state.LoseFocus(); Paint(); status.Text = "Paused while this window is inactive."; };
            Activated += (_, __) => status.Text = "Ready. Press a key to begin.";
            LostKeyboardFocus += (_, __) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsKeyboardFocusWithin) { state.LoseFocus(); Paint(); }
            }));
        }
        private void AddRow(Panel host, Key[] keys, string[] labels)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = 0; i < keys.Length; i++)
            {
                var tile = new Border { Width = keys[i] == Key.Space ? 150 : labels[i].Length > 3 ? 45 : 35, Height = 34,
                    Margin = new Thickness(2), Background = Brushes.White, BorderBrush = Brush("#CBD5E1"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                    Child = new TextBlock { Text = labels[i], HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 }, ToolTip = keys[i].ToString() };
                System.Windows.Automation.AutomationProperties.SetName(tile, labels[i]);
                keyTiles[keys[i]] = tile; row.Children.Add(tile);
            }
            host.Children.Add(row);
        }
        private void OnDown(object sender, KeyEventArgs e)
        {
            if (!IsActive) return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.ImeProcessed) key = e.ImeProcessedKey;
            state.Press((int)key); Paint(); status.Text = "Detected: " + key + (e.IsRepeat ? " (held)" : "");
            // Preserve accessible Tab traversal and activation of focused controls.
            bool controlAction = Keyboard.FocusedElement is System.Windows.Controls.Primitives.ButtonBase && (key == Key.Space || key == Key.Return);
            bool checkboxAction = Keyboard.FocusedElement is CheckBox && key == Key.Space;
            if (key != Key.Tab && !controlAction && !checkboxAction) e.Handled = true;
        }
        private void OnUp(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.ImeProcessed) key = e.ImeProcessedKey;
            if (!IsActive) return;
            state.Press((int)key); // Some system keys report only key-up to an application.
            state.Release((int)key); Paint();
        }
        private void Paint()
        {
            foreach (var item in keyTiles)
            {
                bool held = state.IsHeld((int)item.Key);
                item.Value.Background = held ? Blue : state.IsTested((int)item.Key) ? Tested : Brushes.White;
                ((TextBlock)item.Value.Child).Foreground = held ? Brushes.White : Ink;
            }
        }
        private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color);
        private static Button Button(string text) => new Button { Content = text, MinWidth = 88, Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(6, 6, 0, 0) };
    }
}
