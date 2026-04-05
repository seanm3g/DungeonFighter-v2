using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Holds the last-displayed combat action summary for the action-info strip below the center panel.
    /// Set when displaying an action block during combat; cleared when leaving combat.
    /// </summary>
    public static class CombatActionInfoState
    {
        private static readonly object _lock = new object();
        private static List<string> _lines = new List<string>();

        /// <summary>
        /// Gets a snapshot of the current action info lines (safe to iterate from another thread).
        /// </summary>
        public static IReadOnlyList<string> GetLines()
        {
            lock (_lock)
            {
                return _lines.Count == 0 ? Array.Empty<string>() : new List<string>(_lines);
            }
        }

        /// <summary>
        /// Updates the action info strip with the given lines (e.g. action line, roll summary, status effects).
        /// Call from combat display when an action block is shown.
        /// </summary>
        public static void SetSummary(IReadOnlyList<string>? lines)
        {
            lock (_lock)
            {
                _lines = lines == null || lines.Count == 0
                    ? new List<string>()
                    : new List<string>(lines);
            }
        }

        /// <summary>
        /// Clears the action info strip. Call when leaving combat.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _lines = new List<string>();
            }
        }
    }
}
