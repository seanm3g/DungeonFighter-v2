using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ItemGenerationLabServiceTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ItemGenerationLabService Tests ===\n");
            _run = _pass = _fail = 0;

            DungeonLevelMathTests.RunAllTests();

            TestForcedGenerationConstraints();
            TestSortingByRarityThenTier();
            TestAnyRarityRollsNonCommonDistribution();
            TestBaseRollDistributionLevel1ExcludesMythic();
            TestAnyTierUsesTierDistributionAtLootLevel1();
            TestAnyTierUsesTierDistributionAtLootLevel19();
            TestFixedRarityChancesOverridesAnyAndIgnoresLevelGates();
            TestBaseRatioBuildsGeometricRarityPercents();

            TestBase.PrintSummary("ItemGenerationLabService Tests", _run, _pass, _fail);
            TestBase.ClearCurrentTestName();
        }

        private static void TestForcedGenerationConstraints()
        {
            TestBase.SetCurrentTestName(nameof(TestForcedGenerationConstraints));

            var spec = new ItemGenerationSpec
            {
                ItemType = ItemGenerationItemType.Weapons,
                Rarity = "Rare",
                Tier = 3,
                WeaponType = WeaponType.Sword,
                ArmorSlot = ItemGenerationArmorSlot.Any,
                Seed = 42,
                PlayerLevel = 1,
                DungeonLevel = 1
            };

            var rows = ItemGenerationLabService.GenerateBatch(spec, 50);
            TestBase.AssertTrue(rows.Count > 0, "generates some rows", ref _run, ref _pass, ref _fail);

            bool allWeapons = true;
            bool allTier3 = true;
            bool allRare = true;

            foreach (var r in rows)
            {
                if (r.Item is not WeaponItem) allWeapons = false;
                if (r.Item.Tier != 3) allTier3 = false;
                if (!string.Equals(r.Item.Rarity?.Trim(), "Rare", System.StringComparison.OrdinalIgnoreCase)) allRare = false;
            }

            TestBase.AssertTrue(allWeapons, "all are weapons", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(allTier3, "all are tier 3", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(allRare, "all are Rare", ref _run, ref _pass, ref _fail);
        }

        private static void TestSortingByRarityThenTier()
        {
            TestBase.SetCurrentTestName(nameof(TestSortingByRarityThenTier));

            var commonT5 = new WeaponItem("Common Weapon", tier: 5, baseDamage: 10, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
            {
                Rarity = "Common"
            };
            var rareT1 = new WeaponItem("Rare Weapon", tier: 1, baseDamage: 1, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
            {
                Rarity = "Rare"
            };

            var input = new List<ItemGeneratedRow>
            {
                new() { Item = commonT5, Index = 1, SortKey = "a" },
                new() { Item = rareT1, Index = 2, SortKey = "b" },
            };

            var sorted = ItemGenerationLabService.SortBestToWorst(input);
            TestBase.AssertEqual("Rare", sorted[0].Rarity, "rarity dominates tier", ref _run, ref _pass, ref _fail);
        }

        /// <summary>
        /// Regression: Rarity "Any" used to resolve to Common <see cref="RarityData"/> for every item.
        /// </summary>
        private static void TestAnyRarityRollsNonCommonDistribution()
        {
            TestBase.SetCurrentTestName(nameof(TestAnyRarityRollsNonCommonDistribution));

            var spec = new ItemGenerationSpec
            {
                ItemType = ItemGenerationItemType.Weapons,
                Rarity = "Any",
                Tier = 2,
                WeaponType = WeaponType.Mace,
                ArmorSlot = ItemGenerationArmorSlot.Any,
                Seed = 777,
                PlayerLevel = 10,
                DungeonLevel = 1
            };

            var rows = ItemGenerationLabService.GenerateBatch(spec, 120);
            int nonCommon = rows.Count(r => !string.Equals(r.Item.Rarity?.Trim(), "Common", System.StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(nonCommon >= 5, "Any rarity should roll mixed rarities (not 100% Common)", ref _run, ref _pass, ref _fail);
        }

        private static void TestBaseRollDistributionLevel1ExcludesMythic()
        {
            TestBase.SetCurrentTestName(nameof(TestBaseRollDistributionLevel1ExcludesMythic));

            var cache = LootDataCache.Load();
            var dist = LootRarityProcessor.GetBaseRollDistribution(cache, 1);
            TestBase.AssertTrue(dist.Count > 0, "distribution non-empty", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(dist.Any(d => d.Name.Equals("Common", StringComparison.OrdinalIgnoreCase)), "has Common", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(!dist.Any(d => d.Name.Equals("Mythic", StringComparison.OrdinalIgnoreCase)), "L1 excludes Mythic", ref _run, ref _pass, ref _fail);
            double sum = dist.Sum(d => d.ProbabilityPercent);
            TestBase.AssertTrue(Math.Abs(sum - 100.0) < 0.02, $"percents sum to 100, got {sum}", ref _run, ref _pass, ref _fail);
        }

        /// <summary>
        /// Tier Any must use <see cref="LootTierCalculator"/> (not flat 1–5): hero 50 vs dungeon 1 → loot level 1 → ~93% tier 1.
        /// </summary>
        private static void TestAnyTierUsesTierDistributionAtLootLevel1()
        {
            TestBase.SetCurrentTestName(nameof(TestAnyTierUsesTierDistributionAtLootLevel1));

            var spec = new ItemGenerationSpec
            {
                ItemType = ItemGenerationItemType.Weapons,
                Rarity = "Common",
                Tier = null,
                WeaponType = null,
                ArmorSlot = ItemGenerationArmorSlot.Any,
                Seed = 90210,
                PlayerLevel = 50,
                DungeonLevel = 1
            };

            var rows = ItemGenerationLabService.GenerateBatch(spec, 900);
            TestBase.AssertTrue(rows.Count >= 850, "most rows generate (catalog has weapons per tier)", ref _run, ref _pass, ref _fail);
            int t1 = rows.Count(r => r.Tier == 1);
            double frac = (double)t1 / rows.Count;
            TestBase.AssertTrue(frac >= 0.88, $"tier 1 should dominate at loot level 1, got {frac:0.###}", ref _run, ref _pass, ref _fail);
        }

        /// <summary>
        /// Loot level 19 (hero 1 vs dungeon 10): shipped table is mostly tier 2 (~93%).
        /// </summary>
        private static void TestAnyTierUsesTierDistributionAtLootLevel19()
        {
            TestBase.SetCurrentTestName(nameof(TestAnyTierUsesTierDistributionAtLootLevel19));

            var cache = LootDataCache.Load();
            var calc = new LootTierCalculator(cache, new Random(0));
            int loot = calc.CalculateLootLevel(1, 10);
            TestBase.AssertEqual(19, loot, "hero 1 vs dungeon 10 → loot level 19", ref _run, ref _pass, ref _fail);

            var spec = new ItemGenerationSpec
            {
                ItemType = ItemGenerationItemType.Weapons,
                Rarity = "Common",
                Tier = null,
                WeaponType = null,
                ArmorSlot = ItemGenerationArmorSlot.Any,
                Seed = 31415,
                PlayerLevel = 1,
                DungeonLevel = 10
            };

            var rows = ItemGenerationLabService.GenerateBatch(spec, 800);
            TestBase.AssertTrue(rows.Count >= 750, "most rows generate", ref _run, ref _pass, ref _fail);
            int t2 = rows.Count(r => r.Tier == 2);
            double frac2 = (double)t2 / rows.Count;
            TestBase.AssertTrue(frac2 >= 0.75, $"tier 2 should dominate at loot level 19, got {frac2:0.###}", ref _run, ref _pass, ref _fail);
        }

        private static void TestFixedRarityChancesOverridesAnyAndIgnoresLevelGates()
        {
            TestBase.SetCurrentTestName(nameof(TestFixedRarityChancesOverridesAnyAndIgnoresLevelGates));

            var spec = new ItemGenerationSpec
            {
                ItemType = ItemGenerationItemType.Weapons,
                Rarity = "Any",
                Tier = 1,
                WeaponType = null,
                ArmorSlot = ItemGenerationArmorSlot.Any,
                Seed = 123,
                PlayerLevel = 1,
                DungeonLevel = 1,
                FixedRarityChancesPercent = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Common"] = 0,
                    ["Uncommon"] = 0,
                    ["Rare"] = 0,
                    ["Epic"] = 0,
                    ["Legendary"] = 0,
                    ["Mythic"] = 100
                }
            };

            var rows = ItemGenerationLabService.GenerateBatch(spec, 120);
            TestBase.AssertTrue(rows.Count > 0, "generates some rows", ref _run, ref _pass, ref _fail);
            bool allMythic = rows.All(r => string.Equals(r.Item.Rarity?.Trim(), "Mythic", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(allMythic,
                "fixed rarity chances should force Mythic even at hero level 1. Got: " +
                string.Join(", ", rows.Select(r => (r.Item.Rarity ?? "(null)").Trim()).Distinct().OrderBy(x => x)),
                ref _run, ref _pass, ref _fail);
        }

        private static void TestBaseRatioBuildsGeometricRarityPercents()
        {
            TestBase.SetCurrentTestName(nameof(TestBaseRatioBuildsGeometricRarityPercents));

            var map = RarityChanceMath.BuildGeometricRarityChancesPercent(8.0);
            double sum = map.Values.Sum();
            TestBase.AssertTrue(Math.Abs(sum - 100.0) < 0.0001, $"sum to 100, got {sum}", ref _run, ref _pass, ref _fail);

            // Monotone decreasing: Common > Uncommon > ... > Mythic
            TestBase.AssertTrue(map["Common"] > map["Uncommon"], "Common > Uncommon", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(map["Uncommon"] > map["Rare"], "Uncommon > Rare", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(map["Rare"] > map["Epic"], "Rare > Epic", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(map["Epic"] > map["Legendary"], "Epic > Legendary", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(map["Legendary"] > map["Mythic"], "Legendary > Mythic", ref _run, ref _pass, ref _fail);

            // Ratio property approximately holds (within small numeric tolerance).
            double ratioCU = map["Common"] / map["Uncommon"];
            double ratioUR = map["Uncommon"] / map["Rare"];
            TestBase.AssertTrue(Math.Abs(ratioCU - 8.0) < 0.05, $"C/U approx 8, got {ratioCU:0.###}", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(ratioUR - 8.0) < 0.05, $"U/R approx 8, got {ratioUR:0.###}", ref _run, ref _pass, ref _fail);
        }
    }
}

