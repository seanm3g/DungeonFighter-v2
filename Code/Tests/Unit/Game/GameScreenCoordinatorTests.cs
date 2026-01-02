using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameScreenCoordinator
    /// Tests screen transition coordination, state management, and error handling
    /// </summary>
    public static class GameScreenCoordinatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameScreenCoordinator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameScreenCoordinator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowGameLoop();
            TestShowDungeonCompletion();
            TestShowInventory();
            TestShowMainMenu();
            TestShowDeathScreen();
            TestShowDungeonSelection();
            TestShowSettings();
            TestShowCharacterInfo();
            TestShowWeaponSelection();
            TestShowCharacterCreation();

            TestBase.PrintSummary("GameScreenCoordinator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                TestBase.AssertNotNull(coordinator,
                    "GameScreenCoordinator should be created",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test null stateManager
                try
                {
                    var coordinator2 = new GameScreenCoordinator(null!);
                    TestBase.AssertTrue(false,
                        "Constructor should throw ArgumentNullException for null stateManager",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                catch (ArgumentNullException)
                {
                    TestBase.AssertTrue(true,
                        "Constructor should throw ArgumentNullException for null stateManager",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Constructor test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowGameLoop Tests

        private static void TestShowGameLoop()
        {
            Console.WriteLine("\n--- Testing ShowGameLoop ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with no player (should handle gracefully)
                coordinator.ShowGameLoop();
                
                TestBase.AssertTrue(true,
                    "ShowGameLoop should handle missing player gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with player
                var player = TestDataBuilders.Character()
                    .WithName("TestPlayer")
                    .WithStats(10, 10, 10, 10)
                    .Build();
                
                stateManager.SetCurrentPlayer(player);
                coordinator.ShowGameLoop();

                TestBase.AssertTrue(true,
                    "ShowGameLoop should execute with player",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowGameLoop test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowDungeonCompletion Tests

        private static void TestShowDungeonCompletion()
        {
            Console.WriteLine("\n--- Testing ShowDungeonCompletion ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with null parameters (should handle gracefully)
                coordinator.ShowDungeonCompletion(0, null, new List<LevelUpInfo>(), new List<Item>());
                
                TestBase.AssertTrue(true,
                    "ShowDungeonCompletion should handle null parameters",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowDungeonCompletion test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowInventory Tests

        private static void TestShowInventory()
        {
            Console.WriteLine("\n--- Testing ShowInventory ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with no player (should handle gracefully)
                coordinator.ShowInventory();
                
                TestBase.AssertTrue(true,
                    "ShowInventory should handle missing player gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowInventory test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowMainMenu Tests

        private static void TestShowMainMenu()
        {
            Console.WriteLine("\n--- Testing ShowMainMenu ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with default parameters
                coordinator.ShowMainMenu();
                
                TestBase.AssertTrue(true,
                    "ShowMainMenu should execute with default parameters",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with saved game info
                coordinator.ShowMainMenu(true, "TestCharacter", 5);
                
                TestBase.AssertTrue(true,
                    "ShowMainMenu should execute with saved game info",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowMainMenu test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowDeathScreen Tests

        private static void TestShowDeathScreen()
        {
            Console.WriteLine("\n--- Testing ShowDeathScreen ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                var player = TestDataBuilders.Character()
                    .WithName("TestPlayer")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                // Test with player
                coordinator.ShowDeathScreen(player);
                
                TestBase.AssertTrue(true,
                    "ShowDeathScreen should execute with player",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with null player (should handle gracefully)
                coordinator.ShowDeathScreen(null!);
                
                TestBase.AssertTrue(true,
                    "ShowDeathScreen should handle null player gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowDeathScreen test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowDungeonSelection Tests

        private static void TestShowDungeonSelection()
        {
            Console.WriteLine("\n--- Testing ShowDungeonSelection ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with no player/dungeons (should handle gracefully)
                coordinator.ShowDungeonSelection();
                
                TestBase.AssertTrue(true,
                    "ShowDungeonSelection should handle missing data gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowDungeonSelection test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowSettings Tests

        private static void TestShowSettings()
        {
            Console.WriteLine("\n--- Testing ShowSettings ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                coordinator.ShowSettings();
                
                TestBase.AssertTrue(true,
                    "ShowSettings should execute",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowSettings test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowCharacterInfo Tests

        private static void TestShowCharacterInfo()
        {
            Console.WriteLine("\n--- Testing ShowCharacterInfo ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with no player (should handle gracefully)
                coordinator.ShowCharacterInfo();
                
                TestBase.AssertTrue(true,
                    "ShowCharacterInfo should handle missing player gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowCharacterInfo test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowWeaponSelection Tests

        private static void TestShowWeaponSelection()
        {
            Console.WriteLine("\n--- Testing ShowWeaponSelection ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                // Test with null weapons (should handle gracefully)
                coordinator.ShowWeaponSelection(null!);
                
                TestBase.AssertTrue(true,
                    "ShowWeaponSelection should handle null weapons gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with empty list
                coordinator.ShowWeaponSelection(new List<StartingWeapon>());
                
                TestBase.AssertTrue(true,
                    "ShowWeaponSelection should handle empty weapons list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowWeaponSelection test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region ShowCharacterCreation Tests

        private static void TestShowCharacterCreation()
        {
            Console.WriteLine("\n--- Testing ShowCharacterCreation ---");

            try
            {
                var stateManager = new GameStateManager();
                var coordinator = new GameScreenCoordinator(stateManager);

                var character = TestDataBuilders.Character()
                    .WithName("NewCharacter")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                // Test with character
                coordinator.ShowCharacterCreation(character);
                
                TestBase.AssertTrue(true,
                    "ShowCharacterCreation should execute with character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with null character (should handle gracefully)
                coordinator.ShowCharacterCreation(null!);
                
                TestBase.AssertTrue(true,
                    "ShowCharacterCreation should handle null character gracefully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowCharacterCreation test failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
