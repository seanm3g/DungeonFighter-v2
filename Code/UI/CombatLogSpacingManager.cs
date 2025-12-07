using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Spacing;

namespace RPGGame.UI
{
    /// <summary>
    /// Centralized spacing management system for combat log text.
    /// Facade coordinator that delegates to specialized spacing components.
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
        public const string SingleSpace = SpacingRules.SingleSpace;
        
        /// <summary>
        /// Standard spacing for indented text (roll info, status effects)
        /// </summary>
        public const string IndentSpacing = SpacingRules.IndentSpacing;
        
        #endregion
        
        #region Spacing Helper Methods
        
        /// <summary>
        /// Determines if a space should be added between two text segments.
        /// This is the standard logic used throughout the combat log system.
        /// Includes special handling for same-word detection to prevent spacing issues with multi-color templates.
        /// </summary>
        public static bool ShouldAddSpaceBetween(string? previousText, string? nextText, bool checkWordBoundary = false)
        {
            return SpacingRules.ShouldAddSpaceBetween(previousText, nextText, checkWordBoundary);
        }
        
        /// <summary>
        /// Adds a space between two text segments if needed.
        /// Returns the space string if needed, empty string otherwise.
        /// </summary>
        public static string GetSpaceIfNeeded(string? previousText, string? nextText)
        {
            return SpacingRules.GetSpaceIfNeeded(previousText, nextText);
        }
        
        /// <summary>
        /// Normalizes spacing in a string by ensuring single spaces between words.
        /// Removes multiple consecutive spaces and trims leading/trailing spaces.
        /// Uses regex for efficient processing.
        /// </summary>
        public static string NormalizeSpacing(string text)
        {
            return SpacingRules.NormalizeSpacing(text);
        }
        
        /// <summary>
        /// Formats text with proper spacing, ensuring single spaces between words.
        /// Useful for string interpolation scenarios.
        /// </summary>
        public static string FormatWithSpacing(params string?[] parts)
        {
            return SpacingRules.FormatWithSpacing(parts);
        }
        
        #endregion
        
        #region ColoredText Spacing Helpers
        
        /// <summary>
        /// Adds spacing between ColoredText segments using standard rules.
        /// This should be used when manually building ColoredText lists.
        /// </summary>
        public static List<ColoredText> AddSpacingBetweenSegments(List<ColoredText> segments)
        {
            return SpacingFormatter.AddSpacingBetweenSegments(segments);
        }
        
        #endregion
        
        #region Roll Info Formatting
        
        /// <summary>
        /// Formats roll info with standard indentation and spacing.
        /// This ensures consistent formatting across all roll info displays.
        /// </summary>
        public static string FormatRollInfo(string rollText, string attackText, string? speedText = null, string? amplifierText = null)
        {
            return SpacingFormatter.FormatRollInfo(rollText, attackText, speedText, amplifierText);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates that spacing in a text string is correct (no double spaces, proper punctuation spacing).
        /// Returns a list of issues found, empty if spacing is correct.
        /// Enhanced to detect more spacing issues including missing spaces between words.
        /// </summary>
        public static List<string> ValidateSpacing(string text)
        {
            return SpacingValidator.ValidateSpacing(text);
        }
        
        #endregion
    }
}

