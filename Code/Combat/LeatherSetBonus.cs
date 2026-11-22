using System;

namespace RPGGame
{
    /// <summary>
    /// Class-less Leather armor set: 3+ equipped Leather pieces grant the <c>Lucky</c> status effect
    /// (attack-roll advantage: 2d20 keep highest) on a limited number of rolls each fight, one per Leather piece worn
    /// (3-5). When they are used up Lucky is gone until the next fight. Lucky offsets early bad luck; the starter
    /// pieces' weak stats make the hero trade it away as found gear arrives (playtest 2026-11-18).
    /// New heroes always start wearing Helmet + Armor + Boots stamped Leather so Lucky is active at creation.
    /// </summary>
    public static class LeatherSetBonus
    {
        public const string MaterialName = "Leather";
        public const string LuckyStatusName = "Lucky";
        public const int LuckUnlockCount = 3;

        /// <summary>Lucky rolls spent this fight, per hero (reset by <see cref="ResetFight"/> at each encounter).</summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Character, System.Runtime.CompilerServices.StrongBox<int>> usedThisFight = new();

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

        /// <summary>Lucky rolls a fight: one per Leather piece worn once the set is formed (3-5), else 0.</summary>
        public static int RollsPerFight(Character? hero) => HasLuckyStatus(hero) ? CountEquipped(hero) : 0;

        /// <summary>Lucky rolls a fight for <paramref name="leatherPieces"/> worn Leather pieces.</summary>
        public static int RollsPerFight(int leatherPieces) => leatherPieces >= LuckUnlockCount ? leatherPieces : 0;

        /// <summary>Lucky rolls left in the current fight.</summary>
        public static int RollsLeft(Character? hero)
        {
            int per = RollsPerFight(hero);
            if (per <= 0 || hero == null)
                return 0;
            return usedThisFight.TryGetValue(hero, out var used) ? Math.Max(0, per - used.Value) : per;
        }

        /// <summary>New encounter: Lucky's rolls come back.</summary>
        public static void ResetFight(Character? hero)
        {
            if (hero != null && usedThisFight.TryGetValue(hero, out var used))
                used.Value = 0;
        }

        /// <summary>True while the formed Leather set still has a Lucky roll left this fight.</summary>
        public static bool HasLuckAdvantage(Character? hero) => RollsLeft(hero) > 0;

        /// <summary>
        /// Lucky: attack-roll advantage while a Lucky roll is left this fight. Previews leave <paramref name="consume"/>
        /// false; the resolving roll passes true and spends one.
        /// </summary>
        public static void CollectAdvantageFlags(Character? hero, ref bool advantage, ref bool disadvantage, bool consume = false)
        {
            if (!HasLuckAdvantage(hero))
                return;
            advantage = true;
            if (consume)
                usedThisFight.GetValue(hero!, _ => new System.Runtime.CompilerServices.StrongBox<int>(0)).Value++;
        }

        /// <summary>STATUS EFFECTS line for the Leather Lucky status, e.g. <c>Lucky 3/4</c> (rolls left this fight).</summary>
        public static string FormatLuckyStatusLine(Character? hero) =>
            $"{LuckyStatusName} {RollsLeft(hero)}/{RollsPerFight(hero)}";

        /// <summary>Plain-language summary of what Lucky does for <paramref name="leatherPieces"/> pieces.</summary>
        public static string Describe(int leatherPieces) =>
            $"Lucky (roll twice, keep the higher, on your first {RollsPerFight(leatherPieces)} rolls each fight)";

        /// <summary>True when <paramref name="line"/> is the Lucky status HUD row.</summary>
        public static bool IsLuckyStatusLine(string? line) =>
            line != null && (string.Equals(line, LuckyStatusName, StringComparison.Ordinal)
                || line.StartsWith(LuckyStatusName + " ", StringComparison.Ordinal));

        /// <summary>Compact HUD line, e.g. <c>Leather 3/5</c>.</summary>
        public static string FormatHudLine(int count) =>
            // Out of 5 like every material: each piece from the third adds a Lucky roll, so 5 is the full set.
            $"{MaterialName} {Math.Max(0, count)}/5";

        public static string FormatHudLine(Character? hero) =>
            FormatHudLine(CountEquipped(hero));

        /// <summary>Hover / comparison lines for the Leather Lucky set.</summary>
        public static System.Collections.Generic.IEnumerable<string> FormatStatusLines(Character? hero)
        {
            int n = CountEquipped(hero);
            yield return FormatHudLine(n);
            yield return $"{LuckUnlockCount}+ pieces worn: {LuckyStatusName} (roll twice, keep the higher) on 1 roll per Leather piece each fight";
            if (n >= LuckUnlockCount)
                yield return $"{LuckyStatusName}: {RollsLeft(hero)} of {RollsPerFight(hero)} rolls left this fight";
            else
                yield return $"{LuckyStatusName}: needs {LuckUnlockCount} Leather pieces ({n} equipped)";
        }
    }
}
