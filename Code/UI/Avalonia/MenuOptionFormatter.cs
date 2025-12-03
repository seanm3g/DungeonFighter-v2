namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Unified formatter for menu options - eliminates duplication across renderers
    /// </summary>
    public static class MenuOptionFormatter
    {
        /// <summary>
        /// Formats a menu option with number and text
        /// </summary>
        public static string Format(int number, string text)
        {
            return $"[{number}] {text}";
        }

        /// <summary>
        /// Formats a menu option with number, text, and optional metadata
        /// </summary>
        public static string FormatWithMetadata(int number, string text, string? metadata = null)
        {
            if (string.IsNullOrEmpty(metadata))
                return Format(number, text);
            
            return $"[{number}] {text} {metadata}";
        }

        /// <summary>
        /// Formats a menu option with selected indicator
        /// </summary>
        public static string FormatSelected(int number, string text, bool selected = false)
        {
            string prefix = selected ? "► " : "  ";
            return $"{prefix}[{number}] {text}";
        }

        /// <summary>
        /// Formats a menu option for display text (used in ClickableElement)
        /// </summary>
        public static string FormatDisplayText(int number, string text, string? suffix = null)
        {
            if (string.IsNullOrEmpty(suffix))
                return Format(number, text);
            
            return $"[{number}] {text}{suffix}";
        }

        /// <summary>
        /// Formats a dungeon option with level
        /// </summary>
        public static string FormatDungeon(int number, string dungeonName, int level)
        {
            return FormatWithMetadata(number, dungeonName, $"(lvl {level})");
        }

        /// <summary>
        /// Formats an item option
        /// </summary>
        public static string FormatItem(int number, string itemName)
        {
            return Format(number, itemName);
        }

        /// <summary>
        /// Formats a slot option with item name
        /// </summary>
        public static string FormatSlot(int number, string slotName, string itemName)
        {
            return $"[{number}] {slotName}: {itemName}";
        }
    }
}

