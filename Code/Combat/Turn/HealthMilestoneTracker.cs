using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Turn
{
    /// <summary>
    /// Tracks and processes health milestones for combat entities
    /// </summary>
    public static class HealthMilestoneTracker
    {
        /// <summary>
        /// Checks health milestones and displays events
        /// </summary>
        /// <param name="actor">The Actor to check</param>
        /// <param name="damageAmount">The amount of damage taken</param>
        /// <param name="healthTracker">The battle health tracker instance</param>
        public static void CheckHealthMilestones(Actor actor, int damageAmount, BattleHealthTracker? healthTracker)
        {
            if (healthTracker == null)
                return;

            var events = healthTracker.CheckHealthMilestones(actor, damageAmount);
            foreach (var evt in events)
            {
                // Parse string message to ColoredText for consistent display
                var coloredEvent = ColoredTextParser.Parse(evt);
                if (coloredEvent.Count > 0)
                {
                    TextDisplayIntegration.DisplayCombatAction(coloredEvent, new List<ColoredText>(), null, null);
                }
            }
        }
    }
}

