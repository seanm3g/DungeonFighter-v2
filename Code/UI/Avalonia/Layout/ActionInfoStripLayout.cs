namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Shared layout math for the action-info strip (per-action panels at the top of the center column, above the combat log).
    /// Must stay in sync with <see cref="Renderers.DungeonRenderer.RoomAndCombat.RenderActionInfoStrip"/>.
    /// <paramref name="panelCount"/> in <see cref="GetPanelRect"/> and <see cref="TryGetPanelIndex"/> is the display slot count (after padding to <see cref="LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT"/> when applicable).
    /// </summary>
    public static class ActionInfoStripLayout
    {
        /// <summary>
        /// Display slot count for the strip: at least <see cref="LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT"/>, or the combo length if longer.
        /// </summary>
        public static int GetDisplayPanelCount(int filledActionCount)
        {
            int min = LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT;
            if (filledActionCount <= 0)
                return min;
            return System.Math.Max(filledActionCount, min);
        }

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
            topGap = LayoutConstants.ACTION_INFO_PANEL_TOP_GAP;
            int edgeMargin = LayoutConstants.ACTION_INFO_PANEL_EDGE_MARGIN;
            int count = panelCount;
            gap = LayoutConstants.ACTION_INFO_PANEL_GAP;
            // When the center column is narrow, avail/count can be 0 so remainder pixels go only to leading panels and
            // trailing slots get zero width (breaks drag/hover for the last cards). Shrink horizontal gap first.
            while (count > 0 && gap > 0)
            {
                int totalGaps = System.Math.Max(0, count - 1) * gap;
                int insetTotal = edgeMargin * 2;
                int avail = System.Math.Max(0, stripW - totalGaps - insetTotal);
                if (avail >= count)
                    break;
                gap--;
            }

            int totalGapsFinal = System.Math.Max(0, count - 1) * gap;
            int insetTotalFinal = edgeMargin * 2;
            int availFinal = System.Math.Max(0, stripW - totalGapsFinal - insetTotalFinal);
            panelWidth = count > 0 ? availFinal / count : 0;
            remainder = count > 0 ? availFinal % count : 0;
            panelRowY = stripY + topGap;
            panelH = System.Math.Max(1, stripH - topGap - edgeMargin);
        }

        /// <summary>
        /// Panel width at <paramref name="index"/> when <paramref name="avail"/> is split across <paramref name="panelCount"/> cells.
        /// Remainder pixels are spread across the first <c>remainder</c> panels so no single cell (e.g. the last) is visibly wider.
        /// </summary>
        internal static int GetPanelWidthForIndex(int index, int panelCount, int panelWidth, int remainder)
        {
            if (panelCount <= 0 || index < 0 || index >= panelCount)
                return panelWidth;
            return panelWidth + (index < remainder ? 1 : 0);
        }

        /// <summary>
        /// Left edge X of panel <paramref name="index"/> (same coordinate space as <see cref="GetPanelRect"/>).
        /// </summary>
        internal static int GetPanelLeftX(int index, int stripX, int edgeMargin, int gap, int panelWidth, int remainder)
        {
            return stripX + edgeMargin + index * gap + index * panelWidth + System.Math.Min(index, remainder);
        }

        /// <summary>
        /// Gets the grid rectangle for panel <paramref name="index"/> (0-based).
        /// </summary>
        public static void GetPanelRect(int index, int panelCount, out int px, out int py, out int pw, out int ph)
        {
            GetStripLayout(panelCount, out int stripX, out _, out _, out _, out int gap, out _, out int panelWidth, out int remainder, out int panelRowY, out int panelH);
            int edgeMargin = LayoutConstants.ACTION_INFO_PANEL_EDGE_MARGIN;
            px = GetPanelLeftX(index, stripX, edgeMargin, gap, panelWidth, remainder);
            py = panelRowY;
            pw = GetPanelWidthForIndex(index, panelCount, panelWidth, remainder);
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
                out int gap, out _, out int panelWidth, out int remainder, out int panelRowY, out int panelH);

            if (gridY < panelRowY || gridY >= panelRowY + panelH)
                return false;
            if (gridX < stripX || gridX >= stripX + stripW)
                return false;

            int edgeMargin = LayoutConstants.ACTION_INFO_PANEL_EDGE_MARGIN;
            for (int i = 0; i < panelCount; i++)
            {
                int pw = GetPanelWidthForIndex(i, panelCount, panelWidth, remainder);
                int px = GetPanelLeftX(i, stripX, edgeMargin, gap, panelWidth, remainder);
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
