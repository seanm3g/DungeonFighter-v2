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
        /// True for Ctrl+C (Windows/Linux) or Cmd+C (macOS) — used to copy the combat log while combat UI is active.
        /// </summary>
        public static bool IsCombatLogCopyChord(Key key, KeyModifiers modifiers)
        {
            if (key != Key.C)
                return false;
            return modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta);
        }

        /// <summary>
        /// True for Ctrl+/- or Cmd+/- (main or numpad) — UI zoom for the active game font.
        /// </summary>
        public static bool IsUiZoomChord(Key key, KeyModifiers modifiers)
        {
            if (!modifiers.HasFlag(KeyModifiers.Control) && !modifiers.HasFlag(KeyModifiers.Meta))
                return false;
            return key is Key.OemPlus or Key.Add or Key.OemMinus or Key.Subtract;
        }

        /// <summary>+1 for zoom in, -1 for zoom out. Only meaningful when <see cref="IsUiZoomChord"/> is true.</summary>
        public static int GetUiZoomDirection(Key key) =>
            key is Key.OemPlus or Key.Add ? 1 : -1;

        /// <summary>
        /// True for Ctrl+0 / Cmd+0 (main or numpad) — reset active font zoom to its per-font default.
        /// Shift variants are reserved for <see cref="IsUiZoomSetDefaultChord"/>.
        /// </summary>
        public static bool IsUiZoomResetChord(Key key, KeyModifiers modifiers)
        {
            if (key is not (Key.D0 or Key.NumPad0))
                return false;
            if (!modifiers.HasFlag(KeyModifiers.Control) && !modifiers.HasFlag(KeyModifiers.Meta))
                return false;
            return !modifiers.HasFlag(KeyModifiers.Shift);
        }

        /// <summary>
        /// True for Ctrl+Shift+0 / Cmd+Shift+0 (main or numpad) — save current zoom as the per-font default.
        /// </summary>
        public static bool IsUiZoomSetDefaultChord(Key key, KeyModifiers modifiers)
        {
            if (key is not (Key.D0 or Key.NumPad0))
                return false;
            if (!modifiers.HasFlag(KeyModifiers.Control) && !modifiers.HasFlag(KeyModifiers.Meta))
                return false;
            return modifiers.HasFlag(KeyModifiers.Shift);
        }

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
            bool isAlt = modifiers.HasFlag(KeyModifiers.Alt);
            bool isMeta = modifiers.HasFlag(KeyModifiers.Meta);
            
            // Use Ctrl+Up/Down or Shift+Up/Down for page scrolling (30 lines)
            // Shift takes priority if both are pressed
            bool isPageScroll = isShift || isCtrl;

            // Letter shortcuts (e.g. Skill Tree L=Learn, WASD). Skip chord modifiers so Ctrl/Cmd/Alt combos stay free.
            if (!isCtrl && !isAlt && !isMeta)
            {
                string? letter = TryConvertLetterKey(key, isShift);
                if (letter != null)
                    return letter;
            }

            // Ctrl/Cmd+/- is UI zoom — do not forward numpad +/- as inventory shortcuts.
            if ((isCtrl || isMeta) && key is Key.Add or Key.Subtract or Key.OemPlus or Key.OemMinus)
                return null;

            // Ctrl/Cmd+0 (+ optional Shift) is UI zoom reset / set-default — not menu option 0.
            if ((isCtrl || isMeta) && key is Key.D0 or Key.NumPad0)
                return null;
            
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
                Key.Multiply => "*",
                Key.Add => "+",
                Key.Subtract => "-",
                Key.Divide => "/",
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

        /// <summary>
        /// Maps A–Z to single-character game inputs (lowercase, or uppercase with Shift).
        /// </summary>
        private static string? TryConvertLetterKey(Key key, bool isShift)
        {
            char? lower = key switch
            {
                Key.A => 'a', Key.B => 'b', Key.C => 'c', Key.D => 'd', Key.E => 'e',
                Key.F => 'f', Key.G => 'g', Key.H => 'h', Key.I => 'i', Key.J => 'j',
                Key.K => 'k', Key.L => 'l', Key.M => 'm', Key.N => 'n', Key.O => 'o',
                Key.P => 'p', Key.Q => 'q', Key.R => 'r', Key.S => 's', Key.T => 't',
                Key.U => 'u', Key.V => 'v', Key.W => 'w', Key.X => 'x', Key.Y => 'y',
                Key.Z => 'z',
                _ => null
            };
            if (lower == null)
                return null;
            char c = isShift ? char.ToUpperInvariant(lower.Value) : lower.Value;
            return c.ToString();
        }
    }
}

