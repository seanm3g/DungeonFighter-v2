using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Caches narrative analysis results to prevent duplicate analysis of the same event.
    /// Also tracks which events have had their narratives displayed to prevent duplicate displays.
    /// Extracted from BattleNarrative to improve Single Responsibility Principle compliance.
    /// </summary>
    public class BattleNarrativeCache
    {
        private BattleEvent? lastCachedEvent;
        private List<string>? lastCachedNarratives;
        private int lastCachedEventIndex = -1;
        private int lastDisplayedEventIndex = -1;

        /// <summary>
        /// Caches narratives for an event
        /// </summary>
        /// <param name="evt">The event to cache</param>
        /// <param name="narratives">The narratives for this event</param>
        /// <param name="eventIndex">The index of the event in the events list</param>
        public void CacheNarratives(BattleEvent evt, List<string> narratives, int eventIndex)
        {
            lastCachedEvent = evt;
            lastCachedNarratives = new List<string>(narratives);
            lastCachedEventIndex = eventIndex;
        }
        
        /// <summary>
        /// Marks narratives for an event as displayed
        /// </summary>
        /// <param name="eventIndex">The index of the event that was displayed</param>
        public void MarkAsDisplayed(int eventIndex)
        {
            if (eventIndex > lastDisplayedEventIndex)
            {
                lastDisplayedEventIndex = eventIndex;
            }
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        public void Clear()
        {
            lastCachedEvent = null;
            lastCachedNarratives = null;
            lastCachedEventIndex = -1;
            lastDisplayedEventIndex = -1;
        }
    }
}
