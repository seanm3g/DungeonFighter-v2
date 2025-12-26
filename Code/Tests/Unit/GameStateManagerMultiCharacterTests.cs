using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for GameStateManager multi-character support
    /// </summary>
    public static class GameStateManagerMultiCharacterTests
    {
        public static void RunAllTests()
        {
            TestCharacterRegistration();
            TestCharacterSwitching();
            TestCharacterContextManagement();
            TestActiveCharacterComputedProperty();
            TestCharacterRemoval();
            Console.WriteLine("All GameStateManager multi-character tests passed!");
        }

        private static void TestCharacterRegistration()
        {
            var stateManager = new GameStateManager();
            var character1 = new Character("TestChar1", 1);
            var character2 = new Character("TestChar2", 5);

            // Test adding first character
            var id1 = stateManager.AddCharacter(character1);
            Assert(id1 != null, "Character ID should not be null");
            Assert(stateManager.GetAllCharacters().Count == 1, "Should have 1 character");
            Assert(stateManager.GetActiveCharacter() == character1, "First character should be active");

            // Test adding second character
            var id2 = stateManager.AddCharacter(character2);
            Assert(id2 != null, "Second character ID should not be null");
            Assert(id1 != id2, "Character IDs should be unique");
            Assert(stateManager.GetAllCharacters().Count == 2, "Should have 2 characters");
            Assert(stateManager.GetActiveCharacter() == character1, "First character should still be active");

            // Test adding duplicate character (should return existing ID)
            var id1Again = stateManager.AddCharacter(character1);
            Assert(id1 == id1Again, "Adding duplicate character should return existing ID");
            Assert(stateManager.GetAllCharacters().Count == 2, "Should still have 2 characters");
        }

        private static void TestCharacterSwitching()
        {
            var stateManager = new GameStateManager();
            var character1 = new Character("Char1", 1);
            var character2 = new Character("Char2", 2);

            var id1 = stateManager.AddCharacter(character1);
            var id2 = stateManager.AddCharacter(character2);

            // Test switching to second character
            bool switched = stateManager.SwitchCharacter(id2);
            Assert(switched, "Switch should succeed");
            Assert(stateManager.GetActiveCharacter() == character2, "Second character should be active");
            Assert(stateManager.GetActiveCharacterId() == id2, "Active character ID should match");

            // Test switching back
            switched = stateManager.SwitchCharacter(id1);
            Assert(switched, "Switch back should succeed");
            Assert(stateManager.GetActiveCharacter() == character1, "First character should be active again");

            // Test switching to invalid ID
            switched = stateManager.SwitchCharacter("invalid_id");
            Assert(!switched, "Switch to invalid ID should fail");
            Assert(stateManager.GetActiveCharacter() == character1, "Active character should not change");
        }

        private static void TestCharacterContextManagement()
        {
            var stateManager = new GameStateManager();
            var character = new Character("TestChar", 1);
            var id = stateManager.AddCharacter(character);

            var context = stateManager.GetActiveCharacterContext();
            Assert(context != null, "Context should not be null");
            if (context != null)
            {
                Assert(context.Character == character, "Context character should match");
                Assert(context.CharacterId == id, "Context ID should match");

                // Test dungeon state in context
                var dungeon = new Dungeon("Test Dungeon", 1, 3, "Forest");
                stateManager.SetCurrentDungeon(dungeon);
                Assert(context.ActiveDungeon == dungeon, "Context should store dungeon");

                // Test room state in context
                var room = new Environment("Test Room", "A test room for context testing", isHostile: true, theme: "Forest");
                stateManager.SetCurrentRoom(room);
                Assert(context.ActiveRoom == room, "Context should store room");

                // Test context clearing
                context.ClearDungeonState();
                Assert(context.ActiveDungeon == null, "Dungeon should be cleared");
                Assert(context.ActiveRoom == null, "Room should be cleared");
            }
        }

        private static void TestActiveCharacterComputedProperty()
        {
            var stateManager = new GameStateManager();
            var character1 = new Character("Char1", 1);
            var character2 = new Character("Char2", 2);

            var id1 = stateManager.AddCharacter(character1);
            var id2 = stateManager.AddCharacter(character2);

            // CurrentPlayer should return active character
            Assert(stateManager.CurrentPlayer == character1, "CurrentPlayer should return active character");

            // Switch and verify CurrentPlayer updates
            stateManager.SwitchCharacter(id2);
            Assert(stateManager.CurrentPlayer == character2, "CurrentPlayer should update after switch");
        }

        private static void TestCharacterRemoval()
        {
            var stateManager = new GameStateManager();
            var character1 = new Character("Char1", 1);
            var character2 = new Character("Char2", 2);

            var id1 = stateManager.AddCharacter(character1);
            var id2 = stateManager.AddCharacter(character2);

            // Remove non-active character
            bool removed = stateManager.RemoveCharacter(id2);
            Assert(removed, "Removal should succeed");
            Assert(stateManager.GetAllCharacters().Count == 1, "Should have 1 character remaining");
            Assert(stateManager.GetActiveCharacter() == character1, "Active character should remain");

            // Remove active character (should switch to remaining)
            var character3 = new Character("Char3", 3);
            var id3 = stateManager.AddCharacter(character3);
            removed = stateManager.RemoveCharacter(id1);
            Assert(removed, "Removal of active character should succeed");
            Assert(stateManager.GetActiveCharacter() == character3, "Should switch to remaining character");

            // Remove last character
            removed = stateManager.RemoveCharacter(id3);
            Assert(removed, "Removal of last character should succeed");
            Assert(stateManager.GetAllCharacters().Count == 0, "Should have no characters");
            Assert(stateManager.GetActiveCharacter() == null, "Active character should be null");
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

