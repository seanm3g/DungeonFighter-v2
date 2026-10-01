using System;
using System.Collections.Generic;
using System.Globalization;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Result of WoW-style Defense DR applied to one incoming hit.
    /// Class Tempo/Counter/Shield/Grit fields stay 0 on the live path (unused for this test).
    /// </summary>
    public readonly struct HeroMitigationResult
    {
        public double BlockPercent { get; init; }
        public double ActionMult { get; init; }
        public double EffectiveDefense { get; init; }
        public double K { get; init; }
        public double DrPercent { get; init; }
        public int Rating { get; init; }
        public int GritIgnored { get; init; }
        public int ShieldAbsorbed { get; init; }
        public int TempoPct { get; init; }
        public int CounterPct { get; init; }
        public int Remaining { get; init; }
        public int ReducedAmount { get; init; }
    }

    /// <summary>
    /// Defense DR for heroes and enemies: remaining = incoming × (1 − effective / (effective + K)).
    /// effective = rating × actionMult. Tick 0 of an incoming multi-hit uses standing BLOCK;
    /// later ticks use 1.0×. A live hit then resets standing to 100%. Class Tempo/Counter/Shield/Grit
    /// helpers remain but are not applied here.
    /// </summary>
    public static class ClassDefenseCalculator
    {
        public const double DefenseScaleK = 20.0;
        public const int WarriorTempoCap = 25;
        public const int RogueCounterCap = 30;
        public const int BarbarianGritCap = 25;
        public const int WizardShieldPerDefense = 2;
        public const double DefaultArmorReductionK = 100.0;

        public static WeaponType? GetDefenseWeaponType(Character? hero)
        {
            if (hero?.Weapon is WeaponItem weapon)
                return weapon.WeaponType;
            return null;
        }

        public static double GetBlockPercent(Character? hero) =>
            StandingBlock.ClampMultiplier(hero?.StandingBlockPercent ?? StandingBlock.DefaultBlockPercent);

        /// <summary>Base Defense rating (hero level + gear, or enemy Armor, plus shred), before stance.</summary>
        public static int GetBaseDefenseRating(Character? defender) =>
            defender == null ? 0 : Math.Max(0, defender.GetMaxArmor());

        /// <summary>Live panel Defense: base rating × standing BLOCK (100% = base).</summary>
        public static int GetDisplayedDefense(Character? defender)
        {
            int rating = GetBaseDefenseRating(defender);
            double mult = GetBlockPercent(defender);
            return (int)Math.Round(rating * mult, MidpointRounding.AwayFromZero);
        }

        /// <summary>K in DR = effective / (effective + K). From Combat.ArmorReductionFactor (default 100).</summary>
        public static double GetK()
        {
            double k = GameConfiguration.Instance?.Combat?.ArmorReductionFactor ?? DefaultArmorReductionK;
            if (k <= 0)
                return DefaultArmorReductionK;
            return k;
        }

        public static double ComputeDr(double effectiveDefense, double k, bool pierce)
        {
            if (pierce || effectiveDefense <= 0 || k <= 0)
                return 0;
            return effectiveDefense / (effectiveDefense + k);
        }

        public static double ResolveActionMult(Character hero, int mitigationHitIndex)
        {
            if (mitigationHitIndex > 0)
                return StandingBlock.DefaultBlockPercent;
            return GetBlockPercent(hero);
        }

        /// <summary>Shared diminishing scale: round(cap * r / (r + K)). Unused by live DR.</summary>
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

        /// <summary>Absorbs damage into the wizard shield. Returns remaining damage. Unused by live DR.</summary>
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
        /// WoW DR for a Character (hero or enemy). Pierce skips DR. Does not apply Grit/Shield or mint Tempo/Counter.
        /// <paramref name="mitigationHitIndex"/> 0 uses standing BLOCK; later ticks use 1.0×.
        /// Does not consume standing BLOCK (live damage path calls <see cref="StandingBlock.ConsumeAfterHit"/>).
        /// </summary>
        public static HeroMitigationResult ApplyIncoming(Character hero, int incoming, bool pierce, int mitigationHitIndex = 0)
        {
            int start = Math.Max(0, incoming);
            int rating = GetBaseDefenseRating(hero);
            double k = GetK();
            double actionMult = pierce ? 0.0 : ResolveActionMult(hero, mitigationHitIndex);
            double effective = pierce ? 0.0 : rating * actionMult;
            double dr = ComputeDr(effective, k, pierce);
            int remaining = pierce ? start : RoundMul(start, 1.0 - dr);

            return new HeroMitigationResult
            {
                BlockPercent = pierce ? 0.0 : GetBlockPercent(hero),
                ActionMult = actionMult,
                EffectiveDefense = effective,
                K = k,
                DrPercent = dr,
                Rating = rating,
                Remaining = remaining,
                ReducedAmount = start - remaining
            };
        }

        /// <summary>Sequence HUD DEFENSE beats: BLOCK %, rating × stance, DR %.</summary>
        public static List<string> FormatHudLines(Character hero, bool pierce)
        {
            var mit = ApplyIncoming(hero, 100, pierce, mitigationHitIndex: 0);
            var lines = new List<string>
            {
                $"BLOCK {(int)Math.Round(mit.BlockPercent * 100.0, MidpointRounding.AwayFromZero)}%"
            };

            if (pierce)
            {
                lines.Add("pierce");
                lines.Add("DR 0%");
                return lines;
            }

            int stancePts = (int)Math.Round(mit.ActionMult * 100.0, MidpointRounding.AwayFromZero);
            int effPts = (int)Math.Round(mit.EffectiveDefense, MidpointRounding.AwayFromZero);
            int kPts = (int)Math.Round(mit.K, MidpointRounding.AwayFromZero);
            int drPts = (int)Math.Round(mit.DrPercent * 100.0, MidpointRounding.AwayFromZero);
            lines.Add($"def {mit.Rating} × {stancePts}% = {effPts}");
            lines.Add($"{effPts}/({effPts}+{kPts})={drPts}%");
            return lines;
        }

        /// <summary>Combat-log footer: damage reduction percent only.</summary>
        public static string FormatCombatFooter(Character hero, bool pierce)
        {
            var mit = ApplyIncoming(hero, 100, pierce, mitigationHitIndex: 0);
            int drPts = (int)Math.Round(mit.DrPercent * 100.0, MidpointRounding.AwayFromZero);
            return $"reduction: {drPts}%";
        }

        public static string FormatDrLine(Character hero)
        {
            var mit = ApplyIncoming(hero, 100, pierce: false, mitigationHitIndex: 0);
            int drPts = (int)Math.Round(mit.DrPercent * 100.0, MidpointRounding.AwayFromZero);
            return $"DR {drPts}%";
        }

        public static string FormatKInvariant(double k) =>
            k.ToString("0.##", CultureInfo.InvariantCulture);

        private static int RoundMul(int value, double factor) =>
            Math.Max(0, (int)Math.Round(value * factor, MidpointRounding.AwayFromZero));
    }
}
