using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Tuning;
using RPGGame.Tuning.Suggesters;

namespace RPGGame
{
    /// <summary>
    /// Automated tuning engine that analyzes battle results and suggests specific adjustments
    /// Facade coordinating tuning suggestion generation and application
    /// </summary>
    public static class AutomatedTuningEngine
    {
        // Nested classes for backward compatibility
        public class TuningSuggestion : RPGGame.Tuning.TuningSuggestion { }
        public class TuningAnalysis : RPGGame.Tuning.TuningAnalysis { }

        /// <summary>
        /// Analyze battle results and generate tuning suggestions
        /// </summary>
        public static TuningAnalysis AnalyzeAndSuggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult)
        {
            var analysis = new TuningAnalysis
            {
                OverallWinRate = testResult.OverallWinRate,
                AverageCombatDuration = testResult.OverallAverageTurns
            };

            if (testResult.WeaponStatistics.Count > 0)
            {
                var weaponWinRates = testResult.WeaponStatistics.Values.Select(w => w.WinRate).ToList();
                analysis.WeaponVariance = weaponWinRates.Max() - weaponWinRates.Min();
            }

            if (testResult.EnemyStatistics.Count > 0)
            {
                var enemyWinRates = testResult.EnemyStatistics.Values.Select(e => e.WinRate).ToList();
                analysis.EnemyVariance = enemyWinRates.Max() - enemyWinRates.Min();
            }

            analysis.QualityScore = BalanceTuningGoals.CalculateQualityScore(
                analysis.OverallWinRate,
                analysis.AverageCombatDuration,
                analysis.WeaponVariance,
                analysis.EnemyVariance);

            var suggestions = new List<RPGGame.Tuning.TuningSuggestion>();

            suggestions.AddRange(GlobalAdjustmentSuggester.Suggest(testResult, analysis));
            suggestions.AddRange(PlayerAdjustmentSuggester.Suggest(testResult, analysis));
            suggestions.AddRange(EnemyBaselineAdjustmentSuggester.Suggest(testResult, analysis));
            suggestions.AddRange(WeaponBalanceSuggester.Suggest(testResult, analysis));
            suggestions.AddRange(EnemyAdjustmentSuggester.Suggest(testResult, analysis));
            suggestions.AddRange(DurationAdjustmentSuggester.Suggest(testResult, analysis));

            suggestions = suggestions.OrderBy(s => (int)s.Priority).ThenByDescending(s => Math.Abs(s.AdjustmentMagnitude)).ToList();

            analysis.Suggestions = suggestions;

            analysis.SuggestionCounts = suggestions
                .GroupBy(s => s.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            analysis.Summary = TuningSummaryGenerator.Generate(analysis, suggestions);

            return analysis;
        }

        /// <summary>
        /// Apply a tuning suggestion
        /// </summary>
        public static bool ApplySuggestion(TuningSuggestion suggestion)
        {
            var baseSuggestion = new RPGGame.Tuning.TuningSuggestion
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
            return TuningSuggestionApplier.Apply(baseSuggestion);
        }
    }
}
