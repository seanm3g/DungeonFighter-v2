using System;

namespace RPGGame
{
    /// <summary>
    /// Manages combat state for environmental effects in a room.
    /// Tracks whether the environment should act during combat with probability scaling.
    /// </summary>
    public class EnvironmentCombatStateManager
    {
        private int environmentActionCount = 0;
        private int maxEnvironmentActions = 2; // Maximum 2 environmental actions per fight
        private int failedAttempts = 0; // Track failed attempts to increase chance over time
        private double baseEnvironmentChance = 0.05; // Start with 5% chance
        private double environmentChanceIncrease = 0.05; // Increase by 5% each failed attempt
        private Random random;

        public EnvironmentCombatStateManager()
        {
            random = new Random();
        }

        /// <summary>
        /// Resets the environment action count and state for a new fight.
        /// </summary>
        public void ResetForNewFight()
        {
            environmentActionCount = 0;
            failedAttempts = 0;
        }

        /// <summary>
        /// Checks if the environment should act in combat.
        /// Uses a probability that increases with failed attempts to ensure actions occur.
        /// </summary>
        /// <param name="isHostile">Whether the environment is hostile (only hostile environments can act)</param>
        /// <returns>True if the environment should act this turn</returns>
        public bool ShouldEnvironmentAct(bool isHostile)
        {
            // Don't act if we've already used our maximum actions for this fight
            if (environmentActionCount >= maxEnvironmentActions)
                return false;

            if (!isHostile)
                return false;

            // Calculate current chance: base chance + (failed attempts * increase per attempt)
            double currentChance = baseEnvironmentChance + (failedAttempts * environmentChanceIncrease);

            // Cap the chance at 50% to prevent it from becoming too predictable
            currentChance = Math.Min(currentChance, 0.50);

            if (random.NextDouble() < currentChance)
            {
                // Environmental action triggered - reset failed attempts and increment action count
                environmentActionCount++;
                failedAttempts = 0;
                return true;
            }
            else
            {
                // Environmental action failed - increment failed attempts for next time
                failedAttempts++;
                return false;
            }
        }

        /// <summary>
        /// Gets the number of environmental actions that have occurred in this fight.
        /// </summary>
        public int GetActionCount() => environmentActionCount;

        /// <summary>
        /// Gets the maximum number of environmental actions allowed per fight.
        /// </summary>
        public int GetMaxActions() => maxEnvironmentActions;

        /// <summary>
        /// Checks if the environment has reached its maximum action limit.
        /// </summary>
        public bool HasReachedActionLimit() => environmentActionCount >= maxEnvironmentActions;
    }
}

