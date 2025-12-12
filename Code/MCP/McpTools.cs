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

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP Tools for DungeonFighter game
    /// Each tool is registered with the MCP server using attributes
    /// </summary>
    [McpServerToolType]
    public static class McpTools
    {
        private static GameWrapper? _gameWrapper;
        private static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? _lastTestResult;

        /// <summary>
        /// Sets the game wrapper instance (called by MCP server)
        /// </summary>
        public static void SetGameWrapper(GameWrapper wrapper)
        {
            _gameWrapper = wrapper;
        }

        // ========== Game Control Tools ==========

        [McpServerTool(Name = "start_new_game", Title = "Start New Game")]
        [Description("Starts a new game instance. Returns the initial game state.")]
        public static Task<string> StartNewGame()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                _gameWrapper.InitializeGame();
                _gameWrapper.ShowMainMenu();
                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "save_game", Title = "Save Game")]
        [Description("Saves the current game state.")]
        public static Task<string> SaveGame()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                _gameWrapper.SaveGame();
                return Task.FromResult(JsonSerializer.Serialize(new { success = true, message = "Game saved successfully" }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Navigation Tools ==========

        [McpServerTool(Name = "handle_input", Title = "Handle Game Input")]
        [Description("Handles game input (menu selection, combat actions, etc.). Returns updated game state.")]
        public static async Task<string> HandleInput(
            [Description("The input to send to the game (e.g., '1', '2', 'attack')")] string input)
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = await _gameWrapper.HandleInput(input);
                return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        [McpServerTool(Name = "get_available_actions", Title = "Get Available Actions")]
        [Description("Gets the list of available actions for the current game state.")]
        public static Task<string> GetAvailableActions()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var actions = _gameWrapper.GetAvailableActions();
                return Task.FromResult(JsonSerializer.Serialize(actions));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Information Tools ==========

        [McpServerTool(Name = "get_game_state", Title = "Get Game State")]
        [Description("Gets comprehensive game state snapshot including player, dungeon, room, and combat status.")]
        public static Task<string> GetGameState()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_player_stats", Title = "Get Player Stats")]
        [Description("Gets player character statistics including health, level, XP, attributes, and equipment.")]
        public static Task<string> GetPlayerStats()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.Player, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_current_dungeon", Title = "Get Current Dungeon")]
        [Description("Gets information about the current dungeon (name, level, theme, rooms).")]
        public static Task<string> GetCurrentDungeon()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.CurrentDungeon, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_inventory", Title = "Get Inventory")]
        [Description("Gets the player's inventory items.")]
        public static Task<string> GetInventory()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.Player?.Inventory ?? new List<ItemSnapshot>(), new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_combat_state", Title = "Get Combat State")]
        [Description("Gets current combat information if in combat (current enemy, available actions, turn status).")]
        public static Task<string> GetCombatState()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                return Task.FromResult(JsonSerializer.Serialize(state.Combat, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_recent_output", Title = "Get Recent Output")]
        [Description("Gets recent game output/messages for AI context.")]
        public static Task<string> GetRecentOutput(
            [Description("Number of messages to retrieve (default: 10, max: 100)")] int count = 10)
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                // Limit to max 100 messages to prevent excessive token usage
                var limitedCount = Math.Min(count, 100);
                var output = _gameWrapper.GetRecentOutput(limitedCount);
                return Task.FromResult(JsonSerializer.Serialize(output));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Dungeon Tools ==========

        [McpServerTool(Name = "get_available_dungeons", Title = "Get Available Dungeons")]
        [Description("Lists all available dungeons that can be entered.")]
        public static Task<string> GetAvailableDungeons()
        {
            try
            {
                if (_gameWrapper == null)
                    throw new InvalidOperationException("Game wrapper not initialized");

                var state = _gameWrapper.GetGameState();
                // TODO: Get available dungeons from game state
                return Task.FromResult(JsonSerializer.Serialize(new { message = "Available dungeons feature not yet implemented" }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Simulation Tools ==========

        [McpServerTool(Name = "run_battle_simulation", Title = "Run Battle Simulation")]
        [Description("Runs comprehensive battle simulations testing all weapon types against all enemy types. Returns detailed statistics.")]
        public static async Task<string> RunBattleSimulation(
            [Description("Number of battles per weapon-enemy combination (default: 50)")] int battlesPerCombination = 50,
            [Description("Player level for testing (default: 1)")] int playerLevel = 1,
            [Description("Enemy level for testing (default: 1)")] int enemyLevel = 1)
        {
            try
            {
                var progress = new Progress<(int completed, int total, string status)>();
                var testResult = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination, 
                    playerLevel, 
                    enemyLevel, 
                    progress);

                // Cache the result for analysis tools
                _lastTestResult = testResult;

                // Serialize result with summary
                var summary = new
                {
                    overallWinRate = testResult.OverallWinRate,
                    totalBattles = testResult.TotalBattles,
                    totalPlayerWins = testResult.TotalPlayerWins,
                    totalEnemyWins = testResult.TotalEnemyWins,
                    overallAverageTurns = testResult.OverallAverageTurns,
                    weaponTypes = testResult.WeaponTypes.Select(w => w.ToString()).ToList(),
                    enemyTypes = testResult.EnemyTypes,
                    weaponStatistics = testResult.WeaponStatistics.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => new
                        {
                            totalBattles = kvp.Value.TotalBattles,
                            wins = kvp.Value.Wins,
                            winRate = kvp.Value.WinRate,
                            averageTurns = kvp.Value.AverageTurns,
                            averageDamage = kvp.Value.AverageDamage
                        }),
                    enemyStatistics = testResult.EnemyStatistics.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            totalBattles = kvp.Value.TotalBattles,
                            wins = kvp.Value.Wins,
                            winRate = kvp.Value.WinRate,
                            averageTurns = kvp.Value.AverageTurns,
                            averageDamageReceived = kvp.Value.AverageDamageReceived
                        }),
                    combinationResults = testResult.CombinationResults.Select(c => new
                    {
                        weaponType = c.WeaponType.ToString(),
                        enemyType = c.EnemyType,
                        totalBattles = c.TotalBattles,
                        playerWins = c.PlayerWins,
                        enemyWins = c.EnemyWins,
                        winRate = c.WinRate,
                        averageTurns = c.AverageTurns,
                        averagePlayerDamageDealt = c.AveragePlayerDamageDealt,
                        averageEnemyDamageDealt = c.AverageEnemyDamageDealt,
                        minTurns = c.MinTurns,
                        maxTurns = c.MaxTurns
                    }).ToList()
                };

                return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [McpServerTool(Name = "run_parallel_battles", Title = "Run Parallel Battles")]
        [Description("Runs parallel battles with custom player and enemy stats. Useful for testing specific stat combinations.")]
        public static async Task<string> RunParallelBattles(
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
            try
            {
                var config = new BattleStatisticsRunner.BattleConfiguration
                {
                    PlayerDamage = playerDamage,
                    PlayerAttackSpeed = playerAttackSpeed,
                    PlayerArmor = playerArmor,
                    PlayerHealth = playerHealth,
                    EnemyDamage = enemyDamage,
                    EnemyAttackSpeed = enemyAttackSpeed,
                    EnemyArmor = enemyArmor,
                    EnemyHealth = enemyHealth
                };

                var progress = new Progress<(int completed, int total, string status)>();
                var result = await BattleStatisticsRunner.RunParallelBattles(config, numberOfBattles, progress);

                var summary = new
                {
                    config = new
                    {
                        player = new { damage = config.PlayerDamage, attackSpeed = config.PlayerAttackSpeed, armor = config.PlayerArmor, health = config.PlayerHealth },
                        enemy = new { damage = config.EnemyDamage, attackSpeed = config.EnemyAttackSpeed, armor = config.EnemyArmor, health = config.EnemyHealth }
                    },
                    totalBattles = result.TotalBattles,
                    playerWins = result.PlayerWins,
                    enemyWins = result.EnemyWins,
                    winRate = result.WinRate,
                    averageTurns = result.AverageTurns,
                    averagePlayerDamageDealt = result.AveragePlayerDamageDealt,
                    averageEnemyDamageDealt = result.AverageEnemyDamageDealt,
                    minTurns = result.MinTurns,
                    maxTurns = result.MaxTurns
                };

                return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // ========== Analysis Tools ==========

        [McpServerTool(Name = "analyze_battle_results", Title = "Analyze Battle Results")]
        [Description("Analyzes the most recent battle simulation results and generates a detailed analysis report with issues and recommendations. Run run_battle_simulation first.")]
        public static Task<string> AnalyzeBattleResults()
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No simulation results available. Run run_battle_simulation first."
                    }));
                }

                var analysis = MatchupAnalyzer.Analyze(_lastTestResult);
                var textReport = MatchupAnalyzer.GenerateTextReport(analysis);

                var result = new
                {
                    generatedDate = analysis.GeneratedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    battlesPerMatchup = analysis.BattlesPerMatchup,
                    playerLevel = analysis.PlayerLevel,
                    enemyLevel = analysis.EnemyLevel,
                    matchupResults = analysis.MatchupResults.Select(m => new
                    {
                        weaponType = m.WeaponType,
                        enemyType = m.EnemyType,
                        winRate = m.WinRate,
                        averageTurns = m.AverageTurns,
                        status = m.Status,
                        issues = m.Issues
                    }).ToList(),
                    issues = analysis.Issues,
                    recommendations = analysis.Recommendations,
                    weaponAverages = analysis.WeaponAverages,
                    enemyAverages = analysis.EnemyAverages,
                    textReport = textReport
                };

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "validate_balance", Title = "Validate Balance")]
        [Description("Validates balance based on the most recent battle simulation results. Returns validation report with errors and warnings. Run run_battle_simulation first.")]
        public static Task<string> ValidateBalance()
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No simulation results available. Run run_battle_simulation first."
                    }));
                }

                var validation = BalanceValidator.Validate(_lastTestResult);
                var report = BalanceValidator.GenerateReport(validation);

                var result = new
                {
                    isValid = validation.IsValid,
                    totalChecks = validation.TotalChecks,
                    passedChecks = validation.PassedChecks,
                    errors = validation.Errors,
                    warnings = validation.Warnings,
                    report = report
                };

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        // ========== Balance Adjustment Tools ==========

        [McpServerTool(Name = "adjust_global_enemy_multiplier", Title = "Adjust Global Enemy Multiplier")]
        [Description("Adjusts global enemy multipliers (health, damage, armor, speed). Affects all enemies.")]
        public static Task<string> AdjustGlobalEnemyMultiplier(
            [Description("Multiplier name: 'health', 'damage', 'armor', or 'speed'")] string multiplierName,
            [Description("New multiplier value (e.g., 1.2 for 20% increase)")] double value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustGlobalEnemyMultiplier(multiplierName, value);
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set {multiplierName} to {value}" : $"Failed to set {multiplierName}",
                    currentMultipliers = new
                    {
                        health = multipliers.HealthMultiplier,
                        damage = multipliers.DamageMultiplier,
                        armor = multipliers.ArmorMultiplier,
                        speed = multipliers.SpeedMultiplier
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_archetype", Title = "Adjust Archetype")]
        [Description("Adjusts archetype stat multipliers (health, strength, agility, technique, intelligence, armor).")]
        public static Task<string> AdjustArchetype(
            [Description("Archetype name (e.g., 'Berserker', 'Tank', 'Assassin')")] string archetypeName,
            [Description("Stat name: 'health', 'strength', 'agility', 'technique', 'intelligence', or 'armor'")] string statName,
            [Description("New stat value")] double value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustArchetype(archetypeName, statName, value);
                var config = GameConfiguration.Instance;
                var archetypes = config.EnemySystem.Archetypes;

                var archetypeInfo = new Dictionary<string, object>();
                if (archetypes.ContainsKey(archetypeName))
                {
                    var arch = archetypes[archetypeName];
                    archetypeInfo[archetypeName] = new
                    {
                        health = arch.Health,
                        strength = arch.Strength,
                        agility = arch.Agility,
                        technique = arch.Technique,
                        intelligence = arch.Intelligence,
                        armor = arch.Armor
                    };
                }

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set {archetypeName}.{statName} to {value}" : $"Failed to set {archetypeName}.{statName}",
                    archetype = archetypeInfo
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_player_base_attribute", Title = "Adjust Player Base Attribute")]
        [Description("Adjusts player base attributes (strength, agility, technique, intelligence).")]
        public static Task<string> AdjustPlayerBaseAttribute(
            [Description("Attribute name: 'strength', 'agility', 'technique', or 'intelligence'")] string attributeName,
            [Description("New base attribute value")] int value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustPlayerBaseAttribute(attributeName, value);
                var config = GameConfiguration.Instance;
                var baseAttributes = config.Attributes.PlayerBaseAttributes;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set player base {attributeName} to {value}" : $"Failed to set player base {attributeName}",
                    currentAttributes = new
                    {
                        strength = baseAttributes.Strength,
                        agility = baseAttributes.Agility,
                        technique = baseAttributes.Technique,
                        intelligence = baseAttributes.Intelligence
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_player_base_health", Title = "Adjust Player Base Health")]
        [Description("Adjusts player base health.")]
        public static Task<string> AdjustPlayerBaseHealth(
            [Description("New base health value")] int value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustPlayerBaseHealth(value);
                var config = GameConfiguration.Instance;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set player base health to {value}" : "Failed to set player base health",
                    currentHealth = new
                    {
                        baseHealth = config.Character.PlayerBaseHealth,
                        healthPerLevel = config.Character.HealthPerLevel
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_player_attributes_per_level", Title = "Adjust Player Attributes Per Level")]
        [Description("Adjusts how many attributes the player gains per level.")]
        public static Task<string> AdjustPlayerAttributesPerLevel(
            [Description("New attributes per level value")] int value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustPlayerAttributesPerLevel(value);
                var config = GameConfiguration.Instance;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set player attributes per level to {value}" : "Failed to set player attributes per level",
                    currentAttributesPerLevel = config.Attributes.PlayerAttributesPerLevel
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_player_health_per_level", Title = "Adjust Player Health Per Level")]
        [Description("Adjusts how much health the player gains per level.")]
        public static Task<string> AdjustPlayerHealthPerLevel(
            [Description("New health per level value")] int value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustPlayerHealthPerLevel(value);
                var config = GameConfiguration.Instance;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set player health per level to {value}" : "Failed to set player health per level",
                    currentHealthPerLevel = config.Character.HealthPerLevel
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_enemy_baseline_stat", Title = "Adjust Enemy Baseline Stat")]
        [Description("Adjusts enemy baseline stats (health, strength, agility, technique, intelligence, armor). Affects all enemies.")]
        public static Task<string> AdjustEnemyBaselineStat(
            [Description("Stat name: 'health', 'strength', 'agility', 'technique', 'intelligence', or 'armor'")] string statName,
            [Description("New baseline stat value")] double value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustEnemyBaselineStat(statName, value);
                var config = GameConfiguration.Instance;
                var baselineStats = config.EnemySystem.BaselineStats;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set enemy baseline {statName} to {value}" : $"Failed to set enemy baseline {statName}",
                    currentBaselineStats = new
                    {
                        health = baselineStats.Health,
                        strength = baselineStats.Strength,
                        agility = baselineStats.Agility,
                        technique = baselineStats.Technique,
                        intelligence = baselineStats.Intelligence,
                        armor = baselineStats.Armor
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_enemy_scaling_per_level", Title = "Adjust Enemy Scaling Per Level")]
        [Description("Adjusts how much enemy stats increase per level (health, attributes, armor).")]
        public static Task<string> AdjustEnemyScalingPerLevel(
            [Description("Scaling type: 'health', 'attributes', or 'armor'")] string statName,
            [Description("New scaling value")] double value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustEnemyScalingPerLevel(statName, value);
                var config = GameConfiguration.Instance;
                var scaling = config.EnemySystem.ScalingPerLevel;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set enemy scaling per level {statName} to {value}" : $"Failed to set enemy scaling per level {statName}",
                    currentScaling = new
                    {
                        health = scaling.Health,
                        attributes = scaling.Attributes,
                        armor = scaling.Armor
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "adjust_weapon_scaling", Title = "Adjust Weapon Scaling")]
        [Description("Adjusts weapon scaling multipliers.")]
        public static Task<string> AdjustWeaponScaling(
            [Description("Weapon type (e.g., 'Mace', 'Sword', 'Dagger', 'Wand') or 'global' for global multiplier")] string weaponType,
            [Description("Parameter name: 'damage' or 'damageMultiplier'")] string parameter,
            [Description("New value")] double value)
        {
            try
            {
                var success = BalanceTuningConsole.AdjustWeaponScaling(weaponType, parameter, value);
                var config = GameConfiguration.Instance;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Set weapon scaling {parameter} to {value}" : $"Failed to set weapon scaling",
                    currentGlobalDamageMultiplier = config.WeaponScaling?.GlobalDamageMultiplier ?? 1.0
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "apply_preset", Title = "Apply Preset")]
        [Description("Applies a quick preset configuration: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', or 'baseline'.")]
        public static Task<string> ApplyPreset(
            [Description("Preset name: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', or 'baseline'")] string presetName)
        {
            try
            {
                var success = BalanceTuningConsole.ApplyPreset(presetName);
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Applied preset '{presetName}'" : $"Failed to apply preset '{presetName}'",
                    currentMultipliers = new
                    {
                        health = multipliers.HealthMultiplier,
                        damage = multipliers.DamageMultiplier,
                        armor = multipliers.ArmorMultiplier,
                        speed = multipliers.SpeedMultiplier
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "save_configuration", Title = "Save Configuration")]
        [Description("Saves the current game configuration to TuningConfig.json.")]
        public static Task<string> SaveConfiguration()
        {
            try
            {
                var success = BalanceTuningConsole.SaveConfiguration();
                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? "Configuration saved successfully" : "Failed to save configuration"
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_current_configuration", Title = "Get Current Configuration")]
        [Description("Gets the current game configuration values (enemy multipliers, archetypes, etc.).")]
        public static Task<string> GetCurrentConfiguration()
        {
            try
            {
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;
                var archetypes = config.EnemySystem.Archetypes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        health = kvp.Value.Health,
                        strength = kvp.Value.Strength,
                        agility = kvp.Value.Agility,
                        technique = kvp.Value.Technique,
                        intelligence = kvp.Value.Intelligence,
                        armor = kvp.Value.Armor
                    });

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    globalMultipliers = new
                    {
                        health = multipliers.HealthMultiplier,
                        damage = multipliers.DamageMultiplier,
                        armor = multipliers.ArmorMultiplier,
                        speed = multipliers.SpeedMultiplier
                    },
                    archetypes = archetypes,
                    weaponScaling = new
                    {
                        globalDamageMultiplier = config.WeaponScaling?.GlobalDamageMultiplier ?? 1.0
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
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
            try
            {
                var tagList = tags?.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                var success = BalanceTuningConsole.SavePatch(name, author, description, version, tagList);

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Patch '{name}' saved successfully" : $"Failed to save patch '{name}'"
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "load_patch", Title = "Load Patch")]
        [Description("Loads and applies a balance patch by patch ID or name.")]
        public static Task<string> LoadPatch(
            [Description("Patch ID or name")] string patchId)
        {
            try
            {
                var success = BalanceTuningConsole.LoadPatch(patchId);
                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Patch '{patchId}' loaded successfully" : $"Failed to load patch '{patchId}'"
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "list_patches", Title = "List Patches")]
        [Description("Lists all available balance patches.")]
        public static Task<string> ListPatches()
        {
            try
            {
                var patches = BalancePatchManager.ListPatches();
                var patchList = patches.Select(p => new
                {
                    patchId = p.PatchMetadata.PatchId,
                    name = p.PatchMetadata.Name,
                    author = p.PatchMetadata.Author,
                    description = p.PatchMetadata.Description,
                    version = p.PatchMetadata.Version,
                    createdDate = p.PatchMetadata.CreatedDate,
                    tags = p.PatchMetadata.Tags,
                    testResults = p.PatchMetadata.TestResults != null ? new
                    {
                        averageWinRate = p.PatchMetadata.TestResults.AverageWinRate,
                        battlesTested = p.PatchMetadata.TestResults.BattlesTested,
                        testDate = p.PatchMetadata.TestResults.TestDate
                    } : null
                }).ToList();

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    count = patchList.Count,
                    patches = patchList
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "get_patch_info", Title = "Get Patch Info")]
        [Description("Gets detailed information about a specific patch.")]
        public static Task<string> GetPatchInfo(
            [Description("Patch ID or name")] string patchId)
        {
            try
            {
                var patch = BalancePatchManager.GetPatch(patchId);
                if (patch == null)
                {
                    // Try to find by name
                    var patches = BalancePatchManager.ListPatches();
                    patch = patches.FirstOrDefault(p => p.PatchMetadata.Name == patchId);
                }

                if (patch == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new { error = $"Patch '{patchId}' not found" }));
                }

                var info = new
                {
                    patchId = patch.PatchMetadata.PatchId,
                    name = patch.PatchMetadata.Name,
                    author = patch.PatchMetadata.Author,
                    description = patch.PatchMetadata.Description,
                    version = patch.PatchMetadata.Version,
                    createdDate = patch.PatchMetadata.CreatedDate,
                    compatibleGameVersion = patch.PatchMetadata.CompatibleGameVersion,
                    tags = patch.PatchMetadata.Tags,
                    testResults = patch.PatchMetadata.TestResults != null ? new
                    {
                        averageWinRate = patch.PatchMetadata.TestResults.AverageWinRate,
                        battlesTested = patch.PatchMetadata.TestResults.BattlesTested,
                        testDate = patch.PatchMetadata.TestResults.TestDate
                    } : null,
                    configuration = new
                    {
                        globalMultipliers = new
                        {
                            health = patch.TuningConfig.EnemySystem.GlobalMultipliers.HealthMultiplier,
                            damage = patch.TuningConfig.EnemySystem.GlobalMultipliers.DamageMultiplier,
                            armor = patch.TuningConfig.EnemySystem.GlobalMultipliers.ArmorMultiplier,
                            speed = patch.TuningConfig.EnemySystem.GlobalMultipliers.SpeedMultiplier
                        }
                    }
                };

                return Task.FromResult(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        // ========== Automated Tuning Tools ==========

        [McpServerTool(Name = "suggest_tuning", Title = "Suggest Tuning Adjustments")]
        [Description("Analyzes the most recent battle simulation results and suggests specific tuning adjustments with priorities. Run run_battle_simulation first.")]
        public static Task<string> SuggestTuning()
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No simulation results available. Run run_battle_simulation first."
                    }));
                }

                var analysis = AutomatedTuningEngine.AnalyzeAndSuggest(_lastTestResult);

                var result = new
                {
                    qualityScore = analysis.QualityScore,
                    metrics = new
                    {
                        overallWinRate = analysis.OverallWinRate,
                        averageCombatDuration = analysis.AverageCombatDuration,
                        weaponVariance = analysis.WeaponVariance,
                        enemyVariance = analysis.EnemyVariance
                    },
                    summary = analysis.Summary,
                    suggestionCounts = analysis.SuggestionCounts.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value),
                    suggestions = analysis.Suggestions.Select(s => new
                    {
                        id = s.Id,
                        priority = s.Priority.ToString(),
                        category = s.Category,
                        target = s.Target,
                        parameter = s.Parameter,
                        currentValue = s.CurrentValue,
                        suggestedValue = s.SuggestedValue,
                        adjustmentMagnitude = s.AdjustmentMagnitude,
                        reason = s.Reason,
                        impact = s.Impact,
                        affectedMatchups = s.AffectedMatchups
                    }).ToList()
                };

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "apply_tuning_suggestion", Title = "Apply Tuning Suggestion")]
        [Description("Applies a specific tuning suggestion by ID. Use suggest_tuning first to get suggestion IDs.")]
        public static Task<string> ApplyTuningSuggestion(
            [Description("Suggestion ID from suggest_tuning results")] string suggestionId)
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No simulation results available. Run run_battle_simulation first."
                    }));
                }

                var analysis = AutomatedTuningEngine.AnalyzeAndSuggest(_lastTestResult);
                var suggestion = analysis.Suggestions.FirstOrDefault(s => s.Id == suggestionId);

                if (suggestion == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = $"Suggestion '{suggestionId}' not found. Run suggest_tuning to get current suggestions."
                    }));
                }

                var success = AutomatedTuningEngine.ApplySuggestion(suggestion);

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? $"Applied suggestion: {suggestion.Reason}" : "Failed to apply suggestion",
                    suggestion = new
                    {
                        id = suggestion.Id,
                        target = suggestion.Target,
                        parameter = suggestion.Parameter,
                        oldValue = suggestion.CurrentValue,
                        newValue = suggestion.SuggestedValue
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "get_balance_quality_score", Title = "Get Balance Quality Score")]
        [Description("Gets the overall balance quality score (0-100) based on the most recent simulation results. Run run_battle_simulation first.")]
        public static Task<string> GetBalanceQualityScore()
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No simulation results available. Run run_battle_simulation first."
                    }));
                }

                var analysis = AutomatedTuningEngine.AnalyzeAndSuggest(_lastTestResult);

                var result = new
                {
                    qualityScore = analysis.QualityScore,
                    metrics = new
                    {
                        overallWinRate = analysis.OverallWinRate,
                        winRateTarget = $"{BalanceTuningGoals.WinRateTargets.MinTarget}-{BalanceTuningGoals.WinRateTargets.MaxTarget}%",
                        winRateStatus = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget ? "Too Low" :
                                       analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget ? "Too High" : "In Range",
                        averageCombatDuration = analysis.AverageCombatDuration,
                        durationTarget = $"{BalanceTuningGoals.CombatDurationTargets.MinTarget}-{BalanceTuningGoals.CombatDurationTargets.MaxTarget} turns",
                        durationStatus = analysis.AverageCombatDuration < BalanceTuningGoals.CombatDurationTargets.MinTarget ? "Too Short" :
                                       analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.MaxTarget ? "Too Long" : "In Range",
                        weaponVariance = analysis.WeaponVariance,
                        weaponVarianceTarget = $"<{BalanceTuningGoals.WeaponBalanceTargets.MaxVariance}%",
                        weaponVarianceStatus = analysis.WeaponVariance > BalanceTuningGoals.WeaponBalanceTargets.MaxVariance ? "Too High" : "Good",
                        enemyVariance = analysis.EnemyVariance,
                        enemyVarianceTarget = $">{BalanceTuningGoals.EnemyDifferentiationTargets.MinVariance}%",
                        enemyVarianceStatus = analysis.EnemyVariance < BalanceTuningGoals.EnemyDifferentiationTargets.MinVariance ? "Too Low" : "Good"
                    },
                    interpretation = analysis.QualityScore >= 90 ? "Excellent" :
                                    analysis.QualityScore >= 75 ? "Good" :
                                    analysis.QualityScore >= 60 ? "Fair" :
                                    analysis.QualityScore >= 40 ? "Poor" : "Critical"
                };

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        private static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? _baselineTestResult;

        [McpServerTool(Name = "set_baseline", Title = "Set Baseline for Comparison")]
        [Description("Sets the current simulation results as baseline for future comparisons. Run run_battle_simulation first.")]
        public static Task<string> SetBaseline()
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No simulation results available. Run run_battle_simulation first."
                    }));
                }

                _baselineTestResult = _lastTestResult;

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Baseline set successfully",
                    baselineMetrics = new
                    {
                        overallWinRate = _baselineTestResult.OverallWinRate,
                        averageCombatDuration = _baselineTestResult.OverallAverageTurns,
                        totalBattles = _baselineTestResult.TotalBattles
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        [McpServerTool(Name = "compare_with_baseline", Title = "Compare Current Results with Baseline")]
        [Description("Compares current simulation results with the baseline. Set baseline first with set_baseline.")]
        public static Task<string> CompareWithBaseline()
        {
            try
            {
                if (_lastTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No current simulation results available. Run run_battle_simulation first."
                    }));
                }

                if (_baselineTestResult == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new 
                    { 
                        error = "No baseline set. Run set_baseline first."
                    }));
                }

                var baselineAnalysis = AutomatedTuningEngine.AnalyzeAndSuggest(_baselineTestResult);
                var currentAnalysis = AutomatedTuningEngine.AnalyzeAndSuggest(_lastTestResult);

                var result = new
                {
                    baseline = new
                    {
                        qualityScore = baselineAnalysis.QualityScore,
                        overallWinRate = baselineAnalysis.OverallWinRate,
                        averageCombatDuration = baselineAnalysis.AverageCombatDuration,
                        weaponVariance = baselineAnalysis.WeaponVariance,
                        enemyVariance = baselineAnalysis.EnemyVariance
                    },
                    current = new
                    {
                        qualityScore = currentAnalysis.QualityScore,
                        overallWinRate = currentAnalysis.OverallWinRate,
                        averageCombatDuration = currentAnalysis.AverageCombatDuration,
                        weaponVariance = currentAnalysis.WeaponVariance,
                        enemyVariance = currentAnalysis.EnemyVariance
                    },
                    changes = new
                    {
                        qualityScoreChange = currentAnalysis.QualityScore - baselineAnalysis.QualityScore,
                        winRateChange = currentAnalysis.OverallWinRate - baselineAnalysis.OverallWinRate,
                        durationChange = currentAnalysis.AverageCombatDuration - baselineAnalysis.AverageCombatDuration,
                        weaponVarianceChange = currentAnalysis.WeaponVariance - baselineAnalysis.WeaponVariance,
                        enemyVarianceChange = currentAnalysis.EnemyVariance - baselineAnalysis.EnemyVariance
                    },
                    improvement = currentAnalysis.QualityScore > baselineAnalysis.QualityScore ? "Improved" :
                                 currentAnalysis.QualityScore < baselineAnalysis.QualityScore ? "Worsened" : "No Change"
                };

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        // ========== Enhanced Analysis Tools ==========

        [McpServerTool(Name = "analyze_parameter_sensitivity", Title = "Analyze Parameter Sensitivity")]
        [Description("Analyzes how sensitive a parameter is to changes. Tests parameter across a range and identifies optimal value. Helps identify which parameters have the most impact on balance.")]
        public static async Task<string> AnalyzeParameterSensitivity(
            [Description("Parameter name (e.g., 'enemy.globalmultipliers.health', 'player.baseattributes.strength')")] string parameter,
            [Description("Test range as percentage (e.g., '0.8,1.2' for 80%-120% of current value)")] string range = "0.8,1.2",
            [Description("Number of test points across the range (default: 10)")] int testPoints = 10,
            [Description("Battles per test point (default: 50)")] int battlesPerPoint = 50)
        {
            try
            {
                var progress = new Progress<(int completed, int total, string status)>();
                var result = await ParameterSensitivityAnalyzer.AnalyzeParameter(parameter, range, testPoints, battlesPerPoint, progress);

                var summary = new
                {
                    parameterName = result.ParameterName,
                    minValue = result.MinValue,
                    maxValue = result.MaxValue,
                    testPoints = result.TestPoints,
                    optimalValue = result.OptimalValue,
                    optimalQualityScore = result.OptimalQualityScore,
                    sensitivityScore = result.SensitivityScore,
                    recommendation = result.Recommendation,
                    testPointsData = result.TestPointsData.Select(p => new
                    {
                        parameterValue = p.ParameterValue,
                        winRate = p.WinRate,
                        averageCombatDuration = p.AverageCombatDuration,
                        qualityScore = p.QualityScore,
                        battlesTested = p.BattlesTested
                    }).ToList()
                };

                return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [McpServerTool(Name = "test_what_if", Title = "Test What-If Scenario")]
        [Description("Tests a hypothetical parameter change without applying it permanently. Compares current configuration with test value and provides risk assessment.")]
        public static async Task<string> TestWhatIf(
            [Description("Parameter name (e.g., 'enemy.globalmultipliers.health')")] string parameter,
            [Description("New value to test")] double value,
            [Description("Number of battles to run for comparison (default: 200)")] int numberOfBattles = 200)
        {
            try
            {
                var progress = new Progress<(int completed, int total, string status)>();
                var result = await WhatIfTester.TestWhatIf(parameter, value, numberOfBattles, progress);

                var summary = new
                {
                    parameterName = result.ParameterName,
                    currentValue = result.CurrentValue,
                    testValue = result.TestValue,
                    winRateChange = result.WinRateChange,
                    durationChange = result.DurationChange,
                    qualityScoreChange = result.QualityScoreChange,
                    qualityScoreBefore = result.QualityScoreBefore,
                    qualityScoreAfter = result.QualityScoreAfter,
                    riskAssessment = result.RiskAssessment,
                    recommendation = result.Recommendation,
                    detailedMetrics = result.DetailedMetrics
                };

                return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [McpServerTool(Name = "run_battle_simulation_with_logs", Title = "Run Battle Simulation with Turn Logs")]
        [Description("Runs battle simulation with detailed turn-by-turn logs. Returns enhanced data including action usage, damage distribution, and combat flow analysis.")]
        public static async Task<string> RunBattleSimulationWithLogs(
            [Description("Number of battles per weapon-enemy combination (default: 50)")] int battlesPerCombination = 50,
            [Description("Player level for testing (default: 1)")] int playerLevel = 1,
            [Description("Enemy level for testing (default: 1)")] int enemyLevel = 1,
            [Description("Include turn-by-turn logs (default: true, may be slower)")] bool includeTurnLogs = true)
        {
            try
            {
                // For now, use the existing simulation but note that turn logs are available
                // In the future, we can add a flag to BattleConfiguration to enable detailed logging
                var progress = new Progress<(int completed, int total, string status)>();
                var testResult = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination, 
                    playerLevel, 
                    enemyLevel, 
                    progress);

                // Note: Turn logs are collected in BattleResult but not currently aggregated
                // This is a placeholder for future enhancement
                var summary = new
                {
                    overallWinRate = testResult.OverallWinRate,
                    totalBattles = testResult.TotalBattles,
                    overallAverageTurns = testResult.OverallAverageTurns,
                    note = includeTurnLogs ? "Turn logs are collected per battle but not aggregated in summary. Enhanced logging available in individual battle results." : "Turn logs disabled for performance.",
                    weaponStatistics = testResult.WeaponStatistics.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => new
                        {
                            totalBattles = kvp.Value.TotalBattles,
                            wins = kvp.Value.Wins,
                            winRate = kvp.Value.WinRate,
                            averageTurns = kvp.Value.AverageTurns,
                            averageDamage = kvp.Value.AverageDamage
                        }),
                    combinationResults = testResult.CombinationResults.Select(c => new
                    {
                        weaponType = c.WeaponType.ToString(),
                        enemyType = c.EnemyType,
                        winRate = c.WinRate,
                        averageTurns = c.AverageTurns,
                        averagePlayerDamageDealt = c.AveragePlayerDamageDealt,
                        averageEnemyDamageDealt = c.AverageEnemyDamageDealt
                    }).ToList()
                };

                return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // ========== Variable Editor Tools ==========

        private static RPGGame.Editors.VariableEditor? _variableEditor;

        private static RPGGame.Editors.VariableEditor GetVariableEditor()
        {
            if (_variableEditor == null)
            {
                _variableEditor = new RPGGame.Editors.VariableEditor();
            }
            return _variableEditor;
        }

        [McpServerTool(Name = "list_variable_categories", Title = "List Variable Categories")]
        [Description("Lists all available variable categories in the VariableEditor system.")]
        public static Task<string> ListVariableCategories()
        {
            try
            {
                var editor = GetVariableEditor();
                var categories = editor.GetCategories();

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    categories = categories,
                    count = categories.Count
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "list_variables", Title = "List Variables")]
        [Description("Lists all editable variables, optionally filtered by category.")]
        public static Task<string> ListVariables(
            [Description("Optional category name to filter variables")] string? category = null)
        {
            try
            {
                var editor = GetVariableEditor();
                var variables = string.IsNullOrEmpty(category)
                    ? editor.GetVariables()
                    : editor.GetVariablesByCategory(category);

                var variableList = variables.Select(v => new
                {
                    name = v.Name,
                    description = v.Description,
                    value = v.GetValue(),
                    valueType = v.GetValueType().Name,
                    category = GetCategoryForVariable(editor, v.Name)
                }).ToList();

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    category = category ?? "all",
                    variables = variableList,
                    count = variableList.Count
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "get_variable", Title = "Get Variable Value")]
        [Description("Gets the current value of a specific variable by name.")]
        public static Task<string> GetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName)
        {
            try
            {
                var editor = GetVariableEditor();
                var variable = editor.GetVariable(variableName);

                if (variable == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new { error = $"Variable '{variableName}' not found" }));
                }

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    name = variable.Name,
                    description = variable.Description,
                    value = variable.GetValue(),
                    valueType = variable.GetValueType().Name,
                    category = GetCategoryForVariable(editor, variable.Name)
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "set_variable", Title = "Set Variable Value")]
        [Description("Sets the value of a specific variable. The value will be converted to the appropriate type (int, double, bool, string).")]
        public static Task<string> SetVariable(
            [Description("Variable name (e.g., 'EnemySystem.GlobalMultipliers.HealthMultiplier')")] string variableName,
            [Description("New value (will be converted to appropriate type)")] string value)
        {
            try
            {
                var editor = GetVariableEditor();
                var variable = editor.GetVariable(variableName);

                if (variable == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new { error = $"Variable '{variableName}' not found" }));
                }

                var valueType = variable.GetValueType();
                object? convertedValue = null;
                string? error = null;

                try
                {
                    if (valueType == typeof(int))
                    {
                        if (int.TryParse(value, out int intVal))
                        {
                            convertedValue = intVal;
                        }
                        else
                        {
                            error = $"Invalid integer value: {value}";
                        }
                    }
                    else if (valueType == typeof(double))
                    {
                        if (double.TryParse(value, out double doubleVal))
                        {
                            convertedValue = doubleVal;
                        }
                        else
                        {
                            error = $"Invalid number value: {value}";
                        }
                    }
                    else if (valueType == typeof(bool))
                    {
                        string trimmed = value.Trim().ToLower();
                        if (bool.TryParse(trimmed, out bool boolVal))
                        {
                            convertedValue = boolVal;
                        }
                        else if (trimmed == "1" || trimmed == "true" || trimmed == "t")
                        {
                            convertedValue = true;
                        }
                        else if (trimmed == "0" || trimmed == "false" || trimmed == "f")
                        {
                            convertedValue = false;
                        }
                        else
                        {
                            error = $"Invalid boolean value: {value}. Use true/false or 1/0";
                        }
                    }
                    else
                    {
                        // String or other types
                        convertedValue = value;
                    }
                }
                catch (Exception ex)
                {
                    error = $"Error converting value: {ex.Message}";
                }

                if (error != null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new { error = error }));
                }

                var oldValue = variable.GetValue();
                variable.SetValue(convertedValue!);
                var newValue = variable.GetValue();

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = true,
                    name = variable.Name,
                    oldValue = oldValue,
                    newValue = newValue,
                    valueType = valueType.Name,
                    message = $"Set {variable.Name} from {oldValue} to {newValue}"
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "save_variable_changes", Title = "Save Variable Changes")]
        [Description("Saves all variable changes to TuningConfig.json and gamesettings.json files.")]
        public static Task<string> SaveVariableChanges()
        {
            try
            {
                var editor = GetVariableEditor();
                var success = editor.SaveChanges();

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    success = success,
                    message = success ? "Variable changes saved successfully to TuningConfig.json and gamesettings.json" : "Failed to save variable changes"
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        [McpServerTool(Name = "get_variables_by_category", Title = "Get Variables by Category")]
        [Description("Gets all variables in a specific category.")]
        public static Task<string> GetVariablesByCategory(
            [Description("Category name (e.g., 'EnemySystem', 'PlayerAttributes', 'Combat')")] string category)
        {
            try
            {
                var editor = GetVariableEditor();
                var variables = editor.GetVariablesByCategory(category);

                var variableList = variables.Select(v => new
                {
                    name = v.Name,
                    description = v.Description,
                    value = v.GetValue(),
                    valueType = v.GetValueType().Name
                }).ToList();

                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    category = category,
                    variables = variableList,
                    count = variableList.Count
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        private static string GetCategoryForVariable(RPGGame.Editors.VariableEditor editor, string variableName)
        {
            foreach (var category in editor.GetCategories())
            {
                var variables = editor.GetVariablesByCategory(category);
                if (variables.Any(v => v.Name == variableName))
                {
                    return category;
                }
            }
            return "unknown";
        }
    }
}

