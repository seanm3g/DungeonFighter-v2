using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tests.Unit;
using RPGGame.Tests.Unit.UI;
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
        /// <summary>Suites that threw during the last <see cref="RunAllTests"/> (name: exception message).</summary>
        public static List<string> CrashedSuites { get; } = new();

        /// <summary>
        /// Runs one suite; an exception is recorded as a crash and the run continues, so one broken suite cannot hide
        /// every suite after it.
        /// </summary>
        private static void RunSuiteSafely(string suiteName, System.Action run)
        {
            try
            {
                run();
            }
            catch (Exception ex)
            {
                CrashedSuites.Add($"{suiteName}: {ex.GetType().Name}: {ex.Message}");
                TestResultCollector.RecordTest(suiteName + " (crashed)", false, ex.Message);
                Console.WriteLine($"  !! SUITE CRASHED: {suiteName}: {ex.GetType().Name}: {ex.Message}");
                // Where it crashed (top frames), so the suite can be fixed without a debugger.
                foreach (string frame in (ex.StackTrace ?? "").Split('\n').Take(6))
                    Console.WriteLine("       " + frame.Trim());
            }
        }

        /// <summary>Process exit code for CI/tools: 0 when every recorded test passed and no suite crashed.</summary>
        public static int ExitCode()
        {
            var (_, _, failed, _) = TestResultCollector.GetStatistics();
            return failed == 0 && CrashedSuites.Count == 0 ? 0 : 1;
        }

        public static void RunAllTests()
        {
            // Clear previous results and start fresh
            TestResultCollector.Clear();
            CrashedSuites.Clear();
            TestBase.ClearBalancePacingForTests();

            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  COMPREHENSIVE GAME TEST SUITE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            // Phase 1: Core Mechanics
            string phase1 = "PHASE 1: CORE MECHANICS";
            TestResultCollector.SetCurrentCategory(phase1);
            Console.WriteLine($"=== {phase1} ===\n");
            RunSuiteSafely("ActionSystemTests", () => ActionSystemTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionExecutionFlowTests", () => ActionExecutionFlowTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.CombatSequenceBuilderTests", () => RPGGame.Tests.Unit.Combat.CombatSequenceBuilderTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.CombatSequencePresenterTests", () => RPGGame.Tests.Unit.Combat.CombatSequencePresenterTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.CombatSequenceFlavorPresenterTests", () => RPGGame.Tests.Unit.Combat.CombatSequenceFlavorPresenterTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.ActionTextEffectCatalogTests", () => RPGGame.Tests.Unit.Combat.ActionTextEffectCatalogTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.ActionTextEmphasisTypewriterTests", () => RPGGame.Tests.Unit.Combat.ActionTextEmphasisTypewriterTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.UI.TextEmphasisPulseFieldTests", () => RPGGame.Tests.Unit.UI.TextEmphasisPulseFieldTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.UI.EmphasisExplosionGifTests", () => RPGGame.Tests.Unit.UI.EmphasisExplosionGifTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.UI.ActionTextEmphasisWordLocatorTests", () => RPGGame.Tests.Unit.UI.ActionTextEmphasisWordLocatorTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.UI.ActionTextEmphasisTriggerTests", () => RPGGame.Tests.Unit.UI.ActionTextEmphasisTriggerTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionEffectTargetTests", () => ActionEffectTargetTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionSequenceTests", () => ActionSequenceTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionInteractionLabTests", () => ActionInteractionLabTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionLabWindowPlacementTests", () => ActionLabWindowPlacementTests.RunAllTests());
            RunSuiteSafely("FlavorTextWindowPlacementTests", () => FlavorTextWindowPlacementTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("MainWindowStartupSizingTests", () => MainWindowStartupSizingTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionLabEncounterSimulatorTests", () => ActionLabEncounterSimulatorTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("DiceMechanicsTests", () => DiceMechanicsTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("CombatOutcomeTests", () => CombatOutcomeTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RPGGame.Tests.Unit.Data.ActionBonusMechanicsTests", () => RPGGame.Tests.Unit.Data.ActionBonusMechanicsTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("EnemyRollTests", () => EnemyRollTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("DiceRollCategorizationTests", () => DiceRollCategorizationTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("RollBonusTests", () => RollBonusTests.RunAllTests());
            Console.WriteLine();
            ManagerAndCalculatorTestRunner.RunCalculatorTests();
            Console.WriteLine();
            RunSuiteSafely("CharacterAttributesTests", () => CharacterAttributesTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("CharacterProgressionClassReferenceTests", () => CharacterProgressionClassReferenceTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("SkillTreeProgressionTests", () => SkillTreeProgressionTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("SkillEffectRankScalingTests", () => SkillEffectRankScalingTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("AttributeClassNameComposerTests", () => AttributeClassNameComposerTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ClassPresentationConfigTests", () => ClassPresentationConfigTests.RunAllTests());
            Console.WriteLine();

            // Phase 2: Advanced Systems
            string phase2 = "PHASE 2: ADVANCED SYSTEMS";
            TestResultCollector.SetCurrentCategory(phase2);
            Console.WriteLine($"\n=== {phase2} ===\n");
            RunSuiteSafely("ComboAmplifierCurveTests", () => ComboAmplifierCurveTests.RunAllTests());
            RunSuiteSafely("ComboExecutionTests", () => ComboExecutionTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("StatusEffectsTests", () => StatusEffectsTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("EnvironmentalActionsTests", () => EnvironmentalActionsTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ConditionalTriggersTests", () => ConditionalTriggersTests.RunAllTests());
            RunSuiteSafely("ActionTriggerGateTests", () => ActionTriggerGateTests.RunAllTests());
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.MaterialSetControllerTests", () => RPGGame.Tests.Unit.Combat.MaterialSetControllerTests.RunAllTests());
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.LeatherSetBonusTests", () => RPGGame.Tests.Unit.Combat.LeatherSetBonusTests.RunAllTests());
            RunSuiteSafely("RPGGame.Tests.Unit.Combat.CharmAndAnimalLadderTests", () => RPGGame.Tests.Unit.Combat.CharmAndAnimalLadderTests.RunAllTests());
            RunSuiteSafely("ActionTriggerBundleApplicatorTests", () => ActionTriggerBundleApplicatorTests.RunAllTests());
            RunSuiteSafely("StripMutationTests", () => StripMutationTests.RunAllTests());
            RunSuiteSafely("RetriggerTests", () => RetriggerTests.RunAllTests());
            RunSuiteSafely("RollProbabilityContentTests", () => RollProbabilityContentTests.RunAllTests());
            RunSuiteSafely("WeaponModTriggerBridgeTests", () => WeaponModTriggerBridgeTests.RunAllTests());
            RunSuiteSafely("EquippedItemTriggerTests", () => EquippedItemTriggerTests.RunAllTests());
            RunSuiteSafely("StatBonusAnimalSuffixTriggerTests", () => StatBonusAnimalSuffixTriggerTests.RunAllTests());
            RunSuiteSafely("ItemTriggerCombatIntegrationTests", () => ItemTriggerCombatIntegrationTests.RunAllTests());
            RunSuiteSafely("ItemTriggerScenarioTests", () => ItemTriggerScenarioTests.RunAllTests());
            Console.WriteLine();

            // Phase 3: Display and UI
            string phase3 = "PHASE 3: DISPLAY AND UI";
            TestResultCollector.SetCurrentCategory(phase3);
            Console.WriteLine($"\n=== {phase3} ===\n");
            RunSuiteSafely("ColorSystemCoreTests", () => ColorSystemCoreTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ColorSystemApplicationTests", () => ColorSystemApplicationTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ColorSystemRenderingTests", () => ColorSystemRenderingTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("CombatLogDisplayTests", () => CombatLogDisplayTests.RunAllTests());
            Console.WriteLine();

            // Phase 4: Integration
            string phase4 = "PHASE 4: INTEGRATION";
            TestResultCollector.SetCurrentCategory(phase4);
            Console.WriteLine($"\n=== {phase4} ===\n");
            RunSuiteSafely("CombatIntegrationTests", () => CombatIntegrationTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("SystemInteractionTests", () => SystemInteractionTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("GameplayFlowTests", () => GameplayFlowTests.RunAllTests());
            Console.WriteLine();

            // Phase 5: Equipment and Rewards
            string phase5 = "PHASE 5: EQUIPMENT AND REWARDS";
            TestResultCollector.SetCurrentCategory(phase5);
            Console.WriteLine($"\n=== {phase5} ===\n");
            RunSuiteSafely("EquipmentSystemTests", () => EquipmentSystemTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("GearActionNamesTests", () => GearActionNamesTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ItemActionPoolTests", () => ItemActionPoolTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("DungeonRewardsTests", () => DungeonRewardsTests.RunAllTests());
            Console.WriteLine();

            // Phase 6: Progression Systems
            string phase6 = "PHASE 6: PROGRESSION SYSTEMS";
            TestResultCollector.SetCurrentCategory(phase6);
            Console.WriteLine($"\n=== {phase6} ===\n");
            RunSuiteSafely("LevelUpSystemTests", () => LevelUpSystemTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("XPSystemTests", () => XPSystemTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("MultiSourceXPRewardTests", () => MultiSourceXPRewardTests.RunAllTests());
            Console.WriteLine();

            // Phase 7: Persistence and State
            string phase7 = "PHASE 7: PERSISTENCE AND STATE";
            TestResultCollector.SetCurrentCategory(phase7);
            Console.WriteLine($"\n=== {phase7} ===\n");
            RunSuiteSafely("SaveLoadSystemTests", () => SaveLoadSystemTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("GameStateManagementTests", () => GameStateManagementTests.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("LoadCharacterMenuTests", () => LoadCharacterMenuTests.RunAllTests());
            Console.WriteLine();

            // Phase 8: Error Handling
            string phase8 = "PHASE 8: ERROR HANDLING";
            TestResultCollector.SetCurrentCategory(phase8);
            Console.WriteLine($"\n=== {phase8} ===\n");
            RunSuiteSafely("ErrorHandlingTests", () => ErrorHandlingTests.RunAllTests());
            Console.WriteLine();

            // Phase 9: System-Specific Tests
            string phase9 = "PHASE 9: SYSTEM-SPECIFIC TESTS";
            TestResultCollector.SetCurrentCategory(phase9);
            Console.WriteLine($"\n=== {phase9} ===\n");
            RunSuiteSafely("DataSystemTestRunner", () => DataSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ItemsSystemTestRunner", () => ItemsSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ActionsSystemTestRunner", () => ActionsSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("ConfigSystemTestRunner", () => ConfigSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("EntitySystemTestRunner", () => EntitySystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("CombatSystemTestRunner", () => CombatSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("WorldSystemTestRunner", () => WorldSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("GameSystemTestRunner", () => GameSystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("UISystemTestRunner", () => UISystemTestRunner.RunAllTests());
            Console.WriteLine();
            RunSuiteSafely("MCPSystemTestRunner", () => MCPSystemTestRunner.RunAllTests());
            Console.WriteLine();

            // Display overall summary
            DisplayOverallSummary();

            if (CrashedSuites.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"SUITES THAT CRASHED ({CrashedSuites.Count}) - their remaining tests did not run:");
                foreach (var crash in CrashedSuites)
                    Console.WriteLine($"  - {crash}");
            }

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
            Console.WriteLine();
            RPGGame.Tests.Unit.Data.ActionBonusMechanicsTests.RunAllTests();
        }

        /// <summary>
        /// Runs tests that validate Actions Settings menu integration with the game:
        /// modifier queue/consume, action data load/save, rarity/category in loot.
        /// </summary>
        public static void RunActionsSettingsIntegrationTests()
        {
            TestResultCollector.Clear();
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  ACTIONS SETTINGS INTEGRATION TESTS");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            TestResultCollector.SetCurrentCategory("Actions Settings Integration");
            Console.WriteLine("=== Action data load & mechanics ===\n");
            RPGGame.Tests.Unit.Data.SpreadsheetImportTests.RunAllTests();
            Console.WriteLine();
            RPGGame.Tests.Unit.Data.ActionMechanicsTests.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("=== Action execution (modifiers) ===\n");
            ActionExecutionFlowTests.RunAllTests();
            Console.WriteLine();
            ActionEffectTargetTests.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("=== Data & loot (rarity, category) ===\n");
            DataSystemTestRunner.RunAllTests();
            Console.WriteLine();

            Console.WriteLine("=== Actions apply to game (Settings save / refresh pool) ===\n");
            RPGGame.Tests.Unit.UI.SettingsApplyServiceTests.RunAllTests();
            Console.WriteLine();
            RPGGame.Tests.Unit.UI.ActionsTabManagerTests.RunAllTests();
            Console.WriteLine();

            DisplayOverallSummary();
            Console.WriteLine($"\n{GameConstants.StandardSeparator}");
            Console.WriteLine("  ACTIONS SETTINGS INTEGRATION TESTS COMPLETE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");
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
            
            ComboAmplifierCurveTests.RunAllTests();
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
            Console.WriteLine();
            CharacterProgressionClassReferenceTests.RunAllTests();
            Console.WriteLine();
            SkillTreeProgressionTests.RunAllTests();
            Console.WriteLine();
            SkillEffectRankScalingTests.RunAllTests();
            Console.WriteLine();
            AttributeClassNameComposerTests.RunAllTests();
            Console.WriteLine();
            ClassPresentationConfigTests.RunAllTests();
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

        /// <summary>
        /// Runs combat mechanics tests (damage, hit, speed, thresholds, etc.).
        /// Used by the Settings Testing menu; runs CombatSystemTestRunner, not shallow integration only.
        /// </summary>
        public static void RunCombatMechanicsTests()
        {
            Console.WriteLine("=== COMBAT MECHANICS TESTS ===\n");
            CombatSystemTestRunner.RunAllTests();
        }

        /// <summary>
        /// Runs progression tests: level-up, XP, and multi-source XP rewards.
        /// </summary>
        public static void RunProgressionTests()
        {
            Console.WriteLine("=== PROGRESSION TESTS (XP / Level) ===\n");
            LevelUpSystemTests.RunAllTests();
            Console.WriteLine();
            XPSystemTests.RunAllTests();
            Console.WriteLine();
            MultiSourceXPRewardTests.RunAllTests();
        }

        /// <summary>
        /// Runs dungeon and rewards tests: enemy generation and loot/XP rewards.
        /// </summary>
        public static void RunDungeonAndRewardsTests()
        {
            Console.WriteLine("=== DUNGEON & REWARDS TESTS ===\n");
            DungeonEnemyGenerationTests.RunAllTests();
            Console.WriteLine();
            DungeonRewardsTests.RunAllTests();
        }

        /// <summary>
        /// Runs only game mechanics and reliability tests (no loaders, UI, or data-import tests).
        /// Used by the Settings Testing menu "Run all mechanics" button.
        /// </summary>
        public static void RunMechanicsAndReliabilityTests()
        {
            TestResultCollector.Clear();
            Console.WriteLine(GameConstants.StandardSeparator);
            Console.WriteLine("  GAME MECHANICS & RELIABILITY");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");

            TestResultCollector.SetCurrentCategory("Dice & rolls");
            Console.WriteLine("--- Dice & rolls ---\n");
            RunDiceRollMechanicsTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Combat mechanics");
            Console.WriteLine("--- Combat (calculators & thresholds) ---\n");
            RunCombatMechanicsTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Status effects");
            Console.WriteLine("--- Status effects ---\n");
            RunStatusEffectsTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Multi-hit");
            Console.WriteLine("--- Multi-hit ---\n");
            RunMultiHitTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Combo");
            Console.WriteLine("--- Combo ---\n");
            RunComboSystemTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Action mechanics");
            Console.WriteLine("--- Action mechanics ---\n");
            RunActionMechanicsTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Progression");
            Console.WriteLine("--- Progression (XP / level) ---\n");
            RunProgressionTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Dungeon & rewards");
            Console.WriteLine("--- Dungeon & rewards ---\n");
            RunDungeonAndRewardsTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Save/Load");
            Console.WriteLine("--- Save/Load ---\n");
            SaveLoadSystemTests.RunAllTests();
            Console.WriteLine();

            TestResultCollector.SetCurrentCategory("Gameplay flow");
            Console.WriteLine("--- Gameplay flow ---\n");
            GameplayFlowTests.RunAllTests();

            DisplayOverallSummary();
            Console.WriteLine($"\n{GameConstants.StandardSeparator}");
            Console.WriteLine("  MECHANICS & RELIABILITY TESTS COMPLETE");
            Console.WriteLine($"{GameConstants.StandardSeparator}\n");
        }
    }
}

