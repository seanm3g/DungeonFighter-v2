using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// Legacy HIT-threshold path for naiveté. No longer applies — naiveté is miss→advantage charges.
    /// Kept so older call sites compile without changing threshold math.
    /// </summary>
    public static class NaiveteThresholdBonuses
    {
        public static int GetHitSteps(Character attacker) => 0;

        /// <summary>No-op: naiveté no longer adjusts HIT thresholds.</summary>
        public static void Apply(ThresholdManager tm, Character attacker)
        {
        }
    }
}
