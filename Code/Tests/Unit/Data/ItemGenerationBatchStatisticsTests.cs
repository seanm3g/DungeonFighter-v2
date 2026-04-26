using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ItemGenerationBatchStatisticsTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ItemGenerationBatchStatistics Tests ===\n");
            _run = _pass = _fail = 0;

            TestEmptyBatch();
            TestTierRarityCountsAndDuplicates();

            TestBase.PrintSummary("ItemGenerationBatchStatistics Tests", _run, _pass, _fail);
            TestBase.ClearCurrentTestName();
        }

        private static void TestEmptyBatch()
        {
            TestBase.SetCurrentTestName(nameof(TestEmptyBatch));

            var stats = ItemGenerationBatchStatistics.Compute(Array.Empty<ItemGeneratedRow>());
            TestBase.AssertEqual(0, stats.TotalCount, "total 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, stats.UniqueCount, "unique 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, stats.DuplicateCount, "dup 0", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(0, stats.TierRarityCounts.Count, "no tier×rarity rows", ref _run, ref _pass, ref _fail);
        }

        private static void TestTierRarityCountsAndDuplicates()
        {
            TestBase.SetCurrentTestName(nameof(TestTierRarityCountsAndDuplicates));

            WeaponItem makeSword(string name)
            {
                var w = new WeaponItem(name, tier: 1, baseDamage: 10, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
                {
                    Level = 1,
                    Rarity = "Common"
                };
                w.Modifications.Add(new Modification
                {
                    PrefixCategory = "Quality",
                    Name = "Precise",
                    RolledValue = 1.5
                });
                return w;
            }

            // Two identical items (same core + same rolled affix) + one different.
            var a1 = makeSword("WILLOW SHOES of the Storm");
            var a2 = makeSword("WILLOW SHOES of the Storm");
            var b = new WeaponItem("Different", tier: 2, baseDamage: 12, baseAttackSpeed: 1.0, weaponType: WeaponType.Sword)
            {
                Level = 1,
                Rarity = "Rare"
            };

            var rows = new List<ItemGeneratedRow>
            {
                new() { Item = a1, Index = 1, SortKey = "a" },
                new() { Item = a2, Index = 2, SortKey = "b" },
                new() { Item = b, Index = 3, SortKey = "c" },
            };

            var stats = ItemGenerationBatchStatistics.Compute(rows);
            TestBase.AssertEqual(3, stats.TotalCount, "total 3", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, stats.UniqueCount, "unique 2", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, stats.DuplicateCount, "dup 1", ref _run, ref _pass, ref _fail);

            var dict = stats.TierRarityCounts.ToDictionary(k => (k.Tier, k.Rarity), v => v.Count);
            TestBase.AssertEqual(2, dict[(1, "Common")], "T1 Common count", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, dict[(2, "Rare")], "T2 Rare count", ref _run, ref _pass, ref _fail);
        }
    }
}

