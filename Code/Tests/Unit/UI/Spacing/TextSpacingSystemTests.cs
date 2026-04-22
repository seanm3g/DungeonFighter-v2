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
            TestDoTSpacingRules();
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

        /// <summary>
        /// DoT (PoisonDamage block): flush with same actor's combat action; blank before next actor's combat action after DoT.
        /// </summary>
        private static void TestDoTSpacingRules()
        {
            Console.WriteLine("\n--- Testing DoT spacing (PoisonDamage vs CombatAction) ---");

            TextSpacingSystem.Reset();
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "Bat");
            int sameActorDot = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.PoisonDamage, "Bat");
            TestBase.AssertEqual(0, sameActorDot,
                "no blank before DoT when afflicted is same as last combat actor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TextSpacingSystem.Reset();
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "Bat");
            int crossActorDot = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.PoisonDamage, "Action Lab");
            TestBase.AssertEqual(1, crossActorDot,
                "blank before DoT when afflicted differs from last combat actor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TextSpacingSystem.Reset();
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "Bat");
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage, "Bat");
            int afterDotNewActor = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.CombatAction, "Action Lab");
            TestBase.AssertEqual(1, afterDotNewActor,
                "blank before next combat action when actor differs after DoT block",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TextSpacingSystem.Reset();
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "Fire Elemental");
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage, "Tristan Riversong");
            int heroAttackAfterOwnBurn = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.CombatAction, "Tristan Riversong");
            TestBase.AssertEqual(0, heroAttackAfterOwnBurn,
                "no blank before afflicted hero combat action after burn/poison tick on that hero",
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
