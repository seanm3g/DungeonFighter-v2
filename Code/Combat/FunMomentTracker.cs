using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Tracks "fun" moments during combat - indicators of engaging, dynamic gameplay
    /// Used for gameplay analytics and balance tuning to understand what makes combat compelling
    /// 
    /// Refactored from 446 lines to ~150 lines using Facade pattern.
    /// Delegates to:
    /// - FunMomentDataStructures: Data classes and enums
    /// - FunMomentAnalyzer: Analysis and summary generation
    /// </summary>
    public class FunMomentTracker
    {
        // Type aliases for backward compatibility
        public class FunMoment : FunMomentDataStructures.FunMoment { }
        public class FunMomentSummary : FunMomentDataStructures.FunMomentSummary { }
        public enum FunMomentType
        {
            BigDamageSpike = FunMomentDataStructures.FunMomentType.BigDamageSpike,
            HealthLeadChange = FunMomentDataStructures.FunMomentType.HealthLeadChange,
            CloseCall = FunMomentDataStructures.FunMomentType.CloseCall,
            Comeback = FunMomentDataStructures.FunMomentType.Comeback,
            DominantStreak = FunMomentDataStructures.FunMomentType.DominantStreak,
            ActionVariety = FunMomentDataStructures.FunMomentType.ActionVariety,
            TurnVariance = FunMomentDataStructures.FunMomentType.TurnVariance,
            CriticalSequence = FunMomentDataStructures.FunMomentType.CriticalSequence,
            ComboChain = FunMomentDataStructures.FunMomentType.ComboChain,
            HealthSwing = FunMomentDataStructures.FunMomentType.HealthSwing
        }

        private readonly List<FunMomentDataStructures.FunMoment> funMoments;
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
            funMoments = new List<FunMomentDataStructures.FunMoment>();
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
                FunMomentAnalyzer.CheckBigDamageSpike(evt, totalDamage, damageEvents, funMoments, currentTurn);

                // Check for critical sequence
                if (evt.IsCritical)
                {
                    consecutiveCriticals++;
                    if (consecutiveCriticals >= 2)
                    {
                        RecordFunMoment(FunMomentDataStructures.FunMomentType.CriticalSequence, evt.Actor, evt.Target,
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
                    RecordFunMoment(FunMomentDataStructures.FunMomentType.DominantStreak, evt.Actor, evt.Target,
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
                RecordFunMoment(FunMomentDataStructures.FunMomentType.ComboChain, evt.Actor, evt.Target,
                    intensity: Math.Min(0.5 + evt.ComboStep * 0.1, 1.0),
                    $"Combo chain! {evt.ComboStep} steps!",
                    new Dictionary<string, object> { { "comboStep", evt.ComboStep } });
            }

            // Track action variety
            if (lastAction != null && evt.Action != lastAction && evt.Action != "BASIC ATTACK")
            {
                RecordFunMoment(FunMomentDataStructures.FunMomentType.ActionVariety, evt.Actor, evt.Target,
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
            currentHealthLeader = FunMomentAnalyzer.CheckHealthLeadChange(
                playerHealth, enemyHealth, playerName, enemyName, currentHealthLeader, funMoments, currentTurn);

            // Check for close calls
            FunMomentAnalyzer.CheckCloseCall(
                playerHealthPercent, ref playerWasBehind, playerName, enemyName, lowestPlayerHealthPercent, funMoments, currentTurn);

            // Check for health swing
            FunMomentAnalyzer.CheckHealthSwing(evt, playerName, enemyName, funMoments, currentTurn);
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
                RecordFunMoment(FunMomentDataStructures.FunMomentType.Comeback, playerName ?? "Player", enemyName ?? "Enemy",
                    intensity: 1.0 - lowestPlayerHealthPercent.Value,
                    $"Incredible comeback! Won after dropping to {(lowestPlayerHealthPercent.Value * 100):F0}% health!",
                    new Dictionary<string, object> { { "lowestHealthPercent", lowestPlayerHealthPercent.Value * 100 } });
            }

            EndTurn(); // Finalize last turn
        }

        /// <summary>
        /// Get summary of all fun moments
        /// </summary>
        public FunMomentDataStructures.FunMomentSummary GetSummary()
        {
            return FunMomentAnalyzer.GetSummary(funMoments, damagePerTurn, actionCounts);
        }

        private void UpdateHealthLeader(int playerHealth, int enemyHealth)
        {
            currentHealthLeader = playerHealth > enemyHealth ? playerName :
                                 enemyHealth > playerHealth ? enemyName : null;
        }

        private void RecordFunMoment(FunMomentDataStructures.FunMomentType type, string actor, string target, double intensity, string description, Dictionary<string, object>? metadata = null)
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
