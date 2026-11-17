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
        Mythic
    }
}

