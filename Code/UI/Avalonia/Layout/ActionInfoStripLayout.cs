namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Shared layout math for the action-info strip (per-action panels below the center panel).
    /// Must stay in sync with <see cref="Renderers.DungeonRenderer.RoomAndCombat.RenderActionInfoStrip"/>.
    /// </summary>
    public static class ActionInfoStripLayout
    {
        /// <summary>
        /// Computes panel layout for the given number of combo panels.
        /// </summary>
        public static void GetStripLayout(int panelCount, out int stripX, out int stripY, out int stripW, out int stripH,
            out int gap, out int topGap, out int panelWidth, out int remainder, out int panelRowY, out int panelH)
        {
            stripX = LayoutConstants.ACTION_INFO_X;
            stripY = LayoutConstants.ACTION_INFO_Y;
            stripW = LayoutConstants.ACTION_INFO_WIDTH;
            stripH = LayoutConstants.ACTION_INFO_HEIGHT;
            gap = LayoutConstants.ACTION_INFO_PANEL_GAP;
            topGap = LayoutConstants.ACTION_INFO_PANEL_TOP_GAP;
            int count = panelCount;
            int totalGaps = System.Math.Max(0, count - 1) * gap;
            int avail = System.Math.Max(0, stripW - totalGaps);
            panelWidth = count > 0 ? avail / count : 0;
            remainder = count > 0 ? avail % count : 0;
            panelRowY = stripY + topGap;
            panelH = System.Math.Max(1, stripH - topGap);
        }

        /// <summary>
        /// Gets the grid rectangle for panel <paramref name="index"/> (0-based).
        /// </summary>
        public static void GetPanelRect(int index, int panelCount, out int px, out int py, out int pw, out int ph)
        {
            GetStripLayout(panelCount, out int stripX, out _, out _, out _, out int gap, out _, out int panelWidth, out int remainder, out int panelRowY, out int panelH);
            px = stripX + index * (panelWidth + gap);
            py = panelRowY;
            pw = panelWidth + (index == panelCount - 1 ? remainder : 0);
            ph = panelH;
        }

        /// <summary>
        /// Returns whether <paramref name="gridX"/>, <paramref name="gridY"/> lies inside the action strip and which panel (0-based).
        /// </summary>
        public static bool TryGetPanelIndex(int gridX, int gridY, int panelCount, out int index)
        {
            index = -1;
            if (panelCount <= 0)
                return false;

            GetStripLayout(panelCount, out int stripX, out _, out int stripW, out _,
                out _, out _, out int panelWidth, out int remainder, out int panelRowY, out int panelH);

            if (gridY < panelRowY || gridY >= panelRowY + panelH)
                return false;
            if (gridX < stripX || gridX >= stripX + stripW)
                return false;

            int gap = LayoutConstants.ACTION_INFO_PANEL_GAP;
            for (int i = 0; i < panelCount; i++)
            {
                int pw = panelWidth + (i == panelCount - 1 ? remainder : 0);
                int px = stripX + i * (panelWidth + gap);
                if (gridX >= px && gridX < px + pw)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }
    }
}
