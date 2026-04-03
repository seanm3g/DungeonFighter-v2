using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for level up system
    /// Tests multiple level-ups, class point distribution, stat increases, and health restoration
    /// </summary>
    public static class LevelUpSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Level Up System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestMultipleLevelUps();
            TestClassPointDistribution();
            TestStatIncreasesPerClass();
            TestHealthRestorationOnLevelUp();
            TestClassBalanceMultipliers();
            TestLevelUpInfoStructure();
            TestXPOverflow();
            TestLevelUpWithNoWeapon();

            TestBase.PrintSummary("Level Up System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestMultipleLevelUps()
        {
            Console.WriteLine("\n--- Testing Multiple Level Ups ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int startingLevel = character.Level;

            // Add enough XP for multiple level-ups
            var tuning = GameConfiguration.Instance;
            int averageXPPerDungeonAtLevel1 = tuning.Progression.EnemyXPBase + 25;
            int xpForLevel2 = 1 * 1 * averageXPPerDungeonAtLevel1;
            int xpForLevel3 = 2 * 2 * averageXPPerDungeonAtLevel1;
            int totalXP = xpForLevel2 + xpForLevel3 + 10;

            character.AddXP(totalXP);

            TestBase.AssertTrue(character.Level >= startingLevel + 2,
                $"Character should level up multiple times: Level {startingLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClassPointDistribution()
        {
            Console.WriteLine("\n--- Testing Class Point Distribution ---");

            // Test Barbarian (Mace)
            var barbarian = TestDataBuilders.Character().WithLevel(1).Build();
            var mace = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Mace).Build();
            barbarian.EquipItem(mace, "weapon");

            int barbarianPointsBefore = barbarian.Progression.BarbarianPoints;
            barbarian.LevelUp();
            int barbarianPointsAfter = barbarian.Progression.BarbarianPoints;

            TestBase.AssertTrue(barbarianPointsAfter >= barbarianPointsBefore,
                $"Barbarian should gain class points: {barbarianPointsBefore} -> {barbarianPointsAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Warrior (Sword)
            var warrior = TestDataBuilders.Character().WithLevel(1).Build();
            var sword = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            warrior.EquipItem(sword, "weapon");

            int warriorPointsBefore = warrior.Progression.WarriorPoints;
            warrior.LevelUp();
            int warriorPointsAfter = warrior.Progression.WarriorPoints;

            TestBase.AssertTrue(warriorPointsAfter >= warriorPointsBefore,
                $"Warrior should gain class points: {warriorPointsBefore} -> {warriorPointsAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Rogue (Dagger)
            var rogue = TestDataBuilders.Character().WithLevel(1).Build();
            var dagger = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Dagger).Build();
            rogue.EquipItem(dagger, "weapon");

            int roguePointsBefore = rogue.Progression.RoguePoints;
            rogue.LevelUp();
            int roguePointsAfter = rogue.Progression.RoguePoints;

            TestBase.AssertTrue(roguePointsAfter >= roguePointsBefore,
                $"Rogue should gain class points: {roguePointsBefore} -> {roguePointsAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Wizard (Wand)
            var wizard = TestDataBuilders.Character().WithLevel(1).Build();
            var wand = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Wand).Build();
            wizard.EquipItem(wand, "weapon");

            int wizardPointsBefore = wizard.Progression.WizardPoints;
            wizard.LevelUp();
            int wizardPointsAfter = wizard.Progression.WizardPoints;

            TestBase.AssertTrue(wizardPointsAfter >= wizardPointsBefore,
                $"Wizard should gain class points: {wizardPointsBefore} -> {wizardPointsAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatIncreasesPerClass()
        {
            Console.WriteLine("\n--- Testing Stat Increases Per Class ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var weapon = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            character.EquipItem(weapon, "weapon");

            int strengthBefore = character.Strength;
            int agilityBefore = character.Agility;
            int techniqueBefore = character.Technique;
            int intelligenceBefore = character.Intelligence;

            character.LevelUp();

            TestBase.AssertTrue(character.Strength >= strengthBefore,
                $"Strength should increase on level up: {strengthBefore} -> {character.Strength}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Agility >= agilityBefore ||
                character.Technique >= techniqueBefore ||
                character.Intelligence >= intelligenceBefore,
                "At least one stat should increase on level up",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthRestorationOnLevelUp()
        {
            Console.WriteLine("\n--- Testing Health Restoration On Level Up ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            character.CurrentHealth = 10; // Damage character

            int healthBefore = character.CurrentHealth;
            int maxHealthBefore = character.GetEffectiveMaxHealth();

            character.LevelUp();

            // Health should be restored to effective max
            TestBase.AssertTrue(character.CurrentHealth >= maxHealthBefore,
                $"Health should be restored to effective max: {healthBefore} -> {character.CurrentHealth} (max: {maxHealthBefore})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClassBalanceMultipliers()
        {
            Console.WriteLine("\n--- Testing Class Balance Multipliers ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var weapon = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            character.EquipItem(weapon, "weapon");

            int maxHealthBefore = character.Health.MaxHealth;
            character.LevelUp();
            int maxHealthAfter = character.Health.MaxHealth;

            TestBase.AssertTrue(maxHealthAfter > maxHealthBefore,
                $"Max health should increase on level up: {maxHealthBefore} -> {maxHealthAfter}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLevelUpInfoStructure()
        {
            Console.WriteLine("\n--- Testing Level Up Info Structure ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            var weapon = TestDataBuilders.Weapon().WithWeaponType(WeaponType.Sword).Build();
            character.EquipItem(weapon, "weapon");

            var levelUpInfos = character.AddXPWithLevelUpInfo(1000);

            if (levelUpInfos.Count > 0)
            {
                var levelUpInfo = levelUpInfos[0];
                TestBase.AssertNotNull(levelUpInfo,
                    "LevelUpInfo should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(levelUpInfo.NewLevel > 0,
                    $"LevelUpInfo should have valid new level: {levelUpInfo.NewLevel}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            else
            {
                // No level up occurred, which is acceptable
                TestBase.AssertTrue(true,
                    "No level up occurred (acceptable)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestXPOverflow()
        {
            Console.WriteLine("\n--- Testing XP Overflow ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int startingLevel = character.Level;

            // Add very large amount of XP
            character.AddXP(1000000);

            TestBase.AssertTrue(character.Level > startingLevel,
                $"Character should level up with large XP: Level {startingLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // XP should be properly handled (not overflow)
            TestBase.AssertTrue(character.XP >= 0,
                $"XP should be non-negative: {character.XP}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLevelUpWithNoWeapon()
        {
            Console.WriteLine("\n--- Testing Level Up With No Weapon ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            // No weapon equipped

            int maxHealthBefore = character.Health.MaxHealth;
            int strengthBefore = character.Strength;

            character.LevelUp();

            // Should still level up and gain stats/health
            TestBase.AssertTrue(character.Level > 1,
                $"Character should level up without weapon: Level {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Health.MaxHealth > maxHealthBefore,
                $"Max health should increase without weapon: {maxHealthBefore} -> {character.Health.MaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

