using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class DurationAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();

            if (analysis.AverageCombatDuration < BalanceTuningGoals.CombatDurationTargets.MinTarget)
            {
                var magnitude = (BalanceTuningGoals.CombatDurationTargets.OptimalMin - analysis.AverageCombatDuration) / 
                               BalanceTuningGoals.CombatDurationTargets.OptimalMin;
                magnitude = Math.Clamp(magnitude, 0.05, 0.30);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "duration_increase_health",
                    Priority = analysis.AverageCombatDuration < BalanceTuningGoals.CombatDurationTargets.CriticalShort 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier,
                    SuggestedValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier * (1.0 + magnitude),
                    AdjustmentMagnitude = magnitude * 100.0,
                    Reason = $"Average combat duration {analysis.AverageCombatDuration:F1} turns is below target (8-15 turns)",
                    Impact = $"Increase enemy health by {magnitude * 100.0:F1}% to lengthen combat",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            else if (analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.MaxTarget)
            {
                var magnitude = (analysis.AverageCombatDuration - BalanceTuningGoals.CombatDurationTargets.OptimalMax) / 
                               BalanceTuningGoals.CombatDurationTargets.OptimalMax;
                magnitude = Math.Clamp(magnitude, 0.05, 0.30);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "duration_decrease_health",
                    Priority = analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.CriticalLong 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier,
                    SuggestedValue = GameConfiguration.Instance.EnemySystem.GlobalMultipliers.HealthMultiplier * (1.0 - magnitude),
                    AdjustmentMagnitude = magnitude * 100.0,
                    Reason = $"Average combat duration {analysis.AverageCombatDuration:F1} turns is above target (8-15 turns)",
                    Impact = $"Reduce enemy health by {magnitude * 100.0:F1}% to shorten combat",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }
    }
}

