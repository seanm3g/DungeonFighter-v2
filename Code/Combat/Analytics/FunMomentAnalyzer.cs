using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Analyzes fun moments and generates summaries
    /// Extracted from FunMomentTracker to separate analysis logic
    /// </summary>
    public class FunMomentAnalyzer
    {
        /// <summary>
        /// Get summary of all fun moments
        /// </summary>
        public static FunMomentDataStructures.FunMomentSummary GetSummary(
            List<FunMomentDataStructures.FunMoment> funMoments,
            List<int> damagePerTurn,
            Dictionary<string, int> actionCounts)
        {
            var summary = new FunMomentDataStructures.FunMomentSummary
            {
                TotalFunMoments = funMoments.Count,
                MomentsByType = funMoments.GroupBy(m => m.Type)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            if (funMoments.Count > 0)
            {
                summary.AverageIntensity = funMoments.Average(m => m.Intensity);
                summary.TopMoments = funMoments.OrderByDescending(m => m.Intensity).Take(5).ToList();
            }

            // Calculate turn variance
            if (damagePerTurn.Count > 1)
            {
                double avgDamage = damagePerTurn.Average();
                double variance = damagePerTurn.Average(d => Math.Pow(d - avgDamage, 2));
                summary.TurnVariance = Math.Sqrt(variance); // Standard deviation
            }

            // Calculate action variety score
            if (actionCounts.Count > 0)
            {
                // More unique actions = higher score, but also consider distribution
                int uniqueActions = actionCounts.Count;
                double entropy = 0.0;
                int totalActions = actionCounts.Values.Sum();
                foreach (var count in actionCounts.Values)
                {
                    double p = (double)count / totalActions;
                    if (p > 0)
                        entropy -= p * Math.Log2(p);
                }
                // Normalize: max entropy is log2(uniqueActions), scale to 0-1
                summary.ActionVarietyScore = uniqueActions > 1 
                    ? Math.Min(entropy / Math.Log2(uniqueActions), 1.0) 
                    : 0.0;
            }

            summary.HealthLeadChanges = summary.MomentsByType.GetValueOrDefault(FunMomentDataStructures.FunMomentType.HealthLeadChange, 0);
            summary.Comebacks = summary.MomentsByType.GetValueOrDefault(FunMomentDataStructures.FunMomentType.Comeback, 0);
            summary.CloseCalls = summary.MomentsByType.GetValueOrDefault(FunMomentDataStructures.FunMomentType.CloseCall, 0);

            // Calculate overall fun score (0-100)
            summary.FunScore = CalculateFunScore(summary, damagePerTurn);

            return summary;
        }

        /// <summary>
        /// Calculate overall fun score based on various factors
        /// </summary>
        private static double CalculateFunScore(FunMomentDataStructures.FunMomentSummary summary, List<int> damagePerTurn)
        {
            double score = 0.0;

            // Base score from number of fun moments (up to 30 points)
            score += Math.Min(summary.TotalFunMoments * 2.0, 30.0);

            // Intensity bonus (up to 20 points)
            score += summary.AverageIntensity * 20.0;

            // Variety bonus (up to 15 points)
            score += summary.ActionVarietyScore * 15.0;

            // Variance bonus - some variance is good, too much or too little is bad (up to 15 points)
            if (summary.TurnVariance > 0)
            {
                // Optimal variance is around 30-50% of average damage
                double avgDamage = damagePerTurn.Count > 0 ? damagePerTurn.Average() : 0;
                if (avgDamage > 0)
                {
                    double varianceRatio = summary.TurnVariance / avgDamage;
                    if (varianceRatio >= 0.3 && varianceRatio <= 0.5)
                        score += 15.0; // Perfect variance
                    else if (varianceRatio >= 0.2 && varianceRatio <= 0.7)
                        score += 10.0; // Good variance
                    else if (varianceRatio >= 0.1 && varianceRatio <= 1.0)
                        score += 5.0; // Acceptable variance
                }
            }

            // Special moments bonus (up to 20 points)
            score += summary.HealthLeadChanges * 2.0;
            score += summary.Comebacks * 5.0;
            score += summary.CloseCalls * 2.0;

            return Math.Min(score, 100.0);
        }

        /// <summary>
        /// Check for big damage spike
        /// </summary>
        public static void CheckBigDamageSpike(
            BattleEvent evt,
            int totalDamage,
            int damageEvents,
            List<FunMomentDataStructures.FunMoment> funMoments,
            int currentTurn)
        {
            if (damageEvents < 3) return; // Need some data first

            double averageDamage = (double)totalDamage / damageEvents;
            double spikeThreshold = averageDamage * 1.5; // 50% above average

            if (evt.Damage >= spikeThreshold)
            {
                double intensity = Math.Min((evt.Damage / averageDamage - 1.0) / 2.0, 1.0);
                RecordFunMoment(FunMomentDataStructures.FunMomentType.BigDamageSpike, evt.Actor, evt.Target,
                    intensity,
                    $"{evt.Actor} deals massive damage! {evt.Damage} (avg: {averageDamage:F1})",
                    new Dictionary<string, object> { { "damage", evt.Damage }, { "average", averageDamage } },
                    funMoments,
                    currentTurn);
            }
        }

        /// <summary>
        /// Check for health lead change
        /// </summary>
        public static string? CheckHealthLeadChange(
            int playerHealth,
            int enemyHealth,
            string? playerName,
            string? enemyName,
            string? currentHealthLeader,
            List<FunMomentDataStructures.FunMoment> funMoments,
            int currentTurn)
        {
            string? newLeader = playerHealth > enemyHealth ? playerName : 
                               enemyHealth > playerHealth ? enemyName : null;

            if (newLeader != null && newLeader != currentHealthLeader)
            {
                if (currentHealthLeader != null)
                {
                    RecordFunMoment(FunMomentDataStructures.FunMomentType.HealthLeadChange, newLeader, currentHealthLeader,
                        intensity: 0.6,
                        $"Momentum shift! {newLeader} takes the lead!",
                        new Dictionary<string, object> { { "previousLeader", currentHealthLeader }, { "newLeader", newLeader } },
                        funMoments,
                        currentTurn);
                }
                return newLeader;
            }
            return currentHealthLeader;
        }

        /// <summary>
        /// Check for close call
        /// </summary>
        public static bool CheckCloseCall(
            double playerHealthPercent,
            ref bool playerWasBehind,
            string? playerName,
            string? enemyName,
            double? lowestPlayerHealthPercent,
            List<FunMomentDataStructures.FunMoment> funMoments,
            int currentTurn)
        {
            // Close call: health drops below 10% but recovers
            if (playerHealthPercent < 0.1 && !playerWasBehind)
            {
                playerWasBehind = true;
            }
            else if (playerHealthPercent > 0.2 && playerWasBehind)
            {
                RecordFunMoment(FunMomentDataStructures.FunMomentType.CloseCall, playerName ?? "Player", enemyName ?? "Enemy",
                    intensity: 0.8,
                    $"Close call! {playerName} recovers from near-death!",
                    new Dictionary<string, object> { { "lowestHealth", lowestPlayerHealthPercent ?? 0.0 } },
                    funMoments,
                    currentTurn);
                playerWasBehind = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check for health swing
        /// </summary>
        public static void CheckHealthSwing(
            BattleEvent evt,
            string? playerName,
            string? enemyName,
            List<FunMomentDataStructures.FunMoment> funMoments,
            int currentTurn)
        {
            // Large health percentage change in one turn (>15%)
            double healthChange = 0.0;
            string actor = evt.Actor;
            string target = evt.Target;

            if (evt.Actor == playerName && evt.Damage > 0 && evt.TargetHealthBefore > 0)
            {
                // Player dealt damage to enemy
                healthChange = (double)evt.Damage / evt.TargetHealthBefore;
            }
            else if (evt.Actor == enemyName && evt.Damage > 0 && evt.TargetHealthBefore > 0)
            {
                // Enemy dealt damage to player
                healthChange = (double)evt.Damage / evt.TargetHealthBefore;
            }

            if (healthChange > 0.15)
            {
                RecordFunMoment(FunMomentDataStructures.FunMomentType.HealthSwing, actor, target,
                    intensity: Math.Min(healthChange * 2.0, 1.0),
                    $"Massive health swing! {healthChange * 100:F0}% in one hit!",
                    new Dictionary<string, object> { { "healthChangePercent", healthChange * 100 } },
                    funMoments,
                    currentTurn);
            }
        }

        /// <summary>
        /// Record a fun moment
        /// </summary>
        private static void RecordFunMoment(
            FunMomentDataStructures.FunMomentType type,
            string actor,
            string target,
            double intensity,
            string description,
            Dictionary<string, object>? metadata,
            List<FunMomentDataStructures.FunMoment> funMoments,
            int currentTurn)
        {
            funMoments.Add(new FunMomentDataStructures.FunMoment
            {
                Type = type,
                Turn = currentTurn,
                Actor = actor,
                Target = target,
                Intensity = intensity,
                Description = description,
                Metadata = metadata ?? new Dictionary<string, object>()
            });
        }
    }
}

