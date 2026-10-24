using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// ITEMSTATS: generates many real loot drops (LootGenerator, guaranteed drop) per level and reports the rarity mix,
    /// affix counts, suffix/prefix frequencies and an item value estimate — the measuring stick for rarity and affix tuning.
    /// </summary>
    /// <example>DF.exe ITEMSTATS --levels 1,5,10,25,50,100 --count 3000</example>
    public static class LootStatsRunner
    {
        public static readonly string[] RarityOrder = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        public static int Run(string[] args)
        {
            FundamentalsMatrixRunner.ApplySetFlags(args);
            var levels = (TuningCliArgs.GetFlagValue(args, "--levels") ?? "1,5,10,25,50,100")
                .Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            int count = TuningCliArgs.GetIntFlag(args, "--count") ?? 2000;
            bool detail = args.Any(a => a.Equals("--detail", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== ITEMSTATS | {count} guaranteed drops per level ===");
            Console.WriteLine($"{"Level",6}" + string.Concat(RarityOrder.Select(r => $"{r,10}")) + $"{"AvgTier",9}{"Suffix",8}{"Prefix",8}{"Actions",8}{"StatSum",9}");

            var record = new List<object>();
            var suffixCounts = new Dictionary<string, int>();
            var prefixCounts = new Dictionary<string, int>();
            var suffixByRarity = new Dictionary<string, List<int>>();
            var promotionSources = new Dictionary<string, int>();
            // Gear actions by slot: "Mace" / "armor" -> class / neutral / other-class / basic counts.
            var gearActionMix = new SortedDictionary<string, Dictionary<string, int>>();
            var gearActionNames = new SortedDictionary<string, Dictionary<string, int>>();
            var slotCounts = new SortedDictionary<string, int>();

            foreach (int level in levels)
            {
                var items = new List<Item>();
                for (int i = 0; i < count; i++)
                {
                    var item = LootGenerator.GenerateLoot(level, level, null, guaranteedLoot: true);
                    if (item != null)
                        items.Add(item);
                }

                var byRarity = RarityOrder.ToDictionary(r => r,
                    r => items.Count(it => string.Equals(it.Rarity, r, StringComparison.OrdinalIgnoreCase)) / (double)Math.Max(1, items.Count));
                double avgTier = items.Count > 0 ? items.Average(i => i.Tier) : 0;
                double suffixes = items.Count > 0 ? items.Average(i => i.StatBonuses.Count) : 0;
                double prefixes = items.Count > 0 ? items.Average(i => i.Modifications.Count) : 0;
                double actions = items.Count > 0 ? items.Average(i => i.ActionBonuses.Count) : 0;
                double statSum = items.Count > 0 ? items.Average(i => i.StatBonuses.Sum(s => Math.Abs(s.Value))) : 0;

                Console.WriteLine($"{level,6}" + string.Concat(RarityOrder.Select(r => $"{byRarity[r] * 100,9:F1}%")) +
                                  $"{avgTier,9:F2}{suffixes,8:F2}{prefixes,8:F2}{actions,8:F2}{statSum,9:F1}");

                foreach (var it in items)
                {
                    slotCounts[it.Type.ToString()] = slotCounts.GetValueOrDefault(it.Type.ToString()) + 1;
                    foreach (var s in it.StatBonuses)
                    {
                        string key = string.IsNullOrEmpty(s.StatType) ? s.Name : $"{s.Name} ({s.StatType})";
                        suffixCounts[key] = suffixCounts.GetValueOrDefault(key) + 1;
                    }
                    foreach (var m in it.Modifications)
                        prefixCounts[m.Name] = prefixCounts.GetValueOrDefault(m.Name) + 1;
                    string slot = it is WeaponItem gearWeapon ? gearWeapon.WeaponType.ToString() : "armor";
                    foreach (var ab in it.ActionBonuses)
                    {
                        var tags = ActionLoader.GetActionData(ab.Name)?.Tags;
                        string kind = RPGGame.Data.GearActionPools.IsBasicAttack(tags) ? "basic"
                            : RPGGame.Data.GearActionPools.IsNeutral(tags) ? "neutral"
                            : it is WeaponItem ownWeapon && RPGGame.Data.GearActionPools.IsClassAction(tags, ownWeapon.WeaponType) ? "class"
                            : "other-class";
                        if (!gearActionMix.TryGetValue(slot, out var mix))
                            gearActionMix[slot] = mix = new Dictionary<string, int>();
                        mix[kind] = mix.GetValueOrDefault(kind) + 1;
                        if (!gearActionNames.TryGetValue(slot, out var names))
                            gearActionNames[slot] = names = new Dictionary<string, int>();
                        names[ab.Name] = names.GetValueOrDefault(ab.Name) + 1;
                    }
                    // Which affix carries the item's final rarity (rarity is raised to the highest affix rank).
                    if (Array.IndexOf(RarityOrder, it.Rarity) >= 1)
                    {
                        var carriers = it.Modifications.Where(m => string.Equals(m.ItemRank, it.Rarity, StringComparison.OrdinalIgnoreCase)).Select(m => "prefix:" + m.Name)
                            .Concat(it.StatBonuses.Where(sb => string.Equals(sb.Rarity, it.Rarity, StringComparison.OrdinalIgnoreCase)).Select(sb => "suffix:" + sb.Name))
                            .ToList();
                        string carrier = carriers.Count == 0 ? "base roll" : carriers.Count == 1 ? carriers[0] : "multiple";
                        string key = $"{it.Rarity} <- {(carrier.StartsWith("prefix:") ? "prefix" : carrier.StartsWith("suffix:") ? "suffix" : carrier)}";
                        promotionSources[key] = promotionSources.GetValueOrDefault(key) + 1;
                    }
                    if (!suffixByRarity.TryGetValue(it.Rarity, out var list))
                        suffixByRarity[it.Rarity] = list = new List<int>();
                    list.Add(it.StatBonuses.Count);
                }

                record.Add(new { level, byRarity, avgTier, suffixes, prefixes, actions, statSum });
            }

            Console.WriteLine();
            int slotTotal = Math.Max(1, slotCounts.Values.Sum());
            Console.WriteLine("Drops by slot (all levels): " + string.Join("  ", slotCounts.Select(k => $"{k.Key} {100.0 * k.Value / slotTotal:F1}%")));
            Console.WriteLine("Suffix count by rarity: " + string.Join("  ",
                RarityOrder.Where(suffixByRarity.ContainsKey).Select(r => $"{r}={suffixByRarity[r].Average():F2}")));
            Console.WriteLine("What sets the rarity (non-Common items, all levels): " + string.Join(", ", promotionSources.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}")));
            Console.WriteLine("Gear actions by slot (class / neutral / other-class / basic):");
            foreach (var (slot, mix) in gearActionMix)
            {
                int total = Math.Max(1, mix.Values.Sum());
                Console.WriteLine($"  {slot,-7} n={total,4}  " + string.Join("  ", new[] { "class", "neutral", "other-class", "basic" }
                    .Select(k => $"{k} {100.0 * mix.GetValueOrDefault(k) / total:F0}%")));
                if (detail)
                    Console.WriteLine("          top: " + string.Join(", ", gearActionNames[slot].OrderByDescending(k => k.Value).Take(12).Select(k => $"{k.Key}={k.Value}")));
            }
            if (detail)
            {
                Console.WriteLine("Top suffixes: " + string.Join(", ", suffixCounts.OrderByDescending(k => k.Value).Take(25).Select(k => $"{k.Key}={k.Value}")));
                Console.WriteLine("Top prefixes: " + string.Join(", ", prefixCounts.OrderByDescending(k => k.Value).Take(25).Select(k => $"{k.Key}={k.Value}")));
            }

            string outPath = GameConstants.ResolveGameDataPath("TuningRuns", $"itemstats-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, JsonSerializer.Serialize(new { levels = record, suffixCounts, prefixCounts }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Run record: {Path.GetFullPath(outPath)}");
            return 0;
        }
    }
}
