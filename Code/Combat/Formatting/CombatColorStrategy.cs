using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Centralized strategy for determining colors based on combat outcomes
    /// Provides a single source of truth for combat message coloring
    /// </summary>
    public static class CombatColorStrategy
    {
        /// <summary>
        /// Gets the color for the "hits" verb based on the combat outcome
        /// </summary>
        public static ColorPalette GetHitsColor(CombatOutcome outcome)
        {
            if (outcome.IsMiss || outcome.IsBlock || outcome.IsDodge)
            {
                // Misses, blocks, and dodges don't use "hits"
                return ColorPalette.White;
            }
            
            if (outcome.IsCritical)
            {
                return ColorPalette.Critical;
            }
            
            if (outcome.IsComboAction)
            {
                return ColorPalette.Green; // Action color for combo actions
            }
            
            return ColorPalette.Damage; // Normal hit color
        }
        
        /// <summary>
        /// Gets the color for the action name based on the combat outcome
        /// </summary>
        public static ColorPalette GetActionColor(CombatOutcome outcome)
        {
            if (outcome.IsCritical)
            {
                return ColorPalette.Critical;
            }
            
            return ColorPalette.Green; // Default action color
        }
        
        /// <summary>
        /// Gets the color for damage numbers based on the combat outcome
        /// </summary>
        public static ColorPalette GetDamageColor(CombatOutcome outcome)
        {
            if (outcome.IsCritical)
            {
                return ColorPalette.Critical;
            }
            
            return ColorPalette.Damage;
        }
        
        /// <summary>
        /// Gets the color for the miss verb based on the combat outcome
        /// </summary>
        public static ColorPalette GetMissColor(CombatOutcome outcome)
        {
            if (outcome.IsCriticalMiss)
            {
                return ColorPalette.Critical;
            }
            
            return ColorPalette.Miss;
        }
        
        /// <summary>
        /// Gets the color for block messages
        /// </summary>
        public static ColorPalette GetBlockColor()
        {
            return ColorPalette.Block;
        }
        
        /// <summary>
        /// Gets the color for dodge messages
        /// </summary>
        public static ColorPalette GetDodgeColor()
        {
            return ColorPalette.Dodge;
        }
        
        /// <summary>
        /// Determines if an action is a combo action based on its name
        /// </summary>
        public static bool IsComboAction(string? actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return false;
            }
            
            return actionName != "BASIC ATTACK" && 
                   actionName != "CRITICAL BASIC ATTACK";
        }
    }
}

