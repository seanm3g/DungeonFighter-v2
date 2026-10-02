using System;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Inner content band for the left character panel (inside the blue border).
    /// Used to clip overflow and drive mouse-wheel scrolling when HERO/STATS/GEAR/THRESHOLDS exceed the panel height.
    /// </summary>
    public static class LeftPanelViewport
    {
        /// <summary>
        /// Rows below the bottom border to scrub after body paint.
        /// Covers a full CHANCES block (5) when the first row sits on the last visible body line.
        /// </summary>
        public const int BottomSpillScrubRows = 5;

        /// <summary>First row where left-panel body text may paint (below the top border).</summary>
        public static int ContentTop => LayoutConstants.LEFT_PANEL_Y + 1;

        /// <summary>Exclusive bottom of the body band (top of the bottom border row).</summary>
        public static int ContentBottomExclusive =>
            LayoutConstants.LEFT_PANEL_Y + LayoutConstants.LEFT_PANEL_HEIGHT - 1;

        /// <summary>Exclusive bottom of the post-paint scrub band (border row + spill below the panel).</summary>
        public static int ScrubBottomExclusive => ContentBottomExclusive + BottomSpillScrubRows;

        /// <summary>Visible body rows inside the border.</summary>
        public static int ContentHeight => Math.Max(1, ContentBottomExclusive - ContentTop);

        public static bool IsRowVisible(int rowY) =>
            rowY >= ContentTop && rowY < ContentBottomExclusive;

        public static bool IsRangeVisible(int rowY, int height) =>
            height > 0 && rowY < ContentBottomExclusive && rowY + height > ContentTop;

        public static int MaxScrollOffset(int totalContentHeight, int viewportHeight) =>
            Math.Max(0, totalContentHeight - Math.Max(1, viewportHeight));

        public static int ClampScrollOffset(int scrollOffset, int totalContentHeight, int viewportHeight) =>
            Math.Clamp(scrollOffset, 0, MaxScrollOffset(totalContentHeight, viewportHeight));
    }
}
