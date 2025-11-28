using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Centralized logic for merging ColoredText segments.
    /// Handles: same-color merging, space normalization, empty segment removal.
    /// This is the single source of truth for segment merging logic.
    /// </summary>
    public static class ColoredTextMerger
    {
        /// <summary>
        /// Merges adjacent segments with the same color and normalizes spaces.
        /// This is the primary merging method used throughout the color system.
        /// </summary>
        /// <param name="segments">List of ColoredText segments to merge</param>
        /// <returns>Merged and normalized list of segments</returns>
        public static List<ColoredText> MergeAdjacentSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return segments ?? new List<ColoredText>();
            
            var merged = new List<ColoredText>();
            ColoredText? currentSegment = null;
            
            foreach (var segment in segments)
            {
                // Skip empty segments
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                if (currentSegment == null)
                {
                    // First segment
                    currentSegment = new ColoredText(segment.Text, segment.Color);
                }
                else if (ColorValidator.AreColorsEqual(currentSegment.Color, segment.Color))
                {
                    // Same color - merge with current segment
                    // Normalize spaces: if current ends with space and segment starts with space, use only one
                    string currentText = currentSegment.Text;
                    string segmentText = segment.Text;
                    
                    // Check if we need to normalize spaces at the boundary
                    if (currentText.EndsWith(" ") && segmentText.StartsWith(" "))
                    {
                        // Remove one space to avoid double spacing
                        currentText = currentText.TrimEnd() + " " + segmentText.TrimStart();
                    }
                    else
                    {
                        currentText = currentText + segmentText;
                    }
                    
                    currentSegment = new ColoredText(currentText, currentSegment.Color);
                }
                else
                {
                    // Different color - add current segment and start new one
                    // Ensure there's a space between segments of different colors if needed
                    string currentText = currentSegment.Text;
                    string segmentText = segment.Text;
                    
                    // Check if we need to add a space between different-colored segments
                    bool needsSpace = ShouldAddSpaceBetweenDifferentColors(currentText, segmentText);
                    if (needsSpace)
                    {
                        // Add a space to the end of current segment before adding it
                        currentText = currentText + " ";
                        merged.Add(new ColoredText(currentText, currentSegment.Color));
                    }
                    else
                    {
                        merged.Add(currentSegment);
                    }
                    
                    currentSegment = new ColoredText(segment.Text, segment.Color);
                }
            }
            
            // Add the last segment
            if (currentSegment != null)
            {
                merged.Add(currentSegment);
            }
            
            // Normalize spaces between adjacent segments of different colors FIRST
            // This prevents double spaces from being created when segments are merged
            for (int i = 0; i < merged.Count - 1; i++)
            {
                var current = merged[i];
                var next = merged[i + 1];
                
                // If both segments are just spaces, merge them into one
                if (current.Text.Trim().Length == 0 && next.Text.Trim().Length == 0)
                {
                    // Both are space-only segments - merge into one white space segment
                    merged[i] = new ColoredText(" ", Colors.White);
                    merged.RemoveAt(i + 1);
                    i--; // Adjust index after removal
                    continue;
                }
                
                // If current ends with space(s) and next starts with space(s), normalize to single space
                string currentText = current.Text;
                string nextText = next.Text;
                
                // Check if current ends with spaces and next starts with spaces
                bool currentEndsWithSpace = currentText.Length > 0 && char.IsWhiteSpace(currentText[currentText.Length - 1]);
                bool nextStartsWithSpace = nextText.Length > 0 && char.IsWhiteSpace(nextText[0]);
                
                if (currentEndsWithSpace && nextStartsWithSpace)
                {
                    // Remove trailing whitespace from current segment
                    var trimmedCurrent = currentText.TrimEnd();
                    if (trimmedCurrent.Length > 0)
                    {
                        // Keep one space at the end
                        merged[i] = new ColoredText(trimmedCurrent + " ", current.Color);
                    }
                    else
                    {
                        // Current is all whitespace - check if next is also whitespace
                        if (nextText.Trim().Length == 0)
                        {
                            // Both are whitespace-only segments - merge into one space
                            merged[i] = new ColoredText(" ", Colors.White);
                            merged.RemoveAt(i + 1);
                            i--; // Adjust index after removal
                        }
                        else
                        {
                            // Current is whitespace, next is not - remove current
                            merged.RemoveAt(i);
                            i--; // Adjust index after removal
                        }
                        continue;
                    }
                }
            }
            
            // Final pass: normalize any remaining multiple spaces within segments
            // Only normalize spaces (not all whitespace) to preserve newlines and tabs if needed
            for (int i = 0; i < merged.Count; i++)
            {
                var segment = merged[i];
                // Replace multiple consecutive spaces with single space (but preserve other whitespace)
                var normalizedText = Regex.Replace(segment.Text, @" +", " ");
                if (normalizedText != segment.Text)
                {
                    merged[i] = new ColoredText(normalizedText, segment.Color);
                }
            }
            
            // Final pass: normalize spaces between adjacent segments again (after internal normalization)
            // This catches any remaining boundary issues after normalizing within segments
            for (int i = 0; i < merged.Count - 1; i++)
            {
                var current = merged[i];
                var next = merged[i + 1];
                
                string currentText = current.Text;
                string nextText = next.Text;
                
                // Check if current ends with whitespace and next starts with whitespace
                bool currentEndsWithSpace = currentText.Length > 0 && char.IsWhiteSpace(currentText[currentText.Length - 1]);
                bool nextStartsWithSpace = nextText.Length > 0 && char.IsWhiteSpace(nextText[0]);
                
                if (currentEndsWithSpace && nextStartsWithSpace)
                {
                    var trimmedCurrent = currentText.TrimEnd();
                    if (trimmedCurrent.Length > 0)
                    {
                        // Keep one space at the end
                        merged[i] = new ColoredText(trimmedCurrent + " ", current.Color);
                    }
                    else
                    {
                        // Current is all whitespace - remove it
                        merged.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
            
            // Remove any empty segments that might have been created
            merged.RemoveAll(s => string.IsNullOrEmpty(s.Text));
            
            return merged;
        }
        
        /// <summary>
        /// Merges only segments with the same color, preserving space segments separately.
        /// This is used by ColoredTextBuilder which handles spacing differently.
        /// </summary>
        /// <param name="segments">List of ColoredText segments to merge</param>
        /// <returns>Merged list with same-color segments combined</returns>
        public static List<ColoredText> MergeSameColorSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return new List<ColoredText>();
            
            var merged = new List<ColoredText>();
            ColoredText? current = null;
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Check if this is a space-only segment
                bool isSpaceSegment = segment.Text.Trim().Length == 0 && segment.Text.Length > 0;
                
                if (current == null)
                {
                    if (!isSpaceSegment)
                    {
                        // Don't start with a space segment
                        current = new ColoredText(segment.Text, segment.Color);
                    }
                    else
                    {
                        // Start with a space segment - add it directly
                        merged.Add(segment);
                    }
                }
                else if (isSpaceSegment)
                {
                    // Space segment - always keep as separate segment to ensure proper spacing
                    // Save current segment, add space as separate segment
                    merged.Add(current);
                    merged.Add(segment);
                    current = null; // Will be set by next iteration
                }
                else if (ColorValidator.AreColorsEqual(current.Color, segment.Color))
                {
                    // Same color - merge the text (space should already be in current if needed)
                    current = new ColoredText(current.Text + segment.Text, current.Color);
                }
                else
                {
                    // Different color - save current and start new
                    merged.Add(current);
                    current = new ColoredText(segment.Text, segment.Color);
                }
            }
            
            if (current != null)
            {
                merged.Add(current);
            }
            
            return merged;
        }
        
        /// <summary>
        /// Determines if a space should be added between two segments of different colors.
        /// Returns true if segments need spacing (not punctuation boundaries or newlines).
        /// </summary>
        /// <param name="currentText">Text of the current segment</param>
        /// <param name="nextText">Text of the next segment</param>
        /// <returns>True if a space should be added between segments</returns>
        public static bool ShouldAddSpaceBetweenDifferentColors(string currentText, string nextText)
        {
            if (string.IsNullOrEmpty(currentText) || string.IsNullOrEmpty(nextText))
                return false;
            
            // Don't add space if current ends with punctuation that shouldn't have space after
            char currentLast = currentText[currentText.Length - 1];
            if (currentLast == '!' || currentLast == '?' || currentLast == '.' || currentLast == ',' || 
                currentLast == ':' || currentLast == ';' || currentLast == '\n' || currentLast == '\r')
                return false;
            
            // Don't add space if current already ends with space
            if (char.IsWhiteSpace(currentLast))
                return false;
            
            // Don't add space if next starts with punctuation that shouldn't have space before
            char nextFirst = nextText[0];
            if (nextFirst == '!' || nextFirst == '?' || nextFirst == '.' || 
                nextFirst == ',' || nextFirst == ':' || nextFirst == ';' ||
                nextFirst == '\n' || nextFirst == '\r')
                return false;
            
            // Don't add space if next already starts with space
            if (char.IsWhiteSpace(nextFirst))
                return false;
            
            // Add space for all other cases
            return true;
        }
    }
}

