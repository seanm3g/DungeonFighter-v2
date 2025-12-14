using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class WeaponBalanceSuggester
    {
        public static List<TuningSuggestion> Suggest(
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();

            if (analysis.WeaponVariance > BalanceTuningGoals.WeaponBalanceTargets.MaxVariance)
            {
                var weaponStats = testResult.WeaponStatistics.OrderBy(kvp => kvp.Value.WinRate).ToList();
                var worstWeapon = weaponStats.First();
                var bestWeapon = weaponStats.Last();

                var worstWinRate = worstWeapon.Value.WinRate;
                var bestWinRate = bestWeapon.Value.WinRate;
                var targetWinRate = (worstWinRate + bestWinRate) / 2.0;

                if (worstWinRate < BalanceTuningGoals.WinRateTargets.OptimalMin)
                {
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(worstWinRate, targetWinRate, 0.05, 0.30);
                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"weapon_buff_{worstWeapon.Key}",
                        Priority = worstWinRate < BalanceTuningGoals.WinRateTargets.CriticalLow 
                            ? BalanceTuningGoals.TuningPriority.Critical 
                            : BalanceTuningGoals.TuningPriority.High,
                        Category = "weapon",
                        Target = worstWeapon.Key.ToString(),
                        Parameter = "DamageMultiplier",
                        CurrentValue = 1.0,
                        SuggestedValue = 1.0 + magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{worstWeapon.Key} has {worstWinRate:F1}% win rate (worst weapon). Weapon balance variance is {analysis.WeaponVariance:F1}%.",
                        Impact = $"Buff {worstWeapon.Key} damage by {magnitude * 100.0:F1}% to improve balance",
                        AffectedMatchups = testResult.CombinationResults
                            .Where(c => c.WeaponType == worstWeapon.Key)
                            .Select(c => $"{c.WeaponType} vs {c.EnemyType}")
                            .ToList()
                    });
                }

                if (bestWinRate > BalanceTuningGoals.WinRateTargets.OptimalMax)
                {
                    var magnitude = BalanceTuningGoals.GetAdjustmentMagnitude(bestWinRate, targetWinRate, 0.05, 0.30);
                    suggestions.Add(new TuningSuggestion
                    {
                        Id = $"weapon_nerf_{bestWeapon.Key}",
                        Priority = bestWinRate > BalanceTuningGoals.WinRateTargets.CriticalHigh 
                            ? BalanceTuningGoals.TuningPriority.Critical 
                            : BalanceTuningGoals.TuningPriority.High,
                        Category = "weapon",
                        Target = bestWeapon.Key.ToString(),
                        Parameter = "DamageMultiplier",
                        CurrentValue = 1.0,
                        SuggestedValue = 1.0 - magnitude,
                        AdjustmentMagnitude = magnitude * 100.0,
                        Reason = $"{bestWeapon.Key} has {bestWinRate:F1}% win rate (best weapon). Weapon balance variance is {analysis.WeaponVariance:F1}%.",
                        Impact = $"Nerf {bestWeapon.Key} damage by {magnitude * 100.0:F1}% to improve balance",
                        AffectedMatchups = testResult.CombinationResults
                            .Where(c => c.WeaponType == bestWeapon.Key)
                            .Select(c => $"{c.WeaponType} vs {c.EnemyType}")
                            .ToList()
                    });
                }
            }

            return suggestions;
        }
    }
}

