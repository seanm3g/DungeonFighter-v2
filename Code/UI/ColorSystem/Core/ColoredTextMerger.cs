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
        /// Uses a single-pass algorithm for better performance.
        /// </summary>
        /// <param name="segments">List of ColoredText segments to merge</param>
        /// <returns>Merged and normalized list of segments</returns>
        public static List<ColoredText> MergeAdjacentSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return segments ?? new List<ColoredText>();
            
            var merged = new List<ColoredText>(segments.Count); // Pre-allocate with estimated capacity
            ColoredText? currentSegment = null;
            
            // Single pass: merge, normalize spaces, and handle empty segments
            foreach (var segment in segments)
            {
                // Skip empty segments
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Normalize multiple spaces within segment text (preserve other whitespace)
                string normalizedText = Regex.Replace(segment.Text, @" +", " ");
                
                if (currentSegment == null)
                {
                    // First segment - normalize and store
                    currentSegment = new ColoredText(normalizedText, segment.Color);
                }
                else if (ColorValidator.AreColorsEqual(currentSegment.Color, segment.Color))
                {
                    // Same color - merge with current segment, normalizing spaces at boundary
                    string currentText = currentSegment.Text;
                    
                    // Normalize boundary spaces: if both end/start with space, use only one
                    if (currentText.Length > 0 && normalizedText.Length > 0)
                    {
                        bool currentEndsWithSpace = char.IsWhiteSpace(currentText[currentText.Length - 1]);
                        bool nextStartsWithSpace = char.IsWhiteSpace(normalizedText[0]);
                        
                        if (currentEndsWithSpace && nextStartsWithSpace)
                        {
                            // Remove one space to avoid double spacing
                            currentText = currentText.TrimEnd() + " " + normalizedText.TrimStart();
                        }
                        else
                        {
                            currentText = currentText + normalizedText;
                        }
                    }
                    else
                    {
                        currentText = currentText + normalizedText;
                    }
                    
                    currentSegment = new ColoredText(currentText, currentSegment.Color);
                }
                else
                {
                    // Different color - finalize current segment and start new one
                    // Normalize spaces at boundary before adding
                    string currentText = currentSegment.Text;
                    bool needsSpace = ShouldAddSpaceBetweenDifferentColors(currentText, normalizedText);
                    
                    if (needsSpace)
                    {
                        // Ensure space at end of current segment
                        if (currentText.Length == 0 || !char.IsWhiteSpace(currentText[currentText.Length - 1]))
                        {
                            currentText = currentText + " ";
                        }
                    }
                    else
                    {
                        // Remove trailing space if not needed
                        currentText = currentText.TrimEnd();
                    }
                    
                    // Add finalized segment (only if it has content)
                    if (currentText.Length > 0)
                    {
                        merged.Add(new ColoredText(currentText, currentSegment.Color));
                    }
                    
                    // Start new segment
                    currentSegment = new ColoredText(normalizedText, segment.Color);
                }
            }
            
            // Finalize last segment
            if (currentSegment != null && !string.IsNullOrEmpty(currentSegment.Text))
            {
                merged.Add(currentSegment);
            }
            
            // Final pass: merge adjacent whitespace-only segments and remove empty segments
            // This is a lightweight cleanup pass that only handles edge cases
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var segment = merged[i];
                
                // Remove empty segments
                if (string.IsNullOrEmpty(segment.Text))
                {
                    merged.RemoveAt(i);
                    continue;
                }
                
                // Merge adjacent whitespace-only segments
                if (i > 0 && segment.Text.Trim().Length == 0)
                {
                    var prev = merged[i - 1];
                    if (prev.Text.Trim().Length == 0)
                    {
                        // Both are whitespace - merge into one
                        merged[i - 1] = new ColoredText(" ", Colors.White);
                        merged.RemoveAt(i);
                    }
                }
            }
            
            return merged;
        }
        
        /// <summary>
        /// Merges only segments with the same color, preserving space segments separately.
        /// This is used by ColoredTextBuilder which handles spacing differently.
        /// IMPORTANT: Space segments are ALWAYS preserved as separate segments to ensure proper spacing.
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
                
                // Check if this is a space-only segment (whitespace only, no actual content)
                // This is critical for preserving spacing between words
                bool isSpaceSegment = segment.Text.Trim().Length == 0 && segment.Text.Length > 0;
                
                if (current == null)
                {
                    if (!isSpaceSegment)
                    {
                        // Start with a non-space segment
                        current = new ColoredText(segment.Text, segment.Color);
                    }
                    else
                    {
                        // Start with a space segment - add it directly and keep it separate
                        merged.Add(segment);
                    }
                }
                else if (isSpaceSegment)
                {
                    // Space segment - ALWAYS keep as separate segment to ensure proper spacing
                    // This prevents spaces from being merged into adjacent text segments
                    merged.Add(current);
                    merged.Add(segment);
                    current = null; // Reset current so next segment starts fresh
                }
                else if (ColorValidator.AreColorsEqual(current.Color, segment.Color))
                {
                    // Same color and both are non-space segments - merge them
                    // Note: Space segments are handled above, so we know both are non-space here
                    current = new ColoredText(current.Text + segment.Text, current.Color);
                }
                else
                {
                    // Different color - finalize current segment and start new one
                    merged.Add(current);
                    current = new ColoredText(segment.Text, segment.Color);
                }
            }
            
            // Finalize last segment if it exists
            if (current != null)
            {
                merged.Add(current);
            }
            
            return merged;
        }
        
        /// <summary>
        /// Determines if a space should be added between two segments of different colors.
        /// Uses the centralized spacing system from CombatLogSpacingManager.
        /// </summary>
        /// <param name="currentText">Text of the current segment</param>
        /// <param name="nextText">Text of the next segment</param>
        /// <returns>True if a space should be added between segments</returns>
        public static bool ShouldAddSpaceBetweenDifferentColors(string currentText, string nextText)
        {
            // Delegate to centralized spacing manager (no word boundary check needed for different colors)
            return RPGGame.UI.CombatLogSpacingManager.ShouldAddSpaceBetween(currentText, nextText, checkWordBoundary: false);
        }
    }
}

