using System;
using System.IO;
using System.Linq;
using RPGGame;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for CharacterSaveManager multi-file support
    /// </summary>
    public static class CharacterSaveManagerMultiFileTests
    {
        public static void RunAllTests()
        {
            TestCharacterIdFilenameGeneration();
            TestMultiFileSaveLoad();
            TestListAllSavedCharacters();
            TestBackwardCompatibility();
            Console.WriteLine("All CharacterSaveManager multi-file tests passed!");
        }

        private static void TestCharacterIdFilenameGeneration()
        {
            var characterId = "TestChar_1_abc12345";
            var filename = CharacterSaveManager.GetCharacterSaveFilename(characterId);
            
            Assert(filename.Contains("character_"), "Filename should contain 'character_' prefix");
            Assert(filename.Contains(characterId), "Filename should contain character ID");
            Assert(filename.Contains("_save.json"), "Filename should end with '_save.json'");
            Assert(filename.Contains("TestChar"), "Filename should contain sanitized character name");
        }

        private static void TestMultiFileSaveLoad()
        {
            // Create test characters
            var character1 = new Character("TestChar1", 1);
            var character2 = new Character("TestChar2", 5);
            
            character1.Stats.Strength = 15;
            character2.Stats.Strength = 25;

            // Save with character IDs
            var id1 = "test_char1_001";
            var id2 = "test_char2_002";
            
            CharacterSaveManager.SaveCharacter(character1, id1);
            CharacterSaveManager.SaveCharacter(character2, id2);

            // Verify files exist
            var filename1 = CharacterSaveManager.GetCharacterSaveFilename(id1);
            var filename2 = CharacterSaveManager.GetCharacterSaveFilename(id2);
            
            Assert(File.Exists(filename1), $"Save file 1 should exist: {filename1}");
            Assert(File.Exists(filename2), $"Save file 2 should exist: {filename2}");

            // Load characters back
            var loaded1 = CharacterSaveManager.LoadCharacterAsync(id1).GetAwaiter().GetResult();
            var loaded2 = CharacterSaveManager.LoadCharacterAsync(id2).GetAwaiter().GetResult();

            Assert(loaded1 != null, "Loaded character 1 should not be null");
            Assert(loaded2 != null, "Loaded character 2 should not be null");
            if (loaded1 != null && loaded2 != null)
            {
                Assert(loaded1.Name == character1.Name, "Character 1 name should match");
                Assert(loaded2.Name == character2.Name, "Character 2 name should match");
                Assert(loaded1.Stats.Strength == 15, "Character 1 stats should match");
                Assert(loaded2.Stats.Strength == 25, "Character 2 stats should match");
            }

            // Cleanup
            try
            {
                if (File.Exists(filename1)) File.Delete(filename1);
                if (File.Exists(filename2)) File.Delete(filename2);
            }
            catch { }
        }

        private static void TestListAllSavedCharacters()
        {
            // Create and save multiple characters
            var character1 = new Character("ListTest1", 1);
            var character2 = new Character("ListTest2", 3);
            var character3 = new Character("ListTest3", 5);

            var id1 = "list_test1_001";
            var id2 = "list_test2_002";
            var id3 = "list_test3_003";

            CharacterSaveManager.SaveCharacter(character1, id1);
            CharacterSaveManager.SaveCharacter(character2, id2);
            CharacterSaveManager.SaveCharacter(character3, id3);

            // List all saved characters
            var savedCharacters = CharacterSaveManager.ListAllSavedCharacters();

            Assert(savedCharacters.Count >= 3, $"Should find at least 3 saved characters, found {savedCharacters.Count}");
            
            var found1 = savedCharacters.Any(c => c.characterName == "ListTest1");
            var found2 = savedCharacters.Any(c => c.characterName == "ListTest2");
            var found3 = savedCharacters.Any(c => c.characterName == "ListTest3");

            Assert(found1, "Should find ListTest1");
            Assert(found2, "Should find ListTest2");
            Assert(found3, "Should find ListTest3");

            // Cleanup
            try
            {
                var filename1 = CharacterSaveManager.GetCharacterSaveFilename(id1);
                var filename2 = CharacterSaveManager.GetCharacterSaveFilename(id2);
                var filename3 = CharacterSaveManager.GetCharacterSaveFilename(id3);
                if (File.Exists(filename1)) File.Delete(filename1);
                if (File.Exists(filename2)) File.Delete(filename2);
                if (File.Exists(filename3)) File.Delete(filename3);
            }
            catch { }
        }

        private static void TestBackwardCompatibility()
        {
            // Test that loading without characterId still works (backward compatibility)
            var character = new Character("LegacyChar", 1);
            character.Stats.Strength = 20;

            // Save without characterId (should use default filename)
            CharacterSaveManager.SaveCharacter(character);

            // Load without characterId
            var loaded = CharacterSaveManager.LoadCharacterAsync().GetAwaiter().GetResult();

            Assert(loaded != null, "Legacy load should work");
            if (loaded != null)
            {
                Assert(loaded.Name == character.Name, "Legacy character name should match");
                Assert(loaded.Stats.Strength == 20, "Legacy character stats should match");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Test failed: {message}");
            }
        }
    }
}

