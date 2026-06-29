using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame
{
    /// <summary>
    /// Piecewise-linear level win-rate target curve from <see cref="BalanceTuningGoalsConfig.LevelWinRateCurve"/>.
    /// </summary>
    public static class LevelWinRateCurve
    {
        public static LevelWinRateCurveConfig Config =>
            GameConfiguration.Instance.BalanceTuningGoals.LevelWinRateCurve;

        public static bool IsEnabled => Config?.Enabled ?? false;

        /// <summary>Default sweep levels: anchors plus midpoints for tuning feedback.</summary>
        public static IReadOnlyList<int> GetDefaultAnchorLevels() =>
            new[] { 1, 5, 10, 25, 50, 75, 100 };

        /// <summary>Configured curve anchor levels only (sorted).</summary>
        public static IReadOnlyList<int> GetCurveAnchorLevels()
        {
            var anchors = Config?.Anchors;
            if (anchors == null || anchors.Count == 0)
                return GetDefaultAnchorLevels();

            return anchors
                .Select(a => a.Level)
                .Distinct()
                .OrderBy(l => l)
                .ToList();
        }

        public static double GetTolerance(int level)
        {
            _ = level;
            return Config?.TolerancePercent ?? 3.0;
        }

        /// <summary>
        /// Target win rate at <paramref name="level"/> via piecewise linear interpolation between sorted anchors.
        /// </summary>
        public static double GetTargetWinRate(int level)
        {
            if (!IsEnabled)
            {
                var flat = BalanceTuningGoals.WinRateTargets;
                return (flat.OptimalMin + flat.OptimalMax) / 2.0;
            }

            var sorted = GetSortedAnchors();
            if (sorted.Count == 0)
                return 91.5;

            level = Math.Clamp(level, sorted[0].Level, sorted[^1].Level);

            if (level <= sorted[0].Level)
                return sorted[0].TargetWinRate;
            if (level >= sorted[^1].Level)
                return sorted[^1].TargetWinRate;

            for (int i = 1; i < sorted.Count; i++)
            {
                var prev = sorted[i - 1];
                var next = sorted[i];
                if (level > next.Level)
                    continue;

                double span = next.Level - prev.Level;
                if (span <= 0)
                    return next.TargetWinRate;

                double t = (level - prev.Level) / span;
                return prev.TargetWinRate + t * (next.TargetWinRate - prev.TargetWinRate);
            }

            return sorted[^1].TargetWinRate;
        }

        /// <summary>0–100 conformance score for a single level (100 = on target within tolerance).</summary>
        public static double GetConformanceScore(double actualWinRate, int level)
        {
            double target = GetTargetWinRate(level);
            double tolerance = GetTolerance(level);
            double delta = Math.Abs(actualWinRate - target);

            if (delta <= tolerance)
                return 100.0;

            // Linear falloff: 0 score when delta reaches 50 percentage points beyond tolerance
            const double maxDelta = 50.0;
            double excess = delta - tolerance;
            double score = 100.0 * (1.0 - excess / maxDelta);
            return Math.Clamp(score, 0.0, 100.0);
        }

        public static bool IsWithinTolerance(double actualWinRate, int level)
        {
            double target = GetTargetWinRate(level);
            return Math.Abs(actualWinRate - target) <= GetTolerance(level);
        }

        /// <summary>Normalizes 0–1 fractions to percent points; leaves 0–100 values unchanged.</summary>
        public static double NormalizeToPercent(double winRate) =>
            winRate <= 1.0 ? winRate * 100.0 : winRate;

        /// <summary>Whether observed win rate is within curve tolerance at <paramref name="level"/>.</summary>
        public static bool IsWinRateInBand(double actualWinRate, int level)
        {
            double actual = NormalizeToPercent(actualWinRate);
            if (!IsEnabled)
            {
                var flat = BalanceTuningGoals.WinRateTargets;
                return actual >= flat.MinTarget && actual <= flat.MaxTarget;
            }

            return IsWithinTolerance(actual, level);
        }

        /// <summary>Signed delta in percentage points (actual − target).</summary>
        public static double GetWinRateDelta(double actualWinRate, int level) =>
            NormalizeToPercent(actualWinRate) - GetTargetWinRate(level);

        private static List<LevelWinRateAnchor> GetSortedAnchors()
        {
            var anchors = Config?.Anchors;
            if (anchors == null || anchors.Count == 0)
            {
                return new LevelWinRateCurveConfig().Anchors
                    .OrderBy(a => a.Level)
                    .ToList();
            }

            return anchors
                .OrderBy(a => a.Level)
                .ToList();
        }
    }
}
