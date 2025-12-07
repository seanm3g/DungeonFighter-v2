namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Layout constants for persistent panels
    /// </summary>
    public static class LayoutConstants
    {
        // Screen dimensions
        public const int SCREEN_WIDTH = 210;
        public const int SCREEN_HEIGHT = 52;  // Reduced from 60 to make panels less tall
        public const int SCREEN_CENTER = SCREEN_WIDTH / 2;  // The center point for menus
        
        // Left panel (Character Info) - wider for better visibility
        public const int LEFT_PANEL_X = 0;
        public const int LEFT_PANEL_Y = 2;
        public const int LEFT_PANEL_WIDTH = 30;  // Increased from 27
        public const int LEFT_PANEL_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Center panel (Dynamic Content) - narrower to give more space to side panels
        public const int CENTER_PANEL_X = 31;     // After left panel + 1 space
        public const int CENTER_PANEL_Y = 2;
        public const int CENTER_PANEL_WIDTH = 136; // Further reduced from 148 to prevent right panel cutoff
        public const int CENTER_PANEL_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Right panel (Dungeon/Enemy Info) - wider for better visibility, positioned after center panel
        public const int RIGHT_PANEL_X = 168;     // After center panel (31 + 142 + 1 gap)
        public const int RIGHT_PANEL_Y = 2;
        public const int RIGHT_PANEL_WIDTH = 30;  // Increased from 27
        public const int RIGHT_PANEL_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Top bar for title
        public const int TITLE_Y = 0;
    }
}

