using System;
using System.Collections.Generic;
using RPGGame;
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
            TestDungeonPacedXpRequirementCurve();
            TestGuaranteedLevelUpLogic();
            TestXPThresholds();
            TestMultipleLevelUpsFromSingleXP();
            TestAddXpOneThresholdGrantsExactlyOneLevelAndClassPoint();
            TestXPPersistence();
            TestXPOverflowHandling();

            TestBase.PrintSummary("XP System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestXPCalculation()
        {
            Console.WriteLine("\n--- Testing XP Calculation ---");

            var character = TestDataBuilders.Character().WithLevel(30).Build();
            character.XP = 0;
            int originalXP = character.XP;
            int originalLevel = character.Level;

            // Small gain: should add to the bar without consuming the whole grant in level-ups
            character.AddXP(10);

            TestBase.AssertTrue(character.XP > originalXP || character.Level > originalLevel,
                $"XP or level should increase: XP {originalXP} -> {character.XP}, Level {originalLevel} -> {character.Level}",
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

        /// <summary>
        /// Bar cost uses tier-1 dungeon completion as unit × (1, 1.5, 2, 3, …) so pacing stretches over time.
        /// </summary>
        private static void TestDungeonPacedXpRequirementCurve()
        {
            Console.WriteLine("\n--- Testing Dungeon-Paced XP Requirement Curve ---");

            int tier1Completion = CharacterProgression.GetStandardDungeonCompletionXpForLevel(1);
            int need1 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(1);
            int need2 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(2);
            int need3 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(3);
            int need4 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(4);

            TestBase.AssertTrue(need2 > need1 && need3 > need2 && need4 > need3,
                $"XP required should increase with level: {need1}, {need2}, {need3}, {need4}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Ratios ignore global/BaseXPToLevel2 scaling (both numerator and denominator pick up the same factors).
            double r2 = need2 / (double)need1;
            double r3 = need3 / (double)need1;
            double r4 = need4 / (double)need1;
            TestBase.AssertTrue(Math.Abs(r2 - 1.5) < 0.02,
                $"L2 bar should be ~1.5× L1 bar (got {r2:F3})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(r3 - 2.0) < 0.02,
                $"L3 bar should be ~2× L1 bar (got {r3:F3})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(r4 - 3.0) < 0.02,
                $"L4 bar should be ~3× L1 bar (got {r4:F3})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(tier1Completion >= 1,
                $"Tier-1 dungeon completion XP should be positive: {tier1Completion}",
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
            character.XP = 0;

            int xpNeededForLevel2 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(1);

            int levelBefore = character.Level;
            character.AddXP(xpNeededForLevel2);
            int levelAfter = character.Level;

            TestBase.AssertEqual(levelBefore + 1, levelAfter,
                $"Character should level up exactly once with tier-1 XP: Level {levelBefore} -> {levelAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.XP = 0;
            int xpNeededForLevel3 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(2);

            levelBefore = character.Level;
            character.AddXP(xpNeededForLevel3);
            levelAfter = character.Level;

            TestBase.AssertEqual(levelBefore + 1, levelAfter,
                $"Character should level up exactly once with tier-2 XP: Level {levelBefore} -> {levelAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleLevelUpsFromSingleXP()
        {
            Console.WriteLine("\n--- Testing Multiple Level Ups From Single XP ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            character.XP = 0;

            int xpForLevel2 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(1);
            int xpForLevel3 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(2);
            int xpForLevel4 = CharacterProgression.GetXpRequiredToAdvanceFromLevel(3);
            int totalXP = xpForLevel2 + xpForLevel3 + xpForLevel4 + 10;

            int levelBefore = character.Level;
            character.AddXP(totalXP);
            int levelAfter = character.Level;

            TestBase.AssertEqual(levelBefore + 3, levelAfter,
                $"Character should gain exactly three levels from three thresholds + slack: Level {levelBefore} -> {levelAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAddXpOneThresholdGrantsExactlyOneLevelAndClassPoint()
        {
            Console.WriteLine("\n--- Testing AddXP one threshold = one level + one class point ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            character.XP = 0;
            var mace = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Mace).Build();
            character.EquipItem(mace, "weapon");

            int need = CharacterProgression.GetXpRequiredToAdvanceFromLevel(1);
            int ptsBefore = character.BarbarianPoints;
            character.AddXP(need);

            TestBase.AssertEqual(2, character.Level,
                "One XP threshold from level 1 should land on level 2 (not double-count progression + rewards)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(ptsBefore + 1, character.BarbarianPoints,
                "One level-up should award one Barbarian class point",
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

            // Large grant (keeps the test fast; flat per-level XP makes int.MaxValue/2 impractical to simulate)
            character.AddXP(500_000);

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

