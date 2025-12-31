using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration tests for complete gameplay flows
    /// Tests end-to-end scenarios including dungeon runs, character progression, and state management
    /// </summary>
    public static class GameplayFlowTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Gameplay Flow Integration Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCompleteDungeonRun();
            TestFullCharacterProgression();
            TestEquipmentChangesDuringCombat();
            TestMultipleLevelUpsDuringDungeon();
            TestSaveLoadDuringGameplay();
            TestInventoryManagementFlow();

            TestBase.PrintSummary("Gameplay Flow Integration Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCompleteDungeonRun()
        {
            Console.WriteLine("\n--- Testing Complete Dungeon Run ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int originalLevel = character.Level;
            int originalXP = character.XP;

            // Simulate dungeon completion
            var rewardManager = new RewardManager();
            var task = rewardManager.AwardLootAndXPWithReturnsAsync(
                character,
                character.Equipment.Inventory,
                1,
                "Forest"
            );
            task.Wait();

            var (xpGained, lootReceived, levelUpInfos) = task.Result;

            // Character should have gained XP
            TestBase.AssertTrue(character.XP > originalXP || levelUpInfos.Count > 0,
                $"Character should gain XP or level up: XP {originalXP} -> {character.XP}, Levels: {levelUpInfos.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Character should be healed
            TestBase.AssertTrue(character.CurrentHealth >= character.GetEffectiveMaxHealth() * 0.9,
                $"Character should be healed after dungeon: {character.CurrentHealth}/{character.GetEffectiveMaxHealth()}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFullCharacterProgression()
        {
            Console.WriteLine("\n--- Testing Full Character Progression ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int startingLevel = character.Level;
            int startingStrength = character.Strength;

            // Simulate multiple level-ups
            for (int i = 0; i < 5; i++)
            {
                // Add enough XP to level up
                var tuning = GameConfiguration.Instance;
                int xpNeeded = (int)(Math.Pow(character.Level, 2.2) * (tuning.Progression.EnemyXPBase + 25));
                character.AddXP(xpNeeded + 10);
            }

            TestBase.AssertTrue(character.Level > startingLevel,
                $"Character should level up: Level {startingLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Stats should increase
            TestBase.AssertTrue(character.Strength >= startingStrength,
                $"Stats should increase with level: Strength {startingStrength} -> {character.Strength}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentChangesDuringCombat()
        {
            Console.WriteLine("\n--- Testing Equipment Changes During Combat ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalStrength = character.Strength;

            // Equip weapon during "combat" (simulated)
            var weapon = TestDataBuilders.Weapon()
                .WithName("CombatWeapon")
                .WithStatBonus("STR", 5)
                .Build();

            character.EquipItem(weapon, "weapon");

            int strengthWithWeapon = character.Strength;

            // Unequip during "combat"
            character.UnequipItem("weapon");

            int strengthAfterUnequip = character.Strength;

            TestBase.AssertTrue(strengthWithWeapon > originalStrength,
                $"Strength should increase when equipping: {originalStrength} -> {strengthWithWeapon}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(strengthAfterUnequip < strengthWithWeapon,
                $"Strength should decrease when unequipping: {strengthWithWeapon} -> {strengthAfterUnequip}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleLevelUpsDuringDungeon()
        {
            Console.WriteLine("\n--- Testing Multiple Level Ups During Dungeon ---");

            var character = TestDataBuilders.Character().WithLevel(1).Build();
            int startingLevel = character.Level;

            // Add enough XP for multiple level-ups
            var tuning = GameConfiguration.Instance;
            int largeXPAmount = (int)(Math.Pow(2, 2.2) * (tuning.Progression.EnemyXPBase + 25)) +
                               (int)(Math.Pow(3, 2.2) * (tuning.Progression.EnemyXPBase + 25)) + 50;

            var levelUpInfos = character.AddXPWithLevelUpInfo(largeXPAmount);

            TestBase.AssertTrue(character.Level > startingLevel,
                $"Character should level up multiple times: Level {startingLevel} -> {character.Level}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(levelUpInfos.Count > 0,
                $"Should return level-up information: {levelUpInfos.Count} level-ups",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSaveLoadDuringGameplay()
        {
            Console.WriteLine("\n--- Testing Save/Load During Gameplay ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            character.XP = 100;
            var weapon = TestDataBuilders.Weapon().WithName("SavedWeapon").Build();
            character.EquipItem(weapon, "weapon");

            int levelBefore = character.Level;
            int xpBefore = character.XP;
            string weaponNameBefore = character.Equipment.Weapon?.Name ?? "";

            // Save character
            try
            {
                CharacterSaveManager.SaveCharacter(character);
                TestBase.AssertTrue(true,
                    "Character should save successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Load character (async)
                var loadTask = Character.LoadCharacterAsync();
                loadTask.Wait();
                var loadedCharacter = loadTask.Result;

                if (loadedCharacter != null)
                {
                    TestBase.AssertEqual(levelBefore, loadedCharacter.Level,
                        $"Level should persist: {levelBefore} == {loadedCharacter.Level}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertEqual(xpBefore, loadedCharacter.XP,
                        $"XP should persist: {xpBefore} == {loadedCharacter.XP}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertTrue(false,
                        "Loaded character should not be null",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Save/load should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInventoryManagementFlow()
        {
            Console.WriteLine("\n--- Testing Inventory Management Flow ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var item1 = TestDataBuilders.Item().WithName("Item1").Build();
            var item2 = TestDataBuilders.Item().WithName("Item2").Build();

            // Add items to inventory
            character.Equipment.AddToInventory(item1);
            character.Equipment.AddToInventory(item2);

            TestBase.AssertEqual(2, character.Equipment.Inventory.Count,
                $"Inventory should have 2 items: {character.Equipment.Inventory.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Remove item
            bool removed = character.Equipment.RemoveFromInventory(item1);

            TestBase.AssertTrue(removed,
                "RemoveFromInventory should return true",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(1, character.Equipment.Inventory.Count,
                $"Inventory should have 1 item after removal: {character.Equipment.Inventory.Count}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Equip from inventory
            character.EquipItem(item2, "weapon");

            TestBase.AssertTrue(character.Equipment.Weapon == item2,
                "Item should be equipped from inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

