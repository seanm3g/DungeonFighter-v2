using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Models;
using RPGGame;
using RPGGame.Config;
using RPGGame.Editors;
using System.Text;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP Tools for DungeonFighter game
    /// Each tool is registered with the MCP server using attributes
    /// </summary>
    [McpServerToolType]
    public static class McpTools
    {
        /// <summary>
        /// Sets the game wrapper instance (called by MCP server)
        /// </summary>
        public static void SetGameWrapper(GameWrapper wrapper)
        {
            Tools.McpToolState.GameWrapper = wrapper;
        }

        /// <summary>
        /// Gets the game wrapper instance (for backward compatibility)
        /// </summary>
        internal static GameWrapper? GetGameWrapper()
        {
            return Tools.McpToolState.GameWrapper;
        }

        /// <summary>
        /// Sets the last test result (for backward compatibility)
        /// </summary>
        internal static void SetLastTestResult(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? result)
        {
            Tools.McpToolState.LastTestResult = result;
        }

        /// <summary>
        /// Gets the last test result (for backward compatibility)
        /// </summary>
        internal static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? GetLastTestResult()
        {
            return Tools.McpToolState.LastTestResult;
        }

        /// <summary>
        /// Gets the baseline test result (for backward compatibility)
        /// </summary>
        internal static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? GetBaselineTestResult()
        {
            return Tools.McpToolState.BaselineTestResult;
        }

        /// <summary>
        /// Sets the baseline test result (for backward compatibility)
        /// </summary>
        internal static void SetBaselineTestResult(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? result)
        {
            Tools.McpToolState.BaselineTestResult = result;
        }

        /// <summary>
        /// Gets the variable editor (for backward compatibility)
        /// </summary>
        internal static Editors.VariableEditor GetVariableEditor()
        {
            return Tools.McpToolState.GetVariableEditor();
        }

        // ========== Game Control Tools ==========

        [McpServerTool(Name = "start_new_game", Title = "Start New Game")]
        [Description("Starts a new game instance. Returns the initial game state.")]
        public static Task<string> StartNewGame()
        {
            return Tools.GameControlTools.StartNewGame();
        }

        [McpServerTool(Name = "save_game", Title = "Save Game")]
        [Description("Saves the current game state.")]
        public static Task<string> SaveGame()
        {
            return Tools.GameControlTools.SaveGame();
        }

        // ========== Navigation Tools ==========

        [McpServerTool(Name = "handle_input", Title = "Handle Game Input")]
        [Description("Handles game input (menu selection, combat actions, etc.). Returns updated game state.")]
        public static Task<string> HandleInput(
            [Description("The input to send to the game (e.g., '1', '2', 'attack')")] string input)
        {
            return Tools.NavigationTools.HandleInput(input);
        }

        [McpServerTool(Name = "get_available_actions", Title = "Get Available Actions")]
        [Description("Gets the list of available actions for the current game state.")]
        public static Task<string> GetAvailableActions()
        {
            return Tools.NavigationTools.GetAvailableActions();
        }

        // ========== Information Tools ==========

        [McpServerTool(Name = "get_game_state", Title = "Get Game State")]
        [Description("Gets comprehensive game state snapshot including player, dungeon, room, and combat status.")]
        public static Task<string> GetGameState()
        {
            return Tools.InformationTools.GetGameState();
        }

        [McpServerTool(Name = "get_player_stats", Title = "Get Player Stats")]
        [Description("Gets player character statistics including health, level, XP, attributes, and equipment.")]
        public static Task<string> GetPlayerStats()
        {
            return Tools.InformationTools.GetPlayerStats();
        }

        [McpServerTool(Name = "get_current_dungeon", Title = "Get Current Dungeon")]
        [Description("Gets information about the current dungeon (name, level, theme, rooms).")]
        public static Task<string> GetCurrentDungeon()
        {
            return Tools.InformationTools.GetCurrentDungeon();
        }

        [McpServerTool(Name = "get_inventory", Title = "Get Inventory")]
        [Description("Gets the player's inventory items.")]
        public static Task<string> GetInventory()
        {
            return Tools.InformationTools.GetInventory();
        }

        [McpServerTool(Name = "get_combat_state", Title = "Get Combat State")]
        [Description("Gets current combat information if in combat (current enemy, available actions, turn status).")]
        public static Task<string> GetCombatState()
        {
            return Tools.InformationTools.GetCombatState();
        }

        [McpServerTool(Name = "get_recent_output", Title = "Get Recent Output")]
        [Description("Gets recent game output/messages for AI context.")]
        public static Task<string> GetRecentOutput(
            [Description("Number of messages to retrieve (default: 10, max: 100)")] int count = 10)
        {
            return Tools.InformationTools.GetRecentOutput(count);
        }

        // ========== Dungeon Tools ==========

        [McpServerTool(Name = "get_available_dungeons", Title = "Get Available Dungeons")]
        [Description("Lists all available dungeons that can be entered.")]
        public static Task<string> GetAvailableDungeons()
        {
            return Tools.DungeonTools.GetAvailableDungeons();
        }

        // ========== Simulation Tools ==========

        [McpServerTool(Name = "run_battle_simulation", Title = "Run Battle Simulation")]
        [Description("Runs comprehensive battle simulations testing all weapon types against all enemy types. Returns detailed statistics.")]
        public static Task<string> RunBattleSimulation(
            [Description("Number of battles per weapon-enemy combination (default: 50)")] int battlesPerCombination = 50,
            [Description("Player level for testing (default: 1)")] int playerLevel = 1,
            [Description("Enemy level for testing (default: 1)")] int enemyLevel = 1)
        {
            return Tools.SimulationTools.RunBattleSimulation(battlesPerCombination, playerLevel, enemyLevel);
        }

        [McpServerTool(Name = "run_parallel_battles", Title = "Run Parallel Battles")]
        [Description("Runs parallel battles with custom player and enemy stats. Useful for testing specific stat combinations.")]
        public static Task<string> RunParallelBattles(
            [Description("Player damage stat")] int playerDamage,
            [Description("Player attack speed")] double playerAttackSpeed,
            [Description("Player armor stat")] int playerArmor,
            [Description("Player health stat")] int playerHealth,
            [Description("Enemy damage stat")] int enemyDamage,
            [Description("Enemy attack speed")] double enemyAttackSpeed,
            [Description("Enemy armor stat")] int enemyArmor,
            [Description("Enemy health stat")] int enemyHealth,
            [Description("Number of battles to run (default: 100)")] int numberOfBattles = 100)
        {
            return Tools.SimulationTools.RunParallelBattles(
                playerDamage, playerAttackSpeed, playerArmor, playerHealth,
                enemyDamage, enemyAttackSpeed, enemyArmor, enemyHealth,
                numberOfBattles);
        }

        // ========== Analysis Tools ==========

        [McpServerTool(Name = "analyze_battle_results", Title = "Analyze Battle Results")]
        [Description("Analyzes the most recent battle simulation results and generates a detailed analysis report with issues and recommendations. Run run_battle_simulation first.")]
        public static Task<string> AnalyzeBattleResults()
        {
            return Tools.AnalysisTools.AnalyzeBattleResults();
        }

        [McpServerTool(Name = "validate_balance", Title = "Validate Balance")]
        [Description("Validates balance based on the most recent battle simulation results. Returns validation report with errors and warnings. Run run_battle_simulation first.")]
        public static Task<string> ValidateBalance()
        {
            return Tools.AnalysisTools.ValidateBalance();
        }

        [McpServerTool(Name = "analyze_fun_moments", Title = "Analyze Fun Moments")]
        [Description("Analyzes fun moment data from the most recent battle simulation. Shows which weapons/classes create the most engaging gameplay. Run run_battle_simulation first.")]
        public static Task<string> AnalyzeFunMoments()
        {
            return Tools.AnalysisTools.AnalyzeFunMoments();
        }

        [McpServerTool(Name = "get_fun_moment_summary", Title = "Get Fun Moment Summary")]
        [Description("Gets a detailed summary of fun moments from the most recent battle simulation. Shows breakdown by type and intensity. Run run_battle_simulation first.")]
        public static Task<string> GetFunMomentSummary()
        {
            return Tools.AnalysisTools.GetFunMomentSummary();
        }

        // ========== Balance Adjustment Tools ==========

        [McpServerTool(Name = "adjust_global_enemy_multiplier", Title = "Adjust Global Enemy Multiplier")]
        [Description("Adjusts global enemy multipliers (health, damage, armor, speed). Affects all enemies.")]
        public static Task<string> AdjustGlobalEnemyMultiplier(
            [Description("Multiplier name: 'health', 'damage', 'armor', or 'speed'")] string multiplierName,
            [Description("New multiplier value (e.g., 1.2 for 20% increase)")] double value)
        {
            return Tools.BalanceAdjustmentTools.AdjustGlobalEnemyMultiplier(multiplierName, value);
        }

        [McpServerTool(Name = "adjust_archetype", Title = "Adjust Archetype")]
        [Description("Adjusts archetype stat multipliers (health, strength, agility, technique, intelligence, armor).")]
        public static Task<string> AdjustArchetype(
            [Description("Archetype name (e.g., 'Berserker', 'Tank', 'Assassin')")] string archetypeName,
            [Description("Stat name: 'health', 'strength', 'agility', 'technique', 'intelligence', or 'armor'")] string statName,
            [Description("New stat value")] double value)
        {
            return Tools.BalanceAdjustmentTools.AdjustArchetype(archetypeName, statName, value);
        }

        [McpServerTool(Name = "adjust_player_base_attribute", Title = "Adjust Player Base Attribute")]
        [Description("Adjusts player base attributes (strength, agility, technique, intelligence).")]
        public static Task<string> AdjustPlayerBaseAttribute(
            [Description("Attribute name: 'strength', 'agility', 'technique', or 'intelligence'")] string attributeName,
            [Description("New base attribute value")] int value)
        {
            return Tools.BalanceAdjustmentTools.AdjustPlayerBaseAttribute(attributeName, value);
        }

        [McpServerTool(Name = "adjust_player_base_health", Title = "Adjust Player Base Health")]
        [Description("Adjusts player base health.")]
        public static Task<string> AdjustPlayerBaseHealth(
            [Description("New base health value")] int value)
        {
            return Tools.BalanceAdjustmentTools.AdjustPlayerBaseHealth(value);
        }

        [McpServerTool(Name = "adjust_player_attributes_per_level", Title = "Adjust Player Attributes Per Level")]
        [Description("Adjusts how many attributes the player gains per level.")]
        public static Task<string> AdjustPlayerAttributesPerLevel(
            [Description("New attributes per level value")] int value)
        {
            return Tools.BalanceAdjustmentTools.AdjustPlayerAttributesPerLevel(value);
        }

        [McpServerTool(Name = "adjust_player_health_per_level", Title = "Adjust Player Health Per Level")]
        [Description("Adjusts how much health the player gains per level.")]
        public static Task<string> AdjustPlayerHealthPerLevel(
            [Description("New health per level value")] int value)
        {
            return Tools.BalanceAdjustmentTools.AdjustPlayerHealthPerLevel(value);
        }

        [McpServerTool(Name = "adjust_enemy_baseline_stat", Title = "Adjust Enemy Baseline Stat")]
        [Description("Adjusts enemy baseline stats (health, strength, agility, technique, intelligence, armor). Affects all enemies.")]
        public static Task<string> AdjustEnemyBaselineStat(
            [Description("Stat name: 'health', 'strength', 'agility', 'technique', 'intelligence', or 'armor'")] string statName,
            [Description("New baseline stat value")] double value)
        {
            return Tools.BalanceAdjustmentTools.AdjustEnemyBaselineStat(statName, value);
        }

        [McpServerTool(Name = "adjust_enemy_scaling_per_level", Title = "Adjust Enemy Scaling Per Level")]
        [Description("Adjusts how much enemy stats increase per level (health, attributes, armor).")]
        public static Task<string> AdjustEnemyScalingPerLevel(
            [Description("Scaling type: 'health', 'attributes', or 'armor'")] string statName,
            [Description("New scaling value")] double value)
        {
            return Tools.BalanceAdjustmentTools.AdjustEnemyScalingPerLevel(statName, value);
        }

        [McpServerTool(Name = "adjust_weapon_scaling", Title = "Adjust Weapon Scaling")]
        [Description("Adjusts weapon scaling multipliers.")]
        public static Task<string> AdjustWeaponScaling(
            [Description("Weapon type (e.g., 'Mace', 'Sword', 'Dagger', 'Wand') or 'global' for global multiplier")] string weaponType,
            [Description("Parameter name: 'damage' or 'damageMultiplier'")] string parameter,
            [Description("New value")] double value)
        {
            return Tools.BalanceAdjustmentTools.AdjustWeaponScaling(weaponType, parameter, value);
        }

        [McpServerTool(Name = "apply_preset", Title = "Apply Preset")]
        [Description("Applies a quick preset configuration: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', or 'baseline'.")]
        public static Task<string> ApplyPreset(
            [Description("Preset name: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', or 'baseline'")] string presetName)
        {
            return Tools.BalanceAdjustmentTools.ApplyPreset(presetName);
        }

        [McpServerTool(Name = "save_configuration", Title = "Save Configuration")]
        [Description("Saves the current game configuration to TuningConfig.json.")]
        public static Task<string> SaveConfiguration()
        {
            return Tools.BalanceAdjustmentTools.SaveConfiguration();
        }

        [McpServerTool(Name = "get_current_configuration", Title = "Get Current Configuration")]
        [Description("Gets the current game configuration values (enemy multipliers, archetypes, etc.).")]
        public static Task<string> GetCurrentConfiguration()
        {
            return Tools.BalanceAdjustmentTools.GetCurrentConfiguration();
        }

        // ========== Patch Management Tools ==========

        [McpServerTool(Name = "save_patch", Title = "Save Patch")]
        [Description("Saves the current configuration as a balance patch that can be shared or loaded later.")]
        public static Task<string> SavePatch(
            [Description("Patch name")] string name,
            [Description("Author name")] string author,
            [Description("Patch description")] string description,
            [Description("Patch version (default: '1.0')")] string version = "1.0",
            [Description("Optional tags (comma-separated)")] string? tags = null)
        {
            return Tools.PatchManagementTools.SavePatch(name, author, description, version, tags);
        }

        [McpServerTool(Name = "load_patch", Title = "Load Patch")]
        [Description("Loads and applies a balance patch by patch ID or name.")]
        public static Task<string> LoadPatch(
            [Description("Patch ID or name")] string patchId)
        {
            return Tools.PatchManagementTools.LoadPatch(patchId);
        }

        [McpServerTool(Name = "list_patches", Title = "List Patches")]
        [Description("Lists all available balance patches.")]
        public static Task<string> ListPatches()
        {
            return Tools.PatchManagementTools.ListPatches();
        }

        [McpServerTool(Name = "get_patch_info", Title = "Get Patch Info")]
        [Description("Gets detailed information about a specific patch.")]
        public static Task<string> GetPatchInfo(
            [Description("Patch ID or name")] string patchId)
        {
            return Tools.PatchManagementTools.GetPatchInfo(patchId);
        }

        // ========== Automated Tuning Tools ==========

        [McpServerTool(Name = "suggest_tuning", Title = "Suggest Tuning Adjustments")]
        [Description("Analyzes the most recent battle simulation results and suggests specific tuning adjustments with priorities. Run run_battle_simulation first.")]
        public static Task<string> SuggestTuning()
        {
            return Tools.AutomatedTuningTools.SuggestTuning();
        }

        [McpServerTool(Name = "apply_tuning_suggestion", Title = "Apply Tuning Suggestion")]
        [Description("Applies a specific tuning suggestion by ID. Use suggest_tuning first to get suggestion IDs.")]
        public static Task<string> ApplyTuningSuggestion(
            [Description("Suggestion ID from suggest_tuning results")] string suggestionId)
        {
            return Tools.AutomatedTuningTools.ApplyTuningSuggestion(suggestionId);
        }

        [McpServerTool(Name = "get_balance_quality_score", Title = "Get Balance Quality Score")]
        [Description("Gets the overall balance quality score (0-100) based on the most recent simulation results. Run run_battle_simulation first.")]
        public static Task<string> GetBalanceQualityScore()
        {
            return Tools.AutomatedTuningTools.GetBalanceQualityScore();
        }

        [McpServerTool(Name = "set_baseline", Title = "Set Baseline for Comparison")]
        [Description("Sets the current simulation results as baseline for future comparisons. Run run_battle_simulation first.")]
        public static Task<string> SetBaseline()
        {
            return Tools.AutomatedTuningTools.SetBaseline();
        }

        [McpServerTool(Name = "compare_with_baseline", Title = "Compare Current Results with Baseline")]
        [Description("Compares current simulation results with the baseline. Set baseline first with set_baseline.")]
        public static Task<string> CompareWithBaseline()
        {
            return Tools.AutomatedTuningTools.CompareWithBaseline();
        }

        // ========== Enhanced Analysis Tools ==========

        [McpServerTool(Name = "analyze_parameter_sensitivity", Title = "Analyze Parameter Sensitivity")]
        [Description("Analyzes how sensitive a parameter is to changes. Tests parameter across a range and identifies optimal value. Helps identify which parameters have the most impact on balance.")]
        public static Task<string> AnalyzeParameterSensitivity(
            [Description("Parameter name (e.g., 'enemy.globalmultipliers.health', 'player.baseattributes.strength')")] string parameter,
            [Description("Test range as percentage (e.g., '0.8,1.2' for 80%-120% of current value)")] string range = "0.8,1.2",
            [Description("Number of test points across the range (default: 10)")] int testPoints = 10,
            [Description("Battles per test point (default: 50)")] int battlesPerPoint = 50)
        {
            return Tools.EnhancedAnalysisTools.AnalyzeParameterSensitivity(parameter, range, testPoints, battlesPerPoint);
        }

        [McpServerTool(Name = "test_what_if", Title = "Test What-If Scenario")]
        [Description("Tests a hypothetical parameter change without applying it permanently. Compares current configuration with test value and provides risk assessment.")]
        public static Task<string> TestWhatIf(
            [Description("Parameter name (e.g., 'enemy.globalmultipliers.health')")] string parameter,
            [Description("New value to test")] double value,
            [Description("Number of battles to run for comparison (default: 200)")] int numberOfBattles = 200)
        {
            return Tools.EnhancedAnalysisTools.TestWhatIf(parameter, value, numberOfBattles);
        }

        [McpServerTool(Name = "run_battle_simulation_with_logs", Title = "Run Battle Simulation with Turn Logs")]
        [Description("Runs battle simulation with detailed turn-by-turn logs. Returns enhanced data including action usage, damage distribution, and combat flow analysis.")]
        public static Task<string> RunBattleSimulationWithLogs(
            [Description("Number of battles per weapon-enemy combination (default: 50)")] int battlesPerCombination = 50,
            [Description("Player level for testing (default: 1)")] int playerLevel = 1,
            [Description("Enemy level for testing (default: 1)")] int enemyLevel = 1,
            [Description("Include turn-by-turn logs (default: true, may be slower)")] bool includeTurnLogs = true)
        {
            return Tools.EnhancedAnalysisTools.RunBattleSimulationWithLogs(battlesPerCombination, playerLevel, enemyLevel, includeTurnLogs);
        }

        // ========== Variable Editor Tools ==========

        [McpServerTool(Name = "list_variable_categories", Title = "List Variable Categories")]
        [Description("Lists all available variable categories in the VariableEditor system.")]
        public static Task<string> ListVariableCategories()
        {
            return Tools.VariableEditorTools.ListVariableCategories();
        }

        [McpServerTool(Name = "list_variables", Title = "List Variables")]
        [Description("Lists all editable variables, optionally filtered by category.")]
        public static Task<string> ListVariables(
            [Description("Optional category name to filter variables")] string? category = null)
        {
            return Tools.VariableEditorTools.ListVariables(category);
        }

        [McpServerTool(Name = "get_variable", Title = "Get Variable Value")]
        [Description("Gets the current value of a specific variable by name.")]
        public static Task<string> GetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName)
        {
            return Tools.VariableEditorTools.GetVariable(variableName);
        }

        [McpServerTool(Name = "set_variable", Title = "Set Variable Value")]
        [Description("Sets the value of a specific variable. The value will be converted to the appropriate type (int, double, bool, string).")]
        public static Task<string> SetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName,
            [Description("New value (will be converted to appropriate type)")] string value)
        {
            return Tools.VariableEditorTools.SetVariable(variableName, value);
        }

        [McpServerTool(Name = "save_variable_changes", Title = "Save Variable Changes")]
        [Description("Saves all variable changes to TuningConfig.json and gamesettings.json files.")]
        public static Task<string> SaveVariableChanges()
        {
            return Tools.VariableEditorTools.SaveVariableChanges();
        }

        [McpServerTool(Name = "get_variables_by_category", Title = "Get Variables by Category")]
        [Description("Gets all variables in a specific category.")]
        public static Task<string> GetVariablesByCategory(
            [Description("Category name (e.g., 'EnemySystem', 'PlayerAttributes', 'Combat')")] string category)
        {
            return Tools.VariableEditorTools.GetVariablesByCategory(category);
        }

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

        // ========== Specialized Agent Tools ==========

        [McpServerTool(Name = "run_full_cycle", Title = "Run Full Automated Balance Cycle")]
        [Description("Orchestrates complete multi-agent balance tuning cycle: Analysis → Tuning → Testing → Gameplay → Save. Coordinates all specialized agents to reach target balance.")]
        public static Task<string> RunFullCycle(
            [Description("Target win rate percentage (default: 90)")] double targetWinRate = 90.0,
            [Description("Maximum number of tuning iterations (default: 5)")] int maxIterations = 5)
        {
            return Tools.AutomatedTuningLoop.RunFullCycle(targetWinRate, maxIterations);
        }

        [McpServerTool(Name = "tester_agent_run", Title = "Tester Agent - Run Tests")]
        [Description("Launches Tester Agent to run comprehensive balance verification tests.")]
        public static Task<string> TesterAgentRun(
            [Description("Test mode: 'full' (all tests), 'quick' (core metrics), 'regression' (baseline comparison). Default: full")] string mode = "full")
        {
            var testMode = mode.ToLower() switch
            {
                "quick" => Tools.TesterAgent.TestMode.Quick,
                "regression" => Tools.TesterAgent.TestMode.Regression,
                _ => Tools.TesterAgent.TestMode.Full
            };
            return Tools.TesterAgent.RunTests(testMode);
        }

        [McpServerTool(Name = "analysis_agent_run", Title = "Analysis Agent - Deep Diagnostics")]
        [Description("Launches Analysis Agent to run targeted diagnostics on specific balance aspects.")]
        public static Task<string> AnalysisAgentRun(
            [Description("Focus area: 'balance' (overall), 'weapons' (variance), 'enemies' (matchups), 'engagement' (fun moments). Default: balance")] string focus = "balance")
        {
            var focusArea = focus.ToLower() switch
            {
                "weapons" => Tools.AnalysisAgent.FocusArea.Weapons,
                "enemies" => Tools.AnalysisAgent.FocusArea.Enemies,
                "engagement" => Tools.AnalysisAgent.FocusArea.Engagement,
                _ => Tools.AnalysisAgent.FocusArea.Balance
            };
            return Tools.AnalysisAgent.AnalyzeAndReport(focusArea);
        }

        [McpServerTool(Name = "balance_tuner_agent_run", Title = "Balance Tuner Agent - Iterative Tuning")]
        [Description("Launches Balance Tuner Agent to iteratively adjust balance toward target metrics.")]
        public static Task<string> BalanceTunerAgentRun(
            [Description("Target win rate percentage (default: 90)")] double targetWinRate = 90.0,
            [Description("Maximize enemy variance (default: true)")] bool maximizeVariance = true)
        {
            return Tools.BalanceTunerAgent.TuneBalance(targetWinRate, maximizeVariance);
        }

        // ========== Development Agent Tools ==========

        [McpServerTool(Name = "code_review_agent_file", Title = "Code Review Agent - Review File")]
        [Description("Launches Code Review Agent to analyze a specific C# file for quality issues.")]
        public static Task<string> CodeReviewAgentFile(
            [Description("File path relative to project root (e.g., Code/Combat/CombatManager.cs)")] string filePath)
        {
            return Tools.CodeReviewAgent.ReviewFile(filePath);
        }

        [McpServerTool(Name = "code_review_agent_diff", Title = "Code Review Agent - Review Diff")]
        [Description("Launches Code Review Agent to review uncommitted git changes.")]
        public static Task<string> CodeReviewAgentDiff()
        {
            return Tools.CodeReviewAgent.ReviewDiff();
        }

        [McpServerTool(Name = "code_review_agent_pr", Title = "Code Review Agent - Review Pull Request")]
        [Description("Launches Code Review Agent to review current branch against main.")]
        public static Task<string> CodeReviewAgentPr()
        {
            return Tools.CodeReviewAgent.ReviewPullRequest();
        }

        [McpServerTool(Name = "test_engineer_agent_generate", Title = "Test Engineer Agent - Generate Tests")]
        [Description("Launches Test Engineer Agent to generate unit and integration tests for a feature.")]
        public static Task<string> TestEngineerAgentGenerate(
            [Description("Feature name or class to generate tests for")] string featureName)
        {
            return Tools.TestEngineerAgent.GenerateTests(featureName);
        }

        [McpServerTool(Name = "test_engineer_agent_run", Title = "Test Engineer Agent - Run Tests")]
        [Description("Launches Test Engineer Agent to run and verify test suites.")]
        public static Task<string> TestEngineerAgentRun(
            [Description("Test category or name to run")] string category)
        {
            return Tools.TestEngineerAgent.RunTests(category);
        }

        [McpServerTool(Name = "test_engineer_agent_coverage", Title = "Test Engineer Agent - Analyze Coverage")]
        [Description("Launches Test Engineer Agent to analyze and report test coverage gaps.")]
        public static Task<string> TestEngineerAgentCoverage()
        {
            return Tools.TestEngineerAgent.AnalyzeCoverage();
        }

        [McpServerTool(Name = "test_engineer_agent_integration", Title = "Test Engineer Agent - Generate Integration Tests")]
        [Description("Launches Test Engineer Agent to generate integration tests for a system.")]
        public static Task<string> TestEngineerAgentIntegration(
            [Description("System name (e.g., Combat, Character, Inventory)")] string systemName)
        {
            return Tools.TestEngineerAgent.GenerateIntegrationTests(systemName);
        }

        [McpServerTool(Name = "bug_investigator_agent_investigate", Title = "Bug Investigator Agent - Investigate Bug")]
        [Description("Launches Bug Investigator Agent to diagnose a bug from description.")]
        public static Task<string> BugInvestigatorAgentInvestigate(
            [Description("Description of the bug issue")] string description)
        {
            return Tools.BugInvestigatorAgent.InvestigateBug(description);
        }

        [McpServerTool(Name = "bug_investigator_agent_reproduce", Title = "Bug Investigator Agent - Reproduce Bug")]
        [Description("Launches Bug Investigator Agent to reproduce a bug with given steps.")]
        public static Task<string> BugInvestigatorAgentReproduce(
            [Description("Steps to reproduce the bug")] string steps)
        {
            return Tools.BugInvestigatorAgent.ReproduceBug(steps);
        }

        [McpServerTool(Name = "bug_investigator_agent_isolate", Title = "Bug Investigator Agent - Isolate Bug")]
        [Description("Launches Bug Investigator Agent to isolate root cause in a system.")]
        public static Task<string> BugInvestigatorAgentIsolate(
            [Description("System name where bug occurs (e.g., Combat, Enemy, UI)")] string systemName)
        {
            return Tools.BugInvestigatorAgent.IsolateBug(systemName);
        }

        [McpServerTool(Name = "bug_investigator_agent_suggest_fix", Title = "Bug Investigator Agent - Suggest Fixes")]
        [Description("Launches Bug Investigator Agent to generate fix suggestions for a bug.")]
        public static Task<string> BugInvestigatorAgentSuggestFix(
            [Description("Bug ID or identifier")] string bugId)
        {
            return Tools.BugInvestigatorAgent.SuggestFix(bugId);
        }

        [McpServerTool(Name = "performance_profiler_agent_profile", Title = "Performance Profiler Agent - Profile System")]
        [Description("Launches Performance Profiler Agent to identify bottlenecks in a system.")]
        public static Task<string> PerformanceProfilerAgentProfile(
            [Description("Component name to profile (Combat, Enemy, Game, etc.)")] string component)
        {
            return Tools.PerformanceProfilerAgent.ProfileSystem(component);
        }

        [McpServerTool(Name = "performance_profiler_agent_compare", Title = "Performance Profiler Agent - Compare Performance")]
        [Description("Launches Performance Profiler Agent to compare current performance with baseline.")]
        public static Task<string> PerformanceProfilerAgentCompare(
            [Description("Baseline name or version to compare against")] string baseline)
        {
            return Tools.PerformanceProfilerAgent.ComparePerformance(baseline);
        }

        [McpServerTool(Name = "performance_profiler_agent_bottlenecks", Title = "Performance Profiler Agent - Identify Bottlenecks")]
        [Description("Launches Performance Profiler Agent to find performance bottlenecks across all systems.")]
        public static Task<string> PerformanceProfilerAgentBottlenecks()
        {
            return Tools.PerformanceProfilerAgent.IdentifyBottlenecks();
        }

        [McpServerTool(Name = "performance_profiler_agent_benchmark", Title = "Performance Profiler Agent - Benchmark Critical Paths")]
        [Description("Launches Performance Profiler Agent to benchmark critical code paths.")]
        public static Task<string> PerformanceProfilerAgentBenchmark()
        {
            return Tools.PerformanceProfilerAgent.BenchmarkCriticalPaths();
        }

        [McpServerTool(Name = "refactoring_agent_suggest", Title = "Refactoring Agent - Suggest Refactorings")]
        [Description("Launches Refactoring Agent to identify refactoring opportunities in a target.")]
        public static Task<string> RefactoringAgentSuggest(
            [Description("Target to analyze (class name, system, file path, etc.)")] string target)
        {
            return Tools.RefactoringAgent.SuggestRefactorings(target);
        }

        [McpServerTool(Name = "refactoring_agent_apply", Title = "Refactoring Agent - Apply Refactoring")]
        [Description("Launches Refactoring Agent to apply specific refactoring type.")]
        public static Task<string> RefactoringAgentApply(
            [Description("Refactoring type: extract, simplify, consolidate, modernize")] string type,
            [Description("Target to refactor (class name, method, file path, etc.)")] string target)
        {
            return Tools.RefactoringAgent.ApplyRefactoring(type, target);
        }

        [McpServerTool(Name = "refactoring_agent_duplicates", Title = "Refactoring Agent - Remove Duplication")]
        [Description("Launches Refactoring Agent to find and suggest removal of duplicated code.")]
        public static Task<string> RefactoringAgentDuplicates()
        {
            return Tools.RefactoringAgent.RemoveDuplication();
        }

        [McpServerTool(Name = "refactoring_agent_simplify", Title = "Refactoring Agent - Simplify Method")]
        [Description("Launches Refactoring Agent to analyze and simplify a complex method.")]
        public static Task<string> RefactoringAgentSimplify(
            [Description("Method name to simplify")] string methodName)
        {
            return Tools.RefactoringAgent.SimplifyMethod(methodName);
        }

        [McpServerTool(Name = "dependency_analyzer_analyze", Title = "Dependency Analyzer Agent - Analyze Dependencies")]
        [Description("Launches Dependency Analyzer Agent to analyze project dependencies.")]
        public static Task<string> DependencyAnalyzerAnalyze()
        {
            return Tools.DependencyAnalyzerAgent.AnalyzeDependencies();
        }

        [McpServerTool(Name = "dependency_analyzer_outdated", Title = "Dependency Analyzer Agent - Find Outdated Packages")]
        [Description("Launches Dependency Analyzer Agent to find outdated packages.")]
        public static Task<string> DependencyAnalyzerOutdated()
        {
            return Tools.DependencyAnalyzerAgent.FindOutdatedPackages();
        }

        [McpServerTool(Name = "dependency_analyzer_unused", Title = "Dependency Analyzer Agent - Find Unused Dependencies")]
        [Description("Launches Dependency Analyzer Agent to find unused dependencies.")]
        public static Task<string> DependencyAnalyzerUnused()
        {
            return Tools.DependencyAnalyzerAgent.FindUnusedDependencies();
        }

        [McpServerTool(Name = "dependency_analyzer_security", Title = "Dependency Analyzer Agent - Check Security Vulnerabilities")]
        [Description("Launches Dependency Analyzer Agent to scan for security vulnerabilities.")]
        public static Task<string> DependencyAnalyzerSecurity()
        {
            return Tools.DependencyAnalyzerAgent.CheckSecurityVulnerabilities();
        }

        [McpServerTool(Name = "feature_builder_feature", Title = "Feature Builder Agent - Build Feature")]
        [Description("Launches Feature Builder Agent to generate implementation plan for a feature.")]
        public static Task<string> FeatureBuilderFeature(
            [Description("Feature specification")] string spec)
        {
            return Tools.FeatureBuilderAgent.BuildFeature(spec);
        }

        [McpServerTool(Name = "feature_builder_class", Title = "Feature Builder Agent - Generate Class")]
        [Description("Launches Feature Builder Agent to generate a class from properties.")]
        public static Task<string> FeatureBuilderClass(
            [Description("Class name")] string name,
            [Description("Properties as comma-separated list: type1 prop1, type2 prop2")] string properties)
        {
            return Tools.FeatureBuilderAgent.GenerateClass(name, properties);
        }

        [McpServerTool(Name = "feature_builder_system", Title = "Feature Builder Agent - Scaffold System")]
        [Description("Launches Feature Builder Agent to scaffold a new system.")]
        public static Task<string> FeatureBuilderSystem(
            [Description("System name to scaffold")] string systemName)
        {
            return Tools.FeatureBuilderAgent.ScaffoldSystem(systemName);
        }

        [McpServerTool(Name = "feature_builder_endpoint", Title = "Feature Builder Agent - Generate API Endpoint")]
        [Description("Launches Feature Builder Agent to generate an API endpoint.")]
        public static Task<string> FeatureBuilderEndpoint(
            [Description("API path (e.g., /api/users)")] string path,
            [Description("HTTP method (Get, Post, Put, Delete)")] string method)
        {
            return Tools.FeatureBuilderAgent.GenerateApiEndpoint(path, method);
        }
    }
}

