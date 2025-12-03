using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Validates color application consistency throughout the text system.
    /// Detects missing colors, incorrect colors, and double-coloring issues.
    /// </summary>
    public static class ColorApplicationValidator
    {
        /// <summary>
        /// Result of color validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Issues { get; set; } = new List<string>();
            public int MissingColorCount { get; set; }
            public int DoubleColoringCount { get; set; }
            public int IncorrectColorCount { get; set; }
        }

        /// <summary>
        /// Expected color patterns for different text types
        /// </summary>
        public class ExpectedColorPattern
        {
            public string TextType { get; set; } = "";
            public List<string> Keywords { get; set; } = new List<string>();
            public ColorPalette? ExpectedColor { get; set; }
            public bool ShouldHaveColor { get; set; } = true;
        }

        /// <summary>
        /// Validates that colors are applied where expected in combat text.
        /// </summary>
        public static ValidationResult ValidateCombatTextColors(List<ColoredText> segments)
        {
            var result = new ValidationResult { IsValid = true };

            if (segments == null || segments.Count == 0)
                return result;

            // Check for expected color patterns in combat text
            var fullText = ColoredTextRenderer.RenderAsPlainText(segments);
            
            // Check for damage numbers (should be red/damage color)
            var damagePattern = new System.Text.RegularExpressions.Regex(@"\b\d+\s+damage\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (damagePattern.IsMatch(fullText))
            {
                // Find the damage number segment
                foreach (var segment in segments)
                {
                    if (segment != null && !string.IsNullOrEmpty(segment.Text))
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(segment.Text, @"^\d+$"))
                        {
                            // Check if this number is followed by "damage"
                            int index = segments.IndexOf(segment);
                            if (index < segments.Count - 1)
                            {
                                var nextSegment = segments[index + 1];
                                if (nextSegment != null && nextSegment.Text.Contains("damage", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Damage number should have damage color
                                    if (!IsDamageColor(segment.Color))
                                    {
                                        result.IsValid = false;
                                        result.IncorrectColorCount++;
                                        result.Issues.Add($"Damage number '{segment.Text}' should have damage color, got {segment.Color}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Check for player/enemy names (should have appropriate colors)
            // This is more complex and would require entity name detection

            return result;
        }

        /// <summary>
        /// Validates that no double-coloring occurs (explicit codes + keyword coloring).
        /// </summary>
        public static ValidationResult ValidateNoDoubleColoring(string text)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(text))
                return result;

            // Check for explicit color codes
            bool hasExplicitCodes = HasExplicitColorCodes(text);
            
            // Check for template syntax (which might indicate keyword coloring was applied)
            bool hasTemplates = text.Contains("{{") && text.Contains("}}");
            
            // Check for markup syntax
            bool hasMarkup = text.Contains("[color:") || text.Contains("[/color]");

            // If we have both explicit codes and templates/markup, we might have double-coloring
            if (hasExplicitCodes && (hasTemplates || hasMarkup))
            {
                result.IsValid = false;
                result.DoubleColoringCount++;
                result.Issues.Add("Text contains both explicit color codes and template/markup syntax - possible double-coloring");
            }

            // Check for nested color codes (like &R&Gtext&y&y)
            var nestedCodePattern = new System.Text.RegularExpressions.Regex(@"&[a-zA-Z].*?&[a-zA-Z].*?&y");
            if (nestedCodePattern.IsMatch(text))
            {
                result.IsValid = false;
                result.DoubleColoringCount++;
                result.Issues.Add("Text contains nested color codes - possible double-coloring");
            }

            return result;
        }

        /// <summary>
        /// Validates that item names have rarity-based colors.
        /// </summary>
        public static ValidationResult ValidateItemNameColors(string itemName, List<ColoredText> segments, ItemRarity? expectedRarity = null)
        {
            var result = new ValidationResult { IsValid = true };

            if (segments == null || segments.Count == 0)
            {
                result.IsValid = false;
                result.MissingColorCount++;
                result.Issues.Add($"Item name '{itemName}' has no colored segments");
                return result;
            }

            // Check if item name segments have colors (not all white)
            bool hasColor = segments.Any(s => s != null && !ColorValidator.AreColorsEqual(s.Color, Colors.White));
            
            if (!hasColor)
            {
                result.IsValid = false;
                result.MissingColorCount++;
                result.Issues.Add($"Item name '{itemName}' should have rarity-based color, but all segments are white");
            }

            return result;
        }

        /// <summary>
        /// Generates a comprehensive report of color application issues.
        /// </summary>
        public static string GenerateReport(ValidationResult result)
        {
            if (result.IsValid)
                return "✓ Color application validation passed - no issues found.";

            var report = new StringBuilder();
            report.AppendLine("✗ Color application validation failed:");
            report.AppendLine($"  - Missing colors: {result.MissingColorCount}");
            report.AppendLine($"  - Double coloring: {result.DoubleColoringCount}");
            report.AppendLine($"  - Incorrect colors: {result.IncorrectColorCount}");
            report.AppendLine();
            report.AppendLine("Issues:");
            foreach (var issue in result.Issues)
            {
                report.AppendLine($"  • {issue}");
            }

            return report.ToString();
        }

        #region Helper Methods

        private static bool HasExplicitColorCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Check for &X format color codes
            var codePattern = new System.Text.RegularExpressions.Regex(@"&[a-zA-Z]");
            return codePattern.IsMatch(text);
        }

        private static bool IsDamageColor(Color color)
        {
            // Check if color matches damage color palette
            // This is a simplified check - in practice, we'd compare against ColorPalette.Damage
            // For now, we'll check if it's red-ish
            return color.R > color.G && color.R > color.B;
        }

        #endregion
    }

    /// <summary>
    /// Item rarity enum for validation
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Transcendent
    }
}

