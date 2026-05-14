using System;

namespace RPGGame
{
    /// <summary>
    /// Bonuses from room-search potions that last until the current dungeon run ends (cleared on exit, completion, death clone, or new run).
    /// Stat bonuses stack additively with equipment in <see cref="CharacterFacade"/> effective stat getters.
    /// Roll-threshold adjustments are reapplied after each <see cref="Combat.ThresholdManager.ResetThresholds"/> in combat selection (same sign as gear HIT/COMBO/CRIT; crit-miss uses negative values for benefit).
    /// </summary>
    public sealed class DungeonSearchBuffState
    {
        public int StrengthBonus { get; private set; }
        public int AgilityBonus { get; private set; }
        public int TechniqueBonus { get; private set; }
        public int IntelligenceBonus { get; private set; }

        public int HitThresholdAdjustment { get; private set; }
        public int ComboThresholdAdjustment { get; private set; }
        public int CritThresholdAdjustment { get; private set; }
        public int CritMissThresholdAdjustment { get; private set; }

        private const int MaxStatBonusEach = 12;
        private const int MaxThresholdMagnitudeEach = 6;

        public void Clear()
        {
            StrengthBonus = AgilityBonus = TechniqueBonus = IntelligenceBonus = 0;
            HitThresholdAdjustment = ComboThresholdAdjustment = CritThresholdAdjustment = CritMissThresholdAdjustment = 0;
        }

        public void AddStrength(int delta) => StrengthBonus = ClampAdd(StrengthBonus, delta, MaxStatBonusEach);
        public void AddAgility(int delta) => AgilityBonus = ClampAdd(AgilityBonus, delta, MaxStatBonusEach);
        public void AddTechnique(int delta) => TechniqueBonus = ClampAdd(TechniqueBonus, delta, MaxStatBonusEach);
        public void AddIntelligence(int delta) => IntelligenceBonus = ClampAdd(IntelligenceBonus, delta, MaxStatBonusEach);

        public void AddHitThresholdAdjustment(int delta) =>
            HitThresholdAdjustment = ClampAddThreshold(HitThresholdAdjustment, delta, MaxThresholdMagnitudeEach);

        public void AddComboThresholdAdjustment(int delta) =>
            ComboThresholdAdjustment = ClampAddThreshold(ComboThresholdAdjustment, delta, MaxThresholdMagnitudeEach);

        public void AddCritThresholdAdjustment(int delta) =>
            CritThresholdAdjustment = ClampAddThreshold(CritThresholdAdjustment, delta, MaxThresholdMagnitudeEach);

        /// <summary>Negative values make crit-miss less likely (same convention as beneficial CRIT_MISS sheet adjustments).</summary>
        public void AddCritMissThresholdAdjustment(int delta) =>
            CritMissThresholdAdjustment = ClampAddThreshold(CritMissThresholdAdjustment, delta, MaxThresholdMagnitudeEach);

        private static int ClampAdd(int current, int delta, int maxAbs)
        {
            int sum = current + delta;
            return Math.Clamp(sum, 0, maxAbs);
        }

        /// <summary>Allows negative totals for crit-miss (benefit); magnitude capped per direction.</summary>
        private static int ClampAddThreshold(int current, int delta, int maxAbs)
        {
            int sum = current + delta;
            return Math.Clamp(sum, -maxAbs, maxAbs);
        }
    }
}
