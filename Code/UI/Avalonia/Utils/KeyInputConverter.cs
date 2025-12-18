using Avalonia.Input;

namespace RPGGame.UI.Avalonia.Utils
{
    /// <summary>
    /// Centralized utility for converting Avalonia Key events to game input strings.
    /// Handles all key-to-input mapping including modifier keys (Shift, etc.)
    /// </summary>
    public static class KeyInputConverter
    {
        /// <summary>
        /// Converts an Avalonia Key with modifiers to a game input string.
        /// Supports Shift+Up/Down for page scrolling (pageup/pagedown).
        /// </summary>
        /// <param name="key">The Avalonia key that was pressed</param>
        /// <param name="modifiers">The key modifiers (Shift, Ctrl, Alt, etc.)</param>
        /// <returns>The game input string, or null if the key doesn't map to a game input</returns>
        public static string? ConvertKeyToInput(Key key, KeyModifiers modifiers)
        {
            // Check modifiers for page scrolling
            bool isCtrl = modifiers.HasFlag(KeyModifiers.Control);
            bool isShift = modifiers.HasFlag(KeyModifiers.Shift);
            
            // Use Ctrl+Up/Down or Shift+Up/Down for page scrolling (30 lines)
            // Shift takes priority if both are pressed
            bool isPageScroll = isShift || isCtrl;
            
            return key switch
            {
                Key.D1 or Key.NumPad1 => "1",
                Key.D2 or Key.NumPad2 => "2",
                Key.D3 or Key.NumPad3 => "3",
                Key.D4 or Key.NumPad4 => "4",
                Key.D5 or Key.NumPad5 => "5",
                Key.D6 or Key.NumPad6 => "6",
                Key.D7 or Key.NumPad7 => "7",
                Key.D8 or Key.NumPad8 => "8",
                Key.D9 or Key.NumPad9 => "9",
                Key.D0 or Key.NumPad0 => "0",
                Key.Enter => "enter",
                Key.Space => "space",
                Key.Back => "backspace",
                Key.Delete => "delete",
                Key.Left => "left",
                Key.Right => "right",
                Key.Up => isPageScroll ? "pageup" : "up",
                Key.Down => isPageScroll ? "pagedown" : "down",
                Key.PageUp => "pageup",
                Key.PageDown => "pagedown",
                Key.Tab => "tab",
                _ => null
            };
        }
    }
}

