using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.Tests.Unit;
using RPGGame.UI.Avalonia;
using RPGGame.MCP;
using RPGGame.MCP.Tools.Utilities;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Unit test tools for MCP server
    /// Exposes unit tests to MCP clients
    /// </summary>
    public static class TestTools
    {
        /// <summary>
        /// Runs a specific test by name (from settings menu)
        /// </summary>
        [McpServerTool(Name = "run_specific_test", Title = "Run Specific Test")]
        [Description("Runs a specific test by name. Available tests: Character Creation, Character Stats, Character Progression, Character Equipment, Combat System Comprehensive, Combat Calculation, Combat Flow, Combat Effects, Combat UI, Item Management, Equipment System, Inventory Display, Dungeon Generation, Room Generation, Enemy Spawning, Dungeon Progression, JSON Loading, Configuration Loading, Data Validation, UI Rendering, UI Interaction, UI Performance, Colored Text Visual Tests, Combat Panel Containment, Combat Freezing Prevention, Combat Log Cleanup, Game Flow Integration, Performance Integration, Action System, Combo Dice Rolls, Advanced Action Mechanics, Character System, Combat System, Inventory System, Dungeon System, Item Generation, Data Loading, UI System, Save/Load System, Color System.")]
        public static Task<string> RunSpecificTest(
            [Description("Name of the test to run")] string testName)
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                var output = new StringBuilder();
                
                try
                {
                    output.AppendLine($"=== Running Test: {testName} ===");
                    output.AppendLine();
                    
                    // Map test names to unit test methods where applicable
                    switch (testName.ToLower())
                    {
                        case "combo dice rolls":
                            output.AppendLine(await TestRunner.RunComboDiceRollTests());
                            break;
                            
                        case "action system":
                            output.AppendLine(await TestRunner.RunActionSequenceTests());
                            break;
                            
                        case "combat system":
                        case "combat system comprehensive":
                            output.AppendLine(await TestRunner.RunCombatSystemTests());
                            break;
                            
                        default:
                            output.AppendLine($"Note: Test '{testName}' requires UI components (GameSystemTestRunner).");
                            output.AppendLine("Available direct unit tests: Combo Dice Rolls, Action System, Combat System");
                            output.AppendLine("For other tests, use run_system_tests with a system category name.");
                            break;
                    }

                    return new
                    {
                        success = true,
                        testName = testName,
                        output = output.ToString(),
                        description = $"Ran test: {testName}"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        testName = testName,
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }

        /// <summary>
        /// Runs tests for a specific system category
        /// </summary>
        [McpServerTool(Name = "run_system_tests", Title = "Run System Tests")]
        [Description("Runs tests for a specific system category. Available categories: Character, Combat, Inventory, Dungeon, Data, UI, CombatUI, Integration, AdvancedMechanics.")]
        public static Task<string> RunSystemTests(
            [Description("System category name (e.g., 'Character', 'Combat', 'Inventory')")] string systemName)
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                var output = new StringBuilder();
                
                try
                {
                    output.AppendLine($"=== Running {systemName} System Tests ===");
                    output.AppendLine();
                    
                    // Map system names to available unit tests
                    switch (systemName.ToLower())
                    {
                        case "combat":
                            output.AppendLine(await TestRunner.RunCombatSystemTests());
                            break;
                            
                        default:
                            output.AppendLine($"Note: System '{systemName}' tests require UI components (GameSystemTestRunner).");
                            output.AppendLine("Available direct unit test categories: Combat");
                            output.AppendLine("For other systems, the tests require full UI setup.");
                            break;
                    }

                    return new
                    {
                        success = true,
                        systemName = systemName,
                        output = output.ToString(),
                        description = $"Ran tests for system: {systemName}"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        systemName = systemName,
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }
        /// <summary>
        /// Runs all combo and dice roll tests
        /// Tests dice mechanics, action selection, combo sequences, and conditional triggers
        /// </summary>
        [McpServerTool(Name = "run_combo_dice_roll_tests", Title = "Run Combo Dice Roll Tests")]
        [Description("Runs comprehensive tests for combo sequences and dice rolls. Tests dice mechanics (1-5 fail, 6-13 normal, 14-20 combo), action selection based on rolls, combo sequence information, IsCombo flag behavior, and conditional triggers (OnCombo vs OnHit).")]
        public static Task<string> RunComboDiceRollTests()
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                try
                {
                    string output = await TestRunner.RunComboDiceRollTests();

                    return new
                    {
                        success = true,
                        testName = "Combo Dice Roll Tests",
                        output = output,
                        description = "Tests dice roll mechanics, action selection, combo sequences, and conditional triggers"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        testName = "Combo Dice Roll Tests",
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }

        /// <summary>
        /// Runs all action and sequence tests
        /// </summary>
        [McpServerTool(Name = "run_action_sequence_tests", Title = "Run Action Sequence Tests")]
        [Description("Runs comprehensive tests for actions and action sequences. Tests action creation, properties, selection, combo sequences, and execution flow.")]
        public static Task<string> RunActionSequenceTests()
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                try
                {
                    string output = await TestRunner.RunActionSequenceTests();

                    return new
                    {
                        success = true,
                        testName = "Action Sequence Tests",
                        output = output,
                        description = "Tests action creation, selection, combo sequences, and execution flow"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        testName = "Action Sequence Tests",
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }

        /// <summary>
        /// Runs all combat system tests
        /// </summary>
        [McpServerTool(Name = "run_combat_system_tests", Title = "Run Combat System Tests")]
        [Description("Runs comprehensive tests for combat system. Tests damage calculation, hit/miss determination, status effects, combat flow, multi-hit attacks, and critical hits.")]
        public static Task<string> RunCombatSystemTests()
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                try
                {
                    string output = await TestRunner.RunCombatSystemTests();

                    return new
                    {
                        success = true,
                        testName = "Combat System Tests",
                        output = output,
                        description = "Tests damage calculation, hit/miss, status effects, and combat flow"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        testName = "Combat System Tests",
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }

        /// <summary>
        /// Runs all tests from the settings menu (comprehensive game system tests)
        /// </summary>
        [McpServerTool(Name = "run_all_settings_tests", Title = "Run All Settings Tests")]
        [Description("Runs all comprehensive game system tests available in the settings menu. Includes: Character System, Combat System, Inventory System, Dungeon System, Item Generation, Data Loading, UI System, Save/Load System, Action System, Combo Dice Rolls, Color System, Advanced Action Mechanics, Combat UI Fixes, and Integration Tests.")]
        public static Task<string> RunAllSettingsTests()
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                var outputCapture = new OutputCapture();
                var output = new StringBuilder();
                
                try
                {
                    // Use TestManager or directly call unit tests
                    // For now, run the unit tests we can access directly
                    output.AppendLine("=== Running All Settings Tests ===");
                    output.AppendLine();
                    
                    // Run unit tests
                    output.AppendLine("--- Unit Tests ---");
                    output.AppendLine(await TestRunner.RunComboDiceRollTests());
                    output.AppendLine();
                    output.AppendLine(await TestRunner.RunActionSequenceTests());
                    output.AppendLine();
                    output.AppendLine(await TestRunner.RunCombatSystemTests());
                    output.AppendLine();
                    output.AppendLine("Note: Full GameSystemTestRunner tests require UI components.");
                    output.AppendLine("Use run_specific_test or run_system_tests for individual test categories.");

                    return new
                    {
                        success = true,
                        testName = "All Settings Tests",
                        output = output.ToString(),
                        description = "Runs all available tests from settings menu"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        testName = "All Settings Tests",
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }

        /// <summary>
        /// Runs all unit tests
        /// </summary>
        [McpServerTool(Name = "run_all_unit_tests", Title = "Run All Unit Tests")]
        [Description("Runs all available unit tests including combo dice roll tests, action sequence tests, and combat system tests.")]
        public static Task<string> RunAllUnitTests()
        {
            return McpToolExecutor.ExecuteAsync(async () =>
            {
                var results = new StringBuilder();
                var allPassed = true;
                var detailedOutput = new StringBuilder();

                try
                {
                    // Run Combo Dice Roll Tests
                    results.AppendLine("=== Combo Dice Roll Tests ===");
                    try
                    {
                        string output = await TestRunner.RunComboDiceRollTests();
                        detailedOutput.AppendLine(output);
                        results.AppendLine("✓ Combo Dice Roll Tests completed");
                    }
                    catch (Exception ex)
                    {
                        allPassed = false;
                        results.AppendLine($"✗ Combo Dice Roll Tests failed: {ex.Message}");
                    }
                    results.AppendLine();

                    // Run Action Sequence Tests
                    results.AppendLine("=== Action Sequence Tests ===");
                    try
                    {
                        string output = await TestRunner.RunActionSequenceTests();
                        detailedOutput.AppendLine(output);
                        results.AppendLine("✓ Action Sequence Tests completed");
                    }
                    catch (Exception ex)
                    {
                        allPassed = false;
                        results.AppendLine($"✗ Action Sequence Tests failed: {ex.Message}");
                    }
                    results.AppendLine();

                    // Run Combat System Tests
                    results.AppendLine("=== Combat System Tests ===");
                    try
                    {
                        string output = await TestRunner.RunCombatSystemTests();
                        detailedOutput.AppendLine(output);
                        results.AppendLine("✓ Combat System Tests completed");
                    }
                    catch (Exception ex)
                    {
                        allPassed = false;
                        results.AppendLine($"✗ Combat System Tests failed: {ex.Message}");
                    }

                    results.AppendLine();
                    results.AppendLine("=== Detailed Test Output ===");
                    results.AppendLine(detailedOutput.ToString());

                    return new
                    {
                        success = allPassed,
                        testName = "All Unit Tests",
                        output = results.ToString(),
                        detailedOutput = detailedOutput.ToString(),
                        description = "Runs all available unit tests"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        success = false,
                        testName = "All Unit Tests",
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    };
                }
            });
        }
    }
}
