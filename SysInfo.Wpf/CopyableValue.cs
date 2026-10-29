using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
            value.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
                (_, args) => { Copy(value); args.Handled = true; },
                (_, args) => { args.CanExecute = HasValue(value); args.Handled = true; }));
            value.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.C, ModifierKeys.Control));
            value.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.Return, ModifierKeys.None));
            value.InputBindings.Add(new KeyBinding(ApplicationCommands.Copy, Key.Space, ModifierKeys.None));
            value.MouseLeftButtonDown += (_, args) =>
            {
                value.Focus();
                Copy(value);
                args.Handled = true;
            };
            return value;
        }

        private static bool HasValue(TextBlock value) => !string.IsNullOrWhiteSpace(value.Text) && value.Text != "...";

        private static void Copy(TextBlock value)
        {
            if (!HasValue(value)) return;
            try { Clipboard.SetText(value.Text); } // Full current text, including wrapped/ellipsized values and live power.
            catch (Exception ex) when (ex is ExternalException || ex is UnauthorizedAccessException || ex is System.Threading.ThreadStateException)
            {
                MessageBox.Show("Windows could not access the clipboard. Please try copying the value again.", "Copy value", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
