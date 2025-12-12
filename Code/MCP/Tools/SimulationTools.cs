using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Battle simulation tools
    /// </summary>
    public static class SimulationTools
    {
        [McpServerTool(Name = "run_battle_simulation", Title = "Run Battle Simulation")]
        [Description("Runs comprehensive battle simulations testing all weapon types against all enemy types. Returns detailed statistics.")]
        public static async Task<string> RunBattleSimulation(
            [Description("Number of battles per weapon-enemy combination (default: 50)")] int battlesPerCombination = 50,
            [Description("Player level for testing (default: 1)")] int playerLevel = 1,
            [Description("Enemy level for testing (default: 1)")] int enemyLevel = 1)
        {
            return await McpToolExecutor.ExecuteAsync(async () =>
            {
                var progress = new System.Progress<(int completed, int total, string status)>();
                var testResult = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination,
                    playerLevel,
                    enemyLevel,
                    progress);

                // Cache the result for analysis tools
                McpToolState.LastTestResult = testResult;

                // Serialize result with summary including fun moment data
                return new
                {
                    overallWinRate = testResult.OverallWinRate,
                    totalBattles = testResult.TotalBattles,
                    totalPlayerWins = testResult.TotalPlayerWins,
                    totalEnemyWins = testResult.TotalEnemyWins,
                    overallAverageTurns = testResult.OverallAverageTurns,
                    funMoments = new
                    {
                        overallAverageFunScore = testResult.OverallAverageFunScore,
                        overallAverageActionVariety = testResult.OverallAverageActionVariety,
                        overallAverageTurnVariance = testResult.OverallAverageTurnVariance,
                        totalHealthLeadChanges = testResult.TotalHealthLeadChanges,
                        totalComebacks = testResult.TotalComebacks,
                        totalCloseCalls = testResult.TotalCloseCalls
                    },
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
                            averageDamage = kvp.Value.AverageDamage,
                            averageFunScore = kvp.Value.AverageFunScore,
                            averageActionVariety = kvp.Value.AverageActionVariety,
                            averageHealthLeadChanges = kvp.Value.AverageHealthLeadChanges
                        }),
                    enemyStatistics = testResult.EnemyStatistics.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            totalBattles = kvp.Value.TotalBattles,
                            wins = kvp.Value.Wins,
                            winRate = kvp.Value.WinRate,
                            averageTurns = kvp.Value.AverageTurns,
                            averageDamageReceived = kvp.Value.AverageDamageReceived,
                            averageFunScore = kvp.Value.AverageFunScore,
                            averageActionVariety = kvp.Value.AverageActionVariety,
                            averageHealthLeadChanges = kvp.Value.AverageHealthLeadChanges
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
            }, writeIndented: true);
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
            return await McpToolExecutor.ExecuteAsync(async () =>
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

                var progress = new System.Progress<(int completed, int total, string status)>();
                var result = await BattleStatisticsRunner.RunParallelBattles(config, numberOfBattles, progress);

                return new
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
            }, writeIndented: true);
        }
    }
}
