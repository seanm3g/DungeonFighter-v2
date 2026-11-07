using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Partial class for Unit Test Tools
    /// </summary>
    public static partial class McpTools
    {
        // ========== Unit Test Tools ==========

        [McpServerTool(Name = "run_combo_dice_roll_tests", Title = "Run Combo Dice Roll Tests")]
        [Description("Runs comprehensive tests for combo sequences and dice rolls. Tests dice mechanics (1-5 fail, 6-13 normal, 14-20 combo), action selection based on rolls, combo sequence information, IsCombo flag behavior, and conditional triggers (OnCombo vs OnHit).")]
        public static Task<string> RunComboDiceRollTests()
        {
            return Tools.TestTools.RunComboDiceRollTests();
        }

        [McpServerTool(Name = "run_action_sequence_tests", Title = "Run Action Sequence Tests")]
        [Description("Runs comprehensive tests for actions and action sequences. Tests action creation, properties, selection, combo sequences, and execution flow.")]
        public static Task<string> RunActionSequenceTests()
        {
            return Tools.TestTools.RunActionSequenceTests();
        }

        [McpServerTool(Name = "run_combat_system_tests", Title = "Run Combat System Tests")]
        [Description("Runs comprehensive tests for combat system. Tests damage calculation, hit/miss determination, status effects, combat flow, multi-hit attacks, and critical hits.")]
        public static Task<string> RunCombatSystemTests()
        {
            return Tools.TestTools.RunCombatSystemTests();
        }

        [McpServerTool(Name = "run_all_unit_tests", Title = "Run All Unit Tests")]
        [Description("Runs all available unit tests including combo dice roll tests, action sequence tests, and combat system tests.")]
        public static Task<string> RunAllUnitTests()
        {
            return Tools.TestTools.RunAllUnitTests();
        }

        [McpServerTool(Name = "run_all_settings_tests", Title = "Run All Settings Tests")]
        [Description("Runs all comprehensive game system tests available in the settings menu.")]
        public static Task<string> RunAllSettingsTests()
        {
            return Tools.TestTools.RunAllSettingsTests();
        }

        [McpServerTool(Name = "run_specific_test", Title = "Run Specific Test")]
        [Description("Runs a specific test by name from the settings menu.")]
        public static Task<string> RunSpecificTest(
            [Description("Name of the test to run")] string testName)
        {
            return Tools.TestTools.RunSpecificTest(testName);
        }

        [McpServerTool(Name = "run_system_tests", Title = "Run System Tests")]
        [Description("Runs tests for a specific system category (Character, Combat, Inventory, etc.).")]
        public static Task<string> RunSystemTests(
            [Description("System category name")] string systemName)
        {
            return Tools.TestTools.RunSystemTests(systemName);
        }

    }
}
