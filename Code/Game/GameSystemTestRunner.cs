using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.Tests;
using RPGGame.Tests.Unit;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.Game.TestRunners;
using Avalonia.Media;

namespace RPGGame
{
    /// <summary>
    /// Comprehensive test runner for all game systems
    /// Provides accessible tests through the game's settings menu
    /// </summary>
    public class GameSystemTestRunner
    {
        private readonly CanvasUICoordinator uiCoordinator;
        private readonly List<TestResult> testResults = new List<TestResult>();

        public GameSystemTestRunner(CanvasUICoordinator uiCoordinator)
        {
            this.uiCoordinator = uiCoordinator;
        }

        /// <summary>
        /// Runs all game system tests and returns results
        /// </summary>
        public async Task<List<TestResult>> RunAllTests()
        {
            testResults.Clear();
            
            uiCoordinator.WriteLine("=== COMPREHENSIVE GAME SYSTEM TESTS ===");
            uiCoordinator.WriteLine("Running all system tests...");
            uiCoordinator.WriteBlankLine();
            uiCoordinator.ForceRenderDisplayBuffer(); // Render initial message

            // Core System Tests - Delegated to specialized test runners
            var characterRunner = new CharacterSystemTestRunner(uiCoordinator, testResults);
            await characterRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var combatRunner = new CombatSystemTestRunner(uiCoordinator, testResults);
            await combatRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var inventoryRunner = new InventorySystemTestRunner(uiCoordinator, testResults);
            await inventoryRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var dungeonRunner = new DungeonSystemTestRunner(uiCoordinator, testResults);
            await dungeonRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var itemGenRunner = new RPGGame.Game.TestRunners.ItemGenerationTestRunner(uiCoordinator, testResults);
            await itemGenRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var dataLoadingRunner = new DataLoadingTestRunner(uiCoordinator, testResults);
            await dataLoadingRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var uiRunner = new UISystemTestRunner(uiCoordinator, testResults);
            await uiRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var saveLoadRunner = new SaveLoadTestRunner(uiCoordinator, testResults);
            await saveLoadRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var actionRunner = new ActionSystemTestRunner(uiCoordinator, testResults);
            await actionRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            var colorRunner = new ColorSystemTestRunner(uiCoordinator, testResults);
            await colorRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            // Combat UI Fixes
            var combatUIRunner = new RPGGame.Game.TestRunners.CombatUITestRunner(uiCoordinator, testResults);
            await combatUIRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            // Integration Tests
            var integrationRunner = new IntegrationTestRunner(uiCoordinator, testResults);
            await integrationRunner.RunAllTests();
            uiCoordinator.ForceRenderDisplayBuffer();

            // Note: Analysis Tests (Item Generation Analysis, Tier Distribution, Common Item Modification)
            // are skipped in "Run All Tests" because they:
            // 1. Are very long-running (generate 1000s of items)
            // 2. Use blocking console input that freezes the UI
            // 3. Are better run individually from their sub-menus
            // These tests are available in the "Item Tests" sub-menu

            // Final summary
            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine("=== ALL TESTS COMPLETE ===", UIMessageType.System);
            var passed = testResults.Count(r => r.Passed);
            var total = testResults.Count;
            uiCoordinator.WriteLine($"Passed: {passed}/{total}", UIMessageType.System);
            uiCoordinator.ForceRenderDisplayBuffer();

            return new List<TestResult>(testResults);
        }

