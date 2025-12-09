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
        /// Creates a BASIC ATTACK action by loading from JSON
        /// </summary>
        /// <returns>BASIC ATTACK action</returns>
        /// <exception cref="InvalidOperationException">Thrown when BASIC ATTACK is not found in Actions.json</exception>
        public static Action CreateBasicAttack()
        {
            var loadedAction = ActionLoader.GetAction("BASIC ATTACK");
            if (loadedAction == null)
            {
                throw new InvalidOperationException("BASIC ATTACK action not found in Actions.json. Please ensure Actions.json contains a BASIC ATTACK action.");
            }
            return loadedAction;
        }

        /// <summary>
        /// Ensures BASIC ATTACK is available in an Actor's action pool
        /// </summary>
        /// <param name="Actor">The Actor to ensure has BASIC ATTACK</param>
        /// <returns>The BASIC ATTACK action (existing or loaded from JSON)</returns>
        /// <exception cref="InvalidOperationException">Thrown when BASIC ATTACK is not found in Actions.json</exception>
        public static Action EnsureBasicAttackAvailable(Actor Actor)
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
                throw new InvalidOperationException("BASIC ATTACK action not found in Actions.json. Please ensure Actions.json contains a BASIC ATTACK action.");
            }

            Actor.AddAction(loadedAction, 1.0);
            DebugLogger.Log("ActionFactory", $"Added missing BASIC ATTACK to {Actor.Name}'s action pool from JSON");
            return loadedAction;
        }

        /// <summary>
        /// Gets or creates BASIC ATTACK for an Actor, with fallback creation
        /// </summary>
        /// <param name="Actor">The Actor to get BASIC ATTACK for</param>
        /// <returns>BASIC ATTACK action</returns>
        public static Action GetBasicAttack(Actor Actor)
        {
            // First try to find existing BASIC ATTACK
            var existingBasicAttack = Actor.ActionPool
                .FirstOrDefault(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));

            if (existingBasicAttack.action != null)
            {
                return existingBasicAttack.action;
            }

            // If not found, ensure it's available (will create if needed)
            return EnsureBasicAttackAvailable(Actor);
        }
    }
}


