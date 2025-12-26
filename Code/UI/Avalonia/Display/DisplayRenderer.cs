using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Handles rendering of display buffer content to the canvas
    /// Pure rendering logic - no state management or timing
    /// Applies animation effects to dungeon name when rendering
    /// </summary>
    public class DisplayRenderer
    {
        private readonly ColoredTextWriter textWriter;
        private readonly DungeonSelectionAnimationState animationState;
        
        public DisplayRenderer(ColoredTextWriter textWriter)
        {
            this.textWriter = textWriter;
            this.animationState = DungeonSelectionAnimationState.Instance;
        }
        
        /// <summary>
        /// Renders the display buffer content to the specified area
        /// Now uses structured ColoredText segments directly to eliminate round-trip conversions
        /// </summary>
        /// <param name="buffer">The display buffer to render</param>
        /// <param name="contentX">X position of content area</param>
        /// <param name="contentY">Y position of content area</param>
        /// <param name="contentWidth">Width of content area</param>
        /// <param name="contentHeight">Height of content area</param>
        /// <param name="clearContent">Whether to clear the content area before rendering. Defaults to true.</param>
        public void Render(DisplayBuffer buffer, int contentX, int contentY, int contentWidth, int contentHeight, bool clearContent = true)
        {
            var linesToRender = buffer.GetLast(buffer.MaxLines);
            if (linesToRender.Count == 0)
            {
                // Even if no content, clear the area to remove old text
                if (clearContent)
                {
                    // Use contentHeight + 1 to ensure we clear the full area (endY is exclusive)
                    ClearContentArea(contentX, contentY, contentWidth, contentHeight + 1);
                }
                return;
            }
            
            int availableWidth = contentWidth - 2;
            
            // Calculate total height needed for all lines (accounting for text wrapping)
            int totalHeight = 0;
            var lineHeights = new List<int>();
            foreach (var segments in linesToRender)
            {
                int linesNeeded = CalculateWrappedLineCount(segments, availableWidth);
                lineHeights.Add(linesNeeded);
                totalHeight += linesNeeded;
            }
            
            // Calculate scroll offset
            int scrollOffset = CalculateScrollOffset(buffer, totalHeight, contentHeight);
            
            // Clear the content area BEFORE calculating render positions
            // Always clear to ensure old text doesn't show through when scrolling
            // Clear a bit more area to ensure we catch any text that might be above due to scroll offset
            // This prevents old text from showing when content is rendered at a different Y position
            if (clearContent)
            {
                // Clear the full content area plus a buffer above and below to catch any overflow
                // Use contentHeight + 1 to ensure we clear the full area (endY is exclusive)
                // Clear from slightly above contentY to slightly below to catch any text that might shift
                int clearStartY = Math.Max(0, contentY - 2); // Clear 2 lines above to catch any overflow
                int clearEndY = contentY + contentHeight + 2; // Clear 2 lines below as well
                int clearHeight = clearEndY - clearStartY + 1;
                ClearContentArea(contentX, clearStartY, contentWidth, clearHeight);
            }
            
            // Render lines, starting from the scroll offset position
            // Always start at contentY to ensure consistent positioning
            int y = contentY;
            int currentHeight = 0;
            
            for (int i = 0; i < linesToRender.Count; i++)
            {
                var segments = linesToRender[i];
                int linesNeeded = lineHeights[i];
                
                // Skip lines until we reach the scroll offset
                if (currentHeight + linesNeeded <= scrollOffset)
                {
                    currentHeight += linesNeeded;
                    continue;
                }
                
                // Handle partial line scrolling: if scroll offset is in the middle of this line
                int partialOffset = scrollOffset - currentHeight;
                if (partialOffset > 0 && partialOffset < linesNeeded)
                {
                    // We're scrolling into the middle of this wrapped message
                    // For now, skip the partial lines and start from the next full line
                    // This prevents text offset issues
                    currentHeight += linesNeeded;
                    continue;
                }
                
                // Render this line if it fits in the viewport
                if (y < contentY + contentHeight)
                {
                    // Use consistent X position - contentX is the content area start,
                    // add 1 for left padding to match availableWidth calculation (contentWidth - 2)
                    // This ensures text doesn't shift horizontally when scrolling
                    // Use exact integer values to prevent floating point rounding issues
                    int renderX = contentX + 1;
                    int renderY = y;
                    
                    // Check if this is the dungeon name line and apply animation
                    var animatedSegments = ApplyDungeonNameAnimation(segments, renderX, renderY);
                    
                    // Ensure we're using exact coordinates, not calculated positions
                    int linesRendered = textWriter.WriteLineColoredWrapped(animatedSegments, renderX, renderY, availableWidth);
                    
                    // Only advance y if we actually rendered something
                    if (linesRendered > 0)
                    {
                        y += linesRendered;
                    }
                    currentHeight += linesNeeded;
                }
                else
                {
                    // No more room in viewport
                    break;
                }
            }
        }
        
        /// <summary>
        /// Calculates the scroll offset based on buffer state
        /// </summary>
        private int CalculateScrollOffset(DisplayBuffer buffer, int totalHeight, int contentHeight)
        {
            int maxScrollOffset = Math.Max(0, totalHeight - contentHeight);
            
            if (buffer.IsManualScrolling)
            {
                // Use manual scroll offset, clamped to valid range
                // Don't call SetScrollOffset here as it can reset the state incorrectly
                // The offset should only be set by explicit scroll actions, not during rendering
                int scrollOffset = Math.Max(0, Math.Min(buffer.ManualScrollOffset, maxScrollOffset));
                return scrollOffset;
            }
            else if (totalHeight > contentHeight)
            {
                // Auto-scroll to bottom
                return maxScrollOffset;
            }
            else
            {
                // Content fits in viewport, no scrolling needed
                return 0;
            }
        }
        
        /// <summary>
        /// Calculates how many lines a message will take when wrapped
        /// Uses the same wrapping logic as WriteLineColoredWrapped for accuracy
        /// </summary>
        private int CalculateWrappedLineCount(List<ColoredText> segments, int maxWidth)
        {
            if (segments == null || segments.Count == 0)
                return 1; // Empty lines still take one line
            
            // Use actual wrapping logic to get accurate line count
            // This matches the behavior of WriteLineColoredWrapped
            var wrappedLines = textWriter.WrapColoredSegments(segments, maxWidth);
            return wrappedLines.Count;
        }
        
        /// <summary>
        /// Calculates the maximum scroll offset for the given buffer
        /// This is the total height of all wrapped lines minus the viewport height
        /// </summary>
        public int CalculateMaxScrollOffset(DisplayBuffer buffer, int contentWidth, int contentHeight)
        {
            var linesToRender = buffer.GetLast(buffer.MaxLines);
            if (linesToRender.Count == 0)
                return 0;
            
            int availableWidth = contentWidth - 2;
            
            // Calculate total height needed for all lines (accounting for text wrapping)
            int totalHeight = 0;
            foreach (var segments in linesToRender)
            {
                int linesNeeded = CalculateWrappedLineCount(segments, availableWidth);
                totalHeight += linesNeeded;
            }
            
            return Math.Max(0, totalHeight - contentHeight);
        }
        
        /// <summary>
        /// Clears the content area by removing text elements in the specified rectangular area
        /// This properly removes existing text instead of just overlaying spaces
        /// Only clears the center panel area, preserving left and right panels
        /// 
        /// Note: contentHeight should include any buffer needed (e.g., contentHeight + 1)
        /// to ensure the full area is cleared since endY is exclusive
        /// </summary>
        private void ClearContentArea(int contentX, int contentY, int contentWidth, int contentHeight)
        {
            // Clear text elements only in the specified rectangular area
            // This removes existing text elements (like room information) before rendering combat
            // while preserving the left panel (character info) and right panel (location/enemy info)
            // 
            // ClearTextInArea uses exclusive endY, so we need to ensure we clear the full height
            // by potentially adding 1 to contentHeight when calling this method
            textWriter.ClearTextInArea(contentX, contentY, contentWidth, contentHeight);
        }
        
        /// <summary>
        /// Applies animation effects to dungeon/room entry headers and dungeon names
        /// Animates "ENTERING DUNGEON" and "ENTERING ROOM" lines, as well as "Dungeon: " lines
        /// Similar to how DungeonSelectionRenderer animates dungeon names
        /// </summary>
        private List<ColoredText> ApplyDungeonNameAnimation(List<ColoredText> segments, int x, int y)
        {
            if (segments == null || segments.Count == 0)
                return segments ?? new List<ColoredText>();
            
            string fullText = string.Join("", segments.Select(s => s.Text));
            
            // Check if this is an "ENTERING DUNGEON" or "ENTERING ROOM" header line
            bool isEnteringHeader = fullText.Contains("ENTERING DUNGEON", StringComparison.OrdinalIgnoreCase) ||
                                   fullText.Contains("ENTERING ROOM", StringComparison.OrdinalIgnoreCase);
            
            // Check if this line starts with "Dungeon: " or "Room: "
            bool isDungeonNameLine = fullText.StartsWith("Dungeon: ", StringComparison.OrdinalIgnoreCase);
            bool isRoomNameLine = fullText.StartsWith("Room: ", StringComparison.OrdinalIgnoreCase);
            
            // Skip animation for "ENTERING DUNGEON" and "ENTERING ROOM" headers - they should not glow
            if (isEnteringHeader)
                return segments;
            
            if (!isDungeonNameLine && !isRoomNameLine)
                return segments;
            
            // Find where the dungeon/room name starts (after "Dungeon: " or "Room: ")
            int charPosition = 0;
            bool foundPrefix = false;
            string prefix = isDungeonNameLine ? "Dungeon: " : "Room: ";
            
            var result = new List<ColoredText>();
            
            foreach (var segment in segments)
            {
                if (!foundPrefix)
                {
                    // Check if this segment contains or completes "Dungeon: " or "Room: "
                    string segmentText = segment.Text;
                    int prefixIndex = segmentText.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    
                    if (prefixIndex >= 0)
                    {
                        // Found the prefix in this segment
                        foundPrefix = true;
                        
                        // Add the prefix part (including "Dungeon: " or "Room: ")
                        if (prefixIndex > 0)
                        {
                            result.Add(new ColoredText(segmentText.Substring(0, prefixIndex), segment.Color));
                        }
                        result.Add(new ColoredText(prefix, segment.Color));
                        
                        // Handle the rest of this segment (which may contain part of the name)
                        int afterPrefix = prefixIndex + prefix.Length;
                        if (afterPrefix < segmentText.Length)
                        {
                            string namePart = segmentText.Substring(afterPrefix);
                            charPosition = prefix.Length;
                            ApplyAnimationToText(namePart, segment.Color, result, charPosition, y);
                            charPosition += namePart.Length;
                        }
                    }
                    else
                    {
                        // Haven't found the prefix yet, add segment as-is
                        result.Add(segment);
                        charPosition += segmentText.Length;
                    }
                }
                else
                {
                    // We're past the prefix, animate the name
                    ApplyAnimationToText(segment.Text, segment.Color, result, charPosition, y);
                    charPosition += segment.Text.Length;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Applies animation effects character-by-character to text
        /// Similar to DungeonSelectionRenderer's animation logic
        /// </summary>
        private void ApplyAnimationToText(string text, Color baseColor, List<ColoredText> result, int startCharPosition, int lineOffset)
        {
            // Generate a small random offset for this animated element based on line position
            // This makes each animated element look different from others
            // Use a hash of the line offset to get a consistent but varied offset per line
            int elementOffset = GetElementRandomOffset(lineOffset);
            
            foreach (char c in text)
            {
                // Add element offset to position to make this element's animation unique
                int adjustedPosition = startCharPosition + elementOffset;
                
                // Get brightness mask adjustment from centralized state
                float brightnessAdjustment = animationState.GetBrightnessAt(adjustedPosition, lineOffset);
                double brightnessFactor = 1.0 + (brightnessAdjustment / 100.0) * 2.0;
                brightnessFactor = Math.Max(0.3, Math.Min(2.0, brightnessFactor));
                
                // Get position-based undulation brightness (creates sine wave across text)
                double undulationBrightness = animationState.GetUndulationBrightnessAt(adjustedPosition, lineOffset);
                brightnessFactor += undulationBrightness * 3.0;
                brightnessFactor = Math.Max(0.3, Math.Min(2.0, brightnessFactor));
                
                // Apply brightness adjustments to color
                Color adjustedColor = AdjustColorBrightness(baseColor, brightnessFactor);
                result.Add(new ColoredText(c.ToString(), adjustedColor));
                
                startCharPosition++;
            }
        }
        
        /// <summary>
        /// Generates a small random offset for an animated element based on its line position
        /// Uses a simple hash to ensure the same line always gets the same offset
        /// Returns a value between -50 and +50 to create visual variation
        /// </summary>
        private int GetElementRandomOffset(int lineOffset)
        {
            // Use a simple hash function to generate a consistent but varied offset
            // Multiply by a prime number and use modulo to get a pseudo-random value
            int hash = (lineOffset * 7919 + 12345) % 101; // Prime number 7919, offset 12345, modulo 101 gives -50 to +50 range
            return hash - 50; // Shift to -50 to +50 range
        }
        
        /// <summary>
        /// Adjusts the brightness of a color by a factor
        /// Same logic as DungeonSelectionRenderer
        /// </summary>
        private Color AdjustColorBrightness(Color color, double factor)
        {
            factor = Math.Max(0.0, Math.Min(2.0, factor)); // Clamp between 0 and 2
            
            byte r = (byte)Math.Min(255, (int)(color.R * factor));
            byte g = (byte)Math.Min(255, (int)(color.G * factor));
            byte b = (byte)Math.Min(255, (int)(color.B * factor));
            
            return Color.FromRgb(r, g, b);
        }
    }
}

