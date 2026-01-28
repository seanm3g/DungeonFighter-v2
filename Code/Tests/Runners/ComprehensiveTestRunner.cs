using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tests.Unit;
using RPGGame.Tests.Integration;
using RPGGame.Tests.Settings;
using RPGGame.Tests.Runners;
using RPGGame.UI.ColorSystem;

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

            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  COMPREHENSIVE GAME TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

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
            MultiSourceXPRewardTests.RunAllTests();
            Console.WriteLine();

            // Phase 7: Persistence and State
            string phase7 = "PHASE 7: PERSISTENCE AND STATE";
            TestResultCollector.SetCurrentCategory(phase7);
            Console.WriteLine($"\n=== {phase7} ===\n");
            SaveLoadSystemTests.RunAllTests();
            Console.WriteLine();
            GameStateManagementTests.RunAllTests();
            Console.WriteLine();
            LoadCharacterMenuTests.RunAllTests();
            Console.WriteLine();

            // Phase 8: Error Handling
            string phase8 = "PHASE 8: ERROR HANDLING";
            TestResultCollector.SetCurrentCategory(phase8);
            Console.WriteLine($"\n=== {phase8} ===\n");
            ErrorHandlingTests.RunAllTests();
            Console.WriteLine();

            // Phase 9: System-Specific Tests
            string phase9 = "PHASE 9: SYSTEM-SPECIFIC TESTS";
            TestResultCollector.SetCurrentCategory(phase9);
            Console.WriteLine($"\n=== {phase9} ===\n");
            DataSystemTestRunner.RunAllTests();
            Console.WriteLine();
            ItemsSystemTestRunner.RunAllTests();
            Console.WriteLine();
            ActionsSystemTestRunner.RunAllTests();
            Console.WriteLine();
            ConfigSystemTestRunner.RunAllTests();
            Console.WriteLine();
            EntitySystemTestRunner.RunAllTests();
            Console.WriteLine();
            CombatSystemTestRunner.RunAllTests();
            Console.WriteLine();
            WorldSystemTestRunner.RunAllTests();
            Console.WriteLine();
            GameSystemTestRunner.RunAllTests();
            Console.WriteLine();
            UISystemTestRunner.RunAllTests();
            Console.WriteLine();
            MCPSystemTestRunner.RunAllTests();
            Console.WriteLine();

            // Display overall summary
            DisplayOverallSummary();

            Console.WriteLine($"\n{GameConstants.StandardSeparator}");
            Console.WriteLine("  ALL TESTS COMPLETE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");
        }

        /// <summary>
        /// Displays overall test summary with success rate and failed tests grouped by category
        /// </summary>
        private static void DisplayOverallSummary()
        {
            var (total, passed, failed, successRate) = TestResultCollector.GetStatistics();
            var allTestsByCategory = TestResultCollector.GetAllTestsByCategory();
            var failedTestsByCategory = TestResultCollector.GetFailedTestsByCategory();

            Console.WriteLine($"\n{GameConstants.StandardSeparator}");
            Console.WriteLine("  OVERALL TEST SUMMARY");
            Console.WriteLine(GameConstants.StandardSeparator);
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
            }

            // Display passed tests first (grouped by category)
            var passedTestsByCategory = new Dictionary<string, List<TestResultCollector.TestResult>>();
            foreach (var category in allTestsByCategory.Keys)
            {
                var passedTests = allTestsByCategory[category].Where(t => t.Passed).ToList();
                if (passedTests.Count > 0)
                {
                    passedTestsByCategory[category] = passedTests;
                }
            }

            if (passedTestsByCategory.Count > 0)
            {
                Console.WriteLine($"\n{GameConstants.StandardSeparator}");
                Console.WriteLine("  PASSED TESTS BY CATEGORY");
                Console.WriteLine(GameConstants.StandardSeparator);

                foreach (var category in passedTestsByCategory.Keys.OrderBy(k => k))
                {
                    var passedTests = passedTestsByCategory[category];
                    Console.WriteLine($"\n{category}:");
                    foreach (var test in passedTests)
                    {
                        Console.WriteLine($"  ✓ {test.TestName}");
                    }
                }
            }

            // Display failed tests at the bottom (grouped by category)
            if (failedTestsByCategory.Count > 0)
            {
                Console.WriteLine($"\n{GameConstants.StandardSeparator}");
                Console.WriteLine("  FAILED TESTS BY CATEGORY");
                Console.WriteLine(GameConstants.StandardSeparator);

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
        /// Runs spreadsheet import tests
        /// </summary>
        public static void RunSpreadsheetImportTests()
        {
            Console.WriteLine("=== SPREADSHEET IMPORT TESTS ===\n");
            
            RPGGame.Tests.Unit.Data.SpreadsheetImportTests.RunAllTests();
        }

        /// <summary>
        /// Runs action mechanics tests (all mechanics)
        /// </summary>
        public static void RunActionMechanicsTests()
        {
            Console.WriteLine("=== ACTION MECHANICS TESTS (ALL) ===\n");
            
            RPGGame.Tests.Unit.Data.ActionMechanicsTests.RunAllTests();
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

