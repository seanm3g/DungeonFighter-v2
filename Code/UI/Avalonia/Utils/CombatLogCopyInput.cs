using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Utils
{
    /// <summary>
    /// Pointer/key policy for copying the Avalonia combat log.
    /// </summary>
    public static class CombatLogCopyInput
    {
        /// <summary>
        /// True when a right-click should copy the full center display buffer.
        /// </summary>
        public static bool ShouldCopyOnRightClick(
            bool isRightButtonPressed,
            bool isOverlayOpen,
            bool isCombatDisplayActive,
            int gridX,
            int gridY)
        {
            return isRightButtonPressed
                && !isOverlayOpen
                && isCombatDisplayActive
                && LayoutConstants.ContainsCenterPanelContent(gridX, gridY);
        }
    }
}
