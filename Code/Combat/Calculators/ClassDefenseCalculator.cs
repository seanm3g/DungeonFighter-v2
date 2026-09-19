using System;
using System.Collections.Generic;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Result of standing BLOCK + class DEFENSE applied to one incoming hit on the hero.
    /// </summary>
    public readonly struct HeroMitigationResult
    {
        public double BlockPercent { get; init; }
        public int GritIgnored { get; init; }
        public int ShieldAbsorbed { get; init; }
        public int TempoPct { get; init; }
        public int CounterPct { get; init; }
        public int Remaining { get; init; }
        public int ReducedAmount { get; init; }
    }

    /// <summary>
    /// Standing BLOCK % plus class DEFENSE: Tempo (Sword), Counter (Dagger), Shield (Wand), Grit (Mace).
    /// Does not read material sets. Never mints material keywords from armor.
    /// </summary>
    public static class ClassDefenseCalculator
    {
        public const double DefenseScaleK = 20.0;
        public const int WarriorTempoCap = 25;
        public const int RogueCounterCap = 30;
        public const int BarbarianGritCap = 25;
        public const int WizardShieldPerDefense = 2;

        public static WeaponType? GetDefenseWeaponType(Character? hero)
        {
            if (hero?.Weapon is WeaponItem weapon)
                return weapon.WeaponType;
            return null;
        }

        public static double GetBlockPercent(Character? hero) =>
            StandingBlock.ClampFraction(hero?.StandingBlockPercent ?? 0);

        /// <summary>Shared diminishing scale: round(cap * r / (r + K)).</summary>
        public static int ScaleFromDefense(int defenseRating, int cap)
        {
            int r = Math.Max(0, defenseRating);
            if (r <= 0 || cap <= 0)
                return 0;
            return (int)Math.Round(cap * r / (r + DefenseScaleK), MidpointRounding.AwayFromZero);
        }

        public static int GetWarriorTempoSpeedPct(int defenseRating) =>
            ScaleFromDefense(defenseRating, WarriorTempoCap);

        public static int GetRogueCounterDamagePct(int defenseRating) =>
            ScaleFromDefense(defenseRating, RogueCounterCap);

        public static int GetBarbarianGrit(int defenseRating) =>
            ScaleFromDefense(defenseRating, BarbarianGritCap);

        public static int GetWizardShieldPool(int defenseRating) =>
            Math.Max(0, defenseRating * WizardShieldPerDefense);

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
            pool = Math.Max(0, (int)Math.Round(pool * CharmBonusController.GetClassDefenseMultiplier(hero)));
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

        public static void MintWarriorTempoFromDefense(Character hero)
        {
            var weapon = GetDefenseWeaponType(hero);
            if (weapon != null && weapon != WeaponType.Sword)
                return;
            int pct = GetWarriorTempoSpeedPct(Math.Max(0, hero.GetMaxArmor()));
            hero.Effects.PendingDefenseTempoSpeedPct = pct;
        }

        public static void MintRogueCounterFromDefense(Character hero)
        {
            if (GetDefenseWeaponType(hero) != WeaponType.Dagger)
                return;
            int pct = GetRogueCounterDamagePct(Math.Max(0, hero.GetMaxArmor()));
            hero.Effects.PendingDefenseCounterDamagePct = pct;
        }

        /// <summary>
        /// Hero-only: standing BLOCK %, then Grit / Shield, then mint Tempo / Counter.
        /// Pierce skips Block, Grit, and Shield; still mints Counter/Tempo.
        /// </summary>
        public static HeroMitigationResult ApplyIncoming(Character hero, int incoming, bool pierce)
        {
            int start = Math.Max(0, incoming);
            int remaining = start;
            var weapon = GetDefenseWeaponType(hero);
            int rating = Math.Max(0, hero.GetMaxArmor());
            double classMult = CharmBonusController.GetClassDefenseMultiplier(hero);

            double blockPct = pierce ? 0.0 : GetBlockPercent(hero);
            if (blockPct > 0)
                remaining = RoundMul(remaining, 1.0 - blockPct);

            int grit = 0;
            if (!pierce && weapon == WeaponType.Mace)
            {
                grit = Math.Max(0, (int)Math.Round(GetBarbarianGrit(rating) * classMult));
                if (grit > 0)
                    remaining = Math.Max(0, remaining - grit);
            }

            int absorbed = 0;
            if (!pierce && weapon == WeaponType.Wand)
            {
                int before = remaining;
                remaining = AbsorbWizardShield(hero, remaining);
                absorbed = before - remaining;
            }

            int tempoPct = 0;
            int counterPct = 0;
            if (weapon == WeaponType.Dagger)
            {
                counterPct = Math.Max(0, (int)Math.Round(GetRogueCounterDamagePct(rating) * classMult));
                hero.Effects.PendingDefenseCounterDamagePct = counterPct;
            }
            else if (weapon == WeaponType.Sword || weapon == null)
            {
                tempoPct = Math.Max(0, (int)Math.Round(GetWarriorTempoSpeedPct(rating) * classMult));
                hero.Effects.PendingDefenseTempoSpeedPct = tempoPct;
            }

            return new HeroMitigationResult
            {
                BlockPercent = blockPct,
                GritIgnored = grit,
                ShieldAbsorbed = absorbed,
                TempoPct = tempoPct,
                CounterPct = counterPct,
                Remaining = remaining,
                ReducedAmount = start - remaining
            };
        }

        /// <summary>Sequence HUD DEFENSE beats: BLOCK %, class layer.</summary>
        public static List<string> FormatHudLines(Character hero, bool pierce)
        {
            var lines = new List<string>();
            double blockPct = pierce ? 0.0 : GetBlockPercent(hero);
            lines.Add($"BLOCK {(int)Math.Round(blockPct * 100.0, MidpointRounding.AwayFromZero)}%");

            var weapon = GetDefenseWeaponType(hero);
            int rating = Math.Max(0, hero.GetMaxArmor());

            switch (weapon)
            {
                case WeaponType.Dagger:
                    lines.Add($"COUNTER +{GetRogueCounterDamagePct(rating)}%");
                    break;
                case WeaponType.Wand:
                    if (pierce)
                        lines.Add("pierce");
                    else
                        lines.Add($"shield {hero.EnergyShieldCurrent}/{Math.Max(hero.EnergyShieldMax, GetWizardShieldPool(rating))}");
                    break;
                case WeaponType.Mace:
                    if (pierce)
                        lines.Add("pierce");
                    else
                        lines.Add($"GRIT {GetBarbarianGrit(rating)}");
                    break;
                default:
                    lines.Add($"TEMPO +{GetWarriorTempoSpeedPct(rating)}%");
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
