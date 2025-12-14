using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame;
using RPGGame.Config;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Automated tuning tools
    /// </summary>
    public static class AutomatedTuningTools
    {
        [McpServerTool(Name = "suggest_tuning", Title = "Suggest Tuning Adjustments")]
        [Description("Analyzes the most recent battle simulation results and suggests specific tuning adjustments with priorities. Run run_battle_simulation first.")]
        public static Task<string> SuggestTuning()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                var analysis = AutomatedTuningEngine.AnalyzeAndSuggest(lastResult);

                return new
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
            }, writeIndented: true);
        }

        [McpServerTool(Name = "apply_tuning_suggestion", Title = "Apply Tuning Suggestion")]
        [Description("Applies a specific tuning suggestion by ID. Use suggest_tuning first to get suggestion IDs.")]
        public static Task<string> ApplyTuningSuggestion(
            [Description("Suggestion ID from suggest_tuning results")] string suggestionId)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                var analysis = AutomatedTuningEngine.AnalyzeAndSuggest(lastResult);
                var suggestion = analysis.Suggestions.FirstOrDefault(s => s.Id == suggestionId);

                if (suggestion == null)
                {
                    throw new InvalidOperationException($"Suggestion '{suggestionId}' not found. Run suggest_tuning to get current suggestions.");
                }

                var tuningSuggestion = new AutomatedTuningEngine.TuningSuggestion
                {
                    Id = suggestion.Id,
                    Priority = suggestion.Priority,
                    Category = suggestion.Category,
                    Target = suggestion.Target,
                    Parameter = suggestion.Parameter,
                    CurrentValue = suggestion.CurrentValue,
                    SuggestedValue = suggestion.SuggestedValue,
                    AdjustmentMagnitude = suggestion.AdjustmentMagnitude,
                    Reason = suggestion.Reason,
                    Impact = suggestion.Impact,
                    AffectedMatchups = suggestion.AffectedMatchups
                };
                var success = AutomatedTuningEngine.ApplySuggestion(tuningSuggestion);

                return new
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
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_balance_quality_score", Title = "Get Balance Quality Score")]
        [Description("Gets the overall balance quality score (0-100) based on the most recent simulation results. Run run_battle_simulation first.")]
        public static Task<string> GetBalanceQualityScore()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                var analysis = AutomatedTuningEngine.AnalyzeAndSuggest(lastResult);

                return new
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
            }, writeIndented: true);
        }

        [McpServerTool(Name = "set_baseline", Title = "Set Baseline for Comparison")]
        [Description("Sets the current simulation results as baseline for future comparisons. Run run_battle_simulation first.")]
        public static Task<string> SetBaseline()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No simulation results available. Run run_battle_simulation first.");
                }

                McpToolState.BaselineTestResult = lastResult;

                return new
                {
                    success = true,
                    message = "Baseline set successfully",
                    baselineMetrics = new
                    {
                        overallWinRate = lastResult.OverallWinRate,
                        averageCombatDuration = lastResult.OverallAverageTurns,
                        totalBattles = lastResult.TotalBattles
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "compare_with_baseline", Title = "Compare Current Results with Baseline")]
        [Description("Compares current simulation results with the baseline. Set baseline first with set_baseline.")]
        public static Task<string> CompareWithBaseline()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var lastResult = McpToolState.LastTestResult;
                if (lastResult == null)
                {
                    throw new InvalidOperationException("No current simulation results available. Run run_battle_simulation first.");
                }

                var baselineResult = McpToolState.BaselineTestResult;
                if (baselineResult == null)
                {
                    throw new InvalidOperationException("No baseline set. Run set_baseline first.");
                }

                var baselineAnalysis = AutomatedTuningEngine.AnalyzeAndSuggest(baselineResult);
                var currentAnalysis = AutomatedTuningEngine.AnalyzeAndSuggest(lastResult);

                return new
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
            }, writeIndented: true);
        }
    }
}
