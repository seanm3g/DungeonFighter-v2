using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Tracks previous health values for characters and enemies to calculate damage deltas
    /// </summary>
    public class HealthTracker
    {
        private readonly Dictionary<string, int> previousHealthValues;
        private readonly Dictionary<string, int> healthAtDamageTime; // Health value when damage occurred
        private readonly Dictionary<string, DateTime> damageDeltaStartTimes;
        
        public HealthTracker()
        {
            previousHealthValues = new Dictionary<string, int>();
            healthAtDamageTime = new Dictionary<string, int>();
            damageDeltaStartTimes = new Dictionary<string, DateTime>();
        }
        
        /// <summary>
        /// Gets the previous health value for a given entity
        /// </summary>
        public int? GetPreviousHealth(string entityId)
        {
            return previousHealthValues.TryGetValue(entityId, out int health) ? health : null;
        }
        
        /// <summary>
        /// Gets the health value at the time damage occurred (for delta calculation)
        /// </summary>
        public int? GetHealthAtDamageTime(string entityId)
        {
            return healthAtDamageTime.TryGetValue(entityId, out int health) ? health : null;
        }
        
        /// <summary>
        /// Gets the damage delta start time for a given entity
        /// </summary>
        public DateTime? GetDamageDeltaStartTime(string entityId)
        {
            return damageDeltaStartTimes.TryGetValue(entityId, out DateTime startTime) ? startTime : null;
        }
        
        /// <summary>
        /// Updates the health value for an entity and returns the previous value
        /// If health decreased, records the damage delta start time and health at damage time
        /// </summary>
        public int? UpdateHealth(string entityId, int currentHealth)
        {
            int? previousHealth = GetPreviousHealth(entityId);
            
            // If health decreased, record the damage delta start time and health at damage time
            if (previousHealth.HasValue && previousHealth.Value > currentHealth)
            {
                damageDeltaStartTimes[entityId] = DateTime.Now;
                healthAtDamageTime[entityId] = previousHealth.Value; // Store health before damage
            }
            
            // Update previous health for next comparison
            previousHealthValues[entityId] = currentHealth;
            return previousHealth;
        }
        
        /// <summary>
        /// Clears health tracking for a specific entity
        /// </summary>
        public void ClearHealth(string entityId)
        {
            previousHealthValues.Remove(entityId);
            healthAtDamageTime.Remove(entityId);
            damageDeltaStartTimes.Remove(entityId);
        }
        
        /// <summary>
        /// Clears all health tracking
        /// </summary>
        public void ClearAll()
        {
            previousHealthValues.Clear();
            healthAtDamageTime.Clear();
            damageDeltaStartTimes.Clear();
        }
        
        /// <summary>
        /// Checks if there are any active damage deltas (within 2 seconds: 1 second solid + 1 second fade)
        /// Also cleans up expired damage deltas
        /// </summary>
        public bool HasActiveDamageDeltas()
        {
            var now = DateTime.Now;
            var expiredEntities = new List<string>();
            double totalDuration = 2.0; // 1 second solid + 1 second fade
            
            // Check for active deltas and collect expired ones
            foreach (var kvp in damageDeltaStartTimes)
            {
                if ((now - kvp.Value).TotalSeconds >= totalDuration)
                {
                    expiredEntities.Add(kvp.Key);
                }
            }
            
            // Clean up expired damage deltas
            foreach (var entityId in expiredEntities)
            {
                healthAtDamageTime.Remove(entityId);
                damageDeltaStartTimes.Remove(entityId);
            }
            
            return damageDeltaStartTimes.Count > 0;
        }
    }
}

