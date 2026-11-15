using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// <c>itemAffixByRarity</c> in a balance patch: optional rarity roll weights. Line counts per rarity live in
    /// <c>RarityTable.json</c> (MinLines / MaxLines / Explode) and mod tiers in <c>ItemMods.json</c>
    /// (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md). Older patches' perRarity / perItemType / affixRolls keys
    /// are no longer read.
    /// </summary>
    public sealed class ItemAffixByRaritySettings
    {
        public string Description { get; set; } = "Optional rarity roll weights that replace RarityTable.json weights.";

        /// <summary>
        /// Optional base rarity roll weights (rarity name → weight) that replace <c>RarityTable.json</c> weights, so a balance
        /// patch can carry its own rarity curve. Rarities missing here keep their table weight. Empty = table weights.
        /// </summary>
        public Dictionary<string, double> RarityWeights { get; set; } = new();

        /// <summary>Weight for <paramref name="row"/>: <see cref="RarityWeights"/> override when present, else the table weight.</summary>
        public double ResolveRarityWeight(RarityData row)
        {
            if (RarityWeights != null && row?.Name != null)
            {
                foreach (var kv in RarityWeights)
                {
                    if (kv.Key.Equals(row.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                        return Math.Max(0, kv.Value);
                }
            }

            return row?.Weight ?? 0;
        }
    }
}
