using Avalonia.Input;

namespace RPGGame.UI
{
    /// <summary>
    /// Tracks whether the player is holding Alt to expand item/action hover tooltips.
    /// Updated from key and pointer events; tooltip builders read the snapshot at draw time.
    /// </summary>
    public static class HoverTooltipDetailState
    {
        private static readonly object Lock = new object();
        private static bool _altHeld;

        /// <summary>True when Alt is held — tooltips include extended detail sections.</summary>
        public static bool IsAltDetailActive
        {
            get
            {
                lock (Lock) return _altHeld;
            }
        }

        /// <summary>
        /// Updates from a key or pointer modifier mask. Returns true when the Alt-detail flag changed
        /// (caller should refresh an active tooltip).
        /// </summary>
        public static bool SetFromModifiers(KeyModifiers modifiers)
        {
            bool next = modifiers.HasFlag(KeyModifiers.Alt);
            lock (Lock)
            {
                if (_altHeld == next)
                    return false;
                _altHeld = next;
                return true;
            }
        }

        /// <summary>Updates from Left/Right Alt key down/up. Returns true when the flag changed.</summary>
        public static bool SetFromAltKey(Key key, bool isDown)
        {
            if (key != Key.LeftAlt && key != Key.RightAlt)
                return false;

            lock (Lock)
            {
                if (_altHeld == isDown)
                    return false;
                _altHeld = isDown;
                return true;
            }
        }

        /// <summary>Clears Alt-detail (e.g. window deactivate). Does not trigger a render.</summary>
        public static void Clear()
        {
            lock (Lock) _altHeld = false;
        }
    }
}