        /// <summary>
        /// Runs tests for a specific system category
        /// Delegates to specialized test runners
        /// </summary>
        public async Task<List<TestResult>> RunSystemTests(string systemName)
        {
            testResults.Clear();
            
            uiCoordinator.WriteLine($"=== {systemName.ToUpper()} TESTS ===");
            uiCoordinator.WriteLine($"Running {systemName} tests...");
            uiCoordinator.WriteBlankLine();

            switch (systemName.ToLower())
            {
                case "character":
                    var characterRunner = new CharacterSystemTestRunner(uiCoordinator, testResults);
                    await characterRunner.RunAllTests();
                    break;
                    
                case "combat":
                    var combatRunner = new CombatSystemTestRunner(uiCoordinator, testResults);
                    await combatRunner.RunAllTests();
                    break;
                    
                case "inventory":
                    var inventoryRunner = new InventorySystemTestRunner(uiCoordinator, testResults);
                    await inventoryRunner.RunAllTests();
                    break;
                    
                case "dungeon":
                    var dungeonRunner = new DungeonSystemTestRunner(uiCoordinator, testResults);
                    await dungeonRunner.RunAllTests();
                    break;
                    
                case "data":
                case "data loading":
                    var dataLoadingRunner = new DataLoadingTestRunner(uiCoordinator, testResults);
                    await dataLoadingRunner.RunAllTests();
                    break;
                    
                case "ui":
                case "ui system":
                    var uiRunner = new UISystemTestRunner(uiCoordinator, testResults);
                    await uiRunner.RunAllTests();
                    break;
                    
                case "combatui":
                case "combat ui":
                    var combatUIRunner = new RPGGame.Game.TestRunners.CombatUITestRunner(uiCoordinator, testResults);
                    await combatUIRunner.RunAllTests();
                    break;
                    
                case "integration":
                    var integrationRunner = new IntegrationTestRunner(uiCoordinator, testResults);
                    await integrationRunner.RunAllTests();
                    break;
                    
                case "advancedmechanics":
                case "advanced mechanics":
                case "action system":
                    var actionRunner = new ActionSystemTestRunner(uiCoordinator, testResults);
                    await actionRunner.RunAllTests();
                    break;
                    
                case "color system":
                    var colorRunner = new ColorSystemTestRunner(uiCoordinator, testResults);
                    await colorRunner.RunAllTests();
                    break;
                    
                case "save/load":
                case "save load":
                    var saveLoadRunner = new SaveLoadTestRunner(uiCoordinator, testResults);
                    await saveLoadRunner.RunAllTests();
                    break;
                    
                case "item generation":
                    var itemGenRunner = new RPGGame.Game.TestRunners.ItemGenerationTestRunner(uiCoordinator, testResults);
                    await itemGenRunner.RunAllTests();
                    break;
                    
                case "analysis":
                    var analysisRunner = new AnalysisTestRunner(uiCoordinator, testResults);
                    await analysisRunner.RunAllTests();
                    break;
                    
                default:
                    return new List<TestResult> { new TestResult(systemName, false, "Unknown system category") };
            }

            uiCoordinator.WriteBlankLine();
            uiCoordinator.WriteLine($"=== {systemName.ToUpper()} TESTS COMPLETE ===", UIMessageType.System);
            var passed = testResults.Count(r => r.Passed);
            var total = testResults.Count;
            uiCoordinator.WriteLine($"Passed: {passed}/{total}", UIMessageType.System);
            uiCoordinator.ForceRenderDisplayBuffer();

            return new List<TestResult>(testResults);
        }

