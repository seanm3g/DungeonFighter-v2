using RPGGame;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Utils
{
    /// <summary>
    /// Pointer/key policy for copying the Avalonia combat log.
    /// </summary>
    public static class CombatLogCopyInput
    {
        /// <summary>
        /// True when the center display buffer is the live dungeon/combat text log and copy (right-click or Ctrl+C) is allowed.
        /// Includes <see cref="GameState.Dungeon"/> so stay/leave prompts between rooms keep the same copy behavior as combat.
        /// </summary>
        /// <param name="isCombatDisplayActive">True when the center panel is in <c>CombatDisplayMode</c>.</param>
        /// <param name="state">Current <see cref="GameState"/>, or null when no state manager is wired.</param>
        public static bool AllowsClipboardContext(bool isCombatDisplayActive, GameState? state)
        {
            if (isCombatDisplayActive)
                return true;
            return state == GameState.Combat
                || state == GameState.ActionInteractionLab
                || state == GameState.Dungeon;
        }

        /// <summary>
        /// True when a right-click should copy the full center display buffer (entire battle log in memory, not only visible lines).
        /// </summary>
        /// <param name="allowCombatLogCopy">From <see cref="RPGGame.UI.Avalonia.CanvasUICoordinator.IsCombatLogClipboardContext"/> (display mode and/or combat/dungeon game state).</param>
        /// <param name="pointerCanvasLocalX">Pointer X in canvas coordinates; use <c>double.NaN</c> to skip pixel hit-test.</param>
        /// <param name="pointerCanvasLocalY">Pointer Y in canvas coordinates.</param>
        public static bool ShouldCopyOnRightClick(
            bool isRightButtonPressed,
            bool isOverlayOpen,
            bool allowCombatLogCopy,
            int gridX,
            int gridY,
            double pointerCanvasLocalX = double.NaN,
            double pointerCanvasLocalY = double.NaN,
            double charWidth = 0,
            double charHeight = 0)
        {
            bool inCenter = LayoutConstants.ContainsCenterPanelContent(gridX, gridY);
            if (!inCenter && !double.IsNaN(pointerCanvasLocalX) && !double.IsNaN(pointerCanvasLocalY) && charWidth > 0 && charHeight > 0)
                inCenter = LayoutConstants.ContainsCenterPanelPixelHit(pointerCanvasLocalX, pointerCanvasLocalY, charWidth, charHeight);

            return isRightButtonPressed
                && !isOverlayOpen
                && allowCombatLogCopy
                && inCenter;
        }
    }
}
