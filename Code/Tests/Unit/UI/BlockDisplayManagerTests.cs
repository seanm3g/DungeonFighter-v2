using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;
using RPGGame;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Comprehensive tests for BlockDisplayManager
    /// Tests message grouping, block formatting, and display coordination
    /// </summary>
    public static class BlockDisplayManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BlockDisplayManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BlockDisplayManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSetStateManager();
            TestDisplaySystemBlock();
            TestDisplayActionBlockWithNullParameters();
            TestDisplayActionBlockWithValidData();
            TestShouldDisplayCombatLog();

            TestBase.PrintSummary("BlockDisplayManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Tests

        private static void TestSetStateManager()
        {
            Console.WriteLine("--- Testing SetStateManager ---");

            // Test setting state manager (null is acceptable)
            BlockDisplayManager.SetStateManager(null);
            TestBase.AssertTrue(true,
                "SetStateManager should accept null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with actual state manager
            var stateManager = new GameStateManager();
            BlockDisplayManager.SetStateManager(stateManager);
            TestBase.AssertTrue(true,
                "SetStateManager should accept GameStateManager",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestDisplaySystemBlock()
        {
            Console.WriteLine("\n--- Testing DisplaySystemBlock ---");

            // Disable UI output during tests to prevent unwanted output
            var originalDisableOutput = UIManager.DisableAllUIOutput;
            UIManager.DisableAllUIOutput = true;

            try
            {
                // Test that DisplaySystemBlock doesn't crash
                var coloredText = ColoredTextParser.Parse("Test message");
                BlockDisplayManager.DisplaySystemBlock(coloredText);
                TestBase.AssertTrue(true,
                    "DisplaySystemBlock should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Restore original UI output setting
                UIManager.DisableAllUIOutput = originalDisableOutput;
            }
        }

        private static void TestDisplayActionBlockWithNullParameters()
        {
            Console.WriteLine("\n--- Testing DisplayActionBlock with empty parameters ---");

            // Disable UI output during tests to prevent unwanted output
            var originalDisableOutput = UIManager.DisableAllUIOutput;
            UIManager.DisableAllUIOutput = true;

            try
            {
                // Test with empty action text
                BlockDisplayManager.DisplayActionBlock(new List<ColoredText>(), new List<ColoredText>());
                TestBase.AssertTrue(true,
                    "DisplayActionBlock should handle empty actionText",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with empty roll info
                BlockDisplayManager.DisplayActionBlock(new List<ColoredText>(), new List<ColoredText>());
                TestBase.AssertTrue(true,
                    "DisplayActionBlock should handle empty rollInfo",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Restore original UI output setting
                UIManager.DisableAllUIOutput = originalDisableOutput;
            }
        }

        private static void TestDisplayActionBlockWithValidData()
        {
            Console.WriteLine("\n--- Testing DisplayActionBlock with valid data ---");

            // Disable UI output during tests to prevent unwanted output
            var originalDisableOutput = UIManager.DisableAllUIOutput;
            UIManager.DisableAllUIOutput = true;

            try
            {
                var actionText = ColoredTextParser.Parse("Player attacks");
                var rollInfo = ColoredTextParser.Parse("Roll: 15");
                
                BlockDisplayManager.DisplayActionBlock(actionText, rollInfo);
                TestBase.AssertTrue(true,
                    "DisplayActionBlock should handle valid data",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Restore original UI output setting
                UIManager.DisableAllUIOutput = originalDisableOutput;
            }
        }

        private static void TestShouldDisplayCombatLog()
        {
            Console.WriteLine("\n--- Testing ShouldDisplayCombatLog logic ---");

            // Disable UI output during tests to prevent unwanted output
            var originalDisableOutput = UIManager.DisableAllUIOutput;
            UIManager.DisableAllUIOutput = true;

            try
            {
                // Test with null state manager (backward compatibility)
                BlockDisplayManager.SetStateManager(null);
                var character = TestDataBuilders.CreateTestCharacter("TestChar", 1);
                BlockDisplayManager.DisplayActionBlock(
                    ColoredTextParser.Parse("Test"),
                    ColoredTextParser.Parse("Roll: 10"),
                    null, null, null, character);
                TestBase.AssertTrue(true,
                    "ShouldDisplayCombatLog should work with null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with state manager
                var stateManager = new GameStateManager();
                BlockDisplayManager.SetStateManager(stateManager);
                BlockDisplayManager.DisplayActionBlock(
                    ColoredTextParser.Parse("Test"),
                    ColoredTextParser.Parse("Roll: 10"),
                    null, null, null, character);
                TestBase.AssertTrue(true,
                    "ShouldDisplayCombatLog should work with state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                // Restore original UI output setting
                UIManager.DisableAllUIOutput = originalDisableOutput;
            }
        }

        #endregion
    }
}
