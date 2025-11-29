namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Calculates layout positions for menu screens
    /// </summary>
    public static class MenuLayoutCalculator
    {
        // Screen dimensions
        public const int SCREEN_WIDTH = 210;
        public const int SCREEN_CENTER = SCREEN_WIDTH / 2;
        public const int LEFT_MARGIN = 2;
        public const int CONTENT_WIDTH = 206;
        
        /// <summary>
        /// Calculates centered menu position
        /// </summary>
        public static (int x, int y) CalculateCenteredMenu(int x, int y, int width, int height, int optionCount, int menuWidth = 0)
        {
            int menuStartX = menuWidth > 0 
                ? x + (width / 2) - (menuWidth / 2)
                : x + (width / 2) - 15; // Default center offset
            int menuStartY = y + (height / 2) - (optionCount / 2);
            return (menuStartX, menuStartY);
        }
        
        /// <summary>
        /// Calculates top-left justified menu position
        /// </summary>
        public static (int x, int y) CalculateTopLeftMenu(int x, int y, int margin = 2)
        {
            return (x + margin, y + margin);
        }
        
        /// <summary>
        /// Calculates centered text position
        /// </summary>
        public static int CalculateCenteredTextX(int x, int width, int textLength)
        {
            return x + (width / 2) - (textLength / 2);
        }
        
        /// <summary>
        /// Calculates panel division for multi-panel layouts
        /// </summary>
        public static int CalculatePanelHeight(int totalHeight, int panelCount, int spacing = 1)
        {
            return (totalHeight - (spacing * (panelCount - 1))) / panelCount;
        }
    }
}

