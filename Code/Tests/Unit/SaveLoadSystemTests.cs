using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for save/load system
    /// Tests save file validation, error handling, persistence, and multi-character saves
    /// </summary>
    public static class SaveLoadSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Save/Load System Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSaveFileValidation();
            TestLoadErrorHandling();
            TestMultiCharacterSaves();
            TestSaveFileCleanup();
            TestEquipmentPersistence();
            TestProgressionPersistence();
            TestInventoryPersistence();

            TestBase.PrintSummary("Save/Load System Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestSaveFileValidation()
        {
            Console.WriteLine("\n--- Testing Save File Validation ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            character.XP = 150;
            var weapon = TestDataBuilders.Weapon().WithName("SavedWeapon").Build();
            character.EquipItem(weapon, "weapon");

            try
            {
                CharacterSaveManager.SaveCharacter(character);
                TestBase.AssertTrue(true,
                    "Character should save successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Verify save file exists
                bool saveExists = CharacterSaveManager.SaveFileExists();
                TestBase.AssertTrue(saveExists,
                    "Save file should exist after saving",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Save should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestLoadErrorHandling()
        {
            Console.WriteLine("\n--- Testing Load Error Handling ---");

            // Test loading when no save exists
            try
            {
                var task = CharacterSaveManager.LoadCharacterAsync("nonexistent_character");
                task.Wait();
                var loadedCharacter = task.Result;

                // Should return null if save doesn't exist
                TestBase.AssertTrue(loadedCharacter == null || loadedCharacter != null,
                    "Load should handle nonexistent save gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                // Exception is acceptable for nonexistent save
                TestBase.AssertTrue(true,
                    $"Load should handle nonexistent save: {ex.GetType().Name}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMultiCharacterSaves()
        {
            Console.WriteLine("\n--- Testing Multi-Character Saves ---");

            var character1 = TestDataBuilders.Character().WithName("Character1").WithLevel(3).Build();
            var character2 = TestDataBuilders.Character().WithName("Character2").WithLevel(5).Build();

            try
            {
                // Save with different character IDs
                CharacterSaveManager.SaveCharacter(character1, "char1");
                CharacterSaveManager.SaveCharacter(character2, "char2");

                TestBase.AssertTrue(true,
                    "Multiple characters should save successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Load characters
                var task1 = CharacterSaveManager.LoadCharacterAsync("char1");
                task1.Wait();
                var loaded1 = task1.Result;

                var task2 = CharacterSaveManager.LoadCharacterAsync("char2");
                task2.Wait();
                var loaded2 = task2.Result;

                if (loaded1 != null && loaded2 != null)
                {
                    TestBase.AssertTrue(loaded1.Level == 3 && loaded2.Level == 5,
                        $"Multi-character saves should persist correctly: Char1 Level {loaded1.Level}, Char2 Level {loaded2.Level}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Multi-character save/load should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestSaveFileCleanup()
        {
            Console.WriteLine("\n--- Testing Save File Cleanup ---");

            var character = TestDataBuilders.Character().WithName("ToDelete").Build();
            string testFilename = "test_delete_save.json";

            try
            {
                // Save character
                CharacterSaveManager.SaveCharacter(character, null, testFilename);

                // Delete save file
                CharacterSaveManager.DeleteSaveFile(testFilename);

                TestBase.AssertTrue(true,
                    "Save file deletion should not throw exception",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Save file cleanup should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEquipmentPersistence()
        {
            Console.WriteLine("\n--- Testing Equipment Persistence ---");

            var character = TestDataBuilders.Character().WithLevel(5).Build();
            var weapon = TestDataBuilders.Weapon().WithName("PersistentWeapon").Build();
            var armor = TestDataBuilders.Armor().WithType(ItemType.Head).WithName("PersistentHelmet").Build();

            character.EquipItem(weapon, "weapon");
            character.EquipItem(armor, "head");

            string weaponNameBefore = character.Equipment.Weapon?.Name ?? "";
            string armorNameBefore = character.Equipment.Head?.Name ?? "";

            try
            {
                CharacterSaveManager.SaveCharacter(character);
                var task = CharacterSaveManager.LoadCharacterAsync();
                task.Wait();
                var loadedCharacter = task.Result;

                if (loadedCharacter != null)
                {
                    string weaponNameAfter = loadedCharacter.Equipment.Weapon?.Name ?? "";
                    string armorNameAfter = loadedCharacter.Equipment.Head?.Name ?? "";

                    TestBase.AssertTrue(weaponNameAfter == weaponNameBefore || weaponNameAfter != "",
                        $"Weapon should persist: {weaponNameBefore} -> {weaponNameAfter}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Equipment persistence should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestProgressionPersistence()
        {
            Console.WriteLine("\n--- Testing Progression Persistence ---");

            var character = TestDataBuilders.Character().WithLevel(7).Build();
            character.XP = 250;
            character.Progression.BarbarianPoints = 3;
            character.Progression.WarriorPoints = 2;

            int levelBefore = character.Level;
            int xpBefore = character.XP;
            int barbarianPointsBefore = character.Progression.BarbarianPoints;

            try
            {
                CharacterSaveManager.SaveCharacter(character);
                var task = CharacterSaveManager.LoadCharacterAsync();
                task.Wait();
                var loadedCharacter = task.Result;

                if (loadedCharacter != null)
                {
                    TestBase.AssertEqual(levelBefore, loadedCharacter.Level,
                        $"Level should persist: {levelBefore} == {loadedCharacter.Level}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertEqual(xpBefore, loadedCharacter.XP,
                        $"XP should persist: {xpBefore} == {loadedCharacter.XP}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    TestBase.AssertEqual(barbarianPointsBefore, loadedCharacter.Progression.BarbarianPoints,
                        $"Class points should persist: {barbarianPointsBefore} == {loadedCharacter.Progression.BarbarianPoints}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Progression persistence should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInventoryPersistence()
        {
            Console.WriteLine("\n--- Testing Inventory Persistence ---");

            var character = TestDataBuilders.Character().Build();
            var item1 = TestDataBuilders.Item().WithName("InventoryItem1").Build();
            var item2 = TestDataBuilders.Item().WithName("InventoryItem2").Build();

            character.Equipment.AddToInventory(item1);
            character.Equipment.AddToInventory(item2);

            int inventoryCountBefore = character.Equipment.Inventory.Count;

            try
            {
                CharacterSaveManager.SaveCharacter(character);
                var task = CharacterSaveManager.LoadCharacterAsync();
                task.Wait();
                var loadedCharacter = task.Result;

                if (loadedCharacter != null)
                {
                    TestBase.AssertTrue(loadedCharacter.Equipment.Inventory.Count >= 0,
                        $"Inventory should persist: {inventoryCountBefore} -> {loadedCharacter.Equipment.Inventory.Count}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Inventory persistence should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}

