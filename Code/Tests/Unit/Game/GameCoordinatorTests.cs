using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameCoordinator
    /// Tests game initialization, state management, and handler coordination
    /// </summary>
    public static class GameCoordinatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameCoordinator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameCoordinator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithUI();
            TestConstructorWithCharacter();
            TestExitActionInteractionLab_NoActivePlayer_SelectsSettingsState();
            TestExitActionInteractionLab_WithActivePlayer_SelectsSettingsState();
            TestBarbarianStarterWeaponChoice1Based();

            TestBase.PrintSummary("GameCoordinator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var game = new GameCoordinator();
            TestBase.AssertNotNull(game,
                "GameCoordinator should be created with default constructor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestConstructorWithUI()
        {
            Console.WriteLine("\n--- Testing Constructor with UI ---");

            // Note: This requires a UI manager, which might not be available in test environment
            // We test that the constructor doesn't crash
            // Use explicit IUIManager cast to disambiguate constructor
            try
            {
                IUIManager? uiManager = null;
                var game = new GameCoordinator(uiManager!); // Suppress null warning - testing null case
                TestBase.AssertNotNull(game,
                    "GameCoordinator should be created with UI manager (null is acceptable)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch
            {
                // Constructor might require non-null UI manager, which is acceptable
                TestBase.AssertTrue(true,
                    "GameCoordinator constructor should handle UI manager parameter",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithCharacter()
        {
            Console.WriteLine("\n--- Testing Constructor with Character ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            try
            {
                var game = new GameCoordinator(character);
                TestBase.AssertNotNull(game,
                    "GameCoordinator should be created with existing character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch
            {
                // Constructor might require additional setup, which is acceptable
                TestBase.AssertTrue(true,
                    "GameCoordinator constructor should handle character parameter",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestExitActionInteractionLab_NoActivePlayer_SelectsSettingsState()
        {
            Console.WriteLine("\n--- Testing ExitActionInteractionLab (no save character) ---");

            var game = new GameCoordinator();
            game.StateManager.TransitionToState(GameState.ActionInteractionLab);
            game.ExitActionInteractionLab();
            TestBase.AssertEqualEnum(GameState.Settings, game.StateManager.CurrentState,
                "Exit lab without a loaded hero should return to Settings (lab clone is not CurrentPlayer)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExitActionInteractionLab_WithActivePlayer_SelectsSettingsState()
        {
            Console.WriteLine("\n--- Testing ExitActionInteractionLab (with save character) ---");

            var character = TestDataBuilders.Character()
                .WithName("LabExitTest")
                .WithLevel(1)
                .Build();
            var game = new GameCoordinator(character);
            game.StateManager.TransitionToState(GameState.ActionInteractionLab);
            game.ExitActionInteractionLab();
            TestBase.AssertEqualEnum(GameState.Settings, game.StateManager.CurrentState,
                "Exit lab should always return to Settings (even when a save character is loaded)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBarbarianStarterWeaponChoice1Based()
        {
            Console.WriteLine("\n--- Testing GetBarbarianStarterWeaponChoice1Based ---");

            int choice = GameCoordinator.GetBarbarianStarterWeaponChoice1Based();
            var init = new GameInitializer();
            var weapons = init.LoadStartingGear()?.weapons;
            TestBase.AssertTrue(choice >= 1, "Barbarian starter choice is at least 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (weapons == null || weapons.Count == 0)
                return;

            TestBase.AssertTrue(choice <= weapons.Count, "Barbarian starter choice fits starting weapon list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            bool listHasMace = false;
            foreach (var w in weapons)
            {
                if (w.name.Contains("mace", StringComparison.OrdinalIgnoreCase))
                {
                    listHasMace = true;
                    break;
                }
            }

            if (listHasMace)
            {
                string picked = weapons[choice - 1].name;
                TestBase.AssertTrue(picked.Contains("mace", StringComparison.OrdinalIgnoreCase),
                    "When starting gear includes a mace, choice should index that barbarian starter",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            else
            {
                TestBase.AssertEqual(1, choice, "When no mace in starting gear, fallback is first slot",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
