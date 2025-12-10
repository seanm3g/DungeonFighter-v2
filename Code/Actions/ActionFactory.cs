using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Factory class for creating common actions, eliminating duplication across the codebase
    /// </summary>
    public static class ActionFactory
    {
        /// <summary>
        /// Creates a BASIC ATTACK action by loading from JSON (optional - returns null if not found)
        /// </summary>
        /// <returns>BASIC ATTACK action or null if not found</returns>
        public static Action? CreateBasicAttack()
        {
            return ActionLoader.GetAction("BASIC ATTACK");
        }

        /// <summary>
        /// Ensures BASIC ATTACK is available in an Actor's action pool (optional - returns null if not found)
        /// </summary>
        /// <param name="Actor">The Actor to ensure has BASIC ATTACK</param>
        /// <returns>The BASIC ATTACK action (existing or loaded from JSON) or null if not available</returns>
        public static Action? EnsureBasicAttackAvailable(Actor Actor)
        {
            // Check if BASIC ATTACK is already in the action pool
            var existingBasicAttack = Actor.ActionPool
                .FirstOrDefault(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));

            if (existingBasicAttack.action != null)
            {
                return existingBasicAttack.action;
            }

            // Load BASIC ATTACK from JSON
            var loadedAction = ActionLoader.GetAction("BASIC ATTACK");
            if (loadedAction == null)
            {
                // BASIC ATTACK is optional - return null instead of throwing
                return null;
            }

            Actor.AddAction(loadedAction, 1.0);
            return loadedAction;
        }

        /// <summary>
        /// Gets BASIC ATTACK for an Actor, or returns null if not available
        /// </summary>
        /// <param name="Actor">The Actor to get BASIC ATTACK for</param>
        /// <returns>BASIC ATTACK action or null if not available</returns>
        public static Action? GetBasicAttack(Actor Actor)
        {
            // First try to find existing BASIC ATTACK
            var existingBasicAttack = Actor.ActionPool
                .FirstOrDefault(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));

            if (existingBasicAttack.action != null)
            {
                return existingBasicAttack.action;
            }

            // If not found, try to ensure it's available (will return null if not in JSON)
            return EnsureBasicAttackAvailable(Actor);
        }
    }
}


