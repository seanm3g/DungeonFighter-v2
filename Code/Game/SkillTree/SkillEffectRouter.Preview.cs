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
            // Learned rule nodes' passive DAMAGE / SPEED / AMP / MULTIHIT that hold for this action now.
            foreach (var (source, stat, value) in CrossWires.NodeCardBonuses(hero, action))
                list.Add(new SkillActionCardBonus(source, stat, value));
            AddDataDrivenRankDamage(list, hero);
            return list;
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
                list.Add(new SkillActionCardBonus(name, "dmg", node.DamageModPerRank * (kv.Value + CrossWires.SkillRankBonus(hero, kv.Key))));
            }
        }

    }
}
