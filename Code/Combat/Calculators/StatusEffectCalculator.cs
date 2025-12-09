using RPGGame.Utils;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles status effect chance calculations
    /// </summary>
    public static class StatusEffectCalculator
    {
        /// <summary>
        /// Calculates status effect application chance
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <returns>True if status effect should be applied</returns>
        public static bool CalculateStatusEffectChance(Action action, Actor attacker, Actor target)
        {
            // Check if action can cause any status effect (basic or advanced)
            if (!action.CausesBleed && !action.CausesWeaken && !action.CausesSlow && 
                !action.CausesPoison && !action.CausesStun && !action.CausesBurn &&
                !action.CausesVulnerability && !action.CausesHarden && !action.CausesFortify &&
                !action.CausesFocus && !action.CausesExpose && !action.CausesHPRegen &&
                !action.CausesArmorBreak && !action.CausesPierce && !action.CausesReflect &&
                !action.CausesSilence && !action.CausesStatDrain && !action.CausesAbsorb &&
                !action.CausesTemporaryHP && !action.CausesConfusion && !action.CausesCleanse &&
                !action.CausesMark && !action.CausesDisrupt)
            {
                return false; // No status effects possible
            }
            
            // Use 2d2-2 roll for status effect chance
            // 2d2 gives range 2-4, minus 2 gives range 0-2
            // 0 = no effect, 1+ = effect applied
            int roll1 = Dice.Roll(1, 2); // First d2
            int roll2 = Dice.Roll(1, 2); // Second d2
            int result = (roll1 + roll2) - 2; // 2d2-2
            
            // Effect is applied if result is 1 or 2 (66.7% chance)
            return result >= 1;
        }
    }
}

