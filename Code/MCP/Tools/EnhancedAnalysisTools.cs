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
    /// Enhanced analysis tools
    /// </summary>
    public static class EnhancedAnalysisTools
    {
        [McpServerTool(Name = "analyze_parameter_sensitivity", Title = "Analyze Parameter Sensitivity")]
        [Description("Analyzes how sensitive a parameter is to changes. Tests parameter across a range and identifies optimal value. Helps identify which parameters have the most impact on balance.")]
        public static async Task<string> AnalyzeParameterSensitivity(
            [Description("Parameter name (e.g., 'enemy.globalmultipliers.health', 'player.baseattributes.strength')")] string parameter,
            [Description("Test range as percentage (e.g., '0.8,1.2' for 80%-120% of current value)")] string range = "0.8,1.2",
            [Description("Number of test points across the range (default: 10)")] int testPoints = 10,
            [Description("Battles per test point (default: 50)")] int battlesPerPoint = 50)
        {
            return await McpToolExecutor.ExecuteAsync(async () =>
            {
                var progress = new System.Progress<(int completed, int total, string status)>();
                var result = await ParameterSensitivityAnalyzer.AnalyzeParameter(parameter, range, testPoints, battlesPerPoint, progress);

                return new
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
            }, writeIndented: true);
        }

        [McpServerTool(Name = "test_what_if", Title = "Test What-If Scenario")]
        [Description("Tests a hypothetical parameter change without applying it permanently. Compares current configuration with test value and provides risk assessment.")]
        public static async Task<string> TestWhatIf(
            [Description("Parameter name (e.g., 'enemy.globalmultipliers.health')")] string parameter,
            [Description("New value to test")] double value,
            [Description("Number of battles to run for comparison (default: 200)")] int numberOfBattles = 200)
        {
            return await McpToolExecutor.ExecuteAsync(async () =>
            {
                var progress = new System.Progress<(int completed, int total, string status)>();
                var result = await WhatIfTester.TestWhatIf(parameter, value, numberOfBattles, progress);

                return new
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
            }, writeIndented: true);
        }

        [McpServerTool(Name = "run_battle_simulation_with_logs", Title = "Run Battle Simulation with Turn Logs")]
        [Description("Runs battle simulation with detailed turn-by-turn logs. Returns enhanced data including action usage, damage distribution, and combat flow analysis.")]
        public static async Task<string> RunBattleSimulationWithLogs(
            [Description("Number of battles per weapon-enemy combination (default: 50)")] int battlesPerCombination = 50,
            [Description("Player level for testing (default: 1)")] int playerLevel = 1,
            [Description("Enemy level for testing (default: 1)")] int enemyLevel = 1,
            [Description("Include turn-by-turn logs (default: true, may be slower)")] bool includeTurnLogs = true)
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

                return new
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
            }, writeIndented: true);
        }
    }
}
