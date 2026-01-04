using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Caches narrative analysis results to prevent duplicate analysis of the same event.
    /// Extracted from BattleNarrative to improve Single Responsibility Principle compliance.
    /// </summary>
    public class BattleNarrativeCache
    {
        private BattleEvent? lastCachedEvent;
        private List<string>? lastCachedNarratives;
        
        /// <summary>
        /// Gets cached narratives for an event if available, otherwise returns null
        /// </summary>
        public List<string>? GetCachedNarratives(BattleEvent evt)
        {
            if (lastCachedEvent == evt && lastCachedNarratives != null)
            {
                return new List<string>(lastCachedNarratives);
            }
            return null;
        }
        
        /// <summary>
        /// Caches narratives for an event
        /// </summary>
        public void CacheNarratives(BattleEvent evt, List<string> narratives)
        {
            lastCachedEvent = evt;
            lastCachedNarratives = new List<string>(narratives);
        }
        
        /// <summary>
        /// Clears the cache
        /// </summary>
        public void Clear()
        {
            lastCachedEvent = null;
            lastCachedNarratives = null;
        }
    }
}
