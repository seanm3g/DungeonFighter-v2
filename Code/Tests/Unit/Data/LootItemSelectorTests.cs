using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Tests for <see cref="LootItemSelector"/> category weights and armor slot filtering.
    /// </summary>
    public static class LootItemSelectorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== LootItemSelector Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            LootGenerator.Initialize();

            TestRollLootCategory_ApproximateWeights();
            TestRollArmor_PreferredSlotReturnsLegsItem();
            TestRollArmor_PreferredLegsFallsBackToOtherArmorAtTier();
            TestRollArmor_DoesNotAssignRandomGearAction();

            TestBase.PrintSummary("LootItemSelector Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestRollLootCategory_ApproximateWeights()
        {
            Console.WriteLine("--- Testing RollLootCategory approximate 50% / 12.5%×4 split ---");

            var cache = LootDataCache.CreateEmpty();
            var selector = new LootItemSelector(cache, new Random(424242));
            const int n = 24000;
            int weapons = 0, head = 0, chest = 0, legs = 0, feet = 0;
            for (int i = 0; i < n; i++)
            {
                var (isWeapon, slot) = selector.RollLootCategory();
                if (isWeapon)
                    weapons++;
                else if (string.Equals(slot, "head", StringComparison.OrdinalIgnoreCase))
                    head++;
                else if (string.Equals(slot, "chest", StringComparison.OrdinalIgnoreCase))
                    chest++;
                else if (string.Equals(slot, "legs", StringComparison.OrdinalIgnoreCase))
                    legs++;
                else if (string.Equals(slot, "feet", StringComparison.OrdinalIgnoreCase))
                    feet++;
            }

            double tol = 0.02;
            TestBase.AssertTrue(Math.Abs(weapons / (double)n - 0.5) < tol,
                $"weapon fraction ~0.5, got {weapons / (double)n:0.###}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(head / (double)n - 0.125) < tol,
                $"head fraction ~0.125, got {head / (double)n:0.###}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(chest / (double)n - 0.125) < tol,
                $"chest fraction ~0.125, got {chest / (double)n:0.###}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(legs / (double)n - 0.125) < tol,
                $"legs fraction ~0.125, got {legs / (double)n:0.###}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(feet / (double)n - 0.125) < tol,
                $"feet fraction ~0.125, got {feet / (double)n:0.###}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollArmor_PreferredSlotReturnsLegsItem()
        {
            Console.WriteLine("\n--- Testing RollArmor preferred legs returns LegsItem ---");

            var cache = LootDataCache.CreateEmpty();
            cache.ArmorData.Clear();
            cache.ArmorData.Add(new ArmorData
            {
                Slot = "legs",
                Name = "Lab Legs Only",
                Tier = 1,
                Armor = 1
            });

            var selector = new LootItemSelector(cache, new Random(0));
            Item? item = selector.RollArmor(1, "legs");
            TestBase.AssertTrue(item is LegsItem,
                "single legs row at tier should produce LegsItem",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollArmor_PreferredLegsFallsBackToOtherArmorAtTier()
        {
            Console.WriteLine("\n--- Testing RollArmor preferred legs falls back when tier has no legs ---");

            var cache = LootDataCache.CreateEmpty();
            cache.ArmorData.Clear();
            cache.ArmorData.Add(new ArmorData
            {
                Slot = "head",
                Name = "Only Helm T1",
                Tier = 1,
                Armor = 1
            });

            var selector = new LootItemSelector(cache, new Random(0));
            Item? item = selector.RollArmor(1, "legs");
            TestBase.AssertTrue(item is HeadItem,
                "when no legs at tier, pool widens to all armor at tier",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollArmor_DoesNotAssignRandomGearAction()
        {
            Console.WriteLine("\n--- Testing RollArmor does not assign random GearAction ---");

            var cache = LootDataCache.CreateEmpty();
            cache.ArmorData.Clear();
            cache.ArmorData.Add(new ArmorData
            {
                Slot = "head",
                Name = "Plain Helm T1",
                Tier = 1,
                Armor = 1
            });

            var selector = new LootItemSelector(cache, new Random(0));
            Item? item = selector.RollArmor(1, "head");

            TestBase.AssertNotNull(item,
                "armor item should be generated",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (item == null)
                return;

            TestBase.AssertTrue(string.IsNullOrEmpty(item.GearAction),
                $"armor selector should not bypass the Actions affix table via GearAction; got '{item.GearAction}'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, GearActionNames.Resolve(item).Count,
                "plain armor without rolled ActionBonuses should resolve no gear actions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
