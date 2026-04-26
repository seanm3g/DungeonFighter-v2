using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RPGGame
{
    public sealed class ItemGenerationBatchStatistics
    {
        public int TotalCount { get; }
        public int UniqueCount { get; }
        public int DuplicateCount { get; }
        public IReadOnlyList<TierRarityCount> TierRarityCounts { get; }

        public ItemGenerationBatchStatistics(
            int totalCount,
            int uniqueCount,
            int duplicateCount,
            IReadOnlyList<TierRarityCount> tierRarityCounts)
        {
            TotalCount = Math.Max(0, totalCount);
            UniqueCount = Math.Clamp(uniqueCount, 0, TotalCount);
            DuplicateCount = Math.Clamp(duplicateCount, 0, TotalCount);
            TierRarityCounts = tierRarityCounts ?? Array.Empty<TierRarityCount>();
        }

        public static ItemGenerationBatchStatistics Compute(IReadOnlyList<ItemGeneratedRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return new ItemGenerationBatchStatistics(0, 0, 0, Array.Empty<TierRarityCount>());

            var fingerprintCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var tierRarity = new Dictionary<(int Tier, string Rarity), int>();

            foreach (var r in rows)
            {
                if (r?.Item == null)
                    continue;

                string rarity = (r.Rarity ?? r.Item.Rarity ?? "Common").Trim();
                if (rarity.Length == 0) rarity = "Common";
                var key = (Tier: Math.Max(1, r.Tier), Rarity: rarity);
                tierRarity[key] = tierRarity.TryGetValue(key, out int cur) ? cur + 1 : 1;

                string fp = BuildFingerprint(r.Item);
                fingerprintCounts[fp] = fingerprintCounts.TryGetValue(fp, out int seen) ? seen + 1 : 1;
            }

            int total = rows.Count;
            int unique = fingerprintCounts.Count;
            int duplicates = Math.Max(0, total - unique);

            var counts = tierRarity
                .OrderBy(k => k.Key.Tier)
                .ThenBy(k => ItemGenerationLabServiceSortHelpers.IndexOfRarityForDisplay(k.Key.Rarity))
                .ThenBy(k => k.Key.Rarity, StringComparer.OrdinalIgnoreCase)
                .Select(k => new TierRarityCount(k.Key.Tier, k.Key.Rarity, k.Value))
                .ToList();

            return new ItemGenerationBatchStatistics(total, unique, duplicates, counts);
        }

        public string ToDisplayText(int maxLines = 18)
        {
            if (TotalCount <= 0)
                return "(no generated items)";

            var lines = new List<string>
            {
                $"Total: {TotalCount}",
                $"Duplicates: {DuplicateCount} ({FormatPercent(DuplicateCount, TotalCount)})",
                $"Unique: {UniqueCount}"
            };

            if (TierRarityCounts.Count > 0)
            {
                lines.Add("");
                lines.Add("Tier × rarity:");
                foreach (var tr in TierRarityCounts.Take(Math.Max(0, maxLines)))
                    lines.Add($"T{tr.Tier} {tr.Rarity}: {tr.Count}");

                int remaining = TierRarityCounts.Count - maxLines;
                if (remaining > 0)
                    lines.Add($"+{remaining} more…");
            }

            return string.Join("\n", lines);
        }

        private static string FormatPercent(int numerator, int denominator)
        {
            if (denominator <= 0) return "0%";
            double pct = (double)numerator / denominator * 100.0;
            return pct.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static string BuildFingerprint(Item item)
        {
            // Include rolled values so an item only counts as a duplicate when it is effectively identical.
            // This is intentionally "UI-facing identity" rather than reference identity.
            string mods = item.Modifications == null || item.Modifications.Count == 0
                ? ""
                : string.Join("|", item.Modifications
                    .OrderBy(m => m.PrefixCategory ?? "", StringComparer.OrdinalIgnoreCase)
                    .ThenBy(m => m.Name ?? "", StringComparer.OrdinalIgnoreCase)
                    .Select(m => $"{(m.PrefixCategory ?? "").Trim()}:{(m.Name ?? "").Trim()}:{m.RolledValue:0.####}"));

            string stats = item.StatBonuses == null || item.StatBonuses.Count == 0
                ? ""
                : string.Join("|", item.StatBonuses
                    .OrderBy(s => s.StatType ?? "", StringComparer.OrdinalIgnoreCase)
                    .ThenBy(s => s.Name ?? "", StringComparer.OrdinalIgnoreCase)
                    .Select(s => $"{(s.StatType ?? "").Trim()}:{(s.Name ?? "").Trim()}:{s.Value:0.####}"));

            string actions = item.ActionBonuses == null || item.ActionBonuses.Count == 0
                ? ""
                : string.Join("|", item.ActionBonuses
                    .OrderBy(a => a.Name ?? "", StringComparer.OrdinalIgnoreCase)
                    .Select(a => (a.Name ?? "").Trim()));

            string tags = item.Tags == null || item.Tags.Count == 0
                ? ""
                : string.Join("|", item.Tags.OrderBy(t => t ?? "", StringComparer.OrdinalIgnoreCase).Select(t => (t ?? "").Trim()));

            string core = $"{item.Type}|{item.WeaponType}|T{item.Tier}|{(item.Rarity ?? "").Trim()}|{(item.Name ?? "").Trim()}|{item.Level}";

            string primary = item is WeaponItem w
                ? $"dmg:{w.GetTotalDamage()}|spd:{w.GetTotalAttackSpeed():0.####}"
                : item is HeadItem h
                    ? $"arm:{h.GetTotalArmor()}"
                    : item is ChestItem c
                        ? $"arm:{c.GetTotalArmor()}"
                        : item is FeetItem f
                            ? $"arm:{f.GetTotalArmor()}"
                            : "prim:0";

            return $"{core}|{primary}|mods:{mods}|stats:{stats}|actions:{actions}|tags:{tags}";
        }
    }

    public readonly record struct TierRarityCount(int Tier, string Rarity, int Count);

    internal static class ItemGenerationLabServiceSortHelpers
    {
        private static readonly string[] Order = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        public static int IndexOfRarityForDisplay(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 0;
            int idx = Array.FindIndex(Order, r => string.Equals(r, rarity.Trim(), StringComparison.OrdinalIgnoreCase));
            return idx < 0 ? Order.Length : idx;
        }
    }
}

