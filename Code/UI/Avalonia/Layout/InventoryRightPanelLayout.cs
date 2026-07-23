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
        /// Advances Y past a section header + subtitle block the same way
        /// <see cref="RightPanelRenderer.RenderInventoryRightPanel"/> does (<c>y++</c> after header, then <c>y += 2</c> after subtitle).
        /// </summary>
        private static int AdvancePastHeaderAndSubtitle(int y) => y + 1 + 2;

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

            // SEQUENCE header + "(order = strip)"
            y = AdvancePastHeaderAndSubtitle(y);

            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                // "Step: N/M" then blank
                y += 2;

                const int actionPoolReserve = 16;
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
                // "(empty)" then blank
                y += 2;
            }

            y += 1; // spacing before POOL

            // POOL header + "(from gear)"
            y = AdvancePastHeaderAndSubtitle(y);

            // "Total: N" then blank
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

        /// <summary>
        /// Grid row (character Y) of the inventory/bag action pool line for <paramref name="inventoryPoolIndex"/> when visible.
        /// </summary>
        /// <returns>False when the index is invalid or the row is not rendered (scrolled past bottom).</returns>
        public static bool TryGetInventoryActionPoolRowY(Character character, int inventoryPoolIndex, out int rowY)
        {
            rowY = 0;
            if (character == null || inventoryPoolIndex < 0)
                return false;

            var invEntries = InventoryActionPoolEntries.Build(character);
            if (invEntries.Count == 0 || inventoryPoolIndex >= invEntries.Count)
                return false;

            int y = LayoutConstants.RIGHT_PANEL_Y + 1;
            int panelBottom = LayoutConstants.RIGHT_PANEL_Y + LayoutConstants.RIGHT_PANEL_HEIGHT - 2;

            // SEQUENCE header + "(order = strip)"
            y = AdvancePastHeaderAndSubtitle(y);

            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                y += 2;

                const int actionPoolReserve = 16;
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

            y += 1; // spacing before POOL

            // POOL header + "(from gear)"
            y = AdvancePastHeaderAndSubtitle(y);

            var actionPool = character.GetActionPool();
            if (actionPool.Count > 0)
            {
                // "Total: N" then blank
                y += 2;

                int poolIdx = 0;
                while (poolIdx < actionPool.Count && y <= panelBottom - 1)
                {
                    y++;
                    poolIdx++;
                }

                if (poolIdx < actionPool.Count)
                    y++;
            }
            else
            {
                // "(No actions)"
                y++;
            }

            y += 1; // spacing before INVENTORY

            // INVENTORY header + "(from bag)"
            y = AdvancePastHeaderAndSubtitle(y);

            // "Total: N" then blank (empty bag uses a single "(empty)" line and never reaches here)
            y += 2;

            int invFlat = 0;
            while (invFlat < invEntries.Count && y <= panelBottom - 1)
            {
                if (invFlat == inventoryPoolIndex)
                {
                    rowY = y;
                    return true;
                }

                y++;
                invFlat++;
            }

            return false;
        }
    }
}
