using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.UI
{
    /// <summary>
    /// Validates text spacing accuracy in the game's text system.
    /// Detects spacing issues including double spaces, missing spaces, and incorrect blank line spacing.
    /// </summary>
    public static class TextSpacingValidator
    {
        /// <summary>
        /// Result of spacing validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Issues { get; set; } = new List<string>();
            public int DoubleSpaceCount { get; set; }
            public int MissingSpaceCount { get; set; }
            public int PunctuationSpacingIssues { get; set; }
        }

        /// <summary>
        /// Generates a comprehensive report of spacing issues.
        /// </summary>
        public static string GenerateReport(ValidationResult result)
        {
            if (result.IsValid)
                return "✓ Spacing validation passed - no issues found.";

            var report = new StringBuilder();
            report.AppendLine("✗ Spacing validation failed:");
            report.AppendLine($"  - Double spaces: {result.DoubleSpaceCount}");
            report.AppendLine($"  - Missing spaces: {result.MissingSpaceCount}");
            report.AppendLine($"  - Punctuation issues: {result.PunctuationSpacingIssues}");
            report.AppendLine();
            report.AppendLine("Issues:");
            foreach (var issue in result.Issues)
            {
                report.AppendLine($"  • {issue}");
            }

            return report.ToString();
        }

        #region Helper Methods

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        private static bool IsPunctuation(char c)
        {
            return char.IsPunctuation(c) || c == '.' || c == ',' || c == '!' || c == '?' || 
                   c == ':' || c == ';' || c == '(' || c == ')' || c == '[' || c == ']' ||
                   c == '{' || c == '}';
        }

        #endregion
    }
}

