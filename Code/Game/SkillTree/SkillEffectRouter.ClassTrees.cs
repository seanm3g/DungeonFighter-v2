using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;

namespace RPGGame
{
    /// <summary>
    /// Class-tree effects for the level-100 trees (Documentation/SKILL_TREE_DESIGN.md): Kinship (class material
    /// pieces raise the class stat), Synthesis feed (class stat raises material resource mints), convert masteries,
    /// Arcane Weave, capstones, and the Mastery nodes. Magnitudes are per rank × node power.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        /// <summary>Kinship: +1 class stat per equipped class-material piece, per rank.</summary>
        private static readonly (string Effect, string Stat, string ClassTag)[] KinshipRules =
        {
            ("tribal_kinship", "STR", ClassTags.Barbarian),
            ("martial_kinship", "AGI", ClassTags.Warrior),
            ("shadow_kinship", "TEC", ClassTags.Rogue),
            ("arcane_kinship", "INT", ClassTags.Wizard)
        };

        /// <summary>Synthesis feed: every 20 class stat adds +1 to each mint of that class's material resources, per rank.</summary>
        private static readonly (string Effect, string Stat, string ClassTag)[] SynthesisRules =
        {
            ("blood_synthesis", "STR", ClassTags.Barbarian),
            ("drill_synthesis", "AGI", ClassTags.Warrior),
            ("edge_synthesis", "TEC", ClassTags.Rogue),
            ("weave_synthesis", "INT", ClassTags.Wizard)
        };

        public const int SynthesisStatPerMint = 20;

        private enum ConvertRider { HealPctMaxHp, DamageVsLowHp, Fortify, Speed, Slow, Focus, Vulnerable, Expose, Damage, Amp }

        /// <summary>Convert masteries: the convert action's second effect, per unit of its material resource.</summary>
        private static readonly (string Action, string Effect, string Keyword, ConvertRider Rider, double PerUnit)[] ConvertMasteries =
        {
            ("BONE WRATH", "wrath_unbound", "RAGE", ConvertRider.HealPctMaxHp, 1.0),
            ("IRON CULL", "iron_cull", "CRISIS", ConvertRider.DamageVsLowHp, 4.0),
            ("STEEL CREST", "steel_crest", "TRIUMPH", ConvertRider.Fortify, 0.5),
            ("DRUM CUT", "war_drum", "BEAT", ConvertRider.Speed, 2.0),
            ("GOLD ANCHOR", "gold_anchor", "DRAG", ConvertRider.Slow, 0.03),
            ("MITHRIL FLOW", "mithril_flow", "STREAK", ConvertRider.Focus, 0.34),
            ("GLASS NICK", "glass_shatter", "GRAZE", ConvertRider.Vulnerable, 0.34),
            ("UMBRA", "umbral_step", "SWING", ConvertRider.Expose, 0.34),
            ("HEADHUNT", "trophy_hunter", "TROPHY", ConvertRider.Damage, 3.0),
            ("WILLOW LASH", "willow_lash", "CHAIN", ConvertRider.Amp, 3.0),
            ("REFRACT", "refraction", "LENS", ConvertRider.Focus, 0.5),
            ("RESONATE", "resonance", "CONTACT", ConvertRider.Damage, 2.0)
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
                    total += (int)Math.Round(CountClassMaterialPieces(c, classTag) * power);
            }
            return total;
        }

        /// <summary>Equipped pieces made of one of the class's materials.</summary>
        public static int CountClassMaterialPieces(Character c, string classTag)
        {
            var materials = ClassTags.MaterialsOf(classTag);
            return new Item?[] { c.Weapon, c.Head, c.Body, c.Legs, c.Feet }
                .Count(i => i != null && !string.IsNullOrWhiteSpace(i.Material)
                            && materials.Any(m => string.Equals(m, i.Material, StringComparison.OrdinalIgnoreCase)));
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

        /// <summary>Perfect Aim: every 25 TEC also lowers the crit threshold by 1 (× power).</summary>
        public static int PerfectAimCritSteps(Character? c)
        {
            if (c == null || c is Enemy) return 0;
            double power = SkillRanks.GetPower(c, "perfect_aim");
            return power > 0 ? (int)Math.Floor(c.GetEffectiveTechnique() / 25.0 * power) : 0;
        }

        /// <summary>Archmage: multiplier on INT's status-duration bonus (1 = unchanged).</summary>
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
            string? name = evt.Action?.Name;
            if (string.IsNullOrWhiteSpace(name)) return;
            foreach (var m in ConvertMasteries)
            {
                if (!string.Equals(m.Action, name, StringComparison.OrdinalIgnoreCase)) continue;
                double power = GetPower(hero, m.Effect);
                int units = MaterialSetController.GetKeyword(hero, m.Keyword);
                if (power <= 0 || units <= 0) return;
                double amount = units * m.PerUnit * power;
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
                return;
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

        /// <summary>Concentrated Chaos: combo actions gain +7% AMP per empty strip slot (× power).</summary>
        private void ApplyConcentratedChaos(Character hero, CombatEvent evt)
        {
            double power = GetPower(hero, "concentrated_chaos");
            if (power <= 0 || evt.Action?.IsComboAction != true) return;
            int empty = CountEmptyActionSlots(hero);
            if (empty > 0)
                AddAmpMod(hero, 7.0 * empty * power);
        }

        /// <summary>Dagger Mastery: multihit Dagger actions add +1 Bleed per extra hit (× power).</summary>
        private void ApplyDaggerMastery(Character hero, CombatEvent evt)
        {
            double power = GetPower(hero, "dagger_mastery");
            if (power <= 0 || evt.Target == null || evt.Action == null) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != WeaponType.Dagger) return;
            int extraHits = Math.Max(0, evt.Action.Advanced.MultiHitCount - 1) + (int)hero.Effects.ConsumedMultiHitMod;
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
