using System.Collections.Generic;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Applies fight- and dungeon-scoped cadence bonuses each hero roll (peek, no consumption).
    /// </summary>
    public static class CadenceScopedBuffApplicator
    {
        public static void ApplyThresholds(Character? hero, ThresholdManager thresholdManager)
        {
            if (hero == null || hero is Enemy || thresholdManager == null)
                return;

            ApplyScopeThresholds(hero.FightCadenceBuffs, hero, thresholdManager);
            ApplyScopeThresholds(hero.DungeonCadenceBuffs, hero, thresholdManager);
        }

        private static void ApplyScopeThresholds(
            CadenceScopedBonusState scope,
            Character hero,
            ThresholdManager thresholdManager)
        {
            if (!scope.HasAny) return;
            RollModificationManager.ApplyDeferredThresholdPackageSetPhase(hero, scope.CopyBonuses());
        }

        public static void AccumulateModifiers(Character? hero, ref double damageMod, ref double speedMod, ref double multiHitMod, ref double ampMod)
        {
            if (hero == null || hero is Enemy) return;
            AccumulateScopeModifiers(hero.FightCadenceBuffs, ref damageMod, ref speedMod, ref multiHitMod, ref ampMod);
            AccumulateScopeModifiers(hero.DungeonCadenceBuffs, ref damageMod, ref speedMod, ref multiHitMod, ref ampMod);
        }

        private static void AccumulateScopeModifiers(
            CadenceScopedBonusState scope,
            ref double damageMod,
            ref double speedMod,
            ref double multiHitMod,
            ref double ampMod)
        {
            foreach (var b in scope.Bonuses)
            {
                switch ((b.Type ?? "").ToUpperInvariant())
                {
                    case "SPEED_MOD": speedMod += b.Value; break;
                    case "DAMAGE_MOD": damageMod += b.Value; break;
                    case "MULTIHIT_MOD": multiHitMod += b.Value; break;
                    case "AMP_MOD": ampMod += b.Value; break;
                }
            }
        }

        public static void CollectAdvantageFlags(Character? hero, ref bool advantage, ref bool disadvantage)
        {
            if (hero == null || hero is Enemy) return;
            RollModificationManager.CollectAdvantageFlags(hero.FightCadenceBuffs.CopyBonuses(), ref advantage, ref disadvantage);
            RollModificationManager.CollectAdvantageFlags(hero.DungeonCadenceBuffs.CopyBonuses(), ref advantage, ref disadvantage);
        }

        public static void AccumulateRollBonuses(Character? hero, ref int accuracy, ref int hit, ref int combo, ref int crit, ref int critMiss)
        {
            if (hero == null || hero is Enemy) return;
            foreach (var b in hero.FightCadenceBuffs.Bonuses)
                AccumulateRollBonus(b, ref accuracy, ref hit, ref combo, ref crit, ref critMiss);
            foreach (var b in hero.DungeonCadenceBuffs.Bonuses)
                AccumulateRollBonus(b, ref accuracy, ref hit, ref combo, ref crit, ref critMiss);
        }

        private static void AccumulateRollBonus(
            ActionAttackBonusItem bonus,
            ref int accuracy,
            ref int hit,
            ref int combo,
            ref int crit,
            ref int critMiss)
        {
            switch ((bonus.Type ?? "").ToUpperInvariant())
            {
                case "ACCURACY": accuracy += (int)bonus.Value; break;
                case "HIT": hit += (int)bonus.Value; break;
                case "COMBO": combo += (int)bonus.Value; break;
                case "CRIT": crit += (int)bonus.Value; break;
                case "CRIT_MISS": critMiss += (int)bonus.Value; break;
            }
        }

        public static void DepositToScope(Character hero, string scope, IEnumerable<ActionAttackBonusItem>? bonuses, int stackTimes = 1)
        {
            if (bonuses == null || stackTimes < 1) return;
            if (CadenceKeywords.IsFight(scope))
                hero.FightCadenceBuffs.MergeAdditively(bonuses, stackTimes);
            else if (CadenceKeywords.IsDungeon(scope))
                hero.DungeonCadenceBuffs.MergeAdditively(bonuses, stackTimes);
        }
    }
}
