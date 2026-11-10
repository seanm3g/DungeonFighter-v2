using Avalonia.Media;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Joined panel frames (inventory hub): instead of three separately boxed panels with a one-cell gap between
    /// them, each frame's facing edge is pushed half a cell into the gap so neighbors share one line, all in one
    /// color. Outside the hub every panel keeps its own colored box.
    /// </summary>
    public static class PanelJoin
    {
        /// <summary>Set per layout pass by <see cref="CanvasTitleLayoutCoordinator"/>.</summary>
        public static bool Active { get; set; }

        public static Color FrameColor => AsciiArtAssets.Colors.Gray;

        /// <summary>The panel's own border color, or the shared frame color while joined.</summary>
        public static Color Border(Color own) => Active ? FrameColor : own;

        private static double LeftGapHalf =>
            System.Math.Max(0, LayoutConstants.CENTER_PANEL_X - (LayoutConstants.LEFT_PANEL_X + LayoutConstants.LEFT_PANEL_WIDTH)) / 2.0;

        private static double RightGapHalf =>
            System.Math.Max(0, LayoutConstants.RIGHT_PANEL_X - (LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH)) / 2.0;

        public static void ApplyLeftPanel(GameCanvasControl canvas) =>
            canvas.SetBoxOutset(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT,
                0, Active ? LeftGapHalf : 0);

        public static void ApplyRightPanel(GameCanvasControl canvas) =>
            canvas.SetBoxOutset(LayoutConstants.RIGHT_PANEL_X, LayoutConstants.RIGHT_PANEL_Y, LayoutConstants.RIGHT_PANEL_WIDTH, LayoutConstants.RIGHT_PANEL_HEIGHT,
                Active ? RightGapHalf : 0, 0);

        /// <summary>Any center-column frame (spans the full center width).</summary>
        public static void ApplyCenter(GameCanvasControl canvas, int y, int height) =>
            canvas.SetBoxOutset(LayoutConstants.CENTER_PANEL_X, y, LayoutConstants.CENTER_PANEL_WIDTH, height,
                Active ? LeftGapHalf : 0, Active ? RightGapHalf : 0);
    }
}
