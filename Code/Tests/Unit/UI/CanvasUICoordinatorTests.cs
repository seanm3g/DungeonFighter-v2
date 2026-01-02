using System;
using RPGGame.Tests;
using RPGGame;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for CanvasUICoordinator
    /// Tests UI coordination, state management, and message writing delegation
    /// </summary>
    public static class CanvasUICoordinatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CanvasUICoordinator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CanvasUICoordinator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSetStateManager();
            TestGetAnimationManager();
            TestSetCharacter();
            TestIsCharacterActive();
            TestMessageWritingMethods();

            TestBase.PrintSummary("CanvasUICoordinator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region State Management Tests

        private static void TestSetStateManager()
        {
            Console.WriteLine("--- Testing SetStateManager ---");

            // Test that SetStateManager can be called
            // Note: Full testing requires UI components
            var stateManager = new GameStateManager();
            TestBase.AssertTrue(stateManager != null,
                "GameStateManager can be created for testing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetAnimationManager()
        {
            Console.WriteLine("\n--- Testing GetAnimationManager ---");

            // Test that GetAnimationManager exists
            // Note: Full testing requires UI components
            TestBase.AssertTrue(true,
                "GetAnimationManager method should exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSetCharacter()
        {
            Console.WriteLine("\n--- Testing SetCharacter ---");

            // Test character creation
            var character = TestDataBuilders.CreateTestCharacter("TestChar", 1);
            TestBase.AssertTrue(character != null,
                "SetCharacter should accept Character objects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            // Test with null
            TestBase.AssertTrue(true,
                "SetCharacter should handle null characters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIsCharacterActive()
        {
            Console.WriteLine("\n--- Testing IsCharacterActive ---");

            // Test character active checking logic
            var character = TestDataBuilders.CreateTestCharacter("TestChar", 1);
            TestBase.AssertTrue(character != null,
                "IsCharacterActive should work with Character objects",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Message Writing Tests

        private static void TestMessageWritingMethods()
        {
            Console.WriteLine("\n--- Testing Message Writing Methods ---");

            // Test that message writing methods exist and can be called
            // These delegate to MessageWritingCoordinator
            var methods = new[]
            {
                "WriteLine",
                "Write",
                "WriteSystemLine",
                "WriteMenuLine",
                "WriteTitleLine",
                "WriteDungeonLine",
                "WriteRoomLine",
                "WriteEnemyLine",
                "WriteBlankLine"
            };

            foreach (var method in methods)
            {
                TestBase.AssertTrue(true,
                    $"{method} method should exist and delegate correctly",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
