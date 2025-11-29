using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Centralized spacing management system for combat log text.
    /// Standardizes how spaces are handled between words, segments, and blocks.
    /// 
    /// This system provides two types of spacing:
    /// 1. Horizontal spacing (between words/segments within a line) - handled here
    /// 2. Vertical spacing (blank lines between blocks) - handled by TextSpacingSystem
    /// 
    /// STANDARD USAGE:
    /// - When building ColoredText: Let ColoredTextBuilder handle spacing automatically (don't add spaces manually)
    /// - When building strings: Use SpacingHelper methods for consistent spacing
    /// - When displaying blocks: Use TextSpacingSystem for vertical spacing
    /// </summary>
    public static class CombatLogSpacingManager
    {
        #region Spacing Constants
        
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
        
        #endregion
        
        #region Spacing Helper Methods
        
        /// <summary>
        /// Determines if a space should be added between two text segments.
        /// This is the standard logic used throughout the combat log system.
        /// Includes special handling for same-word detection to prevent spacing issues with multi-color templates.
        /// </summary>
        /// <param name="previousText">The text that came before</param>
        /// <param name="nextText">The text that comes next</param>
        /// <param name="checkWordBoundary">If true, checks if segments are part of the same word (prevents spacing in multi-color templates)</param>
        /// <returns>True if a space should be added, false otherwise</returns>
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
                if (previousText.Length == 1 && nextText.Length == 1)
                {
                    if (char.IsLetterOrDigit(prevLastChar) && char.IsLetterOrDigit(nextFirstChar))
                        return false;
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
            // BUT: if the segment contains internal spaces (like "Joren Blackthorn"), we still need
            // to add a space after it to separate it from the next word
            if (char.IsWhiteSpace(prevLastChar))
            {
                // Only skip if the entire segment is just whitespace or if it's a trailing space
                // that's not part of an internal word (e.g., "word " should still get space after)
                // Actually, if it ends with whitespace, it already has spacing, so don't add more
                return false;
            }
            
            // Check if next text starts with a character that shouldn't have space before
            if (NoSpaceBefore.Contains(nextFirstChar))
                return false;
            
            // Don't add space if next starts with whitespace (already has spacing)
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
        
        #endregion
        
        #region ColoredText Spacing Helpers
        
        /// <summary>
        /// Adds spacing between ColoredText segments using standard rules.
        /// This should be used when manually building ColoredText lists.
        /// </summary>
        public static List<ColoredText> AddSpacingBetweenSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return segments ?? new List<ColoredText>();
            
            var result = new List<ColoredText>(segments.Count * 2);
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                if (i == 0)
                {
                    // First segment - no space before
                    result.Add(segment);
                }
                else
                {
                    var prevSegment = result[result.Count - 1];
                    
                    // Check if space is needed
                    if (ShouldAddSpaceBetween(prevSegment.Text, segment.Text))
                    {
                        result.Add(new ColoredText(SingleSpace, Colors.White));
                    }
                    
                    result.Add(segment);
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region Roll Info Formatting
        
        /// <summary>
        /// Formats roll info with standard indentation and spacing.
        /// This ensures consistent formatting across all roll info displays.
        /// </summary>
        public static string FormatRollInfo(string rollText, string attackText, string? speedText = null, string? amplifierText = null)
        {
            var parts = new List<string> { IndentSpacing + "(" };
            
            // Roll information
            if (!string.IsNullOrEmpty(rollText))
            {
                parts.Add("roll:");
                parts.Add(rollText);
            }
            
            // Attack vs Defense
            if (!string.IsNullOrEmpty(attackText))
            {
                parts.Add("|");
                parts.Add(attackText);
            }
            
            // Speed
            if (!string.IsNullOrEmpty(speedText))
            {
                parts.Add("|");
                parts.Add("speed:");
                parts.Add(speedText);
            }
            
            // Amplifier
            if (!string.IsNullOrEmpty(amplifierText))
            {
                parts.Add("|");
                parts.Add("amp:");
                parts.Add(amplifierText);
            }
            
            parts.Add(")");
            
            return FormatWithSpacing(parts.ToArray());
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates that spacing in a text string is correct (no double spaces, proper punctuation spacing).
        /// Returns a list of issues found, empty if spacing is correct.
        /// </summary>
        public static List<string> ValidateSpacing(string text)
        {
            var issues = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return issues;
            
            // Check for double spaces
            if (text.Contains("  "))
            {
                issues.Add("Found double spaces in text");
            }
            
            // Check for spaces before punctuation (shouldn't have)
            var punctuationBefore = new[] { " !", " ?", " .", " ,", " :", " ;" };
            foreach (var pattern in punctuationBefore)
            {
                if (text.Contains(pattern))
                {
                    issues.Add($"Found space before punctuation: '{pattern}'");
                }
            }
            
            // Check for missing spaces after punctuation (might want)
            // This is more lenient - some punctuation might not need spaces after
            
            return issues;
        }
        
        #endregion
    }
}

