using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Encapsulates all information about a combat outcome
    /// Used to determine appropriate colors and formatting for combat messages
    /// </summary>
    public class CombatOutcome
    {
        public bool IsCritical { get; set; }
        public bool IsMiss { get; set; }
        public bool IsBlock { get; set; }
        public bool IsDodge { get; set; }
        public bool IsComboAction { get; set; }
        public bool IsCriticalMiss { get; set; }
        
        /// <summary>
        /// The action that was performed
        /// </summary>
        public Action? Action { get; set; }
        
        /// <summary>
        /// The total roll value (base roll + bonuses)
        /// </summary>
        public int TotalRoll { get; set; }
        
        /// <summary>
        /// The natural (unmodified) roll value
        /// </summary>
        public int NaturalRoll { get; set; }
        
        /// <summary>
        /// Creates a CombatOutcome for a successful hit
        /// </summary>
        /// <param name="resolvedCritical">When set, matches combat resolution (<see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/>);
        /// when null, uses total roll ≥ 20 for callers without execution context (e.g. environmental damage).</param>
        public static CombatOutcome CreateHit(Action? action, int totalRoll, int naturalRoll, bool isComboAction, bool? resolvedCritical = null)
        {
            return new CombatOutcome
            {
                IsCritical = resolvedCritical ?? (totalRoll >= 20),
                IsMiss = false,
                IsBlock = false,
                IsDodge = false,
                IsComboAction = isComboAction,
                IsCriticalMiss = false,
                Action = action,
                TotalRoll = totalRoll,
                NaturalRoll = naturalRoll
            };
        }
        
        /// <summary>
        /// Creates a CombatOutcome for a miss
        /// </summary>
        public static CombatOutcome CreateMiss(Action? action, int totalRoll, int naturalRoll)
        {
            return new CombatOutcome
            {
                IsCritical = false,
                IsMiss = true,
                IsBlock = false,
                IsDodge = false,
                IsComboAction = false,
                IsCriticalMiss = naturalRoll == 1,
                Action = action,
                TotalRoll = totalRoll,
                NaturalRoll = naturalRoll
            };
        }
        
        /// <summary>
        /// Creates a CombatOutcome for a block
        /// </summary>
        public static CombatOutcome CreateBlock()
        {
            return new CombatOutcome
            {
                IsCritical = false,
                IsMiss = false,
                IsBlock = true,
                IsDodge = false,
                IsComboAction = false,
                IsCriticalMiss = false
            };
        }
        
        /// <summary>
        /// Creates a CombatOutcome for a dodge
        /// </summary>
        public static CombatOutcome CreateDodge()
        {
            return new CombatOutcome
            {
                IsCritical = false,
                IsMiss = false,
                IsBlock = false,
                IsDodge = true,
                IsComboAction = false,
                IsCriticalMiss = false
            };
        }
    }
}

