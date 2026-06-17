using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame.Combat
{
    /// <summary>Applies mechanic tags (swift/bludgeon/focus/insight) as next-action bonuses on successful hits.</summary>
    public static class ActionMechanicTagProcessor
    {
        private const double SwiftSpeedPercent = 10.0;
        private const double BludgeonDamagePercent = 15.0;
        private const double FocusAmpPercent = 10.0;
        private const double InsightAccuracy = 2.0;

        public static void QueueNextActionBonuses(Character character, Action action)
        {
            if (character == null || action?.Tags == null || action.Tags.Count == 0)
                return;

            var bonuses = new List<ActionAttackBonusItem>();
            if (ActionTagSyncHelper.HasTag(action.Tags, "swift"))
                bonuses.Add(new ActionAttackBonusItem { Type = "SPEED_MOD", Value = SwiftSpeedPercent });
            if (ActionTagSyncHelper.HasTag(action.Tags, "bludgeon"))
                bonuses.Add(new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = BludgeonDamagePercent });
            if (ActionTagSyncHelper.HasTag(action.Tags, "focus"))
                bonuses.Add(new ActionAttackBonusItem { Type = "AMP_MOD", Value = FocusAmpPercent });
            if (ActionTagSyncHelper.HasTag(action.Tags, "insight"))
                bonuses.Add(new ActionAttackBonusItem { Type = "ACCURACY", Value = InsightAccuracy });

            if (bonuses.Count > 0)
                character.Effects.AddPendingActionBonusesNextHeroRoll(bonuses);
        }
    }
}
