using RPGGame.Actions.RollModification;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles hit/miss calculation logic
    /// </summary>
    public static class HitCalculator
    {
        /// <summary>
        /// Calculates hit/miss based on roll value only
        /// Uses dynamic hit threshold from ThresholdManager (default: 5, meaning 6+ hits)
        /// </summary>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <param name="rollBonus">Roll bonus for the attack</param>
        /// <param name="roll">The attack roll result</param>
        /// <returns>True if the attack hits, false if it misses</returns>
        public static bool CalculateHit(Actor attacker, Actor target, int rollBonus, int roll)
        {
            // Get dynamic hit threshold from ThresholdManager
            // Default is 5 (meaning you need 6+ to hit)
            // If threshold is increased (e.g., to 15), you need 16+ to hit
            int hitThreshold = RollModificationManager.GetThresholdManager().GetHitThreshold(attacker);
            
            // Hit if roll exceeds the threshold (threshold + 1 = minimum roll to hit)
            return roll > hitThreshold;
        }
    }
}

