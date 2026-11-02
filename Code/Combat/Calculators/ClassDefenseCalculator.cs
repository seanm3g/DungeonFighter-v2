using System;
using System.Collections.Generic;
using System.Globalization;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Result of WoW-style Defense DR applied to one incoming hit.
    /// </summary>
    public readonly struct HeroMitigationResult
    {
        public double BlockPercent { get; init; }
        public double ActionMult { get; init; }
        public double EffectiveDefense { get; init; }
        public double K { get; init; }
        public double DrPercent { get; init; }
        public int Rating { get; init; }
        public int Remaining { get; init; }
        public int ReducedAmount { get; init; }
    }

    /// <summary>
    /// Defense DR for heroes and enemies: remaining = incoming × (1 − effective / (effective + K)).
    /// effective = rating × actionMult. Tick 0 of an incoming multi-hit uses standing BLOCK;
    /// later ticks use 1.0×. A live hit then resets standing to 100%.
    /// </summary>
    public static class ClassDefenseCalculator
    {
        public const double DefaultArmorReductionK = 100.0;

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

        /// <summary>
        /// DR = effective / (|effective| + K). Negative Defense (Expose) gives negative DR, i.e. extra damage, capped at +50%.
        /// </summary>
        public static double ComputeDr(double effectiveDefense, double k, bool pierce)
        {
            if (pierce || effectiveDefense == 0 || k <= 0)
                return 0;
            return Math.Max(-0.5, effectiveDefense / (Math.Abs(effectiveDefense) + k));
        }

        public static double ResolveActionMult(Character hero, int mitigationHitIndex)
        {
            if (mitigationHitIndex > 0)
                return StandingBlock.DefaultBlockPercent;
            return GetBlockPercent(hero);
        }

        /// <summary>
        /// WoW DR for a Character (hero or enemy). Pierce skips DR.
        /// <paramref name="mitigationHitIndex"/> 0 uses standing BLOCK; later ticks use 1.0×.
        /// Does not consume standing BLOCK (live damage path calls <see cref="StandingBlock.ConsumeAfterHit"/>).
        /// </summary>
        public static HeroMitigationResult ApplyIncoming(Character hero, int incoming, bool pierce, int mitigationHitIndex = 0)
        {
            int start = Math.Max(0, incoming);
            int rating = GetBaseDefenseRating(hero);
            double k = GetK();
            double actionMult = pierce ? 0.0 : ResolveActionMult(hero, mitigationHitIndex);
            double statusDefense = StatusCombatModifiers.DefenseBonus(hero, k);
            // Pierce ignores Defense the holder has, not the weakness Expose gives.
            double effective = pierce ? Math.Min(0.0, statusDefense) : rating * actionMult + statusDefense;
            double dr = ComputeDr(effective, k, pierce: false);
            int remaining = RoundMul(start, 1.0 - dr);

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

        /// <summary>Sequence HUD DEFENSE: damage reduction percent only (e.g. <c>12%</c>).</summary>
        public static List<string> FormatHudLines(Character hero, bool pierce)
        {
            var mit = ApplyIncoming(hero, 100, pierce, mitigationHitIndex: 0);
            int drPts = (int)Math.Round(mit.DrPercent * 100.0, MidpointRounding.AwayFromZero);
            if (pierce)
                return new List<string> { "pierce", "0%" };
            return new List<string> { $"{drPts}%" };
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
