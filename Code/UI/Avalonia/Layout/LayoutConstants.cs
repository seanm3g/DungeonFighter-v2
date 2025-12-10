namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Layout constants for persistent panels
    /// Now dynamically calculates values based on canvas grid dimensions while maintaining proportions
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
        private const int BASE_PANEL_Y = 2;
        private const int BASE_PANEL_HEIGHT = 53; // Height from row 2 to row 52 (inclusive)
        private const int BASE_TITLE_Y = 0;
        
        // Current grid dimensions (defaults to base, can be updated)
        private static int _gridWidth = BASE_SCREEN_WIDTH;
        private static int _gridHeight = BASE_SCREEN_HEIGHT;
        
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
        /// Calculates a proportional value based on base dimension
        /// </summary>
        private static int ScaleWidth(int baseValue) => (int)((double)baseValue / BASE_SCREEN_WIDTH * _gridWidth);
        private static int ScaleHeight(int baseValue) => (int)((double)baseValue / BASE_SCREEN_HEIGHT * _gridHeight);
        
        // Screen dimensions (now dynamic)
        public static int SCREEN_WIDTH => _gridWidth;
        public static int SCREEN_HEIGHT => _gridHeight;
        public static int SCREEN_CENTER => _gridWidth / 2;
        
        // Left panel (Character Info) - scales proportionally
        public static int LEFT_PANEL_X => 0;
        public static int LEFT_PANEL_Y => ScaleHeight(BASE_PANEL_Y);
        public static int LEFT_PANEL_WIDTH => ScaleWidth(BASE_LEFT_PANEL_WIDTH);
        public static int LEFT_PANEL_HEIGHT => _gridHeight - LEFT_PANEL_Y; // Extends to bottom
        
        // Center panel (Dynamic Content) - scales proportionally
        public static int CENTER_PANEL_X => ScaleWidth(BASE_CENTER_PANEL_X);
        public static int CENTER_PANEL_Y => ScaleHeight(BASE_PANEL_Y);
        public static int CENTER_PANEL_WIDTH => ScaleWidth(BASE_CENTER_PANEL_WIDTH);
        public static int CENTER_PANEL_HEIGHT => _gridHeight - CENTER_PANEL_Y; // Extends to bottom
        
        // Right panel (Dungeon/Enemy Info) - scales proportionally
        public static int RIGHT_PANEL_X => ScaleWidth(BASE_RIGHT_PANEL_X);
        public static int RIGHT_PANEL_Y => ScaleHeight(BASE_PANEL_Y);
        public static int RIGHT_PANEL_WIDTH => ScaleWidth(BASE_RIGHT_PANEL_WIDTH);
        public static int RIGHT_PANEL_HEIGHT => _gridHeight - RIGHT_PANEL_Y; // Extends to bottom
        
        // Top bar for title
        public static int TITLE_Y => ScaleHeight(BASE_TITLE_Y);
    }
}

