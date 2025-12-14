using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class GlobalAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;
            var multipliers = config.EnemySystem.GlobalMultipliers;

            if (analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMin);
                
                var healthAdjustment = 1.0 - (magnitude * 0.6);
                var damageAdjustment = 1.0 - (magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_health_reduce",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = multipliers.HealthMultiplier,
                    SuggestedValue = multipliers.HealthMultiplier * healthAdjustment,
                    AdjustmentMagnitude = (1.0 - healthAdjustment) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target (85-98%). Enemies are too strong.",
                    Impact = $"Reduce enemy health by {(1.0 - healthAdjustment) * 100.0:F1}% to make enemies easier",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_damage_reduce",
                    Priority = analysis.OverallWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "DamageMultiplier",
                    CurrentValue = multipliers.DamageMultiplier,
                    SuggestedValue = multipliers.DamageMultiplier * damageAdjustment,
                    AdjustmentMagnitude = (1.0 - damageAdjustment) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is below target. Reducing enemy damage.",
                    Impact = $"Reduce enemy damage by {(1.0 - damageAdjustment) * 100.0:F1}%",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }
            else if (analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
            {
                var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                    analysis.OverallWinRate, 
                    BalanceTuningGoals.WinRateTargets.OptimalMax);
                
                var healthAdjustment = 1.0 + (magnitude * 0.6);
                var damageAdjustment = 1.0 + (magnitude * 0.4);

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_health_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "HealthMultiplier",
                    CurrentValue = multipliers.HealthMultiplier,
                    SuggestedValue = multipliers.HealthMultiplier * healthAdjustment,
                    AdjustmentMagnitude = (healthAdjustment - 1.0) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target (85-98%). Enemies are too weak.",
                    Impact = $"Increase enemy health by {(healthAdjustment - 1.0) * 100.0:F1}% to make enemies harder",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });

                suggestions.Add(new TuningSuggestion
                {
                    Id = "global_damage_increase",
                    Priority = analysis.OverallWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                        ? BalanceTuningGoals.TuningPriority.Critical 
                        : BalanceTuningGoals.TuningPriority.High,
                    Category = "global",
                    Target = "All Enemies",
                    Parameter = "DamageMultiplier",
                    CurrentValue = multipliers.DamageMultiplier,
                    SuggestedValue = multipliers.DamageMultiplier * damageAdjustment,
                    AdjustmentMagnitude = (damageAdjustment - 1.0) * 100.0,
                    Reason = $"Overall win rate {analysis.OverallWinRate:F1}% is above target. Increasing enemy damage.",
                    Impact = $"Increase enemy damage by {(damageAdjustment - 1.0) * 100.0:F1}%",
                    AffectedMatchups = testResult.CombinationResults.Select(c => $"{c.WeaponType} vs {c.EnemyType}").ToList()
                });
            }

            return suggestions;
        }
    }
}

