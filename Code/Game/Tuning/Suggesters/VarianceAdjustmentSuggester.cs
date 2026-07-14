using System.Collections.Generic;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class VarianceAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var config = GameConfiguration.Instance;
            double compression = config.CombatBalance.RollFeelVarianceCompression;

            if (analysis.TurnDurationStdDev > 4.0 || analysis.AverageMissRate > 0.35)
            {
                double target = System.Math.Min(1.0, compression + 0.1);
                suggestions.Add(new TuningSuggestion
                {
                    Id = "variance_increase_compression",
                    Priority = BalanceTuningGoals.TuningPriority.High,
                    Category = "roll_feel",
                    Target = "Global",
                    Parameter = "rollFeelVarianceCompression",
                    CurrentValue = compression,
                    SuggestedValue = target,
                    AdjustmentMagnitude = (target - compression) * 100.0,
                    Reason = $"High outcome spread (turn σ={analysis.TurnDurationStdDev:F1}, miss rate={analysis.AverageMissRate:P0})",
                    Impact = "Increase variance compression toward more predictable combat"
                });
            }
            else if (analysis.AverageComboStreak < 2.0)
            {
                var comboMult = config.CombatBalance.RollDamageMultipliers?.ComboRollDamageMultiplier ?? 1.0;
                double target = System.Math.Max(1.0, comboMult - 0.05);
                suggestions.Add(new TuningSuggestion
                {
                    Id = "variance_lower_combo_mult",
                    Priority = BalanceTuningGoals.TuningPriority.Medium,
                    Category = "roll_feel",
                    Target = "Global",
                    Parameter = "comboRollDamageMult",
                    CurrentValue = comboMult,
                    SuggestedValue = target,
                    AdjustmentMagnitude = (comboMult - target) * 100.0,
                    Reason = $"Low combo streak average ({analysis.AverageComboStreak:F2}) — soften combo payoff spread",
                    Impact = "Reduce combo damage multiplier slightly"
                });
            }

            return suggestions;
        }
    }
}
