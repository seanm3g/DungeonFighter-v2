using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// Converts remaining naiveté into per-attack HIT threshold improvements for early heroes.
    /// </summary>
    public static class NaiveteThresholdBonuses
    {
        public static int GetHitSteps(Character attacker)
        {
            if (attacker is Enemy)
                return 0;

            return NaiveteBalanceHelper.GetHitSteps(attacker);
        }

        /// <summary>
        /// Applies naiveté HIT threshold deltas (not roll bonus) so miss bands shrink correctly.
        /// </summary>
        public static void Apply(ThresholdManager tm, Character attacker)
        {
            if (attacker is Enemy)
                return;

            int hitSteps = GetHitSteps(attacker);
            for (int i = 0; i < hitSteps; i++)
                tm.AdjustHitThreshold(attacker, adjustment: 1);
        }
    }
}
