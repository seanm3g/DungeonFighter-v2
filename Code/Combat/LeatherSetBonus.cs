using System;

namespace RPGGame
{
    /// <summary>
    /// Class-less Leather armor set: 3+ equipped Leather pieces grant the standing <c>Lucky</c> status effect
    /// (attack-roll advantage: 2d20 keep highest). STATUS EFFECTS shows <c>Lucky: N</c> with remaining
    /// miss→advantage (naiveté) charges when N &gt; 0. Lost when the set drops below 3.
    /// New heroes always start wearing Helmet + Armor + Boots stamped Leather so Lucky is active at creation.
    /// </summary>
    public static class LeatherSetBonus
    {
        public const string MaterialName = "Leather";
        public const string LuckyStatusName = "Lucky";
        public const int LuckUnlockCount = 3;

        public static int CountEquipped(Character? hero) =>
            MaterialSetController.CountEquipped(hero, MaterialName);

        /// <summary>
        /// True when the Leather set grants the Lucky status effect (3+ Leather pieces).
        /// </summary>
        public static bool HasLuckyStatus(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return false;
            return CountEquipped(hero) >= LuckUnlockCount;
        }

        /// <summary>Alias for <see cref="HasLuckyStatus"/> — Lucky is standing attack-roll advantage.</summary>
        public static bool HasLuckAdvantage(Character? hero) => HasLuckyStatus(hero);

        /// <summary>
        /// WHILE_EQUIPPED Lucky status: standing attack-roll advantage. Call alongside other multi-dice collectors.
        /// </summary>
        public static void CollectAdvantageFlags(Character? hero, ref bool advantage, ref bool disadvantage)
        {
            if (HasLuckyStatus(hero))
                advantage = true;
        }

        /// <summary>
        /// STATUS EFFECTS line: <c>Lucky: N</c> when miss→advantage (naiveté) charges remain, else <c>Lucky</c>.
        /// </summary>
        public static string FormatLuckyStatusLine(Character? hero)
        {
            if (hero == null || !HasLuckyStatus(hero))
                return LuckyStatusName;

            int points = NaiveteBalanceHelper.GetDisplayNaivete(hero);
            return points > 0 ? $"{LuckyStatusName}: {points}" : LuckyStatusName;
        }

        /// <summary>True when <paramref name="line"/> is the Lucky status HUD row (with or without a point count).</summary>
        public static bool IsLuckyStatusLine(string? line)
        {
            if (string.IsNullOrEmpty(line))
                return false;
            if (string.Equals(line, LuckyStatusName, StringComparison.Ordinal))
                return true;
            return line.StartsWith(LuckyStatusName + ":", StringComparison.Ordinal)
                || line.StartsWith(LuckyStatusName + " ", StringComparison.Ordinal);
        }

        /// <summary>Compact HUD line, e.g. <c>Leather 3/3</c>.</summary>
        public static string FormatHudLine(int count) =>
            $"{MaterialName} {Math.Max(0, count)}/{LuckUnlockCount}";

        public static string FormatHudLine(Character? hero) =>
            FormatHudLine(CountEquipped(hero));

        /// <summary>Hover / comparison lines for the Leather Lucky set.</summary>
        public static System.Collections.Generic.IEnumerable<string> FormatStatusLines(Character? hero)
        {
            int n = CountEquipped(hero);
            yield return FormatHudLine(n);
            yield return $"WHILE_EQUIPPED {LuckUnlockCount}+ : {LuckyStatusName} status (2d20 keep highest)";
            if (n >= LuckUnlockCount)
            {
                int points = hero != null ? NaiveteBalanceHelper.GetDisplayNaivete(hero) : 0;
                if (points > 0)
                    yield return $"{LuckyStatusName}: {points} miss→advantage charge{(points != 1 ? "s" : "")}";
                else
                    yield return $"{LuckyStatusName}: active";
            }
            else
                yield return $"{LuckyStatusName}: needs {LuckUnlockCount} Leather pieces ({n} equipped)";
        }
    }
}
