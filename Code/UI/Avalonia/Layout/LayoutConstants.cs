using System;

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
        private const int BASE_LEFT_PANEL_WIDTH = 32; // Increased from 30 to provide more space for item names
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
                // Floor with epsilon so float noise just under gridWidth*charWidth does not drop a column.
                int calculatedVisibleWidth = (int)System.Math.Floor(canvasPixelWidth / charWidth + 1e-6);
                _effectiveVisibleWidth = System.Math.Clamp(calculatedVisibleWidth, 1, _gridWidth);
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
        
        // Center column: action-info strip at top, combat log (framed center panel) below. Positioned right after left panel with 1 char gap.
        public static int CENTER_PANEL_X => LEFT_PANEL_X + LEFT_PANEL_WIDTH + 1; // +1 to match original gap
        // Calculate width using effective visible width to ensure right panel stays within visible area
        // Total effective width = LEFT_PANEL_WIDTH + gap(1) + CENTER_PANEL_WIDTH + gap(1) + RIGHT_PANEL_WIDTH
        // Clamp: before first Arrange or very narrow windows, raw width can go negative and breaks strip hit-tests / clears.
        public static int CENTER_PANEL_WIDTH => Math.Max(1, EffectiveVisibleWidth - LEFT_PANEL_WIDTH - RIGHT_PANEL_WIDTH - 3); // Accounts for gaps between panels
        private const int BASE_ACTION_INFO_STRIP_HEIGHT = 11;
        public static int ACTION_INFO_STRIP_HEIGHT => BASE_ACTION_INFO_STRIP_HEIGHT;
        /// <summary>First row of the action-info strip (top of center column, aligned with side panels).</summary>
        public static int ACTION_INFO_Y => 0;
        /// <summary>Combat log and main center content start below the action-info strip.</summary>
        public static int CENTER_PANEL_Y => ACTION_INFO_STRIP_HEIGHT;
        /// <summary>Height of the framed center panel (combat log); leaves room for the action-info strip above.</summary>
        public static int CENTER_PANEL_HEIGHT => _gridHeight + 1 - ACTION_INFO_STRIP_HEIGHT;

        // Action info strip - center column band for combo action cards and modifiers during combat (and related UI)
        public static int ACTION_INFO_X => CENTER_PANEL_X;
        public static int ACTION_INFO_WIDTH => CENTER_PANEL_WIDTH;
        public static int ACTION_INFO_HEIGHT => ACTION_INFO_STRIP_HEIGHT;
        /// <summary>Horizontal gap in character columns between per-action panels in the action-info strip.</summary>
        public const int ACTION_INFO_PANEL_GAP = 1;
        /// <summary>
        /// Inset from the strip’s left and right edges for card panels (character columns), and row count left empty below the cards before the center frame.
        /// Matches the horizontal padding implied by <see cref="ACTION_INFO_CONTENT_X"/> / <see cref="ACTION_INFO_CONTENT_WIDTH"/> (1 column each side when this is 1).
        /// </summary>
        public const int ACTION_INFO_PANEL_EDGE_MARGIN = 1;
        /// <summary>Extra rows between the action-info strip top and the per-action card borders. Use 0 to align card tops with <see cref="ACTION_INFO_Y"/> and the side panels.</summary>
        public const int ACTION_INFO_PANEL_TOP_GAP = 0;
        /// <summary>Minimum number of action strip panels shown when a character is present (empty slots render as placeholders).</summary>
        public const int ACTION_INFO_STRIP_FIXED_SLOT_COUNT = 5;
        /// <summary>Content area for action info (inside border).</summary>
        public static int ACTION_INFO_CONTENT_X => ACTION_INFO_X + 1;
        public static int ACTION_INFO_CONTENT_Y => ACTION_INFO_Y + 1;
        public static int ACTION_INFO_CONTENT_WIDTH => ACTION_INFO_WIDTH - 2;
        public static int ACTION_INFO_CONTENT_HEIGHT => ACTION_INFO_HEIGHT - 2;
        
        // Right panel (Dungeon/Enemy Info) - fixed width, positioned at visible right edge
        public static int RIGHT_PANEL_X => EffectiveVisibleWidth - BASE_RIGHT_PANEL_WIDTH; // Positioned at effective visible right edge
        public static int RIGHT_PANEL_Y => 0; // Always start at row 0 (top of grid)
        public static int RIGHT_PANEL_WIDTH => BASE_RIGHT_PANEL_WIDTH; // Fixed width, not scaled
        public static int RIGHT_PANEL_HEIGHT => _gridHeight + 1; // One character taller (rows 0 to _gridHeight)
        
        // Top bar for title
        public static int TITLE_Y => ScaleHeight(BASE_TITLE_Y);

        /// <summary>
        /// True when <paramref name="gridX"/>, <paramref name="gridY"/> lies inside the framed combat-log
        /// center panel (below the action-info strip, cyan border). Used to drop peripheral hover chrome.
        /// </summary>
        public static bool ContainsCenterPanelContent(int gridX, int gridY)
        {
            int x = CENTER_PANEL_X;
            int y = CENTER_PANEL_Y;
            int w = CENTER_PANEL_WIDTH;
            int h = CENTER_PANEL_HEIGHT;
            return gridX >= x && gridX < x + w && gridY >= y && gridY < y + h;
        }

        /// <summary>
        /// Full usable content area when the three-panel chrome (left / center frame / right) is hidden.
        /// Spans full grid height including the row band normally reserved for the action-info strip.
        /// </summary>
        public static (int x, int y, int width, int height) GetChromelessContentRect()
        {
            const int marginX = 1;
            int w = Math.Max(1, EffectiveVisibleWidth - marginX * 2);
            int h = _gridHeight + 1;
            return (marginX, 0, w, h);
        }
    }
}

