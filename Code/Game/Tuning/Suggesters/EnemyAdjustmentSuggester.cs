using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class EnemyAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();

            var problematicMatchups = testResult.CombinationResults
                .Where(c => c.WinRate < BalanceTuningGoals.WinRateTargets.MinTarget || 
                           c.WinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
                .OrderBy(c => Math.Abs(c.WinRate - 91.5))
                .Take(5)
                .ToList();

            foreach (var matchup in problematicMatchups)
            {
                var priority = BalanceTuningGoals.GetMatchupPriority(matchup.WinRate, matchup.AverageTurns);
                
                if (matchup.WinRate < BalanceTuningGoals.WinRateTargets.MinTarget)
                {
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                        matchup.WinRate, 
                        BalanceTuningGoals.WinRateTargets.OptimalMin,
                        0.05, 0.25);

                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"enemy_nerf_{matchup.EnemyType}",
                        Priority = priority,
                        Category = "enemy",
                        Target = matchup.EnemyType,
                        Parameter = "HealthMultiplier",
                        CurrentValue = 1.0,
                        SuggestedValue = 1.0 - magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate (below 85% target)",
                        Impact = $"Reduce {matchup.EnemyType} health by {magnitude * 100.0:F1}%",
                        AffectedMatchups = new List<string> { $"{matchup.WeaponType} vs {matchup.EnemyType}" }
                    });
                }
                else if (matchup.WinRate > BalanceTuningGoals.WinRateTargets.MaxTarget)
                {
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(
                        matchup.WinRate, 
                        BalanceTuningGoals.WinRateTargets.OptimalMax,
                        0.05, 0.25);

                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"enemy_buff_{matchup.EnemyType}",
                        Priority = priority,
                        Category = "enemy",
                        Target = matchup.EnemyType,
                        Parameter = "HealthMultiplier",
                        CurrentValue = 1.0,
                        SuggestedValue = 1.0 + magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate (above 98% target)",
                        Impact = $"Increase {matchup.EnemyType} health by {magnitude * 100.0:F1}%",
                        AffectedMatchups = new List<string> { $"{matchup.WeaponType} vs {matchup.EnemyType}" }
                    });
                }
            }

            return suggestions;
        }
    }
}

