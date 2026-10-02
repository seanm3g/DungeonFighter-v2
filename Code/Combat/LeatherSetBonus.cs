using System;

namespace RPGGame
{
    /// <summary>
    /// Class-less Leather armor set: 3+ equipped Leather pieces grant the standing <c>Lucky</c> status effect
    /// (attack-roll advantage: 2d20 keep highest). Shown under STATUS EFFECTS; lost when the set drops below 3.
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
                yield return $"{LuckyStatusName}: active";
            else
                yield return $"{LuckyStatusName}: needs {LuckUnlockCount} Leather pieces ({n} equipped)";
        }
    }
}
