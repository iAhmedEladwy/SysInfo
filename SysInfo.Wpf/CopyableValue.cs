using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;

namespace CNSysInfo
{
    internal static class CopyableValue
    {
        public static TextBlock Attach(TextBlock value)
        {
            value.Focusable = true;
            value.Cursor = Cursors.Hand;
            KeyboardNavigation.SetIsTabStop(value, true);
            System.Windows.Automation.AutomationProperties.SetHelpText(value,
                "Click to copy this value. When focused, press Ctrl+C, Enter or Space.");
            value.ToolTip = "Click to copy this value. You can also focus it and press Ctrl+C, Enter or Space.";
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = "Copy value", Command = ApplicationCommands.Copy, CommandTarget = value, InputGestureText = "Ctrl+C" });
            value.ContextMenu = menu;
            var feedback = new ToolTip
            {
                Content = "Copied ✓", PlacementTarget = value, Placement = PlacementMode.Bottom,
                VerticalOffset = 3, StaysOpen = true, IsHitTestVisible = false, Focusable = false,
                Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(110, 231, 183)),
                Padding = new Thickness(9, 5, 9, 5)
            };
            var dismiss = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
            dismiss.Tick += (_, __) => { dismiss.Stop(); feedback.IsOpen = false; };
            value.Unloaded += (_, __) => { dismiss.Stop(); feedback.IsOpen = false; };
            Action copy = () =>
            {
                if (!Copy(value)) return;
                dismiss.Stop(); feedback.IsOpen = true; dismiss.Start();
            };
            value.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
                (_, args) => { copy(); args.Handled = true; },
                (_, args) => { args.CanExecute = HasValue(value); args.Handled = true; }));
            value.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.C, ModifierKeys.Control));
            value.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.Return, ModifierKeys.None));
            value.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.Space, ModifierKeys.None));
            value.MouseLeftButtonDown += (_, args) =>
            {
                value.Focus();
                copy();
                args.Handled = true;
            };
            return value;
        }

        private static bool HasValue(TextBlock value) => !string.IsNullOrWhiteSpace(value.Text) && value.Text != "...";

        private static bool Copy(TextBlock value)
        {
            if (!HasValue(value)) return false;
            try { Clipboard.SetText(value.Text); return true; } // Full current text, including wrapped/ellipsized values and live power.
            catch (Exception ex) when (ex is ExternalException || ex is UnauthorizedAccessException || ex is System.Threading.ThreadStateException)
            {
                MessageBox.Show("Windows could not access the clipboard. Please try copying the value again.", "Copy value", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }
    }
}
