using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;
using RPGGame.Tests.Unit;
using RPGGame.Tests.Integration;

namespace RPGGame.Tests.Runners
{
    /// <summary>
    /// Comprehensive test runner that organizes all test categories and provides execution interfaces
    /// </summary>
    public static class ComprehensiveTestRunner
    {
        /// <summary>
        /// Runs all tests in the comprehensive test suite
        /// </summary>
        public static void RunAllTests()
        {
            // Clear previous results and start fresh
            TestResultCollector.Clear();

            Console.WriteLine("========================================");
            Console.WriteLine("  COMPREHENSIVE GAME TEST SUITE");
            Console.WriteLine("========================================\n");

            // Phase 1: Core Mechanics
            string phase1 = "PHASE 1: CORE MECHANICS";
            TestResultCollector.SetCurrentCategory(phase1);
            Console.WriteLine($"=== {phase1} ===\n");
            ActionSystemTests.RunAllTests();
            Console.WriteLine();
            ActionExecutionFlowTests.RunAllTests();
            Console.WriteLine();
            DiceMechanicsTests.RunAllTests();
            Console.WriteLine();
            CombatOutcomeTests.RunAllTests();
            Console.WriteLine();
            EnemyRollTests.RunAllTests();
            Console.WriteLine();
            DiceRollCategorizationTests.RunAllTests();
            Console.WriteLine();
            RollBonusTests.RunAllTests();
            Console.WriteLine();
            CharacterAttributesTests.RunAllTests();
            Console.WriteLine();

            // Phase 2: Advanced Systems
            string phase2 = "PHASE 2: ADVANCED SYSTEMS";
            TestResultCollector.SetCurrentCategory(phase2);
            Console.WriteLine($"\n=== {phase2} ===\n");
            ComboExecutionTests.RunAllTests();
            Console.WriteLine();
            StatusEffectsTests.RunAllTests();
            Console.WriteLine();
            EnvironmentalActionsTests.RunAllTests();
            Console.WriteLine();
            ConditionalTriggersTests.RunAllTests();
            Console.WriteLine();

            // Phase 3: Display and UI
            string phase3 = "PHASE 3: DISPLAY AND UI";
            TestResultCollector.SetCurrentCategory(phase3);
            Console.WriteLine($"\n=== {phase3} ===\n");
            ColorSystemCoreTests.RunAllTests();
            Console.WriteLine();
            ColorSystemApplicationTests.RunAllTests();
            Console.WriteLine();
            ColorSystemRenderingTests.RunAllTests();
            Console.WriteLine();
            CombatLogDisplayTests.RunAllTests();
            Console.WriteLine();

            // Phase 4: Integration
            string phase4 = "PHASE 4: INTEGRATION";
            TestResultCollector.SetCurrentCategory(phase4);
            Console.WriteLine($"\n=== {phase4} ===\n");
            CombatIntegrationTests.RunAllTests();
            Console.WriteLine();
            SystemInteractionTests.RunAllTests();
            Console.WriteLine();
            GameplayFlowTests.RunAllTests();
            Console.WriteLine();

            // Phase 5: Equipment and Rewards
            string phase5 = "PHASE 5: EQUIPMENT AND REWARDS";
            TestResultCollector.SetCurrentCategory(phase5);
            Console.WriteLine($"\n=== {phase5} ===\n");
            EquipmentSystemTests.RunAllTests();
            Console.WriteLine();
            ItemActionPoolTests.RunAllTests();
            Console.WriteLine();
            DungeonRewardsTests.RunAllTests();
            Console.WriteLine();

            // Phase 6: Progression Systems
            string phase6 = "PHASE 6: PROGRESSION SYSTEMS";
            TestResultCollector.SetCurrentCategory(phase6);
            Console.WriteLine($"\n=== {phase6} ===\n");
            LevelUpSystemTests.RunAllTests();
            Console.WriteLine();
            XPSystemTests.RunAllTests();
            Console.WriteLine();

            // Phase 7: Persistence and State
            string phase7 = "PHASE 7: PERSISTENCE AND STATE";
            TestResultCollector.SetCurrentCategory(phase7);
            Console.WriteLine($"\n=== {phase7} ===\n");
            SaveLoadSystemTests.RunAllTests();
            Console.WriteLine();
            GameStateManagementTests.RunAllTests();
            Console.WriteLine();

            // Phase 8: Error Handling
            string phase8 = "PHASE 8: ERROR HANDLING";
            TestResultCollector.SetCurrentCategory(phase8);
            Console.WriteLine($"\n=== {phase8} ===\n");
            ErrorHandlingTests.RunAllTests();
            Console.WriteLine();

            // Display overall summary
            DisplayOverallSummary();

            Console.WriteLine("\n========================================");
            Console.WriteLine("  ALL TESTS COMPLETE");
            Console.WriteLine("========================================\n");
        }

