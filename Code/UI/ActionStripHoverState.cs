using System;

namespace RPGGame
{
    /// <summary>
    /// Tracks which action-info strip panel (0-based combo index) is under the pointer for tooltip display.
    /// Updated from pointer move; render reads the snapshot.
    /// </summary>
    public static class ActionStripHoverState
    {
        private static readonly object _lock = new object();
        private static int _hoveredPanelIndex = -1;

        /// <summary>
        /// Current hovered panel index, or -1 if none.
        /// </summary>
        public static int HoveredPanelIndex
        {
            get
            {
                lock (_lock)
                {
                    return _hoveredPanelIndex;
                }
            }
        }

        /// <summary>
        /// Sets the hovered panel index. Returns true if the value changed (caller may want to force a layout render).
        /// </summary>
        public static bool SetHoveredPanelIndex(int index)
        {
            lock (_lock)
            {
                if (_hoveredPanelIndex == index)
                    return false;
                _hoveredPanelIndex = index;
                return true;
            }
        }

        /// <summary>
        /// Clears hover (e.g. when leaving combat). Does not trigger a render.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _hoveredPanelIndex = -1;
            }
        }
    }
}
