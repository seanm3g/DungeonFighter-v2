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

        [McpServerTool(Name = "run_multi_level_simulation", Title = "Run Multi-Level Battle Simulation")]
        [Description("Runs comprehensive weapon×enemy simulations at multiple same-level anchor points and compares against the level win-rate curve.")]
        public static async Task<string> RunMultiLevelSimulation(
            [Description("Comma-separated levels to test (default: 1,5,10,25,50,75,100)")] string? levels = null,
            [Description("Battles per weapon-enemy combination per level (default: 25)")] int battlesPerCombination = 25)
        {
            return await McpToolExecutor.ExecuteAsync(async () =>
            {
                var levelList = ParseLevels(levels);
                var result = await RPGGame.Tuning.MultiLevelSimulationRunner.RunAsync(
                    levelList,
                    battlesPerCombination,
                    new System.Progress<(int completed, int total, string status)>());

                return BuildMultiLevelResponse(result);
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_level_curve_report", Title = "Get Level Curve Report")]
        [Description("Returns a formatted report of the most recent multi-level simulation vs level win-rate targets.")]
        public static Task<string> GetLevelCurveReport()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var result = McpToolState.LastMultiLevelResult;
                if (result == null)
                    throw new InvalidOperationException("No multi-level simulation results. Run run_multi_level_simulation first.");

                return new
                {
                    report = RPGGame.Tuning.MultiLevelSimulationRunner.FormatReport(result),
                    curveScore = result.OverallCurveScore,
                    allAnchorsPass = result.AllAnchorsWithinTolerance,
                    snapshots = result.LevelSnapshots.Select(s => new
                    {
                        level = s.Level,
                        targetWinRate = s.TargetWinRate,
                        actualWinRate = s.ActualWinRate,
                        delta = s.Delta,
                        averageTurns = s.AverageTurns,
                        withinTolerance = s.WithinTolerance
                    }).ToList()
                };
            }, writeIndented: true);
        }

        private static int[] ParseLevels(string? levels)
        {
            if (string.IsNullOrWhiteSpace(levels))
                return LevelWinRateCurve.GetDefaultAnchorLevels().ToArray();

            return levels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.Parse(s))
                .ToArray();
        }

        private static object BuildMultiLevelResponse(RPGGame.Tuning.MultiLevelSimulationResult result) =>
            new
            {
                report = RPGGame.Tuning.MultiLevelSimulationRunner.FormatReport(result),
                curveScore = result.OverallCurveScore,
                allAnchorsPass = result.AllAnchorsWithinTolerance,
                worstLevel = result.WorstLevel,
                worstDelta = result.WorstDeltaMagnitude,
                snapshots = result.LevelSnapshots.Select(s => new
                {
                    level = s.Level,
                    targetWinRate = s.TargetWinRate,
                    actualWinRate = s.ActualWinRate,
                    delta = s.Delta,
                    averageTurns = s.AverageTurns,
                    withinTolerance = s.WithinTolerance
                }).ToList()
            };
    }
}
