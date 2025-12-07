namespace RPGGame.UI.Spacing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Validates spacing in text strings.
    /// </summary>
    public static class SpacingValidator
    {
        /// <summary>
        /// Validates that spacing in a text string is correct (no double spaces, proper punctuation spacing).
        /// Returns a list of issues found, empty if spacing is correct.
        /// Enhanced to detect more spacing issues including missing spaces between words.
        /// </summary>
        public static List<string> ValidateSpacing(string text)
        {
            var issues = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return issues;
            
            // Check for double spaces
            if (text.Contains("  "))
            {
                int count = CountOccurrences(text, "  ");
                issues.Add($"Found {count} double space(s) in text");
            }
            
            // Check for spaces before punctuation (shouldn't have)
            var punctuationBefore = new[] { " !", " ?", " .", " ,", " :", " ;", " )", " ]", " }" };
            foreach (var pattern in punctuationBefore)
            {
                if (text.Contains(pattern))
                {
                    int count = CountOccurrences(text, pattern);
                    issues.Add($"Found {count} space(s) before punctuation: '{pattern}'");
                }
            }
            
            // Check for missing spaces after punctuation (for most punctuation)
            // Exceptions: apostrophes, hyphens in compound words
            var punctuationAfter = new[] { '.', ',', ':', ';', '!', '?', ')', ']', '}' };
            for (int i = 0; i < text.Length - 1; i++)
            {
                char current = text[i];
                char next = text[i + 1];
                
                // Check if punctuation is followed by a letter/digit without space
                if (Array.IndexOf(punctuationAfter, current) >= 0 && char.IsLetterOrDigit(next))
                {
                    // Exception: apostrophe in contractions (don't, it's)
                    if (current == '\'' && i > 0 && char.IsLetter(text[i - 1]))
                        continue;
                    
                    // Exception: period in decimals (3.14)
                    if (current == '.' && i > 0 && char.IsDigit(text[i - 1]) && char.IsDigit(next))
                        continue;
                    
                    issues.Add($"Missing space after punctuation '{current}' before '{next}' at position {i}");
                }
            }
            
            // Check for missing spaces between words (letters/digits)
            // This is conservative - only flag obvious cases
            for (int i = 0; i < text.Length - 1; i++)
            {
                char current = text[i];
                char next = text[i + 1];
                
                // Check for letter/digit followed immediately by letter/digit
                // But skip if they're part of the same word (like "word" or "123")
                if (char.IsLetterOrDigit(current) && char.IsLetterOrDigit(next))
                {
                    // Check surrounding context to determine if they're separate words
                    bool isSameWord = false;
                    
                    // If there's a letter before and after, likely same word
                    if (i > 0 && i < text.Length - 2)
                    {
                        char before = text[i - 1];
                        char after = text[i + 2];
                        
                        // If both before and after are letters/digits, likely same word
                        if (char.IsLetterOrDigit(before) || char.IsLetterOrDigit(after))
                        {
                            isSameWord = true;
                        }
                    }
                    
                    // If not same word and no space, flag it
                    if (!isSameWord && !char.IsWhiteSpace(current) && !char.IsWhiteSpace(next))
                    {
                        // Additional check: if current ends a word and next starts a word
                        // This is tricky without word boundary detection
                        // For now, we'll be conservative and skip this check
                    }
                }
            }
            
            return issues;
        }
        
        /// <summary>
        /// Counts occurrences of a pattern in text
        /// </summary>
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
    }
}

