using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Canvas
{

    /// <summary>
    /// Manages canvas element storage and clearing operations.
    /// </summary>
    public class CanvasElementManager
    {
        private readonly List<CanvasText> textElements;
        private readonly List<CanvasBox> boxElements;
        private readonly List<CanvasProgressBar> progressBars;
        private readonly List<CanvasSegmentedBar> segmentedBars;
        
        public CanvasElementManager()
        {
            this.textElements = new List<CanvasText>();
            this.boxElements = new List<CanvasBox>();
            this.progressBars = new List<CanvasProgressBar>();
            this.segmentedBars = new List<CanvasSegmentedBar>();
        }
        
        /// <summary>
        /// Gets all text elements (mutable list for rendering)
        /// </summary>
        public List<CanvasText> TextElements => textElements;
        
        /// <summary>
        /// Gets all box elements (mutable list for rendering)
        /// </summary>
        public List<CanvasBox> BoxElements => boxElements;
        
        /// <summary>
        /// Gets all progress bar elements (mutable list for rendering)
        /// </summary>
        public List<CanvasProgressBar> ProgressBars => progressBars;

        /// <summary>
        /// Gets all segmented bar elements (mutable list for rendering)
        /// </summary>
        public List<CanvasSegmentedBar> SegmentedBars => segmentedBars;
        
        /// <summary>
        /// Clears all elements
        /// </summary>
        public void Clear()
        {
            textElements.Clear();
            boxElements.Clear();
            progressBars.Clear();
            segmentedBars.Clear();
        }
        
        /// <summary>
        /// Clears text elements within a specific Y range (inclusive)
        /// Clears ALL text in the Y range across the entire canvas width
        /// This is the standard method for clearing panels/areas - always clears full width
        /// </summary>
        public void ClearTextInRange(int startY, int endY)
        {
            textElements.RemoveAll(text => text.Y >= startY && text.Y <= endY);
        }
        
        /// <summary>
        /// Clears text elements within a specific rectangular area (inclusive)
        /// Use this only when you need to clear a specific rectangular region (e.g., center panel only)
        /// For clearing full-width panels, use ClearTextInRange() instead
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            textElements.RemoveAll(text => 
                text.X >= startX && text.X < endX && 
                text.Y >= startY && text.Y < endY);
        }
        
        /// <summary>
        /// Clears progress bars within a specific rectangular area (inclusive)
        /// Used to clear specific areas of the canvas without affecting other panels
        /// </summary>
        public void ClearProgressBarsInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            progressBars.RemoveAll(bar => 
                bar.X >= startX && bar.X < endX && 
                bar.Y >= startY && bar.Y < endY);
        }

        /// <summary>
        /// Clears segmented bars within a specific rectangular area (inclusive).
        /// </summary>
        public void ClearSegmentedBarsInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            segmentedBars.RemoveAll(bar =>
                bar.X >= startX && bar.X < endX &&
                bar.Y >= startY && bar.Y < endY);
        }

        /// <summary>
        /// Clears box elements (borders) within a specific rectangular area.
        /// Removes boxes whose top-left corner falls inside the given rectangle.
        /// Used to clear panel borders when re-rendering without full canvas clear.
        /// </summary>
        public void ClearBoxesInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            boxElements.RemoveAll(box =>
                box.X >= startX && box.X < endX &&
                box.Y >= startY && box.Y < endY);
        }

        /// <summary>
        /// Removes overlay tooltip text in the rectangle (same grid rules as <see cref="ClearTextInArea"/>).
        /// Used to erase a prior hover panel without touching body copy (non-overlay text).
        /// </summary>
        public void ClearOverlayTextInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            textElements.RemoveAll(text =>
                text.IsOverlay &&
                text.X >= startX && text.X < endX &&
                text.Y >= startY && text.Y < endY);
        }

        /// <summary>
        /// Removes overlay boxes in the rectangle (same grid rules as <see cref="ClearBoxesInArea"/>).
        /// </summary>
        public void ClearOverlayBoxesInArea(int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            boxElements.RemoveAll(box =>
                box.IsOverlay &&
                box.X >= startX && box.X < endX &&
                box.Y >= startY && box.Y < endY);
        }

        /// <summary>
        /// Adds a text element
        /// </summary>
        public void AddText(CanvasText text)
        {
            textElements.Add(text);
        }

        /// <summary>
        /// Replaces any overlay text at the same cell, then adds tooltip/detail line text (see <see cref="CanvasText.IsOverlay"/>).
        /// </summary>
        public void AddOverlayText(int x, int y, string text, Color color)
        {
            textElements.RemoveAll(t => t.X == x && t.Y == y && t.IsOverlay);
            textElements.Add(new CanvasText
            {
                X = x,
                Y = y,
                Content = text,
                Color = color,
                IsOverlay = true
            });
        }

        /// <summary>
        /// Appends to overlay text at the same cell when color matches; otherwise replaces overlay text at that cell.
        /// </summary>
        public void AppendOverlayText(int x, int y, string text, Color color)
        {
            var existing = GetFirstText(t => t.X == x && t.Y == y && t.IsOverlay);
            if (existing != null && existing.Color == color)
                existing.Content += text;
            else
                AddOverlayText(x, y, text, color);
        }
        
        /// <summary>
        /// Removes text elements matching a predicate
        /// </summary>
        public void RemoveText(System.Predicate<CanvasText> match)
        {
            textElements.RemoveAll(match);
        }
        
        /// <summary>
        /// Gets the first text element matching a predicate
        /// </summary>
        public CanvasText? GetFirstText(System.Func<CanvasText, bool> predicate)
        {
            return textElements.FirstOrDefault(predicate);
        }
        
        /// <summary>
        /// Adds a box element
        /// </summary>
        public void AddBox(CanvasBox box)
        {
            boxElements.Add(box);
        }

        public bool TryUpdateBox(int x, int y, int width, int height, Color borderColor, Color backgroundColor)
        {
            bool updated = false;
            foreach (var box in boxElements)
            {
                if (box.IsOverlay || box.X != x || box.Y != y || box.Width != width || box.Height != height)
                    continue;

                box.BorderColor = borderColor;
                box.BackgroundColor = backgroundColor;
                updated = true;
            }

            return updated;
        }
        
        /// <summary>
        /// Adds a progress bar element
        /// </summary>
        public void AddProgressBar(CanvasProgressBar progressBar)
        {
            progressBars.Add(progressBar);
        }

        /// <summary>
        /// Adds a segmented bar element
        /// </summary>
        public void AddSegmentedBar(CanvasSegmentedBar segmentedBar)
        {
            segmentedBars.Add(segmentedBar);
        }
        
        /// <summary>
        /// Adds text with automatic overlap removal and adjacent text merging
        /// Removes any existing text at the exact same position to prevent overlap
        /// Merges adjacent text elements on the same line with the same color to prevent gaps
        /// </summary>
        public bool AddTextWithMerging(int x, int y, string text, Color color)
        {
            // Remove any existing text at the exact same position to prevent overlap
            RemoveText(t => t.X == x && t.Y == y);
            
            // Check for adjacent text elements on the same line that can be merged (same color)
            // This prevents gaps between adjacent segments
            var adjacentText = GetFirstText(t => 
                t.Y == y && 
                t.Color == color &&
                (t.X + t.Content.Length == x || x + text.Length == t.X));
            
            if (adjacentText != null)
            {
                // Merge with adjacent text element
                if (adjacentText.X + adjacentText.Content.Length == x)
                {
                    // Current text comes after adjacent text - append to it
                    adjacentText.Content += text;
                    return true; // Merged, no new element needed
                }
                else if (x + text.Length == adjacentText.X)
                {
                    // Current text comes before adjacent text - prepend to it
                    adjacentText.Content = text + adjacentText.Content;
                    adjacentText.X = x;
                    return true; // Merged, no new element needed
                }
            }
            
            // No merge possible, add as new element
            AddText(new CanvasText { X = x, Y = y, Content = text, Color = color });
            return false; // New element added
        }

        /// <summary>
        /// Reconstructs plain text for a character-grid rectangle from stored <see cref="CanvasText"/> elements
        /// (what was drawn in that area). Later elements overwrite overlapping cells.
        /// </summary>
        public string BuildPlainTextSnapshotInRect(int startX, int startY, int width, int height, bool excludeOverlay)
        {
            if (width <= 0 || height <= 0)
                return "";

            var rows = new char[height][];
            for (int r = 0; r < height; r++)
                rows[r] = Enumerable.Repeat(' ', width).ToArray();

            var ordered = textElements
                .Where(t => !excludeOverlay || !t.IsOverlay)
                .Where(t => t.Y >= startY && t.Y < startY + height)
                .OrderBy(t => t.Y)
                .ThenBy(t => t.X);

            foreach (var t in ordered)
            {
                if (string.IsNullOrEmpty(t.Content))
                    continue;
                int row = t.Y - startY;
                if (row < 0 || row >= height)
                    continue;
                for (int i = 0; i < t.Content.Length; i++)
                {
                    int col = t.X + i - startX;
                    if (col >= 0 && col < width)
                        rows[row][col] = t.Content[i];
                }
            }

            var sb = new StringBuilder(height * (width + 2));
            for (int r = 0; r < height; r++)
            {
                string line = new string(rows[r]).TrimEnd();
                sb.AppendLine(line);
            }

            string result = sb.ToString().TrimEnd();
            return result;
        }
    }
}

