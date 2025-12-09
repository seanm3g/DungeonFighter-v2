namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles hit/miss calculation logic
    /// </summary>
    public static class HitCalculator
    {
        /// <summary>
        /// Calculates hit/miss based on roll value only
        /// 1-5: Miss, 6-13: Regular attack, 14-19: Combo, 20: Combo + Critical
        /// </summary>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <param name="rollBonus">Roll bonus for the attack</param>
        /// <param name="roll">The attack roll result</param>
        /// <returns>True if the attack hits, false if it misses</returns>
        public static bool CalculateHit(Actor attacker, Actor target, int rollBonus, int roll)
        {
            // Hit/miss is based on roll value only, not target defense
            // 1-5: Miss, 6-20: Hit
            return roll >= 6;
        }
    }
}

