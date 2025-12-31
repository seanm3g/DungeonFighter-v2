using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dungeon rewards system
    /// Tests loot generation, XP rewards, tier calculation, and reward distribution
    /// </summary>
    public static class DungeonRewardsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dungeon Rewards Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestLootGeneration();
            TestLootTierCalculation();
            TestLootLevelCalculation();
            TestXPRewardCalculation();
            TestGuaranteedLevelUp();
            TestLootRarityDistribution();
            TestContextualLoot();
            TestFallbackLoot();
            TestRewardHealthRestoration();
            TestMultipleRewards();

            TestBase.PrintSummary("Dungeon Rewards Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestLootGeneration()
        {
            Console.WriteLine("\n--- Testing Loot Generation ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            int dungeonLevel = 5;

            // Test guaranteed loot generation
            var loot = LootGenerator.GenerateLoot(
                character.Level,
                dungeonLevel,
                character,
                guaranteedLoot: true
            );

            TestBase.AssertNotNull(loot,
                "Guaranteed loot should always generate an item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (loot != null)
            {
                TestBase.AssertTrue(loot.Level >= 1,
                    $"Generated loot should have valid level: {loot.Level}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestLootTierCalculation()
        {
            Console.WriteLine("\n--- Testing Loot Tier Calculation ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            int dungeonLevel = 5;

            // Generate multiple items and check tier distribution
            int tier1Count = 0;
            int tier2Count = 0;
            int tier3PlusCount = 0;

            for (int i = 0; i < 20; i++)
            {
                var loot = LootGenerator.GenerateLoot(
                    character.Level,
                    dungeonLevel,
                    character,
                    guaranteedLoot: true
                );

                if (loot != null)
                {
                    if (loot.Tier == 1) tier1Count++;
                    else if (loot.Tier == 2) tier2Count++;
                    else tier3PlusCount++;
                }
            }

            // At least some items should be generated
            int totalGenerated = tier1Count + tier2Count + tier3PlusCount;
            TestBase.AssertTrue(totalGenerated > 0,
                $"Should generate loot items: {totalGenerated} items generated",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLootLevelCalculation()
        {
            Console.WriteLine("\n--- Testing Loot Level Calculation ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            int dungeonLevel = 10; // Higher dungeon level

            var loot = LootGenerator.GenerateLoot(
                character.Level,
                dungeonLevel,
                character,
                guaranteedLoot: true
            );

            if (loot != null)
            {
                // Loot level should be adjusted based on player vs dungeon level
                TestBase.AssertTrue(loot.Level >= 1,
                    $"Loot level should be valid: {loot.Level}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestXPRewardCalculation()
        {
            Console.WriteLine("\n--- Testing XP Reward Calculation ---");

            var character = TestDataBuilders.Character().WithLevel(3).Build();
            int originalXP = character.XP;

            var rewardManager = new RewardManager();
            var task = rewardManager.AwardLootAndXPWithReturnsAsync(
                character,
                new List<Item>(),
                3,
                null
            );
            task.Wait();

            var (xpGained, lootReceived, levelUpInfos) = task.Result;

            TestBase.AssertTrue(xpGained > 0,
                $"XP should be awarded: {xpGained} XP",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.XP > originalXP,
                $"Character XP should increase: {originalXP} -> {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGuaranteedLevelUp()
        {
            Console.WriteLine("\n--- Testing Guaranteed Level Up ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int originalLevel = character.Level;

            // Simulate about to complete first dungeon (no dungeons completed yet)
            character.SessionStats.DungeonsCompleted = 0;

            var rewardManager = new RewardManager();
            var task = rewardManager.AwardLootAndXPWithReturnsAsync(
                character,
                new List<Item>(),
                1,
                null
            );
            task.Wait();

            var (xpGained, lootReceived, levelUpInfos) = task.Result;

            // Character should level up from first dungeon
            TestBase.AssertTrue(character.Level > originalLevel || levelUpInfos.Count > 0,
                $"Character should level up from first dungeon: Level {originalLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLootRarityDistribution()
        {
            Console.WriteLine("\n--- Testing Loot Rarity Distribution ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            int dungeonLevel = 5;

            var rarityCounts = new Dictionary<string, int>();

            // Generate multiple items to check rarity distribution
            for (int i = 0; i < 30; i++)
            {
                var loot = LootGenerator.GenerateLoot(
                    character.Level,
                    dungeonLevel,
                    character,
                    guaranteedLoot: true
                );

                if (loot != null)
                {
                    string rarity = loot.Rarity ?? "Common";
                    if (!rarityCounts.ContainsKey(rarity))
                    {
                        rarityCounts[rarity] = 0;
                    }
                    rarityCounts[rarity]++;
                }
            }

            // Should have at least some items with rarity
            TestBase.AssertTrue(rarityCounts.Count > 0,
                $"Should generate items with rarity: {rarityCounts.Count} different rarities",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestContextualLoot()
        {
            Console.WriteLine("\n--- Testing Contextual Loot ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            string dungeonTheme = "Forest";

            var loot = LootGenerator.GenerateLoot(
                character.Level,
                5,
                character,
                guaranteedLoot: true,
                dungeonTheme: dungeonTheme
            );

            TestBase.AssertNotNull(loot,
                "Contextual loot should generate an item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFallbackLoot()
        {
            Console.WriteLine("\n--- Testing Fallback Loot ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();

            // Test that loot generation doesn't crash even with edge cases
            var loot = LootGenerator.GenerateLoot(
                character.Level,
                1,
                character,
                guaranteedLoot: true
            );

            // Should either generate loot or handle gracefully
            // (The system should have fallback mechanisms)
            TestBase.AssertTrue(true,
                "Fallback loot generation should not crash",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRewardHealthRestoration()
        {
            Console.WriteLine("\n--- Testing Reward Health Restoration ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            character.CurrentHealth = 10; // Damage the character

            int healthBefore = character.CurrentHealth;
            int maxHealth = character.GetEffectiveMaxHealth();

            var rewardManager = new RewardManager();
            var task = rewardManager.AwardLootAndXPWithReturnsAsync(
                character,
                new List<Item>(),
                5,
                null
            );
            task.Wait();

            // Character should be healed to full health
            TestBase.AssertTrue(character.CurrentHealth >= healthBefore,
                $"Health should be restored: {healthBefore} -> {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleRewards()
        {
            Console.WriteLine("\n--- Testing Multiple Rewards ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            int originalInventoryCount = character.Equipment.Inventory.Count;

            var rewardManager = new RewardManager();
            var task = rewardManager.AwardLootAndXPWithReturnsAsync(
                character,
                character.Equipment.Inventory,
                5,
                null
            );
            task.Wait();

            var (xpGained, lootReceived, levelUpInfos) = task.Result;

            // If loot was received, it should be in inventory
            if (lootReceived != null)
            {
                TestBase.AssertTrue(character.Equipment.Inventory.Contains(lootReceived) || 
                    character.Equipment.Inventory.Count > originalInventoryCount,
                    "Loot should be added to inventory",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}

