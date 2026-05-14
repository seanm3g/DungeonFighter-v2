using RPGGame.Actions.RollModification;
using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// After gear threshold shifts are applied in combat selection, applies stored dungeon potion threshold bonuses.
    /// </summary>
    public static class DungeonSearchBuffThresholdApplicator
    {
        public static void Apply(Character? hero, ThresholdManager thresholdManager)
        {
            if (hero == null || hero is Enemy || thresholdManager == null)
                return;

            var b = hero.DungeonSearchBuffs;
            if (b.HitThresholdAdjustment != 0)
                thresholdManager.AdjustHitThreshold(hero, b.HitThresholdAdjustment);
            if (b.ComboThresholdAdjustment != 0)
                thresholdManager.AdjustComboThreshold(hero, b.ComboThresholdAdjustment);
            if (b.CritThresholdAdjustment != 0)
                thresholdManager.AdjustCriticalHitThreshold(hero, b.CritThresholdAdjustment);
            if (b.CritMissThresholdAdjustment != 0)
                thresholdManager.AdjustCriticalMissThreshold(hero, b.CritMissThresholdAdjustment);
        }
    }
}
