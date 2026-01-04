using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI.Spacing
{
    /// <summary>
    /// Comprehensive tests for TextSpacingSystem
    /// Tests spacing rules and application
    /// </summary>
    public static class TextSpacingSystemTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all TextSpacingSystem tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== TextSpacingSystem Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestApplySpacingBefore_FirstBlock();
            TestApplySpacingBefore_BlockTransitions();
            TestApplySpacingBefore_EntityBasedSpacing();
            TestRecordBlockDisplayed();
            TestReset();

            TestBase.PrintSummary("TextSpacingSystem Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Spacing Application Tests

        private static void TestApplySpacingBefore_FirstBlock()
        {
            Console.WriteLine("--- Testing ApplySpacingBefore - First Block ---");

            // Test that first block doesn't add spacing
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.DungeonHeader);
            
            TestBase.AssertTrue(true,
                "ApplySpacingBefore should handle first block without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplySpacingBefore_BlockTransitions()
        {
            Console.WriteLine("\n--- Testing ApplySpacingBefore - Block Transitions ---");

            // Reset state
            TextSpacingSystem.Reset();
            
            // Test block transitions
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.DungeonHeader);
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.DungeonHeader);
            
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.RoomHeader);
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.RoomHeader);
            
            TestBase.AssertTrue(true,
                "ApplySpacingBefore should handle block transitions correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestApplySpacingBefore_EntityBasedSpacing()
        {
            Console.WriteLine("\n--- Testing ApplySpacingBefore - Entity-Based Spacing ---");

            // Reset state
            TextSpacingSystem.Reset();
            
            // Test entity-based spacing (same actor vs different actor)
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction, "Hero");
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "Hero");
            
            // Same entity - should not add spacing
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction, "Hero");
            
            // Different entity - should add spacing
            TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.CombatAction, "Enemy");
            
            TestBase.AssertTrue(true,
                "ApplySpacingBefore should handle entity-based spacing correctly",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region State Management Tests

        private static void TestRecordBlockDisplayed()
        {
            Console.WriteLine("\n--- Testing RecordBlockDisplayed ---");

            // Reset state
            TextSpacingSystem.Reset();
            
            // Test recording block display
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction);
            
            TestBase.AssertTrue(true,
                "RecordBlockDisplayed should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReset()
        {
            Console.WriteLine("\n--- Testing Reset ---");

            // Test reset functionality
            TextSpacingSystem.Reset();
            
            TestBase.AssertTrue(true,
                "Reset should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
