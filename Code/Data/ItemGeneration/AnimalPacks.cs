using System;
using System.Collections.Generic;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>
    /// Pack rule (Documentation/AFFIX_PASS_2026-11-15.md): stat suffixes are animals ("of the Dire Bear" = Strength).
    /// When <see cref="ItemGenerationConfig.AnimalPackPieces"/> or more equipped pieces carry the same animal family, that
    /// family's lines on every one of those pieces count × <see cref="ItemGenerationConfig.AnimalPackMultiplier"/>.
    /// Hybrid creatures (Griffin…) are their own animal: two Griffins form a pack, a Griffin and a Bear do not.
    /// </summary>
    public static class AnimalPacks
    {
        /// <summary>True for a rolled stat suffix line (an animal).</summary>
        public static bool IsAnimalLine(StatBonus? bonus) =>
            bonus?.ModLine is { } line
            && string.Equals(line.Pool, ModPools.Stat, StringComparison.OrdinalIgnoreCase)
            && string.Equals(line.Affix, "suffix", StringComparison.OrdinalIgnoreCase)
            && string.Equals(line.SuffixKind, SuffixKinds.Uncommon, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(line.Family);

        /// <summary>Pieces carrying each animal family (a piece counts once per family).</summary>
        public static Dictionary<string, int> Count(IEnumerable<Item?>? equipped)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (equipped == null)
                return counts;
            foreach (var item in equipped)
            {
                if (item?.StatBonuses == null)
                    continue;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var b in item.StatBonuses)
                    if (IsAnimalLine(b) && seen.Add(b.ModLine!.Family))
                        counts[b.ModLine.Family] = counts.GetValueOrDefault(b.ModLine.Family) + 1;
            }
            return counts;
        }

        /// <summary>The multiplier for one line given the equipped pack counts (1 when it is not in a pack).</summary>
        public static double Multiplier(StatBonus? bonus, IReadOnlyDictionary<string, int> packs)
        {
            if (!IsAnimalLine(bonus))
                return 1.0;
            var cfg = Config();
            return packs.TryGetValue(bonus!.ModLine!.Family, out int n) && n >= cfg.AnimalPackPieces
                ? cfg.AnimalPackMultiplier
                : 1.0;
        }

        /// <summary>Animal families that currently form a pack, with their piece counts.</summary>
        public static IEnumerable<(string Family, int Pieces)> Active(IEnumerable<Item?>? equipped)
        {
            var cfg = Config();
            foreach (var kv in Count(equipped))
                if (kv.Value >= cfg.AnimalPackPieces)
                    yield return (kv.Key, kv.Value);
        }

        private static ItemGenerationConfig Config()
        {
            var cfg = GameConfiguration.Instance?.LootSystem?.ItemGeneration ?? new ItemGenerationConfig();
            return cfg;
        }
    }
}
