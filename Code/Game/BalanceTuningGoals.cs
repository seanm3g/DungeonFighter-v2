using System;
using System.Collections.Generic;
using RPGGame.Config;

namespace RPGGame
{
    /// <summary>
    /// Defines clear goals and targets for balance tuning
    /// Makes simulations valuable by providing actionable targets
    /// Goals are configurable via GameConfiguration.BalanceTuningGoals
    /// </summary>
    public static class BalanceTuningGoals
    {
        /// <summary>
        /// Get win rate targets from configuration
        /// </summary>
        public static WinRateGoalsConfig WinRateTargets => 
            GameConfiguration.Instance.BalanceTuningGoals.WinRate;

        /// <summary>
        /// Get combat duration targets from configuration
        /// </summary>
        public static CombatDurationGoalsConfig CombatDurationTargets => 
            GameConfiguration.Instance.BalanceTuningGoals.CombatDuration;

        /// <summary>
        /// Get weapon balance targets from configuration
        /// </summary>
        public static WeaponBalanceGoalsConfig WeaponBalanceTargets => 
            GameConfiguration.Instance.BalanceTuningGoals.WeaponBalance;

        /// <summary>
        /// Get enemy differentiation targets from configuration
        /// </summary>
        public static EnemyDifferentiationGoalsConfig EnemyDifferentiationTargets => 
            GameConfiguration.Instance.BalanceTuningGoals.EnemyDifferentiation;

        /// <summary>
        /// Get quality score weights from configuration
        /// </summary>
        public static QualityWeightsConfig QualityWeights => 
            GameConfiguration.Instance.BalanceTuningGoals.QualityWeights;

        /// <summary>
        /// Target win rate at a level (curve when enabled, else flat optimal midpoint).
        /// </summary>
        public static double GetTargetWinRate(int level) =>
            LevelWinRateCurve.GetTargetWinRate(level);

        /// <summary>
        /// Average anchor conformance score (0–100) from a multi-level simulation result.
        /// </summary>
        public static double CalculateLevelCurveQualityScore(Tuning.MultiLevelSimulationResult result)
        {
            if (result?.LevelSnapshots == null || result.LevelSnapshots.Count == 0)
                return 0.0;

            var anchorLevels = new HashSet<int>(LevelWinRateCurve.GetCurveAnchorLevels());
            var anchorSnapshots = result.LevelSnapshots
                .Where(s => anchorLevels.Contains(s.Level))
                .ToList();

            if (anchorSnapshots.Count == 0)
                anchorSnapshots = result.LevelSnapshots;

            double sum = anchorSnapshots.Sum(s =>
                LevelWinRateCurve.GetConformanceScore(s.ActualWinRate, s.Level));
            return Math.Round(sum / anchorSnapshots.Count, 1);
        }

        /// <summary>
        /// Average per-level combat duration score (0–100) from a multi-level simulation result.
        /// </summary>
        public static double CalculateMultiLevelDurationQualityScore(Tuning.MultiLevelSimulationResult result)
        {
            if (result?.LevelSnapshots == null || result.LevelSnapshots.Count == 0)
                return 0.0;

            double sum = result.LevelSnapshots.Sum(s => ScoreCombatDuration(s.AverageTurns));
            return Math.Round(sum / result.LevelSnapshots.Count, 1);
        }

        private static double ScoreCombatDuration(double averageCombatDuration)
        {
            var duration = CombatDurationTargets;

            if (averageCombatDuration < duration.MinTarget || averageCombatDuration > duration.MaxTarget)
            {
                if (averageCombatDuration < duration.CriticalShort || averageCombatDuration > duration.CriticalLong)
                    return 0.0;
                return 50.0;
            }

            if (averageCombatDuration >= duration.OptimalMin && averageCombatDuration <= duration.OptimalMax)
                return 100.0;

            return 80.0;
        }

        private static double ScoreWeaponVariance(double weaponVariance)
        {
            var weapon = WeaponBalanceTargets;

            if (weaponVariance > weapon.CriticalVariance)
                return 0.0;
            if (weaponVariance > weapon.MaxVariance)
                return 50.0;
            if (weaponVariance <= weapon.OptimalVariance)
                return 100.0;
            return 80.0;
        }

        private static double ScoreEnemyVariance(double enemyVariance)
        {
            var enemy = EnemyDifferentiationTargets;

            if (enemyVariance < enemy.CriticalVariance)
                return 0.0;
            if (enemyVariance < enemy.MinVariance)
                return 50.0;
            if (enemyVariance >= enemy.OptimalVariance)
                return 100.0;
            return 80.0;
        }

