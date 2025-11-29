using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Items.Helpers
{
    /// <summary>
    /// Formats action information for display
    /// </summary>
    public static class ActionDisplayFormatter
    {
        /// <summary>
        /// Calculates the speed percentage for an action
        /// </summary>
        public static double CalculateActionSpeedPercentage(Action action) => 100.0 / action.Length;

        /// <summary>
        /// Gets a description of the action speed
        /// </summary>
        public static string GetSpeedDescription(double speedPercentage)
        {
            if (speedPercentage >= 150) return "Very Fast";
            if (speedPercentage >= 120) return "Fast";
            if (speedPercentage >= 100) return "Normal";
            if (speedPercentage >= 80) return "Slow";
            return "Very Slow";
        }

        /// <summary>
        /// Formats action stats for display
        /// </summary>
        public static string FormatActionStats(Action action, int timesInCombo, int timesAvailable)
        {
            double speedPercentage = CalculateActionSpeedPercentage(action);
            string speedText = GetSpeedDescription(speedPercentage);
            string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";
            
            string statsLine = $"      {action.Description} | Damage: {action.DamageMultiplier:F1}x | Speed: {speedPercentage:F0}% ({speedText})";
            
            var effects = new List<string>();
            if (action.CausesBleed) effects.Add("Causes Bleed");
            if (action.CausesWeaken) effects.Add("Causes Weaken");
            if (action.CausesSlow) effects.Add("Causes Slow");
            if (action.CausesPoison) effects.Add("Causes Poison");
            if (action.CausesStun) effects.Add("Causes Stun");
            
            if (effects.Count > 0)
                statsLine += ", " + string.Join(", ", effects);
            
            return statsLine;
        }
    }
}

