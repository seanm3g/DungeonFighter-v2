using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the expanded/collapsed state of the stats panel
    /// Tracks stats area bounds for click detection
    /// </summary>
    public class StatsPanelStateManager
    {
        private bool isExpanded = false;
        private int statsAreaX = -1;
        private int statsAreaY = -1;
        private int statsAreaWidth = -1;
        private int statsAreaHeight = -1;
        
        /// <summary>
        /// Gets or sets whether the stats panel is expanded
        /// </summary>
        public bool IsExpanded
        {
            get => isExpanded;
            set => isExpanded = value;
        }
        
        /// <summary>
        /// Toggles the expansion state
        /// </summary>
        public void ToggleExpansion()
        {
            isExpanded = !isExpanded;
        }
        
        /// <summary>
        /// Sets the stats area bounds for click detection
        /// </summary>
        public void SetStatsAreaBounds(int x, int y, int width, int height)
        {
            statsAreaX = x;
            statsAreaY = y;
            statsAreaWidth = width;
            statsAreaHeight = height;
        }
        
        /// <summary>
        /// Gets the stats area bounds
        /// </summary>
        public (int x, int y, int width, int height) GetStatsAreaBounds()
        {
            return (statsAreaX, statsAreaY, statsAreaWidth, statsAreaHeight);
        }
        
        /// <summary>
        /// Checks if the given coordinates are within the stats area
        /// </summary>
        public bool IsInStatsArea(int x, int y)
        {
            if (statsAreaX < 0 || statsAreaY < 0 || statsAreaWidth < 0 || statsAreaHeight < 0)
                return false;
                
            return x >= statsAreaX && x < statsAreaX + statsAreaWidth &&
                   y >= statsAreaY && y < statsAreaY + statsAreaHeight;
        }
        
        /// <summary>
        /// Resets the stats area bounds (call when stats area is not visible)
        /// </summary>
        public void ResetStatsAreaBounds()
        {
            statsAreaX = -1;
            statsAreaY = -1;
            statsAreaWidth = -1;
            statsAreaHeight = -1;
        }
    }
}
