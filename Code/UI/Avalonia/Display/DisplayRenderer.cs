using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Renderers.Text;
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
        
        public DisplayRenderer(ColoredTextWriter textWriter)
        {
            this.textWriter = textWriter;
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
            
            // Clear the content area BEFORE calculating render positions.
            // Do not extend the clear band into the action-info strip (rows above framed center content).
            if (clearContent)
            {
                int scrollOverflowPad = Math.Max(0, contentY - 2);
                int firstRowBelowActionStrip = LayoutConstants.ACTION_INFO_Y + LayoutConstants.ACTION_INFO_HEIGHT;
                int clearStartY = contentY >= firstRowBelowActionStrip
                    ? Math.Max(scrollOverflowPad, firstRowBelowActionStrip)
                    : scrollOverflowPad;
                int clearEndY = contentY + contentHeight;
                int clearHeight = clearEndY - clearStartY;
                if (clearHeight > 0)
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
        /// Scrolling starts when content reaches 2 lines from the bottom
        /// When scrolling, maintains a gap at the bottom by only showing contentHeight - 2 lines
        /// This ensures there's always 2 blank lines at the bottom when text reaches the threshold
        /// </summary>
        private int CalculateScrollOffset(DisplayBuffer buffer, int totalHeight, int contentHeight)
        {
            // Calculate the threshold at which scrolling should start (2 lines from bottom)
            const int BOTTOM_PADDING_LINES = 2;
            int scrollThreshold = contentHeight - BOTTOM_PADDING_LINES;
            // Calculate the visible area we want to show (all but bottom 2 lines)
            int visibleArea = contentHeight - BOTTOM_PADDING_LINES;
            // Maximum scroll offset if we were to fill the entire viewport
            int maxScrollOffsetFull = Math.Max(0, totalHeight - contentHeight);
            // Scroll offset to maintain bottom gap (only show visibleArea lines)
            int maxScrollOffsetWithGap = Math.Max(0, totalHeight - visibleArea);
            
            if (buffer.IsManualScrolling)
            {
                // Use manual scroll offset, clamped to valid range
                // Don't call SetScrollOffset here as it can reset the state incorrectly
                // The offset should only be set by explicit scroll actions, not during rendering
                int scrollOffset = Math.Max(0, Math.Min(buffer.ManualScrollOffset, maxScrollOffsetFull));
                return scrollOffset;
            }
            else if (totalHeight > scrollThreshold)
            {
                // Auto-scroll when content exceeds threshold (2 lines from bottom)
                // Scroll to maintain a gap at the bottom (only show visibleArea lines)
                // This ensures there's always 2 blank lines at the bottom when text reaches the threshold
                return maxScrollOffsetWithGap;
            }
            else
            {
                // Content fits within threshold, no scrolling needed
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
        /// This is the total height of all wrapped lines minus the visible area (contentHeight - 2 lines)
        /// Scrolling starts when content reaches 2 lines from the bottom
        /// When scrolling, maintains a gap at the bottom by only showing contentHeight - 2 lines
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
            
            // Calculate the threshold at which scrolling should start (2 lines from bottom)
            const int BOTTOM_PADDING_LINES = 2;
            int scrollThreshold = contentHeight - BOTTOM_PADDING_LINES;
            // Calculate the visible area we want to show (all but bottom 2 lines)
            int visibleArea = contentHeight - BOTTOM_PADDING_LINES;
            
            // If content exceeds the threshold, calculate scroll offset
            // Scroll to maintain a gap at the bottom (only show visibleArea lines)
            if (totalHeight > scrollThreshold)
            {
                return Math.Max(0, totalHeight - visibleArea);
            }
            
            // Content fits within threshold, no scrolling needed
            return 0;
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

            // Level-up banner: gold "LEVEL UP!" with dungeon-selection-style shimmer (exact phrase from LevelUpDisplayColoredText)
            if (fullText.Contains("LEVEL UP!", StringComparison.Ordinal))
            {
                var animated = new List<ColoredText>();
                int charPos = 0;
                foreach (var segment in segments)
                {
                    UndulatingTextHelper.AppendAnimatedChars(segment.Text, segment.Color, animated, ref charPos, y, null);
                }
                return animated;
            }
            
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
                            UndulatingTextHelper.AppendAnimatedChars(namePart, segment.Color, result, ref charPosition, y, segment.SourceTemplate);
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
                    UndulatingTextHelper.AppendAnimatedChars(segment.Text, segment.Color, result, ref charPosition, y, segment.SourceTemplate);
                }
            }
            
            return result;
        }
    }
}

