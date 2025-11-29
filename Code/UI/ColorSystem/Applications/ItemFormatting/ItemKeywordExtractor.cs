namespace RPGGame.UI.ColorSystem.Applications.ItemFormatting
{
    /// <summary>
    /// Extracts the keyword from a modification or stat bonus name
    /// For "of the X" patterns, returns only "X" (the keyword)
    /// For other patterns, returns the full name
    /// Shared utility used by both ItemDisplayColoredText and ItemColorThemeSystem
    /// </summary>
    public static class ItemKeywordExtractor
    {
        /// <summary>
        /// Extracts the keyword from a modification or stat bonus name
        /// For "of the X" patterns, returns only "X" (the keyword)
        /// For other patterns, returns the full name
        /// </summary>
        public static (string prefix, string keyword) ExtractKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
                return ("", "");
            
            // Check for "of the X" pattern
            if (name.StartsWith("of the ", System.StringComparison.OrdinalIgnoreCase))
            {
                string keyword = name.Substring(7); // "of the " is 7 characters
                return ("of the ", keyword);
            }
            
            // Check for "of X" pattern (without "the")
            if (name.StartsWith("of ", System.StringComparison.OrdinalIgnoreCase))
            {
                string keyword = name.Substring(3); // "of " is 3 characters
                return ("of ", keyword);
            }
            
            // No prefix pattern, return full name as keyword
            return ("", name);
        }
    }
}

