using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Room search consumables: generator produces only consumables; use service applies food heal and potion buffs.
    /// </summary>
    public static class RoomSearchConsumableTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== RoomSearchConsumable Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGeneratorProducesConsumableOnly();
            TestFoodHealPositive();
            TestPotionApplyImmediatelyBuffsStats();
            TestComboPotionReflectedInThresholdHud();

            TestBase.PrintSummary("RoomSearchConsumable Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestGeneratorProducesConsumableOnly()
        {
            Console.WriteLine("--- Testing generator produces consumables ---");
            TestBase.SetCurrentTestName(nameof(TestGeneratorProducesConsumableOnly));
            var rng = new Random(42);
            var player = new Character("Tester", 3);
            for (int i = 0; i < 30; i++)
            {
                var item = RoomSearchConsumableGenerator.Generate(rng, player, dungeonLevel: 5);
                TestBase.AssertTrue(item.Type == ItemType.Consumable, "search loot must be consumable type", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(item.RoomSearchConsumableKind != RoomSearchConsumableKind.None, "kind must be set", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.ClearCurrentTestName();
        }

        private static void TestFoodHealPositive()
        {
            Console.WriteLine("\n--- Testing food heal bounds ---");
            TestBase.SetCurrentTestName(nameof(TestFoodHealPositive));
            var rng = new Random(7);
            var player = new Character("Healer", 1);
            bool sawFood = false;
            for (int i = 0; i < 200; i++)
            {
                var item = RoomSearchConsumableGenerator.Generate(rng, player, dungeonLevel: 2);
                if (item.RoomSearchConsumableKind == RoomSearchConsumableKind.Food)
                {
                    sawFood = true;
                    TestBase.AssertTrue(item.ConsumableHealAmount >= 1, "food heal at least 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(item.ConsumableHealAmount <= player.MaxHealth, "food heal not above max HP", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    break;
                }
            }

            TestBase.AssertTrue(sawFood, "expected at least one food in 200 rolls", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestPotionApplyImmediatelyBuffsStats()
        {
            Console.WriteLine("\n--- Testing potion apply immediately ---");
            TestBase.SetCurrentTestName(nameof(TestPotionApplyImmediatelyBuffsStats));
            var player = new Character("Potioner", 5);
            player.DungeonSearchBuffs.Clear();
            var item = new Item(ItemType.Consumable, "Test STR potion", 3, 0)
            {
                RoomSearchConsumableKind = RoomSearchConsumableKind.PotionStrength,
                ConsumablePotionPotency = 3,
                ConsumableHealAmount = 0
            };
            int strBefore = player.GetEffectiveStrength();
            bool ok = SearchConsumableUseService.ApplyImmediately(player, item, out string msg);
            TestBase.AssertTrue(ok, "apply should succeed: " + msg, ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(player.GetEffectiveStrength() >= strBefore + 3, "STR buff applied", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(player.DungeonSearchBuffs.StrengthBonus >= 3, "buff state recorded", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.ClearCurrentTestName();
        }

        private static void TestComboPotionReflectedInThresholdHud()
        {
            Console.WriteLine("\n--- Testing combo potion shows in threshold HUD ---");
            TestBase.SetCurrentTestName(nameof(TestComboPotionReflectedInThresholdHud));
            var player = new Character("FlowDrinker", 5);
            player.DungeonSearchBuffs.Clear();
            var item = new Item(ItemType.Consumable, "Serum of Flow", 3, 0)
            {
                RoomSearchConsumableKind = RoomSearchConsumableKind.PotionCombo,
                ConsumablePotionPotency = 3,
                ConsumableHealAmount = 0
            };
            bool ok = SearchConsumableUseService.ApplyImmediately(player, item, out string msg);
            TestBase.AssertTrue(ok, "apply should succeed: " + msg, ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, player.DungeonSearchBuffs.ComboThresholdAdjustment,
                "combo threshold buff stored", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var snapshot = DiceRollThresholdResolver.Resolve(player);
            int defaultCombo = GameConfiguration.Instance.RollSystem.ComboThreshold.Min > 0
                ? GameConfiguration.Instance.RollSystem.ComboThreshold.Min
                : 14;
            TestBase.AssertEqual(defaultCombo - 3, snapshot.EffectiveCombo,
                "HUD combo threshold should include dungeon potion (+3 easier)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.ClearCurrentTestName();
        }
    }
}
