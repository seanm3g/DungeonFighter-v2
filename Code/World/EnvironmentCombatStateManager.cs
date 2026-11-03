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

        /// <summary>Default room activity (no tag, or <c>cycling</c>): strike faces ×1, at most 2 acts per fight.</summary>
        public const int DefaultMaxActions = 2;

        public void ConfigureActivityFromTags(IEnumerable<string>? tags)
        {
            var profile = GetActivityProfile(tags);
            strikeFacesMultiplier = profile.StrikeFacesMultiplier;
            maxEnvironmentActions = profile.MaxActions;
        }

        /// <summary>
        /// Activity tag (<c>dormant</c> / <c>active</c> / <c>cycling</c>) → strike-face multiplier and acts per fight.
        /// Tag is the winning activity tag, or null for the default.
        /// </summary>
        public static (string? Tag, double StrikeFacesMultiplier, int MaxActions) GetActivityProfile(IEnumerable<string>? tags)
        {
            if (tags == null)
                return (null, 1.0, DefaultMaxActions);
            if (tags.Any(t => string.Equals(t, "dormant", StringComparison.OrdinalIgnoreCase)))
                return ("dormant", 0.25, 1);
            if (tags.Any(t => string.Equals(t, "active", StringComparison.OrdinalIgnoreCase)))
                return ("active", 2.0, 3);
            if (tags.Any(t => string.Equals(t, "cycling", StringComparison.OrdinalIgnoreCase)))
                return ("cycling", 1.0, DefaultMaxActions);
            return (null, 1.0, DefaultMaxActions);
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
