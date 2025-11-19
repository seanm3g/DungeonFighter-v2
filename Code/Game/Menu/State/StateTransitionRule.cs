using RPGGame;

namespace DungeonFighter.Game.Menu.State
{
    /// <summary>
    /// Defines a valid state transition rule.
    /// Specifies which states can transition to which other states.
    /// </summary>
    public class StateTransitionRule
    {
        /// <summary>
        /// The source state (from).
        /// </summary>
        public GameState FromState { get; }

        /// <summary>
        /// The target state (to).
        /// </summary>
        public GameState ToState { get; }

        /// <summary>
        /// Optional condition function that must return true for transition to be allowed.
        /// If null, transition is always allowed if FromState matches.
        /// </summary>
        public Func<bool>? Condition { get; }

        /// <summary>
        /// Description of this transition (for logging and debugging).
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Creates a new state transition rule.
        /// </summary>
        public StateTransitionRule(
            GameState from,
            GameState to,
            string? description = null,
            Func<bool>? condition = null)
        {
            FromState = from;
            ToState = to;
            Description = description ?? $"{from} → {to}";
            Condition = condition;
        }

        /// <summary>
        /// Checks if this rule allows the transition.
        /// </summary>
        public bool IsValid()
        {
            return Condition?.Invoke() ?? true;
        }

        /// <summary>
        /// Checks if this rule matches the given state pair.
        /// </summary>
        public bool Matches(GameState from, GameState to)
        {
            return FromState == from && ToState == to;
        }

        public override string ToString()
        {
            return Description;
        }
    }
}

