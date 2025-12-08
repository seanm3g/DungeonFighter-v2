using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Events;

namespace RPGGame.Combat.Outcomes
{
    /// <summary>
    /// Handles conditional outcomes (enemy dies, HP thresholds, combo ends)
    /// </summary>
    public class ConditionalOutcomeHandler : IOutcomeHandler
    {
        public string OutcomeType => "conditional";

        public void HandleOutcome(CombatEvent evt, Actor source, Actor? target, Action? action)
        {
            // Handle enemy death
            if (evt.Type == CombatEventType.EnemyDied && target != null)
            {
                OnEnemyDeath(source, target, action);
            }

            // Handle HP thresholds
            if (evt.Type == CombatEventType.EnemyHealthThreshold)
            {
                OnHealthThreshold(source, target, evt.HealthPercentage);
            }

            // Handle combo end
            if (evt.Type == CombatEventType.ComboEnded)
            {
                OnComboEnd(source, action);
            }
        }

        private void OnEnemyDeath(Actor source, Actor target, Action? action)
        {
            // Trigger any on-kill effects
            if (source is Character character)
            {
                // Could grant XP, trigger effects, etc.
            }
        }

        private void OnHealthThreshold(Actor source, Actor? target, double healthPercentage)
        {
            // Trigger effects at health thresholds (50%, 25%, 10%)
            // Implementation would check action properties for threshold triggers
        }

        private void OnComboEnd(Actor source, Action? action)
        {
            // Trigger effects when combo ends naturally
            // Implementation would check action properties for combo end triggers
        }
    }
}

