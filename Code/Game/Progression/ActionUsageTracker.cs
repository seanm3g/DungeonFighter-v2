using System.Collections.Generic;
using RPGGame;

namespace RPGGame.Progression
{
    /// <summary>
    /// Tracks action usage for scaling outcomes
    /// </summary>
    public class ActionUsageTracker
    {
        private static readonly ActionUsageTracker _instance = new ActionUsageTracker();
        private readonly Dictionary<Actor, Dictionary<string, int>> _usageCounts = new Dictionary<Actor, Dictionary<string, int>>();

        private ActionUsageTracker() { }

        public static ActionUsageTracker Instance => _instance;

        /// <summary>
        /// Records that an action was used
        /// </summary>
        public void RecordActionUsage(Actor actor, Action action)
        {
            if (!_usageCounts.ContainsKey(actor))
            {
                _usageCounts[actor] = new Dictionary<string, int>();
            }

            var counts = _usageCounts[actor];
            if (!counts.ContainsKey(action.Name))
            {
                counts[action.Name] = 0;
            }

            counts[action.Name]++;
        }

        /// <summary>
        /// Gets the number of times an action has been used
        /// </summary>
        public int GetUsageCount(Actor actor, Action action)
        {
            if (_usageCounts.TryGetValue(actor, out var counts))
            {
                return counts.TryGetValue(action.Name, out var count) ? count : 0;
            }
            return 0;
        }

        /// <summary>
        /// Resets usage counts for an actor
        /// </summary>
        public void ResetActorUsage(Actor actor)
        {
            _usageCounts.Remove(actor);
        }

        /// <summary>
        /// Clears all usage tracking
        /// </summary>
        public void Clear()
        {
            _usageCounts.Clear();
        }
    }
}

