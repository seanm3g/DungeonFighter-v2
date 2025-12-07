using System.Collections.Generic;
using System.Linq;
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
        
        public CanvasElementManager()
        {
            this.textElements = new List<CanvasText>();
            this.boxElements = new List<CanvasBox>();
            this.progressBars = new List<CanvasProgressBar>();
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
        /// Clears all elements
        /// </summary>
        public void Clear()
        {
            textElements.Clear();
            boxElements.Clear();
            progressBars.Clear();
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
        /// Adds a text element
        /// </summary>
        public void AddText(CanvasText text)
        {
            textElements.Add(text);
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
        
        /// <summary>
        /// Adds a progress bar element
        /// </summary>
        public void AddProgressBar(CanvasProgressBar progressBar)
        {
            progressBars.Add(progressBar);
        }
    }
}

