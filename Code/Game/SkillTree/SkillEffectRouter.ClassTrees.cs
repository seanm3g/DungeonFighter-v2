using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Class-tree effects for the level-100 trees (Documentation/SKILL_TREE_DESIGN.md): Kinship (class material
    /// pieces raise the class stat), Synthesis feed (class stat raises material resource mints), convert masteries,
    /// Arcane Weave, capstones, and the Mastery nodes. Magnitudes are per rank × node power.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {

        /// <summary>Synthesis feed: every 20 class stat adds +1 to each mint of that class's material resources, per rank.</summary>
        private static readonly (string Effect, string Stat, string ClassTag)[] SynthesisRules =
        {
            ("blood_synthesis", "STR", ClassTags.Barbarian),
            ("drill_synthesis", "AGI", ClassTags.Warrior),
            ("edge_synthesis", "TEC", ClassTags.Rogue),
            ("weave_synthesis", "INT", ClassTags.Wizard)
        };

        public const int SynthesisStatPerMint = 20;

        /// <summary>
        /// Player lines for learned rule nodes that ride this convert's store (ONCASH:&lt;its keyword&gt; or a rule naming the
        /// action). Empty when none are learned.
        /// </summary>
        public static string DescribeEquippedConvertMastery(Character? hero, string? actionName)
        {
            if (hero == null || hero is Enemy || string.IsNullOrWhiteSpace(actionName))
                return "";
            string? keyword = MaterialBuildsLoader.FindByConvertAction(actionName)?.Keyword;
            var lines = new List<string>();
            foreach (var (node, rule) in CrossWires.LearnedNodeRules(hero))
            {
                bool rides = (keyword != null && rule.When.Equals("ONCASH:" + keyword, StringComparison.OrdinalIgnoreCase))
                    || rule.Conditions.Any(c => c.Subject == "action" && string.Equals(c.Key, actionName, StringComparison.OrdinalIgnoreCase));
                if (rides)
                    lines.Add($"{node.Name}: {CrossWires.Describe(rule)}");
            }
            return string.Join(" ", lines);
        }

        /// <summary>Extra units per mint of <paramref name="material"/>'s resource from the owning class's Synthesis node.</summary>
        public static int SynthesisFeedBonus(Character? hero, string? material)
        {
            if (hero == null || string.IsNullOrWhiteSpace(material))
                return 0;
            int bonus = 0;
            foreach (var (effect, stat, classTag) in SynthesisRules)
            {
                if (!ClassTags.MaterialsOf(classTag).Any(m => string.Equals(m, material, StringComparison.OrdinalIgnoreCase)))
                    continue;
                double power = SkillRanks.GetPower(hero, effect);
                if (power > 0)
                    bonus += (int)Math.Floor(EffectiveStat(hero, stat) / (double)SynthesisStatPerMint * power);
            }
            return bonus;
        }

        private static int EffectiveStat(Character c, string stat) => stat switch
        {
            "STR" => c.GetEffectiveStrength(),
            "AGI" => c.GetEffectiveAgility(),
            "TEC" => c.GetEffectiveTechnique(),
            "INT" => c.GetEffectiveIntelligence(),
            _ => 0
        };

        /// <summary>Titan: raises the STR damage cap by 50 per rank (× power).</summary>
        public static int StrengthCapBonus(Character? c) => (int)Math.Round(50 * SkillRanks.GetPower(c, "titan"));

        /// <summary>Archmage: multiplier on INT's AMP potency and status-duration bonus (1 = unchanged).</summary>
        public static double ArchmageDurationMultiplier(Character? c) => 1.0 + SkillRanks.GetPower(c, "archmage");

        private void ApplyClassTreeOnHit(Character hero, CombatEvent evt)
        {
            ApplyDaggerMastery(hero, evt);
            ApplyCatalyst(hero, evt);
        }

        /// <summary>Dagger Mastery: multihit Dagger actions add +1 Bleed per extra hit (× power).</summary>
        private void ApplyDaggerMastery(Character hero, CombatEvent evt)
        {
            double power = GetPower(hero, "dagger_mastery");
            if (power <= 0 || evt.Target == null || evt.Action == null) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != WeaponType.Dagger) return;
            int extraHits = Math.Max(0, evt.Action.Advanced.MultiHitCount - 1) + (int)hero.Effects.ConsumedMultiHitMod
                + ActionUtilities.GetGearMultiHitBonus(hero, evt.Action);
            int bleed = (int)Math.Round(extraHits * power);
            if (bleed > 0)
                evt.Target.QueueBleedFromHit(bleed);
        }

        /// <summary>Catalyst: hits on targets with Acid armor loss also add Burn (× power).</summary>
        private void ApplyCatalyst(Character hero, CombatEvent evt)
        {
            double power = GetPower(hero, "catalyst");
            if (power <= 0 || evt.Target == null || evt.Target.AcidArmorReduction <= 0) return;
            evt.Target.QueueBurnFromHit(Math.Max(1, (int)Math.Round(power)));
        }

        /// <summary>Wand Mastery: actions in the strip count as Wizard tags while a Wand is equipped.</summary>
        public static bool WandMasteryTagsStrip(Character? hero) =>
            hero?.Weapon is WeaponItem w && w.WeaponType == WeaponType.Wand && SkillRanks.GetRank(hero, "wand_mastery") > 0;
    }
}
