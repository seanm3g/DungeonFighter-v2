using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Tracks "fun" moments during combat - indicators of engaging, dynamic gameplay
    /// Used for gameplay analytics and balance tuning to understand what makes combat compelling
    /// </summary>
    public class FunMomentTracker
    {
        /// <summary>
        /// Represents a single "fun" moment that occurred during combat
        /// </summary>
        public class FunMoment
        {
            public FunMomentType Type { get; set; }
            public int Turn { get; set; }
            public string Actor { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
            public double Intensity { get; set; } // 0.0-1.0 scale of how "fun" this moment was
            public string Description { get; set; } = string.Empty;
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        /// <summary>
        /// Types of fun moments to track
        /// </summary>
        public enum FunMomentType
        {
            BigDamageSpike,          // Damage significantly above average
            HealthLeadChange,        // Momentum shift - health advantage changes
            CloseCall,               // Near-death recovery
            Comeback,                // Recovery from low health to win
            DominantStreak,          // Multiple consecutive successful actions
            ActionVariety,           // Different actions used in sequence
            TurnVariance,            // High variance in damage per turn
            CriticalSequence,        // Multiple critical hits in a row
            ComboChain,              // Extended combo sequence
            HealthSwing              // Large health percentage change in one turn
        }

        /// <summary>
        /// Summary of fun moments for a combat session
        /// </summary>
        public class FunMomentSummary
        {
            public int TotalFunMoments { get; set; }
            public double AverageIntensity { get; set; }
            public double FunScore { get; set; } // Overall "fun" score 0-100
            public Dictionary<FunMomentType, int> MomentsByType { get; set; } = new();
            public List<FunMoment> TopMoments { get; set; } = new(); // Top 5 most intense moments
            public double TurnVariance { get; set; } // Variance in damage per turn
            public double ActionVarietyScore { get; set; } // 0-1, higher = more variety
            public int HealthLeadChanges { get; set; }
            public int Comebacks { get; set; }
            public int CloseCalls { get; set; }
        }

        private readonly List<FunMoment> funMoments;
        private readonly List<int> damagePerTurn;
        private readonly List<string> actionsUsed;
        private readonly Dictionary<string, int> actionCounts;
        private int currentTurn;
        private int currentTurnDamage;
        private string? currentHealthLeader;
        private double? lowestPlayerHealthPercent;
        private double? lowestEnemyHealthPercent;
        private bool playerWasBehind;
        private int consecutiveSuccesses;
        private int consecutiveCriticals;
        private string? lastAction;
        private int totalDamage;
        private int damageEvents;
        private string? playerName;
        private string? enemyName;
        private int playerMaxHealth;
        private int enemyMaxHealth;

        public FunMomentTracker()
        {
            funMoments = new List<FunMoment>();
            damagePerTurn = new List<int>();
            actionsUsed = new List<string>();
            actionCounts = new Dictionary<string, int>();
            currentTurn = 0;
            currentTurnDamage = 0;
            playerWasBehind = false;
            consecutiveSuccesses = 0;
            consecutiveCriticals = 0;
        }

        /// <summary>
        /// Initialize tracker for a new combat
        /// </summary>
        public void InitializeCombat(string playerName, string enemyName, int playerMaxHealth, int enemyMaxHealth)
        {
            this.playerName = playerName;
            this.enemyName = enemyName;
            this.playerMaxHealth = playerMaxHealth;
            this.enemyMaxHealth = enemyMaxHealth;
            funMoments.Clear();
            damagePerTurn.Clear();
            actionsUsed.Clear();
            actionCounts.Clear();
            currentTurn = 0;
            currentTurnDamage = 0;
            currentHealthLeader = null;
            lowestPlayerHealthPercent = null;
            lowestEnemyHealthPercent = null;
            playerWasBehind = false;
            consecutiveSuccesses = 0;
            consecutiveCriticals = 0;
            lastAction = null;
            totalDamage = 0;
            damageEvents = 0;

            // Set initial leader
            UpdateHealthLeader(playerMaxHealth, enemyMaxHealth);
        }

        /// <summary>
        /// Record a combat event and check for fun moments
        /// </summary>
        public void RecordEvent(BattleEvent evt, int playerHealth, int enemyHealth)
        {
            // Track action variety
            if (!string.IsNullOrEmpty(evt.Action) && evt.Action != "BASIC ATTACK")
            {
                actionsUsed.Add(evt.Action);
                actionCounts[evt.Action] = actionCounts.GetValueOrDefault(evt.Action, 0) + 1;
            }

            // Track damage
            if (evt.Damage > 0 && evt.IsSuccess)
            {
                currentTurnDamage += evt.Damage;
                totalDamage += evt.Damage;
                damageEvents++;

                // Check for big damage spike
                CheckBigDamageSpike(evt);

                // Check for critical sequence
                if (evt.IsCritical)
                {
                    consecutiveCriticals++;
                    if (consecutiveCriticals >= 2)
                    {
                        RecordFunMoment(FunMomentType.CriticalSequence, evt.Actor, evt.Target,
                            intensity: Math.Min(0.8 + (consecutiveCriticals - 2) * 0.1, 1.0),
                            $"Critical sequence! {consecutiveCriticals} critical hits in a row!",
                            new Dictionary<string, object> { { "count", consecutiveCriticals } });
                    }
                }
                else
                {
                    consecutiveCriticals = 0;
                }
            }

            // Track successful actions
            if (evt.IsSuccess)
            {
                consecutiveSuccesses++;
                if (consecutiveSuccesses >= 3)
                {
                    RecordFunMoment(FunMomentType.DominantStreak, evt.Actor, evt.Target,
                        intensity: Math.Min(0.6 + (consecutiveSuccesses - 3) * 0.05, 0.9),
                        $"{evt.Actor} is on a roll! {consecutiveSuccesses} successful actions!",
                        new Dictionary<string, object> { { "streakLength", consecutiveSuccesses } });
                }
            }
            else
            {
                consecutiveSuccesses = 0;
            }

            // Track combos
            if (evt.IsCombo && evt.ComboStep >= 3)
            {
                RecordFunMoment(FunMomentType.ComboChain, evt.Actor, evt.Target,
                    intensity: Math.Min(0.5 + evt.ComboStep * 0.1, 1.0),
                    $"Combo chain! {evt.ComboStep} steps!",
                    new Dictionary<string, object> { { "comboStep", evt.ComboStep } });
            }

            // Track action variety
            if (lastAction != null && evt.Action != lastAction && evt.Action != "BASIC ATTACK")
            {
                RecordFunMoment(FunMomentType.ActionVariety, evt.Actor, evt.Target,
                    intensity: 0.3,
                    $"{evt.Actor} switches tactics!",
                    new Dictionary<string, object> { { "previousAction", lastAction }, { "newAction", evt.Action } });
            }
            lastAction = evt.Action;

            // Track health changes
            double playerHealthPercent = (double)playerHealth / playerMaxHealth;
            double enemyHealthPercent = (double)enemyHealth / enemyMaxHealth;

            // Track lowest health for comeback detection
            if (playerHealthPercent < (lowestPlayerHealthPercent ?? 1.0))
                lowestPlayerHealthPercent = playerHealthPercent;
            if (enemyHealthPercent < (lowestEnemyHealthPercent ?? 1.0))
                lowestEnemyHealthPercent = enemyHealthPercent;

            // Check for health lead change
            CheckHealthLeadChange(playerHealth, enemyHealth);

            // Check for close calls
            CheckCloseCall(playerHealthPercent, enemyHealthPercent);

            // Check for health swing
            CheckHealthSwing(evt, playerHealth, enemyHealth);
        }

        /// <summary>
        /// End current turn and prepare for next
        /// </summary>
        public void EndTurn()
        {
            if (currentTurnDamage > 0)
            {
                damagePerTurn.Add(currentTurnDamage);
            }
            currentTurnDamage = 0;
            currentTurn++;
            consecutiveSuccesses = 0; // Reset streak at turn boundary
        }

        /// <summary>
        /// Finalize combat and check for final fun moments (comebacks, etc.)
        /// </summary>
        public void FinalizeCombat(bool playerWon, int playerHealth, int playerMaxHealth)
        {
            // Check for comeback
            if (playerWon && lowestPlayerHealthPercent.HasValue && lowestPlayerHealthPercent.Value < 0.25)
            {
                RecordFunMoment(FunMomentType.Comeback, playerName ?? "Player", enemyName ?? "Enemy",
                    intensity: 1.0 - lowestPlayerHealthPercent.Value,
                    $"Incredible comeback! Won after dropping to {(lowestPlayerHealthPercent.Value * 100):F0}% health!",
                    new Dictionary<string, object> { { "lowestHealthPercent", lowestPlayerHealthPercent.Value * 100 } });
            }

            EndTurn(); // Finalize last turn
        }

        /// <summary>
        /// Get summary of all fun moments
        /// </summary>
        public FunMomentSummary GetSummary()
        {
            var summary = new FunMomentSummary
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

            summary.HealthLeadChanges = summary.MomentsByType.GetValueOrDefault(FunMomentType.HealthLeadChange, 0);
            summary.Comebacks = summary.MomentsByType.GetValueOrDefault(FunMomentType.Comeback, 0);
            summary.CloseCalls = summary.MomentsByType.GetValueOrDefault(FunMomentType.CloseCall, 0);

            // Calculate overall fun score (0-100)
            summary.FunScore = CalculateFunScore(summary);

            return summary;
        }

        /// <summary>
        /// Calculate overall fun score based on various factors
        /// </summary>
        private double CalculateFunScore(FunMomentSummary summary)
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

        private void CheckBigDamageSpike(BattleEvent evt)
        {
            if (damageEvents < 3) return; // Need some data first

            double averageDamage = (double)totalDamage / damageEvents;
            double spikeThreshold = averageDamage * 1.5; // 50% above average

            if (evt.Damage >= spikeThreshold)
            {
                double intensity = Math.Min((evt.Damage / averageDamage - 1.0) / 2.0, 1.0);
                RecordFunMoment(FunMomentType.BigDamageSpike, evt.Actor, evt.Target,
                    intensity,
                    $"{evt.Actor} deals massive damage! {evt.Damage} (avg: {averageDamage:F1})",
                    new Dictionary<string, object> { { "damage", evt.Damage }, { "average", averageDamage } });
            }
        }

        private void CheckHealthLeadChange(int playerHealth, int enemyHealth)
        {
            string? newLeader = playerHealth > enemyHealth ? playerName : 
                               enemyHealth > playerHealth ? enemyName : null;

            if (newLeader != null && newLeader != currentHealthLeader)
            {
                if (currentHealthLeader != null)
                {
                    RecordFunMoment(FunMomentType.HealthLeadChange, newLeader, currentHealthLeader,
                        intensity: 0.6,
                        $"Momentum shift! {newLeader} takes the lead!",
                        new Dictionary<string, object> { { "previousLeader", currentHealthLeader }, { "newLeader", newLeader } });
                }
                currentHealthLeader = newLeader;
            }
        }

        private void CheckCloseCall(double playerHealthPercent, double enemyHealthPercent)
        {
            // Close call: health drops below 10% but recovers
            if (playerHealthPercent < 0.1 && !playerWasBehind)
            {
                playerWasBehind = true;
            }
            else if (playerHealthPercent > 0.2 && playerWasBehind)
            {
                RecordFunMoment(FunMomentType.CloseCall, playerName ?? "Player", enemyName ?? "Enemy",
                    intensity: 0.8,
                    $"Close call! {playerName} recovers from near-death!",
                    new Dictionary<string, object> { { "lowestHealth", lowestPlayerHealthPercent ?? 0.0 } });
                playerWasBehind = false;
            }
        }

        private void CheckHealthSwing(BattleEvent evt, int playerHealth, int enemyHealth)
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
                RecordFunMoment(FunMomentType.HealthSwing, actor, target,
                    intensity: Math.Min(healthChange * 2.0, 1.0),
                    $"Massive health swing! {healthChange * 100:F0}% in one hit!",
                    new Dictionary<string, object> { { "healthChangePercent", healthChange * 100 } });
            }
        }

        private void UpdateHealthLeader(int playerHealth, int enemyHealth)
        {
            currentHealthLeader = playerHealth > enemyHealth ? playerName :
                                 enemyHealth > playerHealth ? enemyName : null;
        }

        private void RecordFunMoment(FunMomentType type, string actor, string target, double intensity, string description, Dictionary<string, object>? metadata = null)
        {
            funMoments.Add(new FunMoment
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
