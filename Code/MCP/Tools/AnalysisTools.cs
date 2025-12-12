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
    /// Analysis tools for battle results
    /// </summary>
    public static class AnalysisTools
    {
        [McpServerTool(Name = "analyze_battle_results", Title = "Analyze Battle Results")]
        [Description("Analyzes the most recent battle simulation results and generates a detailed analysis report with issues and recommendations. Run run_battle_simulation first.")]
        public static Task<string> AnalyzeBattleResults()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                var analysis = MatchupAnalyzer.Analyze(lastResult);
                var textReport = MatchupAnalyzer.GenerateTextReport(analysis);

                return new
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
            }, writeIndented: true);
        }

        [McpServerTool(Name = "validate_balance", Title = "Validate Balance")]
        [Description("Validates balance based on the most recent battle simulation results. Returns validation report with errors and warnings. Run run_battle_simulation first.")]
        public static Task<string> ValidateBalance()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                var validation = BalanceValidator.Validate(lastResult);
                var report = BalanceValidator.GenerateReport(validation);

                return new
                {
                    isValid = validation.IsValid,
                    totalChecks = validation.TotalChecks,
                    passedChecks = validation.PassedChecks,
                    errors = validation.Errors,
                    warnings = validation.Warnings,
                    report = report
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "analyze_fun_moments", Title = "Analyze Fun Moments")]
        [Description("Analyzes fun moment data from the most recent battle simulation. Shows which weapons/classes create the most engaging gameplay. Run run_battle_simulation first.")]
        public static Task<string> AnalyzeFunMoments()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                // Collect all fun moment summaries from battle results
                var allFunSummaries = lastResult.CombinationResults
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.FunMomentSummary != null)
                    .Select(r => r.FunMomentSummary!)
                    .ToList();

                if (allFunSummaries.Count == 0)
                {
                    return new
                    {
                        message = "No fun moment data available in simulation results.",
                        totalBattles = lastResult.TotalBattles
                    };
                }

                // Aggregate fun moment data by weapon type
                var weaponFunData = lastResult.WeaponTypes.ToDictionary(
                    weaponType => weaponType.ToString(),
                    weaponType =>
                    {
                        var weaponResults = lastResult.CombinationResults
                            .Where(c => c.WeaponType == weaponType)
                            .SelectMany(c => c.BattleResults)
                            .Where(r => r.FunMomentSummary != null)
                            .Select(r => r.FunMomentSummary!)
                            .ToList();

                        if (weaponResults.Count == 0)
                            return null;

                        return new
                        {
                            averageFunScore = weaponResults.Average(r => r.FunScore),
                            averageActionVariety = weaponResults.Average(r => r.ActionVarietyScore),
                            averageTurnVariance = weaponResults.Average(r => r.TurnVariance),
                            totalHealthLeadChanges = weaponResults.Sum(r => r.HealthLeadChanges),
                            totalComebacks = weaponResults.Sum(r => r.Comebacks),
                            totalCloseCalls = weaponResults.Sum(r => r.CloseCalls),
                            totalFunMoments = weaponResults.Sum(r => r.TotalFunMoments),
                            momentsByType = weaponResults
                                .SelectMany(r => r.MomentsByType)
                                .GroupBy(kvp => kvp.Key.ToString())
                                .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value))
                        };
                    });

                // Aggregate fun moment data by enemy type
                var enemyFunData = lastResult.EnemyTypes.ToDictionary(
                    enemyType => enemyType,
                    enemyType =>
                    {
                        var enemyResults = lastResult.CombinationResults
                            .Where(c => c.EnemyType == enemyType)
                            .SelectMany(c => c.BattleResults)
                            .Where(r => r.FunMomentSummary != null)
                            .Select(r => r.FunMomentSummary!)
                            .ToList();

                        if (enemyResults.Count == 0)
                            return null;

                        return new
                        {
                            averageFunScore = enemyResults.Average(r => r.FunScore),
                            averageActionVariety = enemyResults.Average(r => r.ActionVarietyScore),
                            averageTurnVariance = enemyResults.Average(r => r.TurnVariance),
                            totalHealthLeadChanges = enemyResults.Sum(r => r.HealthLeadChanges),
                            totalComebacks = enemyResults.Sum(r => r.Comebacks),
                            totalCloseCalls = enemyResults.Sum(r => r.CloseCalls),
                            totalFunMoments = enemyResults.Sum(r => r.TotalFunMoments)
                        };
                    });

                // Overall fun moment statistics
                var overallStats = new
                {
                    overallAverageFunScore = allFunSummaries.Average(r => r.FunScore),
                    overallAverageActionVariety = allFunSummaries.Average(r => r.ActionVarietyScore),
                    overallAverageTurnVariance = allFunSummaries.Average(r => r.TurnVariance),
                    totalHealthLeadChanges = allFunSummaries.Sum(r => r.HealthLeadChanges),
                    totalComebacks = allFunSummaries.Sum(r => r.Comebacks),
                    totalCloseCalls = allFunSummaries.Sum(r => r.CloseCalls),
                    totalFunMoments = allFunSummaries.Sum(r => r.TotalFunMoments),
                    averageFunMomentsPerBattle = allFunSummaries.Average(r => r.TotalFunMoments),
                    momentsByType = allFunSummaries
                        .SelectMany(r => r.MomentsByType)
                        .GroupBy(kvp => kvp.Key.ToString())
                        .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value))
                };

                // Find top weapons by fun score
                var topWeapons = weaponFunData
                    .Where(kvp => kvp.Value != null)
                    .OrderByDescending(kvp => kvp.Value!.averageFunScore)
                    .Take(5)
                    .Select(kvp => new { weapon = kvp.Key, funScore = kvp.Value!.averageFunScore, actionVariety = kvp.Value.averageActionVariety })
                    .ToList();

                return new
                {
                    overallStats = overallStats,
                    weaponAnalysis = weaponFunData.Where(kvp => kvp.Value != null).ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!),
                    enemyAnalysis = enemyFunData.Where(kvp => kvp.Value != null).ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!),
                    topWeaponsByFun = topWeapons,
                    interpretation = new
                    {
                        bestWeaponForFun = topWeapons.FirstOrDefault()?.weapon ?? "N/A",
                        overallFunRating = overallStats.overallAverageFunScore >= 70 ? "High" :
                                          overallStats.overallAverageFunScore >= 50 ? "Medium" : "Low",
                        recommendation = overallStats.overallAverageFunScore < 50 
                            ? "Consider increasing action variety, damage variance, or health lead changes to improve engagement"
                            : overallStats.overallAverageFunScore >= 70
                            ? "Excellent engagement! Gameplay is dynamic and compelling."
                            : "Good engagement. Some improvements could enhance the experience further."
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_fun_moment_summary", Title = "Get Fun Moment Summary")]
        [Description("Gets a detailed summary of fun moments from the most recent battle simulation. Shows breakdown by type and intensity. Run run_battle_simulation first.")]
        public static Task<string> GetFunMomentSummary()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                // Collect all fun moment summaries
                var allFunSummaries = lastResult.CombinationResults
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.FunMomentSummary != null)
                    .Select(r => r.FunMomentSummary!)
                    .ToList();

                if (allFunSummaries.Count == 0)
                {
                    return new
                    {
                        message = "No fun moment data available.",
                        totalBattles = lastResult.TotalBattles
                    };
                }

                // Aggregate all fun moments by type
                var allMoments = lastResult.CombinationResults
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.FunMomentSummary != null)
                    .SelectMany(r => r.FunMomentSummary!.TopMoments)
                    .ToList();

                return new
                {
                    overall = new
                    {
                        totalBattlesWithData = allFunSummaries.Count,
                        averageFunScore = allFunSummaries.Average(r => r.FunScore),
                        averageIntensity = allFunSummaries.Average(r => r.AverageIntensity),
                        averageActionVariety = allFunSummaries.Average(r => r.ActionVarietyScore),
                        averageTurnVariance = allFunSummaries.Average(r => r.TurnVariance),
                        totalHealthLeadChanges = allFunSummaries.Sum(r => r.HealthLeadChanges),
                        totalComebacks = allFunSummaries.Sum(r => r.Comebacks),
                        totalCloseCalls = allFunSummaries.Sum(r => r.CloseCalls)
                    },
                    momentsByType = allFunSummaries
                        .SelectMany(r => r.MomentsByType)
                        .GroupBy(kvp => kvp.Key.ToString())
                        .ToDictionary(
                            g => g.Key,
                            g => new
                            {
                                totalCount = g.Sum(kvp => kvp.Value),
                                averagePerBattle = g.Sum(kvp => kvp.Value) / (double)allFunSummaries.Count
                            }),
                    topMoments = allMoments
                        .OrderByDescending(m => m.Intensity)
                        .Take(10)
                        .Select(m => new
                        {
                            type = m.Type.ToString(),
                            turn = m.Turn,
                            actor = m.Actor,
                            target = m.Target,
                            intensity = m.Intensity,
                            description = m.Description
                        })
                        .ToList(),
                    weaponComparison = lastResult.WeaponTypes.ToDictionary(
                        w => w.ToString(),
                        w =>
                        {
                            var weaponStats = lastResult.WeaponStatistics.GetValueOrDefault(w);
                            return weaponStats != null ? new
                            {
                                averageFunScore = weaponStats.AverageFunScore,
                                averageActionVariety = weaponStats.AverageActionVariety,
                                averageHealthLeadChanges = weaponStats.AverageHealthLeadChanges
                            } : null;
                        })
                };
            }, writeIndented: true);
        }
    }
}
