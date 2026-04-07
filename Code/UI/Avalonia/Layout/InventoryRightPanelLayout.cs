using System;
using RPGGame;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Geometry for the inventory / combo-management right panel (sequence + pool).
    /// Must stay in sync with <see cref="RightPanelRenderer.RenderInventoryRightPanel"/>.
    /// </summary>
    public static class InventoryRightPanelLayout
    {
        /// <summary>
        /// Ideal left column for a tooltip box centered on the boundary between the center column and the right panel.
        /// </summary>
        public static int GetPoolTooltipIdealBoxLeft(int boxWidth) =>
            LayoutConstants.RIGHT_PANEL_X - boxWidth / 2;

        /// <summary>
        /// Clamps tooltip horizontal position to the logical character grid.
        /// </summary>
        public static int ClampPoolTooltipBoxLeft(int idealLeft, int boxWidth)
        {
            int maxLeft = Math.Max(1, LayoutConstants.SCREEN_WIDTH - boxWidth);
            return Math.Max(1, Math.Min(idealLeft, maxLeft));
        }

        /// <summary>
        /// Grid row (character Y) of the action pool line for <paramref name="poolIndex"/> when that row is visible.
        /// </summary>
        /// <returns>False when the index is invalid or the row is not rendered (scrolled past bottom).</returns>
        public static bool TryGetActionPoolRowY(Character character, int poolIndex, out int rowY)
        {
            rowY = 0;
            if (character == null || poolIndex < 0)
                return false;

            var actionPool = character.GetActionPool();
            if (actionPool.Count == 0 || poolIndex >= actionPool.Count)
                return false;

            int y = LayoutConstants.RIGHT_PANEL_Y + 1;
            int panelBottom = LayoutConstants.RIGHT_PANEL_Y + LayoutConstants.RIGHT_PANEL_HEIGHT - 2;

            y++;
            y++;
            y += 2;

            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                y += 2;

                const int actionPoolReserve = 9;
                int comboLinesAvailable = panelBottom - y - actionPoolReserve;
                int maxDisplay = Math.Max(0, Math.Min(comboActions.Count, comboLinesAvailable - 1));
                if (maxDisplay == 0 && comboActions.Count > 0)
                    maxDisplay = Math.Min(comboActions.Count, comboLinesAvailable);

                for (int i = 0; i < maxDisplay; i++)
                    y++;

                if (comboActions.Count > maxDisplay)
                    y++;
            }
            else
            {
                y += 2;
            }

            y += 1;

            y++;
            y++;
            y += 2;

            y += 2;

            int poolIdx = 0;
            while (poolIdx < actionPool.Count && y <= panelBottom - 1)
            {
                if (poolIdx == poolIndex)
                {
                    rowY = y;
                    return true;
                }

                y++;
                poolIdx++;
            }

            return false;
        }
    }
}
