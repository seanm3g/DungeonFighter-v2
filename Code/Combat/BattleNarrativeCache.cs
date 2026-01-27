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
        /// Gets cached narratives for an event if available and not yet displayed, otherwise returns null
        /// </summary>
        /// <param name="evt">The event to get narratives for</param>
        /// <param name="eventIndex">The index of the event in the events list</param>
        /// <returns>Cached narratives if available and not displayed, null otherwise</returns>
        public List<string>? GetCachedNarratives(BattleEvent evt, int eventIndex)
        {
            // Check if this is the cached event and it hasn't been displayed yet
            if (lastCachedEvent == evt && lastCachedNarratives != null && lastCachedEventIndex == eventIndex)
            {
                // Only return narratives if this event hasn't been displayed yet
                if (eventIndex > lastDisplayedEventIndex)
                {
                    return new List<string>(lastCachedNarratives);
                }
            }
            return null;
        }
        
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
        /// Checks if an event has already had its narratives displayed
        /// </summary>
        /// <param name="eventIndex">The index of the event to check</param>
        /// <returns>True if the event has been displayed, false otherwise</returns>
        public bool HasBeenDisplayed(int eventIndex)
        {
            return eventIndex <= lastDisplayedEventIndex;
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
