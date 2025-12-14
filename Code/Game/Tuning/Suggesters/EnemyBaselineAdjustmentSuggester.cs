using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class EnemyBaselineAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;
            var baselineStats = config.EnemySystem.BaselineStats;

            if (analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMin);

                var currentStrength = baselineStats.Strength;
                var strengthDecrease = (int)Math.Ceiling(currentStrength * magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_strength_decrease",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = Math.Max(1, currentStrength - strengthDecrease),
                    AdjustmentMagnitude = (strengthDecrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Enemies deal too much damage.",
                    Impact = $"Decrease enemy baseline Strength by {strengthDecrease} (from {currentStrength} to {Math.Max(1, currentStrength - strengthDecrease)})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                var currentHealth = baselineStats.Health;
                var healthDecrease = (int)Math.Ceiling(currentHealth * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_health_decrease",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineHealth",
                    CurrentValue = currentHealth,
                    SuggestedValue = Math.Max(1, currentHealth - healthDecrease),
                    AdjustmentMagnitude = (healthDecrease / (double)currentHealth) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Enemies have too much health.",
                    Impact = $"Decrease enemy baseline health by {healthDecrease} (from {currentHealth} to {Math.Max(1, currentHealth - healthDecrease)})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            else if (analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMax);

                var currentStrength = baselineStats.Strength;
                var strengthIncrease = (int)Math.Ceiling(currentStrength * magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_strength_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = currentStrength + strengthIncrease,
                    AdjustmentMagnitude = (strengthIncrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Enemies need more damage.",
                    Impact = $"Increase enemy baseline Strength by {strengthIncrease} (from {currentStrength} to {currentStrength + strengthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                var currentHealth = baselineStats.Health;
                var healthIncrease = (int)Math.Ceiling(currentHealth * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "enemy_baseline_health_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "enemy_baseline",
                    Target = "All Enemies",
                    Parameter = "BaselineHealth",
                    CurrentValue = currentHealth,
                    SuggestedValue = currentHealth + healthIncrease,
                    AdjustmentMagnitude = (healthIncrease / (double)currentHealth) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Enemies need more health.",
                    Impact = $"Increase enemy baseline health by {healthIncrease} (from {currentHealth} to {currentHealth + healthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }
    }
}

