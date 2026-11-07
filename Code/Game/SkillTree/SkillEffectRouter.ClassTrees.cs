using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Class-tree effects for the level-100 trees (Documentation/SKILL_TREE_DESIGN.md): Kinship (set-material
    /// pieces raise the class stat), Synthesis feed (class stat raises every material's mints), convert masteries
    /// (each class adds its own rider to every convert, whatever the material),
    /// Arcane Weave, capstones, and the Mastery nodes. Magnitudes are per rank × node power.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        /// <summary>Kinship: +1 class stat per equipped set-material piece (any of the twelve), per rank.</summary>
        private static readonly (string Effect, string Stat, string ClassTag)[] KinshipRules =
        {
            ("tribal_kinship", "STR", ClassTags.Barbarian),
            ("martial_kinship", "AGI", ClassTags.Warrior),
            ("shadow_kinship", "TEC", ClassTags.Rogue),
            ("arcane_kinship", "INT", ClassTags.Wizard)
        };

        /// <summary>Synthesis feed: every 20 class stat adds +1 to each mint of any material's resource, per rank.</summary>
        private static readonly (string Effect, string Stat, string ClassTag)[] SynthesisRules =
        {
            ("blood_synthesis", "STR", ClassTags.Barbarian),
            ("drill_synthesis", "AGI", ClassTags.Warrior),
            ("edge_synthesis", "TEC", ClassTags.Rogue),
            ("weave_synthesis", "INT", ClassTags.Wizard)
        };

        public const int SynthesisStatPerMint = 20;

        private enum ConvertRider { HealPctMaxHp, DamageVsLowHp, Fortify, Speed, Slow, Focus, Vulnerable, Expose, Damage, Amp }

        /// <summary>
        /// Convert masteries: a class's rider on every convert it casts (any material), per cast with a non-empty bank,
        /// per rank x power. Materials are shared, so each class tree helps every convert in its own way.
        /// </summary>
        private static readonly (string Effect, ConvertRider Rider, double PerCast)[] ConvertMasteries =
        {
            ("wrath_unbound", ConvertRider.HealPctMaxHp, 5.0),
            ("iron_cull", ConvertRider.DamageVsLowHp, 20.0),
            ("steel_crest", ConvertRider.Fortify, 2.0),
            ("war_drum", ConvertRider.Speed, 15.0),
            ("gold_anchor", ConvertRider.Slow, 0.15),
            ("mithril_flow", ConvertRider.Focus, 3.0),
            ("glass_shatter", ConvertRider.Vulnerable, 3.0),
            ("umbral_step", ConvertRider.Expose, 2.0),
            ("trophy_hunter", ConvertRider.Damage, 15.0),
            ("willow_lash", ConvertRider.Amp, 20.0),
            ("refraction", ConvertRider.Expose, 2.0),
            ("resonance", ConvertRider.Damage, 15.0)
        };

        /// <summary>Standing stat from Kinship nodes (folded into the effective stat getters).</summary>
        private static int KinshipBonus(Character c, string statCode)
        {
            int total = 0;
            foreach (var (effect, stat, classTag) in KinshipRules)
            {
                if (!string.Equals(stat, statCode, StringComparison.OrdinalIgnoreCase))
                    continue;
                double power = SkillRanks.GetPower(c, effect);
                if (power > 0)
                    total += (int)Math.Round(CountSetMaterialPieces(c) * power);
            }
            return total;
        }

        /// <summary>Equipped pieces made of any of the twelve set materials.</summary>
        public static int CountSetMaterialPieces(Character c) =>
            new Item?[] { c.Weapon, c.Head, c.Body, c.Legs, c.Feet }
                .Count(i => i != null && ItemMaterialRules.IsSetMaterial(i.Material));

        /// <summary>
        /// Player line for a convert mastery the hero has actually ranked. Empty when the node is unranked.
        /// </summary>
        public static string DescribeEquippedConvertMastery(Character? hero, string? actionName)
        {
            if (hero == null || hero is Enemy || string.IsNullOrWhiteSpace(actionName)
                || MaterialBuildsLoader.FindByConvertAction(actionName) == null)
                return "";
            var lines = new List<string>();
            foreach (var mastery in ConvertMasteries)
            {
                double power = SkillRanks.GetPower(hero, mastery.Effect);
                if (power <= 0)
                    continue;
                string per = (mastery.PerCast * power).ToString("0.##", CultureInfo.InvariantCulture);
                lines.Add(mastery.Rider switch
                {
                    ConvertRider.HealPctMaxHp => $"Your mastery heals {per}% max HP.",
                    ConvertRider.DamageVsLowHp => $"Your mastery adds +{per}% damage against foes at 25% HP or less.",
                    ConvertRider.Fortify => $"Your mastery grants {per} Fortify.",
                    ConvertRider.Speed => $"Your mastery adds +{per}% speed.",
                    ConvertRider.Slow => "Your mastery slows the target.",
                    ConvertRider.Focus => $"Your mastery grants {per} Focus.",
                    ConvertRider.Vulnerable => $"Your mastery applies {per} Vulnerability.",
                    ConvertRider.Expose => $"Your mastery applies {per} Expose.",
                    ConvertRider.Damage => $"Your mastery adds +{per}% damage.",
                    ConvertRider.Amp => $"Your mastery adds +{per}% AMP.",
                    _ => ""
                });
            }
            return string.Join(" ", lines.Where(l => l.Length > 0));
        }

        /// <summary>Extra units per mint of <paramref name="material"/>'s resource from every learned Synthesis node.</summary>
        public static int SynthesisFeedBonus(Character? hero, string? material)
        {
            if (hero == null || string.IsNullOrWhiteSpace(material))
                return 0;
            int bonus = 0;
            if (!ItemMaterialRules.IsSetMaterial(material))
                return 0;
            foreach (var (effect, stat, _) in SynthesisRules)
            {
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

        /// <summary>Perfect Aim: every 25 TEC also lowers the crit threshold by 1 (× power).</summary>
        public static int PerfectAimCritSteps(Character? c)
        {
            if (c == null || c is Enemy) return 0;
            double power = SkillRanks.GetPower(c, "perfect_aim");
            return power > 0 ? (int)Math.Floor(c.GetEffectiveTechnique() / 25.0 * power) : 0;
        }

        /// <summary>Archmage: multiplier on INT's AMP potency and status-duration bonus (1 = unchanged).</summary>
        public static double ArchmageDurationMultiplier(Character? c) => 1.0 + SkillRanks.GetPower(c, "archmage");

        private void ApplyClassTreeOnHit(Character hero, CombatEvent evt)
        {
            ApplyConvertMastery(hero, evt);
            ApplyArcaneWeave(hero, evt);
            ApplyConcentratedChaos(hero, evt);
            ApplyDaggerMastery(hero, evt);
            ApplyCatalyst(hero, evt);
        }

        private void ApplyConvertMastery(Character hero, CombatEvent evt)
        {
            var build = MaterialBuildsLoader.FindByConvertAction(evt.Action?.Name);
            if (build == null || MaterialSetController.GetKeyword(hero, build.Keyword) <= 0) return;
            foreach (var m in ConvertMasteries)
            {
                double power = GetPower(hero, m.Effect);
                if (power <= 0) continue;
                double amount = m.PerCast * power;
                var target = evt.Target;
                switch (m.Rider)
                {
                    case ConvertRider.HealPctMaxHp:
                        hero.Heal(Math.Max(1, (int)Math.Round(hero.MaxHealth * amount / 100.0)));
                        break;
                    case ConvertRider.DamageVsLowHp:
                        if (target is Character foe && foe.MaxHealth > 0 && foe.CurrentHealth <= foe.MaxHealth / 4)
                            AddDamageMod(hero, amount);
                        break;
                    case ConvertRider.Fortify:
                        hero.FortifyStacks = (hero.FortifyStacks ?? 0) + Math.Max(1, (int)amount);
                        hero.FortifyTurns = Math.Max(hero.FortifyTurns, 2);
                        break;
                    case ConvertRider.Speed:
                        AddSpeedMod(hero, amount);
                        break;
                    case ConvertRider.Slow:
                        if (target is Character slowed)
                        {
                            slowed.Effects.SlowMultiplier = Math.Min(slowed.Effects.SlowMultiplier, Math.Max(0.5, 1.0 - amount));
                            slowed.Effects.SlowTurns = Math.Max(slowed.Effects.SlowTurns, 2);
                        }
                        break;
                    case ConvertRider.Focus:
                        hero.FocusStacks = (hero.FocusStacks ?? 0) + Math.Max(1, (int)amount);
                        hero.FocusTurns = Math.Max(hero.FocusTurns, 2);
                        break;
                    case ConvertRider.Vulnerable:
                        if (target != null)
                        {
                            target.VulnerabilityStacks = (target.VulnerabilityStacks ?? 0) + Math.Max(1, (int)amount);
                            target.VulnerabilityTurns = Math.Max(target.VulnerabilityTurns, 2);
                        }
                        break;
                    case ConvertRider.Expose:
                        if (target != null)
                        {
                            target.ExposeStacks = (target.ExposeStacks ?? 0) + Math.Max(1, (int)amount);
                            target.ExposeTurns = Math.Max(target.ExposeTurns, 2);
                        }
                        break;
                    case ConvertRider.Damage:
                        AddDamageMod(hero, amount);
                        break;
                    case ConvertRider.Amp:
                        AddAmpMod(hero, amount);
                        break;
                }
            }
        }

        /// <summary>Arcane Weave: every 10 INT adds +2% AMP on Wand actions (× power).</summary>
        private void ApplyArcaneWeave(Character hero, CombatEvent evt)
        {
            double power = GetPower(hero, "arcane_weave");
            if (power <= 0 || evt.Action == null) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != WeaponType.Wand) return;
            AddAmpMod(hero, Math.Floor(hero.GetEffectiveIntelligence() / 10.0) * 2.0 * power);
        }

        /// <summary>Concentrated Chaos: combo actions gain +7% AMP per strip slot filled beyond the first (× power).</summary>
        private void ApplyConcentratedChaos(Character hero, CombatEvent evt)
        {
            double power = GetPower(hero, "concentrated_chaos");
            if (power <= 0 || evt.Action?.IsComboAction != true) return;
            int filled = CountExtraFilledActionSlots(hero);
            if (filled > 0)
                AddAmpMod(hero, 7.0 * filled * power);
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
