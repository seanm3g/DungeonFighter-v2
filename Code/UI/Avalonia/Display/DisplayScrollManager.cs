using RPGGame.UI.Avalonia.Display.Mode;
using RPGGame.UI.Avalonia.Display.Render;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Helper class for display scroll operations.
    /// Extracted from CenterPanelDisplayManager to separate scroll management from buffer operations.
    /// </summary>
    public class DisplayScrollManager
    {
        private readonly DisplayBuffer buffer;
        private readonly RenderCoordinator renderCoordinator;
        private readonly DisplayModeManager modeManager;

        public DisplayScrollManager(
            DisplayBuffer buffer,
            RenderCoordinator renderCoordinator,
            DisplayModeManager modeManager)
        {
            this.buffer = buffer;
            this.renderCoordinator = renderCoordinator;
            this.modeManager = modeManager;
        }

        /// <summary>
        /// Scrolls the display up
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            // Calculate max scroll offset to ensure we don't scroll beyond valid range
            int maxOffset = renderCoordinator.CalculateMaxScrollOffset();
            buffer.ScrollUp(lines, maxOffset);
            // Force render with force=true to allow scrolling in Settings state (for test results)
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender(force: true));
        }

        /// <summary>
        /// Scrolls the display down
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            // Calculate max scroll offset based on current content
            int maxOffset = renderCoordinator.CalculateMaxScrollOffset();
            buffer.ScrollDown(lines, maxOffset);
            // Force render with force=true to allow scrolling in Settings state (for test results)
            modeManager.Timing.ForceRender(() => renderCoordinator.PerformRender(force: true));
        }

        /// <summary>
        /// Resets scrolling to auto-scroll mode
        /// </summary>
        public void ResetScroll()
        {
            buffer.ResetScroll();
            modeManager.Timing.ScheduleRender(() => renderCoordinator.PerformRender());
        }
    }
}