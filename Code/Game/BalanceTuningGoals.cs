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

    }
}

