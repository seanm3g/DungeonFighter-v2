using System.Collections.Generic;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Counts menu/combat actions spent at each hero level during a playthrough run.
    /// </summary>
    internal sealed class PlaythroughLevelTurnTracker
    {
        private readonly Dictionary<int, int> _actionsByLevel = new();

        public void RecordAction(int playerLevel)
        {
            int level = playerLevel > 0 ? playerLevel : 1;
            _actionsByLevel.TryGetValue(level, out int count);
            _actionsByLevel[level] = count + 1;
        }

        public Dictionary<int, int> ToDictionary() => new(_actionsByLevel);
    }
}
