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
        /// Validates word spacing in a text string.
        /// Checks for double spaces, missing spaces between words, and punctuation spacing issues.
        /// </summary>
        public static ValidationResult ValidateWordSpacing(string text)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(text))
                return result;

            // Check for double spaces
            if (text.Contains("  "))
            {
                int count = CountOccurrences(text, "  ");
                result.DoubleSpaceCount = count;
                result.Issues.Add($"Found {count} double space(s) in text");
                result.IsValid = false;
            }

            // Check for spaces before punctuation (shouldn't have)
            var punctuationBefore = new[] { " !", " ?", " .", " ,", " :", " ;", " )", " ]", " }" };
            foreach (var pattern in punctuationBefore)
            {
                if (text.Contains(pattern))
                {
                    int count = CountOccurrences(text, pattern);
                    result.PunctuationSpacingIssues += count;
                    result.Issues.Add($"Found {count} space(s) before punctuation: '{pattern}'");
                    result.IsValid = false;
                }
            }

            // Check for missing spaces between words
            // This is more complex - we need to detect cases where two words are adjacent without space
            var missingSpaces = DetectMissingSpaces(text);
            if (missingSpaces.Count > 0)
            {
                result.MissingSpaceCount = missingSpaces.Count;
                result.Issues.AddRange(missingSpaces);
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Validates blank line spacing between blocks.
        /// Compares actual spacing against expected spacing rules.
        /// </summary>
        public static ValidationResult ValidateBlankLineSpacing(
            TextSpacingSystem.BlockType? previousBlock,
            TextSpacingSystem.BlockType currentBlock,
            int actualBlankLines)
        {
            var result = new ValidationResult { IsValid = true };

            int expectedBlankLines = TextSpacingSystem.GetSpacingBefore(currentBlock);

            if (actualBlankLines != expectedBlankLines)
            {
                result.IsValid = false;
                result.Issues.Add(
                    $"Block transition ({previousBlock} -> {currentBlock}): " +
                    $"Expected {expectedBlankLines} blank line(s), got {actualBlankLines}");
            }

            return result;
        }

        /// <summary>
        /// Validates spacing in a list of ColoredText segments.
        /// Checks for proper spacing between segments.
        /// </summary>
        public static ValidationResult ValidateColoredTextSpacing(List<RPGGame.UI.ColorSystem.ColoredText> segments)
        {
            var result = new ValidationResult { IsValid = true };

            if (segments == null || segments.Count <= 1)
                return result;

            // Build full text from segments
            var fullText = new StringBuilder();
            foreach (var segment in segments)
            {
                if (segment != null && !string.IsNullOrEmpty(segment.Text))
                {
                    fullText.Append(segment.Text);
                }
            }

            // Validate the combined text
            var textResult = ValidateWordSpacing(fullText.ToString());
            result.IsValid = textResult.IsValid;
            result.Issues.AddRange(textResult.Issues);
            result.DoubleSpaceCount = textResult.DoubleSpaceCount;
            result.MissingSpaceCount = textResult.MissingSpaceCount;
            result.PunctuationSpacingIssues = textResult.PunctuationSpacingIssues;

            // Also check segment boundaries for spacing issues
            for (int i = 1; i < segments.Count; i++)
            {
                var prev = segments[i - 1];
                var curr = segments[i];

                if (prev != null && curr != null &&
                    !string.IsNullOrEmpty(prev.Text) &&
                    !string.IsNullOrEmpty(curr.Text))
                {
                    // Check if space is needed between segments
                    bool shouldHaveSpace = CombatLogSpacingManager.ShouldAddSpaceBetween(prev.Text, curr.Text);
                    
                    // Check if space actually exists
                    bool hasSpace = prev.Text.EndsWith(" ") || curr.Text.StartsWith(" ");

                    if (shouldHaveSpace && !hasSpace)
                    {
                        result.IsValid = false;
                        result.Issues.Add($"Missing space between segments: '{prev.Text}' and '{curr.Text}'");
                        result.MissingSpaceCount++;
                    }
                    else if (!shouldHaveSpace && hasSpace)
                    {
                        result.IsValid = false;
                        result.Issues.Add($"Unnecessary space between segments: '{prev.Text}' and '{curr.Text}'");
                    }
                }
            }

            return result;
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

        private static List<string> DetectMissingSpaces(string text)
        {
            var issues = new List<string>();
            
            // Split text into words and punctuation
            // Look for cases where a letter/digit is followed immediately by another letter/digit
            // without a space (but not if they're part of the same word like "word" or "123")
            
            for (int i = 0; i < text.Length - 1; i++)
            {
                char current = text[i];
                char next = text[i + 1];

                // Check if we have two word characters that should be separated
                if (char.IsLetterOrDigit(current) && char.IsLetterOrDigit(next))
                {
                    // Check if they're part of the same word (look for word boundaries)
                    // This is a simplified check - we look for context clues
                    bool isSameWord = false;
                    
                    // If there's no space before current and no space after next in a reasonable range,
                    // they might be the same word
                    // For now, we'll be conservative and only flag obvious cases
                    
                    // Skip if it's clearly part of a word (like "word" or "123")
                    if (i > 0 && i < text.Length - 2)
                    {
                        // Check surrounding context
                        char before = text[i - 1];
                        char after = (i + 2 < text.Length) ? text[i + 2] : ' ';
                        
                        // If both before and after are letters/digits, it's likely the same word
                        if (char.IsLetterOrDigit(before) || char.IsLetterOrDigit(after))
                        {
                            isSameWord = true;
                        }
                    }
                    
                    if (!isSameWord)
                    {
                        // Check if there should be a space (e.g., "word1word2" vs "word word")
                        // This is tricky - we'll be conservative and only flag if we're confident
                        // For now, skip this check as it's too complex without more context
                    }
                }
                
                // Check for punctuation followed by letter without space (some cases are OK, like "word's")
                if (IsPunctuation(current) && char.IsLetter(next) && current != '\'')
                {
                    // Most punctuation should have space after (except apostrophe, hyphen in compound words)
                    if (current != '-' && current != '_')
                    {
                        issues.Add($"Missing space after punctuation '{current}' before '{next}' at position {i}");
                    }
                }
            }

            return issues;
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

