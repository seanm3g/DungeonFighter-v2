using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages combat state for environmental effects in a room.
    /// The environment rolls a d20 each turn and acts on a high enough face; every quiet turn widens the strike range.
    /// </summary>
    public class EnvironmentCombatStateManager
    {
        private int environmentActionCount = 0;
        private int maxEnvironmentActions = 2;
        private int failedAttempts = 0;
        private const int BaseStrikeFaces = 1;
        private const int StrikeFacesPerFailedAttempt = 1;
        private const int MaxStrikeFaces = 10;
        private double strikeFacesMultiplier = 1.0;

        public void ConfigureActivityFromTags(IEnumerable<string>? tags)
        {
            strikeFacesMultiplier = 1.0;
            maxEnvironmentActions = 2;
            if (tags == null)
                return;

            if (tags.Any(t => string.Equals(t, "dormant", StringComparison.OrdinalIgnoreCase)))
            {
                strikeFacesMultiplier = 0.25;
                maxEnvironmentActions = 1;
            }
            else if (tags.Any(t => string.Equals(t, "active", StringComparison.OrdinalIgnoreCase)))
            {
                strikeFacesMultiplier = 2.0;
                maxEnvironmentActions = 3;
            }
            else if (tags.Any(t => string.Equals(t, "cycling", StringComparison.OrdinalIgnoreCase)))
            {
                strikeFacesMultiplier = 1.0;
                maxEnvironmentActions = 2;
            }
        }

        public void ResetForNewFight()
        {
            environmentActionCount = 0;
            failedAttempts = 0;
        }

        /// <summary>
        /// Lowest d20 face on which the environment's next turn acts (21 once it has used its actions or isn't hostile).
        /// </summary>
        public int GetStrikeThreshold(bool isHostile)
        {
            if (environmentActionCount >= maxEnvironmentActions || !isHostile)
                return 21;

            int faces = (int)Math.Floor((BaseStrikeFaces + failedAttempts * StrikeFacesPerFailedAttempt) * strikeFacesMultiplier);
            faces = Math.Clamp(faces, 1, MaxStrikeFaces);
            return 21 - faces;
        }

        public bool ShouldEnvironmentAct(bool isHostile)
        {
            int threshold = GetStrikeThreshold(isHostile);
            if (threshold > 20)
            {
                LastRoll = 0;
                return false;
            }

            // Unforced so scripted/lab d20 queues meant for actors aren't consumed by the room.
            LastRoll = Dice.RollUnforced(20);
            LastThreshold = threshold;
            if (LastRoll >= threshold)
            {
                environmentActionCount++;
                failedAttempts = 0;
                return true;
            }

            failedAttempts++;
            return false;
        }

        /// <summary>Face and strike threshold of the last <see cref="ShouldEnvironmentAct"/> roll (0 before any roll).</summary>
        public int LastRoll { get; private set; }
        public int LastThreshold { get; private set; }

        public int GetActionCount() => environmentActionCount;
        public int GetMaxActions() => maxEnvironmentActions;
        public bool HasReachedActionLimit() => environmentActionCount >= maxEnvironmentActions;
    }
}
