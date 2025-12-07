namespace RPGGame.UI.Spacing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines spacing rules and constants for combat log text.
    /// </summary>
    public static class SpacingRules
    {
        /// <summary>
        /// Standard single space character
        /// </summary>
        public const string SingleSpace = " ";
        
        /// <summary>
        /// Standard spacing for indented text (roll info, status effects)
        /// </summary>
        public const string IndentSpacing = "    "; // 4 spaces
        
        /// <summary>
        /// Characters that should NOT have spaces after them
        /// </summary>
        private static readonly HashSet<char> NoSpaceAfter = new HashSet<char>
        {
            '!', '?', '.', ',', ':', ';', '[', '(', '{', '\n', '\r'
        };
        
        /// <summary>
        /// Characters that should NOT have spaces before them
        /// </summary>
        private static readonly HashSet<char> NoSpaceBefore = new HashSet<char>
        {
            '!', '?', '.', ',', ':', ';', ']', ')', '}', '\'', '\n', '\r'  // Apostrophe added to prevent "Vault 's" spacing issues
        };
        
        /// <summary>
        /// Determines if a space should be added between two text segments.
        /// This is the standard logic used throughout the combat log system.
        /// Includes special handling for same-word detection to prevent spacing issues with multi-color templates.
        /// </summary>
        public static bool ShouldAddSpaceBetween(string? previousText, string? nextText, bool checkWordBoundary = false)
        {
            // Null or empty checks
            if (string.IsNullOrEmpty(previousText) || string.IsNullOrEmpty(nextText))
                return false;
            
            // Don't add space if either segment is whitespace-only
            if (string.IsNullOrWhiteSpace(previousText) || string.IsNullOrWhiteSpace(nextText))
                return false;
            
            // Get boundary characters for analysis
            char prevLastChar = previousText[previousText.Length - 1];
            char nextFirstChar = nextText[0];
            
            // Word boundary detection: prevent spacing between segments that are part of the same word
            // This is important for multi-color templates that split words into character-by-character segments
            if (checkWordBoundary)
            {
                // If both segments are single characters that are letters/digits, they're part of the same word
                // This is the most common case for multi-color templates (e.g., "T" + "i" in "Time Chamber")
                if (previousText.Length == 1 && nextText.Length == 1)
                {
                    if (char.IsLetterOrDigit(prevLastChar) && char.IsLetterOrDigit(nextFirstChar))
                        return false;
                }
                
                // If one segment is a single character and the other is also a single character or short,
                // and both are letters/digits, they're likely part of the same word
                // This handles cases like "T" (single char) + "i" (single char) or "Ma" (2 chars) + "g" (single char)
                if ((previousText.Length == 1 || previousText.Length <= 2) && 
                    (nextText.Length == 1 || nextText.Length <= 2))
                {
                    if (char.IsLetterOrDigit(prevLastChar) && char.IsLetterOrDigit(nextFirstChar))
                    {
                        // Check that neither segment contains whitespace (which would indicate word boundary)
                        bool prevHasNoWhitespace = !previousText.Any(char.IsWhiteSpace);
                        bool nextHasNoWhitespace = !nextText.Any(char.IsWhiteSpace);
                        
                        // If both segments have no whitespace, they're part of the same word
                        if (prevHasNoWhitespace && nextHasNoWhitespace)
                        {
                            return false;
                        }
                    }
                }
                
                // Check if boundary characters are letters/digits (part of same word)
                if (char.IsLetterOrDigit(prevLastChar) && char.IsLetterOrDigit(nextFirstChar))
                {
                    // Check that neither segment contains whitespace (which would indicate word boundary)
                    bool prevHasNoWhitespace = !previousText.Any(char.IsWhiteSpace);
                    bool nextHasNoWhitespace = !nextText.Any(char.IsWhiteSpace);
                    
                    // If both segments have no whitespace, they're part of the same word
                    // This prevents spacing in multi-color templates like "MagmaPool" where each character
                    // or merged character groups are separate segments but form one word
                    if (prevHasNoWhitespace && nextHasNoWhitespace)
                    {
                        // Additional check: if the combined text would form a valid word (no internal spaces),
                        // then these segments are definitely part of the same word
                        // This handles cases where segments are merged (e.g., "Ma" + "g" = "Mag" part of "MagmaPool")
                        return false;
                    }
                }
            }
            
            // Check if previous text ends with a character that shouldn't have space after
            if (NoSpaceAfter.Contains(prevLastChar))
                return false;
            
            // Don't add space if previous ends with whitespace (already has spacing)
            // This prevents double spaces when segments already contain trailing spaces
            if (char.IsWhiteSpace(prevLastChar))
            {
                // Previous segment already ends with whitespace - don't add another space
                return false;
            }
            
            // Check if next text starts with a character that shouldn't have space before
            if (NoSpaceBefore.Contains(nextFirstChar))
                return false;
            
            // Don't add space if next starts with whitespace (already has spacing)
            // This prevents double spaces when segments already contain leading spaces
            if (char.IsWhiteSpace(nextFirstChar))
                return false;
            
            // For word-like segments (letters/digits), always add space between them
            // This ensures proper spacing between words in combat text
            bool prevEndsWithWordChar = char.IsLetterOrDigit(prevLastChar) || prevLastChar == '\'' || prevLastChar == '-';
            bool nextStartsWithWordChar = char.IsLetterOrDigit(nextFirstChar) || nextFirstChar == '\'' || nextFirstChar == '-';
            
            if (prevEndsWithWordChar && nextStartsWithWordChar)
            {
                // Both are word-like characters - add space between them
                // This handles cases like "hits" + "Temporal" or "affected" + "by" or "uses" + "Ancient"
                return true;
            }
            
            // Check for common word boundaries that need spaces
            // Words ending with letters/digits followed by words starting with letters/digits need spaces
            if (char.IsLetterOrDigit(prevLastChar) && char.IsLetterOrDigit(nextFirstChar))
            {
                return true;
            }
            
            // Handle hyphenated words and apostrophes more carefully
            // If previous ends with hyphen/apostrophe and next starts with letter, they're likely part of same word
            if ((prevLastChar == '-' || prevLastChar == '\'') && char.IsLetter(nextFirstChar))
            {
                // Check if this is part of a compound word or contraction
                // If previous text is a single character or short, likely part of same word
                if (previousText.Length <= 2)
                {
                    return false; // Part of same word (e.g., "don't", "well-known")
                }
                // Otherwise, might be separate (e.g., "word - next")
                return true;
            }
            
            // Default: add space (conservative approach - better to have spaces than missing them)
            return true;
        }
        
        /// <summary>
        /// Adds a space between two text segments if needed.
        /// Returns the space string if needed, empty string otherwise.
        /// </summary>
        public static string GetSpaceIfNeeded(string? previousText, string? nextText)
        {
            return ShouldAddSpaceBetween(previousText, nextText) ? SingleSpace : string.Empty;
        }
        
        /// <summary>
        /// Normalizes spacing in a string by ensuring single spaces between words.
        /// Removes multiple consecutive spaces and trims leading/trailing spaces.
        /// Uses regex for efficient processing.
        /// </summary>
        public static string NormalizeSpacing(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // Replace multiple consecutive spaces with single space (preserves other whitespace like newlines)
            text = System.Text.RegularExpressions.Regex.Replace(text, @" +", " ");
            
            return text.Trim();
        }
        
        /// <summary>
        /// Formats text with proper spacing, ensuring single spaces between words.
        /// Useful for string interpolation scenarios.
        /// </summary>
        public static string FormatWithSpacing(params string?[] parts)
        {
            var result = new List<string>();
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                
                var normalized = NormalizeSpacing(part);
                if (!string.IsNullOrEmpty(normalized))
                {
                    result.Add(normalized);
                }
            }
            
            return string.Join(SingleSpace, result);
        }
    }
}

