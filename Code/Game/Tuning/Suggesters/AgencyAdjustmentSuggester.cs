using System.Collections.Generic;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class AgencyAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var attrs = GameConfiguration.Instance.Attributes.PlayerBaseAttributes;

            if (analysis.AverageLossSeverity > 50 || analysis.AverageMissRate > 0.4)
            {
                int currentTec = attrs.Technique;
                int target = currentTec + 1;
                suggestions.Add(new TuningSuggestion
                {
                    Id = "agency_raise_base_technique",
                    Priority = BalanceTuningGoals.TuningPriority.High,
                    Category = "player",
                    Target = "Player",
                    Parameter = "BaseTechnique",
                    CurrentValue = currentTec,
                    SuggestedValue = target,
                    AdjustmentMagnitude = 1,
                    Reason = $"High loss severity ({analysis.AverageLossSeverity:F0}) or miss rate ({analysis.AverageMissRate:P0}) — improve roll control",
                    Impact = "Raise base TEC for threshold milestones and reliability"
                });
            }

            return suggestions;
        }
    }
}
