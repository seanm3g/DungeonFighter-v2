using System;
using System.Collections.Generic;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Result of standing BLOCK + class DEFENSE applied to one incoming hit on the hero.
    /// </summary>
    public readonly struct HeroMitigationResult
    {
        public bool Dodged { get; init; }
        public double BlockPercent { get; init; }
        public double WarriorArmorPercent { get; init; }
        public int ShieldAbsorbed { get; init; }
        public int RageMinted { get; init; }
        public int Remaining { get; init; }
        public int ReducedAmount { get; init; }
    }

    /// <summary>
    /// Standing BLOCK % (dominant when &gt; 0) plus class interpretation of the DEFENSE rating.
    /// Does not read material sets.
    /// </summary>
    public static class ClassDefenseCalculator
    {
        public const double WarriorArmorPercentCap = 0.25;
        public const double WarriorArmorCurveK = 20.0;
        public const double RogueDodgePerPoint = 0.015;
        public const double RogueDodgeCap = 0.35;
        public const int WizardShieldPerDefense = 2;
        public const int BarbarianRagePerDefense = 8;

        public static WeaponType? GetDefenseWeaponType(Character? hero)
        {
            if (hero?.Weapon is WeaponItem weapon)
                return weapon.WeaponType;
            return null;
        }

        public static double GetBlockPercent(Character? hero) =>
            StandingBlock.ClampFraction(hero?.StandingBlockPercent ?? 0);

        /// <summary>Warrior / unarmed: always-on armor % from DEFENSE rating (diminishing, cap 25%).</summary>
        public static double GetWarriorArmorPercent(int defenseRating)
        {
            int r = Math.Max(0, defenseRating);
            return WarriorArmorPercentCap * r / (r + WarriorArmorCurveK);
        }

        public static double GetRogueDodgeChance(int defenseRating)
        {
            int r = Math.Max(0, defenseRating);
            return Math.Min(RogueDodgeCap, RogueDodgePerPoint * r);
        }

        public static int GetWizardShieldPool(int defenseRating) =>
            Math.Max(0, defenseRating * WizardShieldPerDefense);

        public static int GetBarbarianRageOnTakeHit(int defenseRating) =>
            defenseRating <= 0 ? 0 : Math.Max(0, defenseRating / BarbarianRagePerDefense);

        public static void RefillWizardShield(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return;
            if (GetDefenseWeaponType(hero) != WeaponType.Wand)
            {
                hero.EnergyShieldCurrent = 0;
                hero.EnergyShieldMax = 0;
                return;
            }

            int pool = GetWizardShieldPool(Math.Max(0, hero.GetMaxArmor()));
            hero.EnergyShieldMax = pool;
            hero.EnergyShieldCurrent = pool;
        }

        /// <summary>Absorbs damage into the wizard shield. Returns remaining damage.</summary>
        public static int AbsorbWizardShield(Character hero, int damage)
        {
            if (damage <= 0 || hero.EnergyShieldCurrent <= 0)
                return damage;
            int absorbed = Math.Min(hero.EnergyShieldCurrent, damage);
            hero.EnergyShieldCurrent -= absorbed;
            return damage - absorbed;
        }

        public static void MintBarbarianRageFromDefense(Character hero)
        {
            if (GetDefenseWeaponType(hero) != WeaponType.Mace)
                return;
            int rage = GetBarbarianRageOnTakeHit(Math.Max(0, hero.GetMaxArmor()));
            if (rage > 0)
                hero.Effects.AddMaterialKeyword("RAGE", rage);
        }

        /// <summary>
        /// Hero-only mitigation: Rogue dodge (even on pierce), standing BLOCK %, Warrior DEFENSE %,
        /// Wizard shield absorb, Barbarian Rage mint. Standing 0 skips BLOCK.
        /// </summary>
        public static HeroMitigationResult ApplyIncoming(Character hero, int incoming, bool pierce)
        {
            int start = Math.Max(0, incoming);
            int remaining = start;
            var weapon = GetDefenseWeaponType(hero);
            int rating = Math.Max(0, hero.GetMaxArmor());

            if (weapon == WeaponType.Dagger && TryRogueDodge(rating))
            {
                return new HeroMitigationResult
                {
                    Dodged = true,
                    Remaining = 0,
                    ReducedAmount = start
                };
            }

            double blockPct = pierce ? 0.0 : GetBlockPercent(hero);
            if (blockPct > 0)
                remaining = RoundMul(remaining, 1.0 - blockPct);

            double warriorPct = 0.0;
            bool warriorLayer = !pierce && (weapon == WeaponType.Sword || weapon == null);
            if (warriorLayer)
            {
                warriorPct = GetWarriorArmorPercent(rating);
                if (warriorPct > 0)
                    remaining = RoundMul(remaining, 1.0 - warriorPct);
            }

            int absorbed = 0;
            if (!pierce && weapon == WeaponType.Wand)
            {
                int before = remaining;
                remaining = AbsorbWizardShield(hero, remaining);
                absorbed = before - remaining;
            }

            int rage = 0;
            if (weapon == WeaponType.Mace)
            {
                rage = GetBarbarianRageOnTakeHit(rating);
                MintBarbarianRageFromDefense(hero);
            }

            return new HeroMitigationResult
            {
                BlockPercent = blockPct,
                WarriorArmorPercent = warriorPct,
                ShieldAbsorbed = absorbed,
                RageMinted = rage,
                Remaining = remaining,
                ReducedAmount = start - remaining
            };
        }

        public static bool TryRogueDodge(int defenseRating)
        {
            double chance = GetRogueDodgeChance(defenseRating);
            if (chance <= 0)
                return false;
            int threshold = Math.Max(1, (int)Math.Round(chance * 100.0, MidpointRounding.AwayFromZero));
            int roll = Dice.RollUnforced(100);
            return roll <= threshold;
        }

        /// <summary>Sequence HUD DEFENSE beats: BLOCK %, class layer.</summary>
        public static List<string> FormatHudLines(Character hero, bool pierce)
        {
            var lines = new List<string>();
            double blockPct = pierce ? 0.0 : GetBlockPercent(hero);
            lines.Add($"BLOCK {(int)Math.Round(blockPct * 100.0, MidpointRounding.AwayFromZero)}%");

            var weapon = GetDefenseWeaponType(hero);
            int rating = Math.Max(0, hero.GetMaxArmor());
            if (pierce && weapon != WeaponType.Dagger)
            {
                lines.Add("pierce");
                return lines;
            }

            switch (weapon)
            {
                case WeaponType.Dagger:
                    lines.Add($"dodge {(int)Math.Round(GetRogueDodgeChance(rating) * 100.0, MidpointRounding.AwayFromZero)}%");
                    break;
                case WeaponType.Wand:
                    lines.Add($"shield {hero.EnergyShieldCurrent}/{Math.Max(hero.EnergyShieldMax, GetWizardShieldPool(rating))}");
                    break;
                case WeaponType.Mace:
                    lines.Add($"RAGE +{GetBarbarianRageOnTakeHit(rating)}");
                    break;
                default:
                    lines.Add($"DEFENSE {(int)Math.Round(GetWarriorArmorPercent(rating) * 100.0, MidpointRounding.AwayFromZero)}%");
                    break;
            }

            return lines;
        }

        public static string FormatCombatFooter(Character hero, bool pierce)
        {
            return string.Join(" | ", FormatHudLines(hero, pierce));
        }

        private static int RoundMul(int value, double factor) =>
            Math.Max(0, (int)Math.Round(value * factor, MidpointRounding.AwayFromZero));
    }
}