        /// <summary>
        /// Displays overall test summary with success rate and failed tests grouped by category
        /// </summary>
        private static void DisplayOverallSummary()
        {
            var (total, passed, failed, successRate) = TestResultCollector.GetStatistics();
            var failedTestsByCategory = TestResultCollector.GetFailedTestsByCategory();

            Console.WriteLine("\n========================================");
            Console.WriteLine("  OVERALL TEST SUMMARY");
            Console.WriteLine("========================================");
            Console.WriteLine($"Total Tests: {total}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");
            Console.WriteLine($"Success Rate: {successRate:F1}%");

            if (failed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {failed} test(s) failed");

                // Display failed tests grouped by category
                if (failedTestsByCategory.Count > 0)
                {
                    Console.WriteLine("\n========================================");
                    Console.WriteLine("  FAILED TESTS BY CATEGORY");
                    Console.WriteLine("========================================");

                    foreach (var category in failedTestsByCategory.Keys.OrderBy(k => k))
                    {
                        var failedTests = failedTestsByCategory[category];
                        Console.WriteLine($"\n{category}:");
                        foreach (var test in failedTests)
                        {
                            Console.WriteLine($"  ✗ {test.TestName}");
                            if (!string.IsNullOrEmpty(test.Message))
                            {
                                Console.WriteLine($"    {test.Message}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Runs quick tests (fast unit tests)
        /// </summary>
        public static void RunQuickTests()
        {
            Console.WriteLine("=== QUICK TESTS ===\n");
            
            DiceMechanicsTests.RunAllTests();
            Console.WriteLine();
            CombatOutcomeTests.RunAllTests();
            Console.WriteLine();
            DiceRollCategorizationTests.RunAllTests();
            Console.WriteLine();
            RollBonusTests.RunAllTests();
        }

        /// <summary>
        /// Runs action system tests
        /// </summary>
        public static void RunActionSystemTests()
        {
            Console.WriteLine("=== ACTION SYSTEM TESTS ===\n");
            
            ActionSystemTests.RunAllTests();
            Console.WriteLine();
            ActionExecutionFlowTests.RunAllTests();
        }

        /// <summary>
        /// Runs dice mechanics tests
        /// </summary>
        public static void RunDiceMechanicsTests()
        {
            Console.WriteLine("=== DICE MECHANICS TESTS ===\n");
            
            DiceMechanicsTests.RunAllTests();
            Console.WriteLine();
            CombatOutcomeTests.RunAllTests();
            Console.WriteLine();
            DiceRollCategorizationTests.RunAllTests();
            Console.WriteLine();
            RollBonusTests.RunAllTests();
        }

        /// <summary>
        /// Runs combo system tests
        /// </summary>
        public static void RunComboSystemTests()
        {
            Console.WriteLine("=== COMBO SYSTEM TESTS ===\n");
            
            ComboExecutionTests.RunAllTests();
        }

        /// <summary>
        /// Runs color system tests
        /// </summary>
        public static void RunColorSystemTests()
        {
            Console.WriteLine("=== COLOR SYSTEM TESTS ===\n");
            
            ColorSystemCoreTests.RunAllTests();
            Console.WriteLine();
            ColorSystemApplicationTests.RunAllTests();
            Console.WriteLine();
            ColorSystemRenderingTests.RunAllTests();
        }

        /// <summary>
        /// Runs display system tests
        /// </summary>
        public static void RunDisplaySystemTests()
        {
            Console.WriteLine("=== DISPLAY SYSTEM TESTS ===\n");
            
            CombatLogDisplayTests.RunAllTests();
        }

        /// <summary>
        /// Runs character system tests
        /// </summary>
        public static void RunCharacterSystemTests()
        {
            Console.WriteLine("=== CHARACTER SYSTEM TESTS ===\n");
            
            CharacterAttributesTests.RunAllTests();
        }

        /// <summary>
        /// Runs status effects tests
        /// </summary>
        public static void RunStatusEffectsTests()
        {
            Console.WriteLine("=== STATUS EFFECTS TESTS ===\n");
            
            StatusEffectsTests.RunAllTests();
        }

        /// <summary>
        /// Runs integration tests
        /// </summary>
        public static void RunIntegrationTests()
        {
            Console.WriteLine("=== INTEGRATION TESTS ===\n");
            
            CombatIntegrationTests.RunAllTests();
            Console.WriteLine();
            SystemInteractionTests.RunAllTests();
        }

        /// <summary>
        /// Runs combat-specific integration tests
        /// </summary>
        public static void RunCombatTests()
        {
            Console.WriteLine("=== COMBAT TESTS ===\n");
            
            CombatIntegrationTests.RunAllTests();
        }

        /// <summary>
        /// Runs dungeon-specific integration tests
        /// </summary>
        public static void RunDungeonTests()
        {
            Console.WriteLine("=== DUNGEON TESTS ===\n");
            
            DungeonIntegrationTests.RunAllTests();
        }

        /// <summary>
        /// Runs action block display tests
        /// </summary>
        public static void RunActionBlockTests()
        {
            Console.WriteLine("=== ACTION BLOCK TESTS ===\n");
            
            ActionBlockTests.RunAllTests();
        }

        /// <summary>
        /// Runs comprehensive dice roll mechanics tests
        /// </summary>
        public static void RunDiceRollMechanicsTests()
        {
            Console.WriteLine("=== DICE ROLL MECHANICS TESTS ===\n");
            
            DiceRollMechanicsTests.RunAllTests();
        }

        /// <summary>
        /// Runs dungeon and enemy generation tests
        /// </summary>
        public static void RunDungeonEnemyGenerationTests()
        {
            Console.WriteLine("=== DUNGEON AND ENEMY GENERATION TESTS ===\n");
            
            DungeonEnemyGenerationTests.RunAllTests();
        }

        /// <summary>
        /// Runs action execution tests
        /// </summary>
        public static void RunActionExecutionTests()
        {
            Console.WriteLine("=== ACTION EXECUTION TESTS ===\n");
            
            ActionExecutionTests.RunAllTests();
        }

        /// <summary>
        /// Runs multi-hit tests
        /// </summary>
        public static void RunMultiHitTests()
        {
            Console.WriteLine("=== MULTI-HIT TESTS ===\n");
            
            MultiHitTests.RunAllTests();
        }
    }
}

