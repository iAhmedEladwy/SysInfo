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
        private readonly Dictionary<Key, List<Border>> keyTiles = new Dictionary<Key, List<Border>>();
        private readonly Grid numpad;
        private readonly TextBlock status = new TextBlock { FontSize = 11, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center };
        private static readonly Brush Ink = Brush("#0B1F3A"), Muted = Brush("#5B6B80"), Blue = Brush("#0078D4"), Tested = Brush("#D7EDFF");
        private const double Unit = 38;

        public KeyboardTestWindow()
        {
            DiagnosticUi.Apply(this);
            Title = "Keyboard test"; SizeToContent = SizeToContent.WidthAndHeight;
            MaxWidth = SystemParameters.WorkArea.Width; MaxHeight = SystemParameters.WorkArea.Height;
            ResizeMode = ResizeMode.CanMinimize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Focusable = true;
            Background = Brushes.White; Foreground = Ink; FontFamily = new FontFamily("Segoe UI"); FontSize = 12;
            var body = new DockPanel { Margin = new Thickness(12) };
            var header = new DockPanel { Margin = new Thickness(2, 0, 2, 8) };
            DockPanel.SetDock(header, Dock.Top);
            var reset = Button("Reset"); reset.Margin = new Thickness(10, 0, 0, 0); reset.Padding = new Thickness(12, 4, 12, 4); reset.MinWidth = 64;
            reset.Click += (_, __) => { state.Reset(); Paint(); SetStatus("Ready"); Keyboard.Focus(this); };
            DockPanel.SetDock(reset, Dock.Right); header.Children.Add(reset); header.Children.Add(status); body.Children.Add(header);

            var keyboard = new StackPanel { Orientation = Orientation.Horizontal };
            var main = KeyGrid(15); keyboard.Children.Add(main);
            AddKey(main, Key.Escape, "Esc", 0, 0);
            var functions = new[] { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12 };
            for (int i = 0; i < functions.Length; i++) AddKey(main, functions[i], "F" + (i + 1), 2 + i + (i / 4) * 0.5, 0);
            AddKey(main, Key.Oem3, "`", 0, 1);
            var digits = new[] { Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.D0 };
            for (int i = 0; i < digits.Length; i++) AddKey(main, digits[i], i == 9 ? "0" : (i + 1).ToString(), i + 1, 1);
            AddKey(main, Key.OemMinus, "-", 11, 1); AddKey(main, Key.OemPlus, "=", 12, 1); AddKey(main, Key.Back, "Backspace", 13, 1, 2);
            AddKey(main, Key.Tab, "Tab", 0, 2, 1.5);
            AddLetters(main, "QWERTYUIOP", 1.5, 2);
            AddKey(main, Key.OemOpenBrackets, "[", 11.5, 2); AddKey(main, Key.OemCloseBrackets, "]", 12.5, 2); AddKey(main, Key.Oem5, "\\", 13.5, 2, 1.5);
            AddKey(main, Key.Capital, "Caps", 0, 3, 1.75); AddLetters(main, "ASDFGHJKL", 1.75, 3);
            AddKey(main, Key.Oem1, ";", 10.75, 3); AddKey(main, Key.OemQuotes, "'", 11.75, 3); AddKey(main, Key.Return, "Enter", 12.75, 3, 2.25);
            AddKey(main, Key.LeftShift, "Shift", 0, 4, 2.25); AddLetters(main, "ZXCVBNM", 2.25, 4);
            AddKey(main, Key.OemComma, ",", 9.25, 4); AddKey(main, Key.OemPeriod, ".", 10.25, 4); AddKey(main, Key.OemQuestion, "/", 11.25, 4); AddKey(main, Key.RightShift, "Shift", 12.25, 4, 2.75);
            AddKey(main, Key.LeftCtrl, "Ctrl", 0, 5, 1.25); AddKey(main, Key.LWin, "Win", 1.25, 5, 1.25); AddKey(main, Key.LeftAlt, "Alt", 2.5, 5, 1.25);
            AddKey(main, Key.Space, "Space", 3.75, 5, 6.25); AddKey(main, Key.RightAlt, "Alt", 10, 5, 1.25); AddKey(main, Key.RWin, "Win", 11.25, 5, 1.25); AddKey(main, Key.Apps, "Menu", 12.5, 5, 1.25); AddKey(main, Key.RightCtrl, "Ctrl", 13.75, 5, 1.25);

            var navigation = KeyGrid(3); navigation.Margin = new Thickness(12, 0, 12, 0); keyboard.Children.Add(navigation);
            AddKey(navigation, Key.PrintScreen, "PrtSc", 0, 0); AddKey(navigation, Key.Scroll, "ScrLk", 1, 0); AddKey(navigation, Key.Pause, "Pause", 2, 0);
            AddKey(navigation, Key.Insert, "Ins", 0, 1); AddKey(navigation, Key.Home, "Home", 1, 1); AddKey(navigation, Key.Prior, "PgUp", 2, 1);
            AddKey(navigation, Key.Delete, "Del", 0, 2); AddKey(navigation, Key.End, "End", 1, 2); AddKey(navigation, Key.Next, "PgDn", 2, 2);
            AddKey(navigation, Key.Up, "↑", 1, 4); AddKey(navigation, Key.Left, "←", 0, 5); AddKey(navigation, Key.Down, "↓", 1, 5); AddKey(navigation, Key.Right, "→", 2, 5);

            numpad = KeyGrid(4); keyboard.Children.Add(numpad);
            AddKey(numpad, Key.NumLock, "Num", 0, 1); AddKey(numpad, Key.Divide, "/", 1, 1); AddKey(numpad, Key.Multiply, "*", 2, 1); AddKey(numpad, Key.Subtract, "-", 3, 1);
            AddKey(numpad, Key.NumPad7, "7", 0, 2); AddKey(numpad, Key.NumPad8, "8", 1, 2); AddKey(numpad, Key.NumPad9, "9", 2, 2); AddKey(numpad, Key.Add, "+", 3, 2, 1, 2);
            AddKey(numpad, Key.NumPad4, "4", 0, 3); AddKey(numpad, Key.NumPad5, "5", 1, 3); AddKey(numpad, Key.NumPad6, "6", 2, 3);
            AddKey(numpad, Key.NumPad1, "1", 0, 4); AddKey(numpad, Key.NumPad2, "2", 1, 4); AddKey(numpad, Key.NumPad3, "3", 2, 4); AddKey(numpad, Key.Return, "Enter", 3, 4, 1, 2);
            AddKey(numpad, Key.NumPad0, "0", 0, 5, 2); AddKey(numpad, Key.Decimal, ".", 2, 5);
            var viewer = new ScrollViewer { Content = keyboard, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxWidth = Math.Max(100, SystemParameters.WorkArea.Width - 48), MaxHeight = Math.Max(100, SystemParameters.WorkArea.Height - 110) };
            body.Children.Add(viewer); Content = body;
            SetStatus("Ready");
            PreviewKeyDown += OnDown; PreviewKeyUp += OnUp;
            Loaded += (_, __) => Keyboard.Focus(this);
            Deactivated += (_, __) => { state.LoseFocus(); Paint(); SetStatus("Paused"); };
            Activated += (_, __) => SetStatus("Ready");
            LostKeyboardFocus += (_, __) => Dispatcher.BeginInvoke(new Action(() => { if (!IsKeyboardFocusWithin) { state.LoseFocus(); Paint(); } }));
        }

        private void SetStatus(string message) => status.Text = "Press keys · Blue: pressed / Pale blue: tested / White: not tested · " + message;

        private static Grid KeyGrid(int columns)
        {
            var grid = new Grid { Width = columns * Unit, Height = 6 * Unit };
            // Quarter-unit columns support natural key widths; rowspans support numpad + and Enter.
            for (int i = 0; i < columns * 4; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Unit / 4) });
            for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Unit) });
            return grid;
        }

        private void AddLetters(Grid grid, string letters, double column, int row)
        {
            for (int i = 0; i < letters.Length; i++) AddKey(grid, (Key)Enum.Parse(typeof(Key), letters[i].ToString()), letters[i].ToString(), column + i, row);
        }

        private void AddKey(Grid grid, Key key, string label, double column, int row, double width = 1, int height = 1)
        {
            var tile = new Border { Margin = new Thickness(2), Background = Brushes.White, BorderBrush = Brush("#CBD5E1"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Child = new TextBlock { Text = label, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 },
                ToolTip = key == Key.Return ? "Main Enter and numpad Enter share this indication." : key.ToString() };
            Grid.SetColumn(tile, (int)(column * 4)); Grid.SetColumnSpan(tile, (int)(width * 4)); Grid.SetRow(tile, row); Grid.SetRowSpan(tile, height);
            System.Windows.Automation.AutomationProperties.SetName(tile, key.ToString());
            if (!keyTiles.TryGetValue(key, out var tiles)) { tiles = new List<Border>(); keyTiles.Add(key, tiles); }
            tiles.Add(tile); grid.Children.Add(tile);
        }
        private void OnDown(object sender, KeyEventArgs e)
        {
            if (!IsActive) return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.ImeProcessed) key = e.ImeProcessedKey;
            state.Press((int)key); Paint(); SetStatus("Detected: " + key + (e.IsRepeat ? " (held)" : ""));
            // Preserve accessible Tab traversal and activation of focused controls.
            bool controlAction = Keyboard.FocusedElement is System.Windows.Controls.Primitives.ButtonBase && (key == Key.Space || key == Key.Return);
            if (key != Key.Tab && !controlAction) e.Handled = true;
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
                foreach (var tile in item.Value)
                {
                    tile.Background = held ? Blue : state.IsTested((int)item.Key) ? Tested : Brushes.White;
                    ((TextBlock)tile.Child).Foreground = held ? Brushes.White : Ink;
                }
            }
        }
        private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color);
        private static Button Button(string text) => new Button { Content = text, MinWidth = 88, Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(6, 6, 0, 0) };
    }
}
