using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Events;

namespace RPGGame.Combat.Outcomes
{
    /// <summary>
    /// Base interface for outcome handlers
    /// </summary>
    public interface IOutcomeHandler
    {
        /// <summary>
        /// Handles an outcome event
        /// </summary>
        void HandleOutcome(CombatEvent evt, Actor source, Actor? target, Action? action);
        
        /// <summary>
        /// Gets the outcome type this handler processes
        /// </summary>
        string OutcomeType { get; }
    }
}

