using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    /// <summary>
    /// Suggests a single knob adjustment to move same-level win rate toward the level curve.
    /// </summary>
    public static class LevelCurveAdjustmentSuggester
    {
        private const double MaxMagnitudePerIteration = 0.25;

        public static List<TuningSuggestion> Suggest(MultiLevelSimulationResult multiResult)
        {
            var suggestions = new List<TuningSuggestion>();
            if (multiResult?.LevelSnapshots == null || multiResult.LevelSnapshots.Count == 0)
                return suggestions;

            var worst = multiResult.LevelSnapshots
                .OrderByDescending(s => Math.Abs(s.Delta))
                .First();

            if (worst.WithinTolerance)
                return suggestions;

            var config = GameConfiguration.Instance;
            double target = worst.TargetWinRate;
            double actual = worst.ActualWinRate;
            double rawMagnitude = BalanceTuningGoals.GetAdjustmentMagnitude(actual, target);
            double magnitude = Math.Min(rawMagnitude, MaxMagnitudePerIteration);

            bool tooEasy = actual > target;
            int level = worst.Level;

            if (tooEasy)
            {
                if (level <= 10)
                {
                    suggestions.Add(BuildScalingHealthSuggestion(config, magnitude, level, actual, target));
                }
                else
                {
                    suggestions.Add(BuildAttributeGrowthSuggestion(config, magnitude, level, actual, target));
                }
            }
            else
            {
                if (level <= 10)
                {
                    suggestions.Add(BuildPlayerHealthSuggestion(config, magnitude, level, actual, target));
                }
                else
                {
                    suggestions.Add(BuildDamageReduceSuggestion(config, magnitude, level, actual, target));
                }
            }

            return suggestions.Take(1).ToList();
        }

        private static TuningSuggestion BuildScalingHealthSuggestion(
            GameConfiguration config, double magnitude, int level, double actual, double target)
        {
            var scaling = config.EnemySystem.ScalingPerLevel;
            int current = scaling.Health;
            int delta = Math.Max(1, (int)Math.Ceiling(Math.Max(current, 1) * magnitude));
            int suggested = current + delta;

            return new TuningSuggestion
            {
                Id = $"level_curve_health_scaling_L{level}",
                Priority = BalanceTuningGoals.TuningPriority.High,
                Category = "enemy_scaling",
                Target = $"Level {level} curve",
                Parameter = "health",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = magnitude * 100.0,
                Reason = $"Level {level} win rate {actual:F1}% exceeds target {target:F1}%. Enemy HP growth per level is too low.",
                Impact = $"Increase enemy health per level from {current} to {suggested}",
                AffectedMatchups = new List<string> { $"Same-level L{level}" }
            };
        }

        private static TuningSuggestion BuildAttributeGrowthSuggestion(
            GameConfiguration config, double magnitude, int level, double actual, double target)
        {
            var prog = config.EnemySystem.ProgressionScales;
            double current = prog.AttributeGrowthScale;
            double suggested = current * (1.0 + magnitude);

            return new TuningSuggestion
            {
                Id = $"level_curve_attr_growth_L{level}",
                Priority = BalanceTuningGoals.TuningPriority.High,
                Category = "enemy_progression",
                Target = $"Level {level} curve",
                Parameter = "AttributeGrowthScale",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = magnitude * 100.0,
                Reason = $"Level {level} win rate {actual:F1}% exceeds target {target:F1}%. High-level enemy attribute growth is too low.",
                Impact = $"Increase attribute growth scale from {current:F3} to {suggested:F3}",
                AffectedMatchups = new List<string> { $"Same-level L{level}" }
            };
        }

        private static TuningSuggestion BuildPlayerHealthSuggestion(
            GameConfiguration config, double magnitude, int level, double actual, double target)
        {
            int current = config.Character.PlayerBaseHealth;
            int delta = Math.Max(1, (int)Math.Ceiling(current * magnitude * 0.5));
            int suggested = current + delta;

            return new TuningSuggestion
            {
                Id = $"level_curve_player_hp_L{level}",
                Priority = BalanceTuningGoals.TuningPriority.High,
                Category = "player",
                Target = $"Level {level} curve",
                Parameter = "BaseHealth",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = magnitude * 100.0,
                Reason = $"Level {level} win rate {actual:F1}% is below target {target:F1}%. Player survivability is too low.",
                Impact = $"Increase player base health from {current} to {suggested}",
                AffectedMatchups = new List<string> { $"Same-level L{level}" }
            };
        }

        private static TuningSuggestion BuildDamageReduceSuggestion(
            GameConfiguration config, double magnitude, int level, double actual, double target)
        {
            var multipliers = config.EnemySystem.GlobalMultipliers;
            double current = multipliers.DamageMultiplier;
            double suggested = current * (1.0 - magnitude * 0.5);

            return new TuningSuggestion
            {
                Id = $"level_curve_enemy_dmg_L{level}",
                Priority = BalanceTuningGoals.TuningPriority.High,
                Category = "global",
                Target = $"Level {level} curve",
                Parameter = "DamageMultiplier",
                CurrentValue = current,
                SuggestedValue = Math.Max(0.1, suggested),
                AdjustmentMagnitude = magnitude * 100.0,
                Reason = $"Level {level} win rate {actual:F1}% is below target {target:F1}%. Enemy damage is too high at this level band.",
                Impact = $"Reduce global enemy damage multiplier from {current:F3} to {Math.Max(0.1, suggested):F3}",
                AffectedMatchups = new List<string> { $"Same-level L{level}" }
            };
        }
    }
}
