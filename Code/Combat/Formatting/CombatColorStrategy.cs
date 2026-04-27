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
        /// Gets the color for the "hits" verb in the combat log (always white; outcome is reflected in action name and damage numerals).
        /// </summary>
        public static ColorPalette GetHitsColor(CombatOutcome outcome)
        {
            _ = outcome;
            return ColorPalette.White;
        }
        
        /// <summary>
        /// Gets the color for the action name based on the combat outcome and whether the attacker is an enemy.
        /// Enemy actions use purple (normal) and orange (critical) so they are not confused with player green,
        /// roll-info cyan, or damage red.
        /// </summary>
        public static ColorPalette GetActionColor(CombatOutcome outcome, bool attackerIsEnemy)
        {
            if (outcome.IsCritical)
            {
                return attackerIsEnemy ? ColorPalette.Orange : ColorPalette.Critical;
            }

            return attackerIsEnemy ? ColorPalette.Purple : ColorPalette.Green;
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
        /// Gets the color for the "misses" / "MISS" verb in the combat log (always white; critical miss is still called out by the leading "CRITICAL" segment).
        /// </summary>
        public static ColorPalette GetMissColor(CombatOutcome outcome)
        {
            _ = outcome;
            return ColorPalette.White;
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
        /// All actions in the game are combo actions
        /// </summary>
        public static bool IsComboAction(string? actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return false;
            }
            
            // All actions in the game are combo actions
            return true;
        }
    }
}

