using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class PlayerAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;

            if (analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMin);

                var currentStrength = config.Attributes.PlayerBaseAttributes.Strength;
                var strengthIncrease = (int)Math.Ceiling(currentStrength * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "player_strength_increase",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = currentStrength + strengthIncrease,
                    AdjustmentMagnitude = (strengthIncrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Player needs more damage output.",
                    Impact = $"Increase player base Strength by {strengthIncrease} (from {currentStrength} to {currentStrength + strengthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                var currentHealth = config.Character.PlayerBaseHealth;
                var healthIncrease = (int)Math.Ceiling(currentHealth * magnitude * 0.3);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "player_health_increase",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseHealth",
                    CurrentValue = currentHealth,
                    SuggestedValue = currentHealth + healthIncrease,
                    AdjustmentMagnitude = (healthIncrease / (double)currentHealth) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Player needs more survivability.",
                    Impact = $"Increase player base health by {healthIncrease} (from {currentHealth} to {currentHealth + healthIncrease})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            else if (analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMax);

                var currentStrength = config.Attributes.PlayerBaseAttributes.Strength;
                var strengthDecrease = (int)Math.Ceiling(currentStrength * magnitude * 0.5);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "player_strength_decrease",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseStrength",
                    CurrentValue = currentStrength,
                    SuggestedValue = Math.Max(1, currentStrength - strengthDecrease),
                    AdjustmentMagnitude = (strengthDecrease / (double)currentStrength) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Player is too strong.",
                    Impact = $"Decrease player base Strength by {strengthDecrease} (from {currentStrength} to {Math.Max(1, currentStrength - strengthDecrease)})",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }
    }
}

