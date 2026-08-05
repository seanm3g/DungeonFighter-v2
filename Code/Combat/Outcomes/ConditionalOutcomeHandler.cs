using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Events;

namespace RPGGame.Combat.Outcomes
{
    /// <summary>
    /// Handles conditional outcomes (enemy dies, HP thresholds, combo ends).
    /// Status effect application for ONKILL / ONHEALTHTHRESHOLD is done by
    /// <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/> so messages join the swing log.
    /// </summary>
    public class ConditionalOutcomeHandler : IOutcomeHandler
    {
        public string OutcomeType => "conditional";

        public void HandleOutcome(CombatEvent evt, Actor source, Actor? target, Action? action)
        {
            // Reserved for non-status outcomes (XP hooks, narrative, etc.).
            // Status gating for EnemyDied / EnemyHealthThreshold is applied in ActionExecutionFlow.
            if (evt.Type == CombatEventType.ComboEnded)
                OnComboEnd(source, action);
        }

        private void OnComboEnd(Actor source, Action? action)
        {
            // ComboEnded is not published by the live combat flow yet; reserved for future routing.
        }
    }
}
