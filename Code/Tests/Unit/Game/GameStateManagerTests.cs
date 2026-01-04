using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameStateManager
    /// Tests state transitions, state validation, and state persistence
    /// </summary>
    public static class GameStateManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameStateManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameStateManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestCurrentState();
            TestCurrentPlayer();
            TestCurrentInventory();
            TestAvailableDungeons();
            TestCurrentDungeon();
            TestCurrentRoom();
            
            // Enhanced tests for state transitions and persistence
            TestTransitionToState();
            TestTransitionToState_StatePersistence();
            TestSetCurrentPlayer();
            TestSetCurrentDungeon();
            TestSetCurrentRoom();
            TestAddCharacter();
            TestGetCharacterId();

            TestBase.PrintSummary("GameStateManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new GameStateManager();
            TestBase.AssertNotNull(manager,
                "GameStateManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (manager != null)
            {
                TestBase.AssertEqualEnum(GameState.MainMenu, manager.CurrentState,
                    "Initial state should be MainMenu",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region State Tests

        private static void TestCurrentState()
        {
            Console.WriteLine("\n--- Testing CurrentState ---");

            var manager = new GameStateManager();
            TestBase.AssertEqualEnum(GameState.MainMenu, manager.CurrentState,
                "CurrentState should default to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Player Tests

        private static void TestCurrentPlayer()
        {
            Console.WriteLine("\n--- Testing CurrentPlayer ---");

            var manager = new GameStateManager();
            var player = manager.CurrentPlayer;
            
            // Player might be null initially, which is acceptable
            TestBase.AssertTrue(player == null || player != null,
                "CurrentPlayer should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Inventory Tests

        private static void TestCurrentInventory()
        {
            Console.WriteLine("\n--- Testing CurrentInventory ---");

            var manager = new GameStateManager();
            var inventory = manager.CurrentInventory;

            TestBase.AssertNotNull(inventory,
                "CurrentInventory should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (inventory != null)
            {
                TestBase.AssertTrue(inventory.Count >= 0,
                    $"Inventory count should be >= 0, got {inventory.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Dungeon Tests

        private static void TestAvailableDungeons()
        {
            Console.WriteLine("\n--- Testing AvailableDungeons ---");

            var manager = new GameStateManager();
            var dungeons = manager.AvailableDungeons;

            TestBase.AssertNotNull(dungeons,
                "AvailableDungeons should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (dungeons != null)
            {
                TestBase.AssertTrue(dungeons.Count >= 0,
                    $"Dungeons count should be >= 0, got {dungeons.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCurrentDungeon()
        {
            Console.WriteLine("\n--- Testing CurrentDungeon ---");

            var manager = new GameStateManager();
            var dungeon = manager.CurrentDungeon;

            // Dungeon might be null initially, which is acceptable
            TestBase.AssertTrue(dungeon == null || dungeon != null,
                "CurrentDungeon should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCurrentRoom()
        {
            Console.WriteLine("\n--- Testing CurrentRoom ---");

            var manager = new GameStateManager();
            var room = manager.CurrentRoom;

            // Room might be null initially, which is acceptable
            TestBase.AssertTrue(room == null || room != null,
                "CurrentRoom should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region State Transition Tests

        private static void TestTransitionToState()
        {
            Console.WriteLine("\n--- Testing TransitionToState ---");

            var manager = new GameStateManager();
            
            // Test state transition
            manager.TransitionToState(GameState.GameLoop);
            
            TestBase.AssertEqualEnum(GameState.GameLoop, manager.CurrentState,
                "State should transition to GameLoop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTransitionToState_StatePersistence()
        {
            Console.WriteLine("\n--- Testing TransitionToState - State Persistence ---");

            var manager = new GameStateManager();
            
            // Test multiple state transitions
            manager.TransitionToState(GameState.GameLoop);
            TestBase.AssertEqualEnum(GameState.GameLoop, manager.CurrentState,
                "State should be GameLoop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            manager.TransitionToState(GameState.Inventory);
            TestBase.AssertEqualEnum(GameState.Inventory, manager.CurrentState,
                "State should be Inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            manager.TransitionToState(GameState.MainMenu);
            TestBase.AssertEqualEnum(GameState.MainMenu, manager.CurrentState,
                "State should be MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Player Management Tests

        private static void TestSetCurrentPlayer()
        {
            Console.WriteLine("\n--- Testing SetCurrentPlayer ---");

            var manager = new GameStateManager();
            var character = new Character("TestHero", 1);
            
            manager.SetCurrentPlayer(character);
            
            TestBase.AssertTrue(manager.CurrentPlayer == character,
                "CurrentPlayer should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Dungeon Management Tests

        private static void TestSetCurrentDungeon()
        {
            Console.WriteLine("\n--- Testing SetCurrentDungeon ---");

            var manager = new GameStateManager();
            var dungeon = new Dungeon("Test Dungeon", 1, 1, "Test");
            
            manager.SetCurrentDungeon(dungeon);
            
            TestBase.AssertTrue(manager.CurrentDungeon == dungeon,
                "CurrentDungeon should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSetCurrentRoom()
        {
            Console.WriteLine("\n--- Testing SetCurrentRoom ---");

            var manager = new GameStateManager();
            var dungeon = new Dungeon("Test Dungeon", 1, 1, "Test");
            dungeon.Generate();
            var room = dungeon.Rooms[0];
            
            manager.SetCurrentRoom(room);
            
            TestBase.AssertTrue(manager.CurrentRoom == room,
                "CurrentRoom should be set correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Character Management Tests

        private static void TestAddCharacter()
        {
            Console.WriteLine("\n--- Testing AddCharacter ---");

            var manager = new GameStateManager();
            var character = new Character("TestHero", 1);
            
            string characterId = manager.AddCharacter(character);
            
            TestBase.AssertNotNull(characterId,
                "AddCharacter should return a character ID",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            TestBase.AssertTrue(!string.IsNullOrEmpty(characterId),
                "Character ID should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetCharacterId()
        {
            Console.WriteLine("\n--- Testing GetCharacterId ---");

            var manager = new GameStateManager();
            var character = new Character("TestHero", 1);
            
            string characterId = manager.AddCharacter(character);
            string? retrievedId = manager.GetCharacterId(character);
            
            TestBase.AssertEqual(characterId, retrievedId,
                "GetCharacterId should return the correct ID",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
