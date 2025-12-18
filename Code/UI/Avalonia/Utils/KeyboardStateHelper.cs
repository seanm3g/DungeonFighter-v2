using Avalonia.Input;

namespace RPGGame.UI.Avalonia.Utils
{
    /// <summary>
    /// Helper to reliably check keyboard state, including modifier keys.
    /// </summary>
    public static class KeyboardStateHelper
    {
        /// <summary>
        /// Gets the effective modifiers from the key event.
        /// In Avalonia, we rely on the event's KeyModifiers property.
        /// </summary>
        public static KeyModifiers GetEffectiveModifiers(KeyEventArgs e)
        {
            return e.KeyModifiers;
        }
    }
}

