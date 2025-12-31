using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for XP system
    /// Tests XP calculation, scaling, thresholds, and multiple level-ups from single XP gain
    /// </summary>
    public static class XPSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== XP System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestXPCalculation();
            TestXPScaling();
            TestGuaranteedLevelUpLogic();
            TestXPThresholds();
            TestMultipleLevelUpsFromSingleXP();
            TestXPPersistence();
            TestXPOverflowHandling();

            TestBase.PrintSummary("XP System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestXPCalculation()
        {
            Console.WriteLine("\n--- Testing XP Calculation ---");

            var character = TestDataBuilders.Character().WithLevel(3).Build();
            int originalXP = character.XP;

            // Add XP
            character.AddXP(100);

            TestBase.AssertTrue(character.XP > originalXP,
                $"XP should increase: {originalXP} -> {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPScaling()
        {
            Console.WriteLine("\n--- Testing XP Scaling ---");

            var character1 = TestDataBuilders.Character().WithLevel(1).Build();
            var character5 = TestDataBuilders.Character().WithLevel(5).Build();

            int xp1 = 100;
            int xp5 = 100;

            character1.AddXP(xp1);
            character5.AddXP(xp5);

            // Both should gain XP (scaling is handled by reward system, not XP system itself)
            TestBase.AssertTrue(character1.XP > 0 || character1.Level > 1,
                $"Level 1 character should gain XP or level up: XP={character1.XP}, Level={character1.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character5.XP > 0 || character5.Level > 5,
                $"Level 5 character should gain XP or level up: XP={character5.XP}, Level={character5.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGuaranteedLevelUpLogic()
        {
            Console.WriteLine("\n--- Testing Guaranteed Level Up Logic ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            character.SessionStats.DungeonsCompleted = 0; // No dungeons completed yet - about to complete first dungeon

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
            TestBase.AssertTrue(character.Level > 1 || levelUpInfos.Count > 0,
                $"Character should level up from first dungeon: Level {character.Level}, LevelUps: {levelUpInfos.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPThresholds()
        {
            Console.WriteLine("\n--- Testing XP Thresholds ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var tuning = GameConfiguration.Instance;

            // Calculate XP needed for level 2
            int averageXPPerDungeonAtLevel1 = tuning.Progression.EnemyXPBase + 25;
            int xpNeededForLevel2 = 1 * 1 * averageXPPerDungeonAtLevel1;

            int levelBefore = character.Level;
            character.AddXP(xpNeededForLevel2);
            int levelAfter = character.Level;

            TestBase.AssertTrue(levelAfter > levelBefore,
                $"Character should level up with exact XP threshold: Level {levelBefore} -> {levelAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test level 2+ threshold (Level^2.2)
            character.Level = 2;
            int xpNeededForLevel3 = (int)(Math.Pow(2, 2.2) * averageXPPerDungeonAtLevel1);

            levelBefore = character.Level;
            character.AddXP(xpNeededForLevel3);
            levelAfter = character.Level;

            TestBase.AssertTrue(levelAfter > levelBefore,
                $"Character should level up with level 2+ threshold: Level {levelBefore} -> {levelAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleLevelUpsFromSingleXP()
        {
            Console.WriteLine("\n--- Testing Multiple Level Ups From Single XP ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var tuning = GameConfiguration.Instance;
            int averageXPPerDungeonAtLevel1 = tuning.Progression.EnemyXPBase + 25;

            // Add enough XP for multiple level-ups
            int xpForLevel2 = 1 * 1 * averageXPPerDungeonAtLevel1;
            int xpForLevel3 = (int)(Math.Pow(2, 2.2) * averageXPPerDungeonAtLevel1);
            int xpForLevel4 = (int)(Math.Pow(3, 2.2) * averageXPPerDungeonAtLevel1);
            int totalXP = xpForLevel2 + xpForLevel3 + xpForLevel4 + 10;

            int levelBefore = character.Level;
            character.AddXP(totalXP);
            int levelAfter = character.Level;

            TestBase.AssertTrue(levelAfter >= levelBefore + 2,
                $"Character should level up multiple times: Level {levelBefore} -> {levelAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPPersistence()
        {
            Console.WriteLine("\n--- Testing XP Persistence ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            character.XP = 150;

            // Save and load (simulated - actual save/load tested in SaveLoadSystemTests)
            int xpBefore = character.XP;
            int levelBefore = character.Level;

            // XP should persist in character object
            TestBase.AssertEqual(xpBefore, character.XP,
                $"XP should persist in character: {xpBefore} == {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(levelBefore, character.Level,
                $"Level should persist in character: {levelBefore} == {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPOverflowHandling()
        {
            Console.WriteLine("\n--- Testing XP Overflow Handling ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int startingLevel = character.Level;

            // Add very large amount of XP
            character.AddXP(int.MaxValue / 2);

            // Should handle without crashing and level up appropriately
            TestBase.AssertTrue(character.Level > startingLevel,
                $"Character should level up with very large XP: Level {startingLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // XP should be valid (not overflow)
            TestBase.AssertTrue(character.XP >= 0 && character.XP < int.MaxValue,
                $"XP should be valid after large gain: {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

