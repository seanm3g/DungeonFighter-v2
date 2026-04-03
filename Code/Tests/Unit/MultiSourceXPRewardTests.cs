using System;
using RPGGame;
using RPGGame.Progression;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for multi-source XP reward system
    /// Tests XP rewards from enemies killed, rooms entered, items found, and dungeons completed
    /// Verifies immediate XP awarding and mid-dungeon level-ups
    /// </summary>
    public static class MultiSourceXPRewardTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Multi-Source XP Reward System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEnemyKillXP();
            TestRoomEntryXP();
            TestItemFoundXP();
            TestDungeonCompletionXP();
            TestImmediateXPAwarding();
            TestMidDungeonLevelUp();
            TestXPScalingWithLevel();
            TestXPAmountsRelative();
            TestMultipleXPSources();
            TestFirstDungeonLevelUpGuarantee();

            TestBase.PrintSummary("Multi-Source XP Reward System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestEnemyKillXP()
        {
            Console.WriteLine("\n--- Testing Enemy Kill XP ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var enemy = TestDataBuilders.Enemy().WithLevel(5).Build();
            
            int xpBefore = character.XP;
            int levelBefore = character.Level;
            int expectedXP = enemy.XPReward; // Enemy's XPReward property

            XPRewardSystem.AwardEnemyKillXP(character, enemy);

            // Calculate total XP gained including any consumed for leveling up
            int levelsGained = character.Level - levelBefore;
            int xpConsumedForLevels = 0;
            for (int i = 0; i < levelsGained; i++)
            {
                int level = levelBefore + i;
                var tuning = GameConfiguration.Instance;
                int averageXPPerDungeonAtLevel1 = tuning.Progression.EnemyXPBase + 25;
                if (averageXPPerDungeonAtLevel1 <= 0) averageXPPerDungeonAtLevel1 = 25;
                // Consistent curve: Level^2 * base for all levels
                xpConsumedForLevels += level * level * averageXPPerDungeonAtLevel1;
            }
            int totalXPGained = (character.XP - xpBefore) + xpConsumedForLevels;

            TestBase.AssertTrue(character.XP > xpBefore || character.Level > levelBefore,
                $"XP should increase or level should increase after enemy kill: XP {xpBefore} -> {character.XP}, Level {levelBefore} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(totalXPGained >= expectedXP,
                $"Total XP gained (including level-ups) should be at least enemy's XPReward: {totalXPGained} >= {expectedXP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRoomEntryXP()
        {
            Console.WriteLine("\n--- Testing Room Entry XP ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int roomLevel = 5;
            
            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0) baseXP = 25;
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0) xpPerLevel = 5;
            
            int expectedBaseXP = baseXP + (roomLevel * xpPerLevel);
            int expectedXP = (int)(expectedBaseXP * 0.5); // 0.5x multiplier

            int xpBefore = character.XP;
            int levelBefore = character.Level;

            XPRewardSystem.AwardRoomEntryXP(character, roomLevel);

            // Calculate total XP gained including any consumed for leveling up
            int levelsGained = character.Level - levelBefore;
            int xpConsumedForLevels = 0;
            for (int i = 0; i < levelsGained; i++)
            {
                int level = levelBefore + i;
                int averageXPPerDungeonAtLevel1 = baseXP + 25;
                if (averageXPPerDungeonAtLevel1 <= 0) averageXPPerDungeonAtLevel1 = 25;
                // Consistent curve: Level^2 * base for all levels
                xpConsumedForLevels += level * level * averageXPPerDungeonAtLevel1;
            }
            int totalXPGained = (character.XP - xpBefore) + xpConsumedForLevels;

            TestBase.AssertTrue(character.XP > xpBefore || character.Level > levelBefore,
                $"XP should increase or level should increase after room entry: XP {xpBefore} -> {character.XP}, Level {levelBefore} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(totalXPGained >= expectedXP,
                $"Total XP gained (including level-ups) should be at least 0.5x base XP: {totalXPGained} >= {expectedXP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestItemFoundXP()
        {
            Console.WriteLine("\n--- Testing Item Found XP ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var item = TestDataBuilders.Item().WithName("TestItem").Build();
            item.Level = 5;
            
            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0) baseXP = 25;
            
            int expectedBaseXP = baseXP + (item.Level * tuning.Progression.EnemyXPPerLevel);
            int expectedXP = (int)(expectedBaseXP * 0.3); // 0.3x multiplier

            int xpBefore = character.XP;

            XPRewardSystem.AwardItemFoundXP(character, item);

            TestBase.AssertTrue(character.XP > xpBefore,
                $"XP should increase after item found: {xpBefore} -> {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.XP >= xpBefore + expectedXP,
                $"XP should be at least 0.3x base XP: {character.XP} >= {xpBefore + expectedXP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDungeonCompletionXP()
        {
            Console.WriteLine("\n--- Testing Dungeon Completion XP ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int dungeonLevel = 5;
            
            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0) baseXP = 25;
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0) xpPerLevel = 5;
            
            int expectedBaseXP = baseXP + (dungeonLevel * xpPerLevel);
            int expectedXP = expectedBaseXP * 10; // 10x multiplier

            int xpBefore = character.XP;
            int levelBefore = character.Level;

            XPRewardSystem.AwardDungeonCompletionXP(character, dungeonLevel, false);

            // Calculate total XP gained including any consumed for leveling up
            int levelsGained = character.Level - levelBefore;
            int xpConsumedForLevels = 0;
            for (int i = 0; i < levelsGained; i++)
            {
                int level = levelBefore + i;
                int averageXPPerDungeonAtLevel1 = baseXP + 25;
                if (averageXPPerDungeonAtLevel1 <= 0) averageXPPerDungeonAtLevel1 = 25;
                // Consistent curve: Level^2 * base for all levels
                xpConsumedForLevels += level * level * averageXPPerDungeonAtLevel1;
            }
            int totalXPGained = (character.XP - xpBefore) + xpConsumedForLevels;

            TestBase.AssertTrue(character.XP > xpBefore || character.Level > levelBefore,
                $"XP should increase or level should increase after dungeon completion: XP {xpBefore} -> {character.XP}, Level {levelBefore} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(totalXPGained >= expectedXP,
                $"Total XP gained (including level-ups) should be at least 10x base XP: {totalXPGained} >= {expectedXP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestImmediateXPAwarding()
        {
            Console.WriteLine("\n--- Testing Immediate XP Awarding ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var enemy = TestDataBuilders.Enemy().WithLevel(1).Build();
            
            int xpBefore = character.XP;

            // Award XP and immediately check
            XPRewardSystem.AwardEnemyKillXP(character, enemy);

            TestBase.AssertTrue(character.XP > xpBefore,
                $"XP should be awarded immediately: {xpBefore} -> {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Award room entry XP immediately
            int xpBeforeRoom = character.XP;
            XPRewardSystem.AwardRoomEntryXP(character, 1);

            TestBase.AssertTrue(character.XP > xpBeforeRoom,
                $"Room entry XP should be awarded immediately: {xpBeforeRoom} -> {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMidDungeonLevelUp()
        {
            Console.WriteLine("\n--- Testing Mid-Dungeon Level Up ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var tuning = GameConfiguration.Instance;
            int averageXPPerDungeonAtLevel1 = tuning.Progression.EnemyXPBase + 25;
            if (averageXPPerDungeonAtLevel1 <= 0) averageXPPerDungeonAtLevel1 = 25;
            
            int xpNeededForLevel2 = 1 * 1 * averageXPPerDungeonAtLevel1;

            // Award enough XP from enemy kill to level up
            int levelBefore = character.Level;
            
            // Create an enemy that gives enough XP
            var enemy = TestDataBuilders.Enemy().WithLevel(10).Build();
            // If enemy doesn't give enough, add more XP manually
            if (enemy.XPReward < xpNeededForLevel2)
            {
                character.AddXP(xpNeededForLevel2 - enemy.XPReward);
            }
            
            XPRewardSystem.AwardEnemyKillXP(character, enemy);

            TestBase.AssertTrue(character.Level >= levelBefore + 1,
                $"Character should level up mid-dungeon: Level {levelBefore} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPScalingWithLevel()
        {
            Console.WriteLine("\n--- Testing XP Scaling With Enemy/Dungeon Level ---");

            var character1 = TestDataBuilders.Character().WithLevel(1).Build();
            var character2 = TestDataBuilders.Character().WithLevel(1).Build();
            
            var enemy1 = TestDataBuilders.Enemy().WithLevel(1).Build();
            var enemy5 = TestDataBuilders.Enemy().WithLevel(5).Build();

            int xp1Before = character1.XP;
            int xp2Before = character2.XP;

            XPRewardSystem.AwardEnemyKillXP(character1, enemy1);
            XPRewardSystem.AwardEnemyKillXP(character2, enemy5);

            int xp1Gained = character1.XP - xp1Before;
            int xp2Gained = character2.XP - xp2Before;

            TestBase.AssertTrue(xp2Gained > xp1Gained,
                $"Higher level enemy should give more XP: Level 1 enemy = {xp1Gained}, Level 5 enemy = {xp2Gained}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPAmountsRelative()
        {
            Console.WriteLine("\n--- Testing XP Amounts Are Relative ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int dungeonLevel = 5;
            
            var tuning = GameConfiguration.Instance;
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0) baseXP = 25;
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0) xpPerLevel = 5;
            
            int baseLevelXP = baseXP + (dungeonLevel * xpPerLevel);
            
            int expectedEnemyXP = baseLevelXP * 1; // 1x
            int expectedRoomXP = (int)(baseLevelXP * 0.5); // 0.5x
            int expectedItemXP = (int)(baseLevelXP * 0.3); // 0.3x
            int expectedDungeonXP = baseLevelXP * 10; // 10x

            // Helper function to calculate total XP gained including level-ups
            int CalculateTotalXPGained(int xpBefore, int levelBefore, int xpAfter, int levelAfter)
            {
                int levelsGained = levelAfter - levelBefore;
                int xpConsumedForLevels = 0;
                for (int i = 0; i < levelsGained; i++)
                {
                    int level = levelBefore + i;
                    int averageXPPerDungeonAtLevel1 = baseXP + 25;
                    if (averageXPPerDungeonAtLevel1 <= 0) averageXPPerDungeonAtLevel1 = 25;
                    // Consistent curve: Level^2 * base for all levels
                    xpConsumedForLevels += level * level * averageXPPerDungeonAtLevel1;
                }
                return (xpAfter - xpBefore) + xpConsumedForLevels;
            }

            // Test enemy XP
            var enemy = TestDataBuilders.Enemy().WithLevel(dungeonLevel).Build();
            int xpBefore = character.XP;
            int levelBefore = character.Level;
            XPRewardSystem.AwardEnemyKillXP(character, enemy);
            int enemyXPGained = CalculateTotalXPGained(xpBefore, levelBefore, character.XP, character.Level);

            // Test room XP
            xpBefore = character.XP;
            levelBefore = character.Level;
            XPRewardSystem.AwardRoomEntryXP(character, dungeonLevel);
            int roomXPGained = CalculateTotalXPGained(xpBefore, levelBefore, character.XP, character.Level);

            // Test item XP
            var item = TestDataBuilders.Item().Build();
            item.Level = dungeonLevel;
            xpBefore = character.XP;
            levelBefore = character.Level;
            XPRewardSystem.AwardItemFoundXP(character, item);
            int itemXPGained = CalculateTotalXPGained(xpBefore, levelBefore, character.XP, character.Level);

            // Test dungeon XP
            xpBefore = character.XP;
            levelBefore = character.Level;
            XPRewardSystem.AwardDungeonCompletionXP(character, dungeonLevel, false);
            int dungeonXPGained = CalculateTotalXPGained(xpBefore, levelBefore, character.XP, character.Level);

            // Verify relative amounts
            TestBase.AssertTrue(roomXPGained < enemyXPGained,
                $"Room XP should be less than enemy XP: {roomXPGained} < {enemyXPGained}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(itemXPGained < roomXPGained,
                $"Item XP should be less than room XP: {itemXPGained} < {roomXPGained}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(dungeonXPGained > enemyXPGained * 5,
                $"Dungeon XP should be much larger than enemy XP: {dungeonXPGained} > {enemyXPGained * 5}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleXPSources()
        {
            Console.WriteLine("\n--- Testing Multiple XP Sources ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int xpBefore = character.XP;

            // Award XP from multiple sources
            var enemy = TestDataBuilders.Enemy().WithLevel(3).Build();
            XPRewardSystem.AwardEnemyKillXP(character, enemy);
            
            XPRewardSystem.AwardRoomEntryXP(character, 3);
            
            var item = TestDataBuilders.Item().Build();
            item.Level = 3;
            XPRewardSystem.AwardItemFoundXP(character, item);

            int totalXPGained = character.XP - xpBefore;

            TestBase.AssertTrue(totalXPGained > 0,
                $"Multiple XP sources should accumulate: Total gained = {totalXPGained}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFirstDungeonLevelUpGuarantee()
        {
            Console.WriteLine("\n--- Testing First Dungeon Level Up Guarantee ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            character.SessionStats.DungeonsCompleted = 0; // First dungeon
            
            int levelBefore = character.Level;

            XPRewardSystem.AwardDungeonCompletionXP(character, 1, true);

            TestBase.AssertTrue(character.Level > levelBefore,
                $"Character should level up from first dungeon: Level {levelBefore} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
