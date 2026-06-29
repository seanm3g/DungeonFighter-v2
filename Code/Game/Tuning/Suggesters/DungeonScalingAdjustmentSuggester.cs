using System.Collections.Generic;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class DungeonScalingAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(TuningAnalysis analysis)
        {
            var suggestions = new List<TuningSuggestion>();
            var scaling = GameConfiguration.Instance.DungeonScaling;
            if (scaling == null)
                return suggestions;

            if (analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.MaxTarget)
            {
                int current = scaling.EnemyCountPerRoom;
                int target = System.Math.Max(1, current - 1);
                suggestions.Add(new TuningSuggestion
                {
                    Id = "dungeon_reduce_enemies_per_room",
                    Priority = BalanceTuningGoals.TuningPriority.Medium,
                    Category = "dungeon_scaling",
                    Target = "Dungeon",
                    Parameter = "EnemyCountPerRoom",
                    CurrentValue = current,
                    SuggestedValue = target,
                    AdjustmentMagnitude = current - target,
                    Reason = "Long dungeon fights — reduce enemy density per room",
                    Impact = $"Reduce enemies per room from {current} to {target}"
                });
            }
            else
            {
                double current = scaling.RoomCountPerLevel;
                double target = current + 0.05;
                suggestions.Add(new TuningSuggestion
                {
                    Id = "dungeon_increase_room_scaling",
                    Priority = BalanceTuningGoals.TuningPriority.Low,
                    Category = "dungeon_scaling",
                    Target = "Dungeon",
                    Parameter = "RoomCountPerLevel",
                    CurrentValue = current,
                    SuggestedValue = target,
                    AdjustmentMagnitude = 0.05,
                    Reason = "Scaling dial — increase room count growth per level",
                    Impact = "Slightly increase dungeon length scaling"
                });
            }

            return suggestions;
        }
    }
}