        /// <summary>
        /// Runs a specific test by name
        /// Delegates to specialized test runners and filters for the specific test
        /// </summary>
        public async Task<TestResult> RunSpecificTest(string testName)
        {
            uiCoordinator.WriteLine($"=== RUNNING TEST: {testName} ===");
            
            // Create a temporary test results list to capture results from the runner
            var tempResults = new List<TestResult>();
            
            // Determine which test runner handles this test and run it
            // Then filter for the specific test name
            try
            {
                // Character System Tests
                if (testName == "Character Creation" || testName == "Character Stats" || 
                    testName == "Character Progression" || testName == "Character Equipment" ||
                    testName == "Character System")
                {
                    var runner = new CharacterSystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Combat System Tests
                else if (testName == "Combat System Comprehensive" || testName == "Combat Calculation" ||
                         testName == "Combat Flow" || testName == "Combat Effects" || testName == "Combat UI" ||
                         testName == "Combat System")
                {
                    var runner = new CombatSystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Inventory System Tests
                else if (testName == "Item Management" || testName == "Equipment System" ||
                         testName == "Inventory Display" || testName == "Inventory System")
                {
                    var runner = new InventorySystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Dungeon System Tests
                else if (testName == "Dungeon Generation" || testName == "Room Generation" ||
                         testName == "Enemy Spawning" || testName == "Dungeon Progression" ||
                         testName == "Dungeon System")
                {
                    var runner = new DungeonSystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Data Loading Tests
                else if (testName == "JSON Loading" || testName == "Configuration Loading" ||
                         testName == "Data Validation" || testName == "Data Loading")
                {
                    var runner = new DataLoadingTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // UI System Tests
                else if (testName == "UI Rendering" || testName == "UI Interaction" ||
                         testName == "UI Performance" || testName == "UI System" ||
                         testName == "Text System Accuracy" || testName == "Color Palette System" ||
                         testName == "Color Pattern System" || testName == "Color Application" ||
                         testName == "Keyword Coloring" || testName == "Damage & Healing Colors" ||
                         testName == "Rarity Colors" || testName == "Status Effect Colors")
                {
                    var runner = new UISystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Color System Tests
                else if (testName == "Colored Text Visual Tests" || testName == "Color System")
                {
                    var runner = new ColorSystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Combat UI Tests
                else if (testName == "Combat Panel Containment" || testName == "Combat Freezing Prevention" ||
                         testName == "Combat Log Cleanup")
                {
                    var runner = new RPGGame.Game.TestRunners.CombatUITestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Integration Tests
                else if (testName == "Game Flow Integration" || testName == "Performance Integration")
                {
                    var runner = new IntegrationTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Action System Tests
                else if (testName == "Action System" || testName == "Combo Dice Rolls" ||
                         testName == "Advanced Action Mechanics")
                {
                    var runner = new ActionSystemTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Analysis Tests
                else if (testName == "Item Generation Analysis" || testName == "Tier Distribution Verification" ||
                         testName == "Common Item Modification Chance")
                {
                    var runner = new AnalysisTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Save/Load Tests
                else if (testName == "Save/Load System" || testName == "Save Load System")
                {
                    var runner = new SaveLoadTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                // Item Generation Tests
                else if (testName == "Item Generation" || testName == "Item Generation System")
                {
                    var runner = new RPGGame.Game.TestRunners.ItemGenerationTestRunner(uiCoordinator, tempResults);
                    await runner.RunAllTests();
                }
                else
                {
                    return new TestResult(testName, false, "Unknown test name");
                }
                
                // Find and return the specific test result
                var result = tempResults.FirstOrDefault(r => r.TestName == testName);
                if (result != null)
                {
                    testResults.Add(result);
                    return result;
                }
                
                // If test name doesn't match exactly, try case-insensitive match
                result = tempResults.FirstOrDefault(r => 
                    r.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase));
                if (result != null)
                {
                    testResults.Add(result);
                    return result;
                }
                
                // If still not found, return the first result or a failure
                if (tempResults.Count > 0)
                {
                    return tempResults[0];
                }
                
                return new TestResult(testName, false, $"Test '{testName}' not found in runner results");
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, $"Exception running test: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a summary of all test results
        /// </summary>
        public string GetTestSummary()
        {
            if (testResults.Count == 0)
            {
                return "No tests have been run yet.";
            }
            
            var passed = testResults.Count(r => r.Passed);
            var total = testResults.Count;
            
            var summary = $"=== TEST SUMMARY ===\n";
            summary += $"Tests Run: {total}\n";
            summary += $"Passed: {passed}\n";
            summary += $"Failed: {total - passed}\n";
            summary += $"Success Rate: {(passed * 100.0 / total):F1}%\n\n";
            
            foreach (var result in testResults)
            {
                var status = result.Passed ? "✅ PASS" : "❌ FAIL";
                summary += $"{status} {result.TestName}\n";
                if (!string.IsNullOrEmpty(result.Message))
                {
                    summary += $"    {result.Message}\n";
                }
            }
            
            return summary;
        }
    }
}
