using RPGGame.Actions.RollModification;
using RPGGame.Data;

namespace RPGGame.Combat
{
    /// <summary>Applies roll tags (confidence/footwork/target/aim) as threshold shifts for the current action roll.</summary>
    public static class ActionRollTagProcessor
    {
        /// <summary>Positive shift lowers hit/combo/crit thresholds (see <see cref="ThresholdManager.AdjustHitThreshold"/>).</summary>
        private const int EaseThresholdShift = 2;

        /// <summary>Negative shift lowers crit-miss threshold (see <see cref="ThresholdManager.AdjustCriticalMissThreshold"/>).</summary>
        private const int CritMissComfortShift = -2;

        public static void ApplyRollTags(Action? action, Actor source)
        {
            if (action?.Tags == null || action.Tags.Count == 0 || source == null)
                return;

            var tm = RollModificationManager.GetThresholdManager();
            if (ActionTagSyncHelper.HasTag(action.Tags, "confidence"))
                tm.AdjustCriticalMissThreshold(source, CritMissComfortShift);
            if (ActionTagSyncHelper.HasTag(action.Tags, "footwork"))
                tm.AdjustHitThreshold(source, EaseThresholdShift);
            if (ActionTagSyncHelper.HasTag(action.Tags, "target"))
                tm.AdjustComboThreshold(source, EaseThresholdShift);
            if (ActionTagSyncHelper.HasTag(action.Tags, "aim"))
                tm.AdjustCriticalHitThreshold(source, EaseThresholdShift);
        }
    }
}
