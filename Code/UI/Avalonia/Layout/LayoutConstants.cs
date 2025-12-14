namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Layout constants for persistent panels
    /// Left and right panels have fixed widths, center panel dynamically fills remaining horizontal space
    /// </summary>
    public static class LayoutConstants
    {
        // Base dimensions used for proportional calculations (original design size)
        private const int BASE_SCREEN_WIDTH = 210;
        private const int BASE_SCREEN_HEIGHT = 52;
        
        // Base panel dimensions (from original design)
        private const int BASE_LEFT_PANEL_WIDTH = 30;
        private const int BASE_CENTER_PANEL_X = 31;
        private const int BASE_CENTER_PANEL_WIDTH = 136;
        private const int BASE_RIGHT_PANEL_X = 168;
        private const int BASE_RIGHT_PANEL_WIDTH = 30;
        private const int BASE_PANEL_Y = 0; // Panels start at the top of the frame (row 0)
        private const int BASE_PANEL_HEIGHT = 52; // Full grid height (rows 0-51)
        private const int BASE_TITLE_Y = 0;
        
        // Current grid dimensions (defaults to base, can be updated)
        private static int _gridWidth = BASE_SCREEN_WIDTH;
        private static int _gridHeight = BASE_SCREEN_HEIGHT;
        
        // Effective visible width - calculated dynamically from actual canvas bounds
        // This accounts for the difference between grid width and actual visible area
        private static int _effectiveVisibleWidth = BASE_SCREEN_WIDTH;
        
        /// <summary>
        /// Updates the grid dimensions for dynamic scaling
        /// Call this when the canvas size changes
        /// </summary>
        public static void UpdateGridDimensions(int gridWidth, int gridHeight)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
        }
        
        /// <summary>
        /// Updates the effective visible width based on actual canvas pixel width and character width
        /// Call this when the canvas bounds change to calculate how many characters actually fit
        /// </summary>
        /// <param name="canvasPixelWidth">The actual pixel width of the canvas</param>
        /// <param name="charWidth">The pixel width of a single character</param>
        public static void UpdateEffectiveVisibleWidth(double canvasPixelWidth, double charWidth)
        {
            if (charWidth > 0)
            {
                // Calculate how many characters actually fit in the visible area
                int calculatedVisibleWidth = (int)(canvasPixelWidth / charWidth);
                _effectiveVisibleWidth = calculatedVisibleWidth;
            }
        }
        
        /// <summary>
        /// Calculates a proportional value based on base dimension
        /// </summary>
        private static int ScaleWidth(int baseValue) => (int)((double)baseValue / BASE_SCREEN_WIDTH * _gridWidth);
        private static int ScaleHeight(int baseValue) => (int)((double)baseValue / BASE_SCREEN_HEIGHT * _gridHeight);
        
        // Screen dimensions (now dynamic)
        public static int SCREEN_WIDTH => _gridWidth;
        public static int SCREEN_HEIGHT => _gridHeight;
        public static int SCREEN_CENTER => _gridWidth / 2;
        
        // Left panel (Character Info) - fixed width
        public static int LEFT_PANEL_X => 1;
        public static int LEFT_PANEL_Y => 0; // Always start at row 0 (top of grid)
        public static int LEFT_PANEL_WIDTH => BASE_LEFT_PANEL_WIDTH; // Fixed width, not scaled
        public static int LEFT_PANEL_HEIGHT => _gridHeight + 1; // One character taller (rows 0 to _gridHeight)
        
        // Effective visible width - dynamically calculated from actual canvas bounds
        // This ensures panels fit within the actual visible area, not just the grid width
        private static int EffectiveVisibleWidth => _effectiveVisibleWidth;
        
        // Center panel (Dynamic Content) - dynamic width fills remaining space
        // Positioned right after left panel with 1 char gap (matching original design)
        public static int CENTER_PANEL_X => LEFT_PANEL_X + LEFT_PANEL_WIDTH + 1; // +1 to match original gap
        public static int CENTER_PANEL_Y => 0; // Always start at row 0 (top of grid)
        // Calculate width using effective visible width to ensure right panel stays within visible area
        // Total effective width = LEFT_PANEL_WIDTH + gap(1) + CENTER_PANEL_WIDTH + gap(1) + RIGHT_PANEL_WIDTH
        public static int CENTER_PANEL_WIDTH => EffectiveVisibleWidth - LEFT_PANEL_WIDTH - RIGHT_PANEL_WIDTH-3; // Accounts for gaps between panels
        public static int CENTER_PANEL_HEIGHT => _gridHeight + 1; // One character taller (rows 0 to _gridHeight)
        
        // Right panel (Dungeon/Enemy Info) - fixed width, positioned at visible right edge
        public static int RIGHT_PANEL_X => EffectiveVisibleWidth - BASE_RIGHT_PANEL_WIDTH; // Positioned at effective visible right edge
        public static int RIGHT_PANEL_Y => 0; // Always start at row 0 (top of grid)
        public static int RIGHT_PANEL_WIDTH => BASE_RIGHT_PANEL_WIDTH; // Fixed width, not scaled
        public static int RIGHT_PANEL_HEIGHT => _gridHeight + 1; // One character taller (rows 0 to _gridHeight)
        
        // Top bar for title
        public static int TITLE_Y => ScaleHeight(BASE_TITLE_Y);
    }
}