        /// <summary>
        /// Calculate balance quality without win-rate weighting (duration + weapon + enemy spread).
        /// </summary>
        public static double CalculateQualityScoreWithoutWinRate(
            double averageCombatDuration,
            double weaponVariance,
            double enemyVariance)
        {
            var weights = QualityWeights;
            double durationScore = ScoreCombatDuration(averageCombatDuration);
            double weaponScore = ScoreWeaponVariance(weaponVariance);
            double enemyScore = ScoreEnemyVariance(enemyVariance);

            double totalWeight = weights.DurationWeight + weights.WeaponBalanceWeight + weights.EnemyDiffWeight;
            if (totalWeight <= 0)
                return Math.Round((durationScore + weaponScore + enemyScore) / 3.0, 1);

            double totalScore =
                durationScore * weights.DurationWeight +
                weaponScore * weights.WeaponBalanceWeight +
                enemyScore * weights.EnemyDiffWeight;

            return Math.Round(totalScore / totalWeight, 1);
        }

        /// <summary>
        /// Calculate overall balance quality score (0-100)
        /// </summary>
        public static double CalculateQualityScore(
            double overallWinRate,
            double averageCombatDuration,
            double weaponVariance,
            double enemyVariance)
        {
            var winRate = WinRateTargets;
            var weights = QualityWeights;

            // Win rate score (0-100)
            double winRateScore = 100.0;
            if (overallWinRate < winRate.MinTarget || overallWinRate > winRate.MaxTarget)
            {
                // Outside target range - penalize heavily
                if (overallWinRate < winRate.CriticalLow || overallWinRate > winRate.CriticalHigh)
                    winRateScore = 0.0;
                else
                    winRateScore = 50.0; // In warning range
            }
            else if (overallWinRate >= winRate.OptimalMin && overallWinRate <= winRate.OptimalMax)
            {
                winRateScore = 100.0; // Optimal
            }
            else
            {
                // In target range but not optimal - slight penalty
                winRateScore = 80.0;
            }

            // Duration score (0-100)
            double durationScore = ScoreCombatDuration(averageCombatDuration);

            // Weapon balance score (0-100)
            double weaponScore = ScoreWeaponVariance(weaponVariance);

            // Enemy differentiation score (0-100)
            double enemyScore = ScoreEnemyVariance(enemyVariance);

            // Weighted average
            double totalScore = 
                winRateScore * weights.WinRateWeight +
                durationScore * weights.DurationWeight +
                weaponScore * weights.WeaponBalanceWeight +
                enemyScore * weights.EnemyDiffWeight;

            return Math.Round(totalScore, 1);
        }

        /// <summary>
        /// Tuning priority order for automated adjustments
        /// </summary>
        public enum TuningPriority
        {
            Critical = 1,    // Fix critical issues first (<80% or >99% win rate)
            High = 2,        // Fix high priority issues (outside optimal range)
            Medium = 3,      // Fix medium priority issues (warnings)
            Low = 4          // Fine-tune for optimization
        }

        /// <summary>
        /// Determine priority level for a matchup issue
        /// </summary>
        public static TuningPriority GetMatchupPriority(double winRate, double averageTurns)
        {
            var winRateTargets = WinRateTargets;
            var durationTargets = CombatDurationTargets;

            // Critical: Win rate way off or duration way off
            if (winRate < winRateTargets.CriticalLow || winRate > winRateTargets.CriticalHigh ||
                averageTurns < durationTargets.CriticalShort || averageTurns > durationTargets.CriticalLong)
            {
                return TuningPriority.Critical;
            }

            // High: Outside optimal range
            if (winRate < winRateTargets.OptimalMin || winRate > winRateTargets.OptimalMax ||
                averageTurns < durationTargets.OptimalMin || averageTurns > durationTargets.OptimalMax)
            {
                return TuningPriority.High;
            }

            // Medium: In warning range
            if (winRate < winRateTargets.WarningLow || winRate > winRateTargets.WarningHigh ||
                averageTurns < durationTargets.WarningShort || averageTurns > durationTargets.WarningLong)
            {
                return TuningPriority.Medium;
            }

            // Low: In target range, fine-tuning
            return TuningPriority.Low;
        }

        /// <summary>
        /// Get target adjustment magnitude based on how far off target we are
        /// </summary>
        public static double GetAdjustmentMagnitude(double currentValue, double targetValue, double minAdjustment = 0.05, double maxAdjustment = 0.50)
        {
            double difference = Math.Abs(currentValue - targetValue);
            double targetRange = WinRateTargets.MaxTarget - WinRateTargets.MinTarget;
            double percentageOff = difference / targetRange;

            // Scale adjustment: 5% for small differences, up to 50% for large differences
            double magnitude = minAdjustment + (percentageOff * (maxAdjustment - minAdjustment));
            return Math.Clamp(magnitude, minAdjustment, maxAdjustment);
        }
    }
}

