using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Partial class for Simulation, Analysis, Balance Adjustment, Patch Management, Automated Tuning, Enhanced Analysis, and Variable Editor tools
    /// </summary>
    public static partial class McpTools
    {
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
    }
}

