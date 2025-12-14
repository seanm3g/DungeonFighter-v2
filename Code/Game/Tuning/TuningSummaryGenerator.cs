using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    public static class TuningSummaryGenerator
    {
        public static string Generate(TuningAnalysis analysis, List<TuningSuggestion> suggestions)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Balance Quality Score: {analysis.QualityScore:F1}/100");
            sb.AppendLine($"Overall Win Rate: {analysis.OverallWinRate:F1}% (Target: 85-98%)");
            sb.AppendLine($"Average Combat Duration: {analysis.AverageCombatDuration:F1} turns (Target: 8-15)");
            sb.AppendLine($"Weapon Balance Variance: {analysis.WeaponVariance:F1}% (Target: <10%)");
            sb.AppendLine($"Enemy Differentiation: {analysis.EnemyVariance:F1}% (Target: >3%)");
            sb.AppendLine();
            sb.AppendLine($"Tuning Suggestions: {suggestions.Count} total");
            foreach (var priority in Enum.GetValues<BalanceTuningGoals.TuningPriority>())
            {
                var count = suggestions.Count(s => s.Priority == priority);
                if (count > 0)
                {
                    sb.AppendLine($"  {priority}: {count} suggestions");
                }
            }

            return sb.ToString();
        }
    }
}

