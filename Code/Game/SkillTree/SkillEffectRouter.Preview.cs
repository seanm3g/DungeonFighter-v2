using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// One guaranteed-on-this-swing class bonus for action-card preview (does not mutate combat state).
    /// </summary>
    public readonly struct SkillActionCardBonus
    {
        public string SourceName { get; }
        /// <summary>dmg, spd, amp, or multihit.</summary>
        public string StatKey { get; }
        public double Value { get; }

        public SkillActionCardBonus(string sourceName, string statKey, double value)
        {
            SourceName = sourceName ?? "";
            StatKey = statKey ?? "";
            Value = value;
        }
    }

    public sealed partial class SkillEffectRouter
    {
        /// <summary>
        /// Standing skill bonuses that apply when <paramref name="action"/> fires from strip slot
        /// <paramref name="comboSlotIndex"/> (0-based). Slot-gated nodes only appear on that card.
        /// Situational on-hit procs (crit-only, target-state, streaks) are omitted.
        /// </summary>
        public List<SkillActionCardBonus> CollectActionCardBonuses(Character? hero, Action? action, int comboSlotIndex)
        {
            var list = new List<SkillActionCardBonus>();
            if (hero == null || hero is Enemy || action == null)
                return list;

            TryAddPercent(list, hero, "first_bronze_age", "dmg",
                8.0 * CountEquippedMatchingTags(hero, BarbarianMaterialTags));
            TryAddPercent(list, hero, "first_formation", "spd",
                8.0 * CountEquippedMatchingTags(hero, WarriorMaterialTags));
            TryAddPercent(list, hero, "first_cut", "dmg",
                8.0 * CountEquippedMatchingTags(hero, RogueMaterialTags));
            TryAddPercent(list, hero, "first_glyph", "amp",
                8.0 * CountEquippedMatchingTags(hero, WizardMaterialTags));

            TryAddPercent(list, hero, "scrap_prefer", "dmg",
                10.0 * CountEquippedMatchingQualities(hero, ScrapPreferQualities));
            TryAddPercent(list, hero, "parade_dress", "dmg",
                10.0 * CountEquippedMatchingQualities(hero, ParadeDressQualities));
            TryAddPercent(list, hero, "fragile_prefer", "dmg",
                10.0 * CountEquippedMatchingQualities(hero, FragilePreferQualities));
            TryAddPercent(list, hero, "pure_prefer", "amp",
                10.0 * CountEquippedMatchingQualities(hero, PurePreferQualities));

            int bronze = CountBronzeItems(hero);
            if (comboSlotIndex == 1 && bronze >= 2)
            {
                int rank = GetRank(hero, "second_bronze_age");
                if (rank > 0)
                    list.Add(new SkillActionCardBonus(ResolveSkillName(hero, "second_bronze_age"), "multihit", (bronze / 2.0) * rank));
            }

            if (comboSlotIndex == 2 && bronze > 0)
            {
                int rank = GetRank(hero, "third_bronze_age");
                if (rank > 0)
                {
                    double speedPct = 6.0 * bronze * rank;
                    double appliedSpeed = Math.Min(30.0, speedPct);
                    list.Add(new SkillActionCardBonus(ResolveSkillName(hero, "third_bronze_age"), "spd", appliedSpeed));
                }
            }

            if (comboSlotIndex == 1)
                TryAddPercent(list, hero, "metronome", "spd", 4.0);

            if ((hero.HardenStacks ?? 0) > 0)
                TryAddPercent(list, hero, "bronze_knuckle", "dmg", 3.0);
            if ((hero.FortifyStacks ?? 0) > 0)
                TryAddPercent(list, hero, "phalanx", "dmg", 2.0);

            AddDataDrivenRankDamage(list, hero);
            return list;
        }

        private void TryAddPercent(List<SkillActionCardBonus> list, Character hero, string customEffectId, string statKey, double perRankValue)
        {
            int rank = GetRank(hero, customEffectId);
            if (rank <= 0 || perRankValue <= 0)
                return;
            list.Add(new SkillActionCardBonus(ResolveSkillName(hero, customEffectId), statKey, perRankValue * rank));
        }

        private void AddDataDrivenRankDamage(List<SkillActionCardBonus> list, Character hero)
        {
            if (hero.Progression == null)
                return;
            WeaponType? path = hero.Progression.GetPrimaryClassWeaponType();
            if (path == null)
                return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != path.Value)
                return;

            var owner = SkillTreeService.Trees.GetTreeForWeapon(path.Value);
            if (owner == null)
                return;

            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node == null || node.DamageModPerRank <= 0)
                    continue;
                if (!owner.Nodes.Any(n => string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                string name = string.IsNullOrWhiteSpace(node.Name) ? node.Id : node.Name;
                list.Add(new SkillActionCardBonus(name, "dmg", node.DamageModPerRank * kv.Value));
            }
        }

        private static string ResolveSkillName(Character hero, string customEffectId)
        {
            if (hero.Progression?.LearnedSkillRanks == null)
                return customEffectId;
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node != null
                    && string.Equals(node.CustomEffectId, customEffectId, StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrWhiteSpace(node.Name) ? customEffectId : node.Name;
                }
            }

            return customEffectId;
        }
    }
}
