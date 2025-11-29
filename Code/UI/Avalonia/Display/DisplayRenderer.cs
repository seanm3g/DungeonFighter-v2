using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Handles rendering of display buffer content to the canvas
    /// Pure rendering logic - no state management or timing
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
            
            // Clear the content area BEFORE calculating render positions
            // Clear a bit more area to ensure we catch any text that might be above due to scroll offset
            // This prevents old text from showing when content is rendered at a different Y position
            if (clearContent)
            {
                // Clear the full content area plus a small buffer above to catch any overflow
                // Use contentHeight + 1 to ensure we clear the full area (endY is exclusive)
                ClearContentArea(contentX, contentY, contentWidth, contentHeight + 1);
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
                
                // Render this line if it fits in the viewport
                if (y < contentY + contentHeight)
                {
                    int linesRendered = textWriter.WriteLineColoredWrapped(segments, contentX + 1, y, availableWidth);
                    y += linesRendered;
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
            if (buffer.IsManualScrolling)
            {
                // Use manual scroll offset, clamped to valid range
                int maxScrollOffset = Math.Max(0, totalHeight - contentHeight);
                int scrollOffset = Math.Max(0, Math.Min(buffer.ManualScrollOffset, maxScrollOffset));
                buffer.SetScrollOffset(scrollOffset, maxScrollOffset);
                return scrollOffset;
            }
            else if (totalHeight > contentHeight)
            {
                // Auto-scroll to bottom
                return totalHeight - contentHeight;
            }
            else
            {
                // Content fits in viewport, no scrolling needed
                return 0;
            }
        }
        
        /// <summary>
        /// Calculates how many lines a message will take when wrapped
        /// Now uses structured ColoredText segments directly
        /// </summary>
        private int CalculateWrappedLineCount(List<ColoredText> segments, int maxWidth)
        {
            if (segments == null || segments.Count == 0)
                return 1; // Empty lines still take one line
            
            // Calculate display length from segments
            int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
            int wrappedLines = Math.Max(1, (int)Math.Ceiling((double)displayLength / maxWidth));
            
            return wrappedLines;
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
    }
}

