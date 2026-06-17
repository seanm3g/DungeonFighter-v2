using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages combat state for environmental effects in a room.
    /// Tracks whether the environment should act during combat with probability scaling.
    /// </summary>
    public class EnvironmentCombatStateManager
    {
        private int environmentActionCount = 0;
        private int maxEnvironmentActions = 2;
        private int failedAttempts = 0;
        private double baseEnvironmentChance = 0.05;
        private double environmentChanceIncrease = 0.05;
        private double chanceMultiplier = 1.0;
        private Random random;

        public EnvironmentCombatStateManager()
        {
            random = new Random();
        }

        public void ConfigureActivityFromTags(IEnumerable<string>? tags)
        {
            chanceMultiplier = 1.0;
            maxEnvironmentActions = 2;
            if (tags == null)
                return;

            if (tags.Any(t => string.Equals(t, "dormant", StringComparison.OrdinalIgnoreCase)))
            {
                chanceMultiplier = 0.25;
                maxEnvironmentActions = 1;
            }
            else if (tags.Any(t => string.Equals(t, "active", StringComparison.OrdinalIgnoreCase)))
            {
                chanceMultiplier = 2.0;
                maxEnvironmentActions = 3;
            }
            else if (tags.Any(t => string.Equals(t, "cycling", StringComparison.OrdinalIgnoreCase)))
            {
                chanceMultiplier = 1.0;
                maxEnvironmentActions = 2;
            }
        }

        public void ResetForNewFight()
        {
            environmentActionCount = 0;
            failedAttempts = 0;
        }

        public bool ShouldEnvironmentAct(bool isHostile)
        {
            if (environmentActionCount >= maxEnvironmentActions)
                return false;

            if (!isHostile)
                return false;

            double currentChance = (baseEnvironmentChance + (failedAttempts * environmentChanceIncrease)) * chanceMultiplier;
            currentChance = Math.Min(currentChance, 0.50);

            if (random.NextDouble() < currentChance)
            {
                environmentActionCount++;
                failedAttempts = 0;
                return true;
            }

            failedAttempts++;
            return false;
        }

        public int GetActionCount() => environmentActionCount;
        public int GetMaxActions() => maxEnvironmentActions;
        public bool HasReachedActionLimit() => environmentActionCount >= maxEnvironmentActions;
    }
}
