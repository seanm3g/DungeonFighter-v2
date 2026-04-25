using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// Converts effective INT into per-row HIT/COMBO/CRIT threshold improvements based on the tuning table.
    /// Applies these as threshold shifts (not roll bonus) so they affect hit/combo/crit bands correctly.
    /// </summary>
    public static class IntelligenceMilestoneThresholdBonuses
    {
        private readonly record struct Milestone(int IntThreshold, int Hit, int Combo, int Crit);

        // Cumulative rows (each reached milestone grants the row's deltas).
        // HIT/COMBO/CRIT here are "steps" where +1 means easier (lower threshold by 1).
        private static readonly Milestone[] Table = new[]
        {
            new Milestone(10, Hit: 1, Combo: 0, Crit: 0),
            new Milestone(15, Hit: 1, Combo: 0, Crit: 0),
            new Milestone(20, Hit: 1, Combo: 0, Crit: 0),
            new Milestone(25, Hit: 1, Combo: 0, Crit: 0),
            new Milestone(30, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(35, Hit: 1, Combo: 0, Crit: 0),
            new Milestone(40, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(45, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(50, Hit: 0, Combo: 0, Crit: 1),
            new Milestone(55, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(60, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(65, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(70, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(75, Hit: 0, Combo: 0, Crit: 1),
            new Milestone(80, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(85, Hit: 0, Combo: 1, Crit: 0),
            new Milestone(90, Hit: 0, Combo: 0, Crit: 1),
            new Milestone(95, Hit: 0, Combo: 0, Crit: 1),
            new Milestone(100, Hit: 0, Combo: 0, Crit: 1),
        };

        public static (int HitSteps, int ComboSteps, int CritSteps) GetSteps(int effectiveInt)
        {
            int hit = 0, combo = 0, crit = 0;
            for (int i = 0; i < Table.Length; i++)
            {
                if (effectiveInt < Table[i].IntThreshold)
                    break;
                hit += Table[i].Hit;
                combo += Table[i].Combo;
                crit += Table[i].Crit;
            }
            return (hit, combo, crit);
        }

        /// <summary>
        /// Applies INT milestone deltas to the actor's thresholds.
        /// Order matches execution: CRIT then COMBO then HIT so cascades are predictable.
        /// </summary>
        public static void Apply(ThresholdManager tm, Character attacker)
        {
            int effectiveInt = attacker.GetEffectiveIntelligence();
            var (hitSteps, comboSteps, critSteps) = GetSteps(effectiveInt);

            for (int i = 0; i < critSteps; i++)
                tm.AdjustCriticalHitThreshold(attacker, adjustment: 1);
            for (int i = 0; i < comboSteps; i++)
                tm.AdjustComboThreshold(attacker, adjustment: 1);
            for (int i = 0; i < hitSteps; i++)
                tm.AdjustHitThreshold(attacker, adjustment: 1);
        }
    }
}

