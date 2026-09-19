using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for DungeonSelectionHandler
    /// Tests dungeon selection and display
    /// </summary>
    public static class DungeonSelectionHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonSelectionHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonSelectionHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowDungeonSelection();
            TestHandleMenuInput_ValidDungeon();
            TestHandleMenuInput_ReturnToGameLoop();
            TestHandleMenuInput_InvalidChoice();
            TestHandleMenuInput_NoCharacter();
            TestHandleMenuInput_CustomLevelThenStart();
            TestHandleMenuInput_CustomLevelWaitsForEnter();
            TestHandleMenuInput_CustomLevelSetsDifficultyAnchor();
            TestHandleMenuInput_ResetDifficultyClearsAnchor();

            TestBase.PrintSummary("DungeonSelectionHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            TestBase.AssertNotNull(handler,
                "DungeonSelectionHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowDungeonSelection()
        {
            Console.WriteLine("\n--- Testing ShowDungeonSelection ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            
            // Test that ShowDungeonSelection doesn't crash
            Task.Run(async () => await handler.ShowDungeonSelection()).Wait();
            TestBase.AssertTrue(true,
                "ShowDungeonSelection should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_ValidDungeon()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Valid Dungeon ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            // Add a dungeon to available dungeons
            var dungeon = new Dungeon("Test Dungeon", 1, 1, "Test");
            stateManager.AvailableDungeons.Add(dungeon);
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.StartDungeonEvent += async () => { await Task.CompletedTask; };
            
            // Test valid dungeon selection
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for valid dungeon should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_ReturnToGameLoop()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Return to Game Loop ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.ShowGameLoopEvent += () => { };
            
            // Test return to game loop
            Task.Run(async () => await handler.HandleMenuInput("0")).Wait();
            
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "State should transition to GameLoop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Invalid Choice ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            string? messageReceived = null;
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test invalid choice
            Task.Run(async () => await handler.HandleMenuInput("99")).Wait();
            
            TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("Invalid"),
                "HandleMenuInput should show error for invalid choice",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_NoCharacter()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - No Character ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            stateManager.SetCurrentPlayer(null);
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            
            // Test input with no character (should return early)
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput should complete even with no character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_CustomLevelThenStart()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Custom level flow ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);

            stateManager.AvailableDungeons.Clear();
            stateManager.AvailableDungeons.Add(new Dungeon("A", 1, 1, "Forest", new List<string> { "Goblin" }));
            stateManager.AvailableDungeons.Add(new Dungeon("B", 2, 2, "Forest", new List<string> { "Goblin" }));
            stateManager.AvailableDungeons.Add(new Dungeon("C", 3, 3, "Forest", new List<string> { "Goblin" }));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonCustomLevelMenuName, 1, 1, "Crypt", new List<string> { "Skeleton" }));

            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.StartDungeonEvent += async () => { await Task.CompletedTask; };

            Task.Run(async () =>
            {
                await handler.HandleMenuInput("4");
                await handler.HandleMenuInput("1");
                await handler.HandleMenuInput("2");
                await handler.HandleMenuInput("enter");
            }).Wait();

            TestBase.AssertNotNull(stateManager.CurrentDungeon,
                "Custom level entry should set current dungeon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (stateManager.CurrentDungeon != null)
            {
                TestBase.AssertEqual(12, stateManager.CurrentDungeon.MinLevel,
                    "Dungeon should use the entered level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(stateManager.CurrentDungeon.Name.Contains("12"),
                    "Dungeon name should include the chosen level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHandleMenuInput_CustomLevelSetsDifficultyAnchor()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Custom level sets difficulty anchor ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 5);
            stateManager.SetCurrentPlayer(character);

            stateManager.AvailableDungeons.Clear();
            stateManager.AvailableDungeons.Add(new Dungeon("A", 4, 4, "Forest", new List<string> { "Goblin" }));
            stateManager.AvailableDungeons.Add(new Dungeon("B", 5, 5, "Forest", new List<string> { "Goblin" }));
            stateManager.AvailableDungeons.Add(new Dungeon("C", 6, 6, "Forest", new List<string> { "Goblin" }));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonCustomLevelMenuName, 5, 5, "Crypt", new List<string> { "Skeleton" }));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonResetDifficultyMenuName, 5, 5, string.Empty));

            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.StartDungeonEvent += async () => { await Task.CompletedTask; };

            Task.Run(async () =>
            {
                await handler.HandleMenuInput("4");
                await handler.HandleMenuInput("2");
                await handler.HandleMenuInput("0");
                await handler.HandleMenuInput("enter");
            }).Wait();

            TestBase.AssertTrue(character.DungeonDifficultyAnchorLevel == 20,
                "Confirming a custom level should store it as the difficulty anchor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            dungeonManager.RegenerateDungeons(character, stateManager.AvailableDungeons);
            var offered = stateManager.AvailableDungeons.FindAll(d => !RPGGame.GameConstants.IsDungeonSelectionUtilityOption(d.Name));
            var levels = offered.ConvertAll(d => d.MinLevel);
            levels.Sort();
            TestBase.AssertEqual(19, levels[0], "Next menu easier option should be 19", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(20, levels[1], "Next menu middle option should be 20", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(21, levels[2], "Next menu harder option should be 21", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_ResetDifficultyClearsAnchor()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Reset difficulty ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 5);
            character.DungeonDifficultyAnchorLevel = 20;
            stateManager.SetCurrentPlayer(character);

            stateManager.AvailableDungeons.Clear();
            stateManager.AvailableDungeons.Add(new Dungeon("A", 19, 19, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon("B", 20, 20, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon("C", 21, 21, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonCustomLevelMenuName, 5, 5, "Crypt"));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonResetDifficultyMenuName, 5, 5, string.Empty));

            bool dungeonStarted = false;
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.StartDungeonEvent += async () => { dungeonStarted = true; await Task.CompletedTask; };

            Task.Run(async () => await handler.HandleMenuInput("5")).Wait();

            TestBase.AssertTrue(character.DungeonDifficultyAnchorLevel == null,
                "Reset option should clear the custom difficulty anchor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!dungeonStarted,
                "Reset option should not start a dungeon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(stateManager.CurrentDungeon == null,
                "Reset option should not set the current dungeon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var offered = stateManager.AvailableDungeons.FindAll(d => !RPGGame.GameConstants.IsDungeonSelectionUtilityOption(d.Name));
            var levels = offered.ConvertAll(d => d.MinLevel);
            levels.Sort();
            TestBase.AssertEqual(4, levels[0], "After reset, easier option should be hero level - 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, levels[1], "After reset, middle option should be hero level", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(6, levels[2], "After reset, harder option should be hero level + 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_CustomLevelWaitsForEnter()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Custom level does not submit on first digit ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);

            stateManager.AvailableDungeons.Clear();
            stateManager.AvailableDungeons.Add(new Dungeon("A", 1, 1, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon("B", 2, 2, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon("C", 3, 3, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonCustomLevelMenuName, 1, 1, "Crypt"));

            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.StartDungeonEvent += async () => { await Task.CompletedTask; };

            Task.Run(async () =>
            {
                await handler.HandleMenuInput("4");
                await handler.HandleMenuInput("5");
            }).Wait();

            TestBase.AssertTrue(stateManager.CurrentDungeon == null,
                "Pressing only the first digit of a two-digit level must not start the dungeon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Task.Run(async () =>
            {
                await handler.HandleMenuInput("0");
                await handler.HandleMenuInput("enter");
            }).Wait();

            TestBase.AssertNotNull(stateManager.CurrentDungeon,
                "After typing 50 and Enter, dungeon should start",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (stateManager.CurrentDungeon != null)
            {
                TestBase.AssertEqual(50, stateManager.CurrentDungeon.MinLevel,
                    "Dungeon level should be 50",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
