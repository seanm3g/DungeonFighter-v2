using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Analyzes battle events and determines which narratives should be triggered.
    /// Encapsulates the complex event checking logic previously in CheckForSignificantEvents.
    /// </summary>
    public class BattleEventAnalyzer
    {
        private readonly NarrativeTextProvider textProvider;
        private readonly NarrativeStateManager stateManager;
        private readonly TauntSystem tauntSystem;
        private readonly NarrativeTriggerEvaluator triggerEvaluator;

        // References for health calculations
        private int initialPlayerHealth;
        private int initialEnemyHealth;
        private int finalPlayerHealth;
        private int finalEnemyHealth;
        private string playerName = "";
        private string enemyName = "";
        private string currentLocation = "";

        public BattleEventAnalyzer(
            NarrativeTextProvider textProvider,
            NarrativeStateManager stateManager,
            TauntSystem tauntSystem)
        {
            this.textProvider = textProvider;
            this.stateManager = stateManager;
            this.tauntSystem = tauntSystem;
            this.triggerEvaluator = new NarrativeTriggerEvaluator(
                textProvider, stateManager, tauntSystem, playerName, enemyName, currentLocation, initialPlayerHealth, initialEnemyHealth);
        }

        /// <summary>
        /// Initializes the analyzer with battle context
        /// </summary>
        public void Initialize(string playerName, string enemyName, string currentLocation,
            int initialPlayerHealth, int initialEnemyHealth)
        {
            this.playerName = playerName;
            this.enemyName = enemyName;
            this.currentLocation = currentLocation;
            this.initialPlayerHealth = initialPlayerHealth;
            this.initialEnemyHealth = initialEnemyHealth;
            this.finalPlayerHealth = initialPlayerHealth;
            this.finalEnemyHealth = initialEnemyHealth;
        }

        /// <summary>
        /// Updates final health values for current event analysis
        /// </summary>
        public void UpdateFinalHealth(int playerHealth, int enemyHealth)
        {
            finalPlayerHealth = playerHealth;
            finalEnemyHealth = enemyHealth;
            triggerEvaluator.UpdateFinalHealth(playerHealth, enemyHealth);
        }

        /// <summary>
        /// Analyzes a battle event and returns triggered narratives
        /// </summary>
        public List<string> AnalyzeEvent(BattleEvent evt, GameSettings settings)
        {
            var triggeredNarratives = new List<string>();

            // First Blood - first successful hit that deals damage
            if (!stateManager.HasFirstBloodOccurred && evt.Damage > 0 && evt.IsSuccess)
            {
                stateManager.SetFirstBloodOccurred();
                string narrative = textProvider.GetRandomNarrative("firstBlood");
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }

            // Critical Hit - when a critical hit occurs
            // Only generate narrative if it's significant (checked by IsSignificantEvent)
            // This prevents every critical hit from generating a narrative
            if (evt.IsCritical && evt.IsSuccess)
            {
                // Decrement cooldown on each critical hit
                stateManager.DecrementCriticalHitCooldown();
                
                // Only generate critical hit narrative if it's significant
                // (very high roll, high narrative balance, and cooldown expired)
                if ((settings.NarrativeBalance >= 0.7 || evt.Roll >= 18) && !stateManager.HasRecentCriticalHitNarrative)
                {
                    stateManager.SetRecentCriticalHitNarrative();
                    var replacements = new Dictionary<string, string> { { "name", evt.Actor } };
                    string narrative = textProvider.ReplacePlaceholders(
                        textProvider.GetRandomNarrative("criticalHit"),
                        replacements);
                    triggeredNarratives.Add(narrative);
                    stateManager.IncrementNarrativeEventCount();
                }
            }

            // Critical Miss - when a critical miss occurs (natural 1 only)
            if (!evt.IsSuccess && evt.NaturalRoll == 1)
            {
                var replacements = new Dictionary<string, string> { { "name", evt.Actor } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("criticalMiss"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }

            // Environmental Action - when environment takes action
            if (!string.IsNullOrEmpty(evt.EnvironmentEffect) && !stateManager.HasEnvironmentalActionOccurred)
            {
                stateManager.SetEnvironmentalActionOccurred();
                var replacements = new Dictionary<string, string> { { "effect", evt.EnvironmentEffect } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("environmentalAction"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }

            // Health Recovery - when someone heals
            if (evt.IsHeal && evt.HealAmount > 0)
            {
                var replacements = new Dictionary<string, string> { { "name", evt.Target } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("healthRecovery"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            // Health Lead Change - when someone gains or loses health advantage
            triggerEvaluator.AddHealthLeadNarratives(evt, triggeredNarratives, settings);

            // Taunt System - characters and enemies taunt periodically
            triggerEvaluator.AddTauntNarratives(evt, triggeredNarratives, settings);

            // Health Thresholds
            triggerEvaluator.AddHealthThresholdNarratives(triggeredNarratives, settings);

            // Intense Battle - when both combatants are below 50% health
            triggerEvaluator.AddIntenseBattleNarrative(triggeredNarratives, settings);

            // Good Combo - when someone gets a 3+ step combo
            if (!stateManager.HasGoodComboOccurred && evt.IsCombo && evt.ComboStep >= 2)
            {
                stateManager.SetGoodComboOccurred();
                string narrative;
                if (evt.Actor == playerName)
                {
                    narrative = $"{playerName} unleashes a devastating combo sequence! Each strike flows into the next with deadly precision!";
                }
                else
                {
                    narrative = $"{enemyName} demonstrates masterful technique with a brutal combo that leaves {playerName} reeling!";
                }
                triggeredNarratives.Add(narrative);
            }

            // Defeat Events - when someone is defeated
            if (!stateManager.HasPlayerDefeated && finalPlayerHealth <= 0)
            {
                stateManager.SetPlayerDefeated();
                var replacements = new Dictionary<string, string> { { "enemy", enemyName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("playerDefeated"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            if (!stateManager.HasEnemyDefeated && finalEnemyHealth <= 0)
            {
                stateManager.SetEnemyDefeated();
                var replacements = new Dictionary<string, string>
                {
                    { "name", enemyName },
                    { "player", playerName }
                };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("enemyDefeated"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            return triggeredNarratives;
        }


        /// <summary>
        /// Determines if an event is significant enough to warrant narrative display
        /// Only truly significant events should show narratives (not every critical hit)
        /// </summary>
        public bool IsSignificantEvent(BattleEvent evt, GameSettings settings)
        {
            // Always show narratives for these events:
            // - First blood (handled by state manager flag)
            // - Defeats (player or enemy)
            // - Critical misses (natural 1)
            // - Environmental actions
            // - Health thresholds (below 50%, below 10%)
            // - Intense battle
            // - Good combos
            
            // Check for first blood (will be handled by state manager)
            if (!stateManager.HasFirstBloodOccurred && evt.Damage > 0 && evt.IsSuccess)
            {
                return true;
            }

            // Defeat events
            if ((!stateManager.HasPlayerDefeated && finalPlayerHealth <= 0) ||
                (!stateManager.HasEnemyDefeated && finalEnemyHealth <= 0))
            {
                return true;
            }

            // Critical miss (natural 1)
            if (!evt.IsSuccess && evt.NaturalRoll == 1)
            {
                return true;
            }

            // Environmental actions
            if (!string.IsNullOrEmpty(evt.EnvironmentEffect) && !stateManager.HasEnvironmentalActionOccurred)
            {
                return true;
            }

            // Health thresholds
            var playerHealthPercentage = (double)finalPlayerHealth / initialPlayerHealth;
            var enemyHealthPercentage = (double)finalEnemyHealth / initialEnemyHealth;
            if ((!stateManager.HasPlayerBelow50Percent && playerHealthPercentage < 0.5 && finalPlayerHealth > 0) ||
                (!stateManager.HasEnemyBelow50Percent && enemyHealthPercentage < 0.5 && finalEnemyHealth > 0) ||
                (!stateManager.HasPlayerBelow10Percent && playerHealthPercentage < 0.1 && finalPlayerHealth > 0) ||
                (!stateManager.HasEnemyBelow10Percent && enemyHealthPercentage < 0.1 && finalEnemyHealth > 0))
            {
                return true;
            }

            // Intense battle
            if (!stateManager.HasIntenseBattleTriggered && playerHealthPercentage < 0.5 && enemyHealthPercentage < 0.5 && finalPlayerHealth > 0 && finalEnemyHealth > 0)
            {
                return true;
            }

            // Good combos
            if (!stateManager.HasGoodComboOccurred && evt.IsCombo && evt.ComboStep >= 2)
            {
                return true;
            }

            // Health lead changes (significant damage only)
            if (evt.Damage >= 3)
            {
                bool playerCurrentlyLeads = finalPlayerHealth > finalEnemyHealth;
                bool enemyCurrentlyLeads = finalEnemyHealth > finalPlayerHealth;
                if ((playerCurrentlyLeads && !stateManager.HasPlayerHealthLead) ||
                    (enemyCurrentlyLeads && !stateManager.HasEnemyHealthLead))
                {
                    return true;
                }
            }

            // Critical hits: Only show for very high rolls or based on narrative balance
            // Higher narrative balance = more frequent critical hit narratives
            // Use cooldown to prevent every critical hit from showing a narrative
            // Note: The narrative is only generated if this returns true (checked in AnalyzeEvent)
            if (evt.IsCritical && evt.IsSuccess)
            {
                // Roll property in BattleEvent is the base roll (without bonuses)
                // Since IsCritical is true, we know totalRoll >= 20 (critical threshold)
                // Only show critical hit narratives if:
                // 1. Narrative balance is high (>= 0.7) AND cooldown has expired, OR
                // 2. Very high base roll (>= 18) AND cooldown has expired
                // This prevents every critical hit from showing a narrative
                if (!stateManager.HasRecentCriticalHitNarrative)
                {
                    if (settings.NarrativeBalance >= 0.7 || evt.Roll >= 18)
                    {
                        return true;
                    }
                }
            }

            // Taunts: Only show if narrative balance is high enough
            if (settings.NarrativeBalance >= 0.6)
            {
                // Taunt logic is handled separately in AddTauntNarratives
                // This is just a placeholder - actual taunt significance is checked there
            }

            // Health recovery: Only show if narrative balance is high
            if (evt.IsHeal && evt.HealAmount > 0 && settings.NarrativeBalance >= 0.7)
            {
                return true;
            }

            return false;
        }

    }
}

