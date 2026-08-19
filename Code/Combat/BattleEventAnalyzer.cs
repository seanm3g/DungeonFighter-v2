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
        private string? creatureTier;

        // Per-event "should display this instance" flags, captured while generating
        // (before / as one-shot state flags mutate). Distinct from Has*Occurred, which
        // suppresses duplicate future triggers.
        private bool currentEventFirstBlood;
        private bool currentEventDefeat;
        private bool currentEventCriticalMiss;
        private bool currentEventEnvironmental;
        private bool currentEventHealthThreshold;
        private bool currentEventIntenseBattle;
        private bool currentEventGoodCombo;
        private bool currentEventHealthLead;
        private bool currentEventCriticalHit;
        private bool currentEventTaunt;
        private bool currentEventHealthRecovery;

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
            int initialPlayerHealth, int initialEnemyHealth, string? creatureTier = null)
        {
            this.playerName = playerName;
            this.enemyName = enemyName;
            this.currentLocation = currentLocation;
            this.creatureTier = creatureTier;
            this.initialPlayerHealth = initialPlayerHealth;
            this.initialEnemyHealth = initialEnemyHealth;
            this.finalPlayerHealth = initialPlayerHealth;
            this.finalEnemyHealth = initialEnemyHealth;
            triggerEvaluator.Initialize(playerName, enemyName, currentLocation, initialPlayerHealth, initialEnemyHealth, creatureTier);
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
            ResetCurrentEventDisplayFlags();

            // First Blood - first successful hit that deals damage.
            // Creature-tier banks (firstBlood_{tier}) author {name} as the enemy, same as
            // below50/below10/enemyDefeated. Generic firstBlood has no tokens; fill is a no-op.
            if (!stateManager.HasFirstBloodOccurred && evt.Damage > 0 && evt.IsSuccess)
            {
                currentEventFirstBlood = true;
                stateManager.SetFirstBloodOccurred();
                var replacements = new Dictionary<string, string> { { "name", enemyName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetCreatureTieredNarrative("firstBlood", creatureTier),
                    replacements);
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
                    currentEventCriticalHit = true;
                    stateManager.SetRecentCriticalHitNarrative();
                    var replacements = new Dictionary<string, string> { { "name", evt.Actor } };
                    string narrative = textProvider.ReplacePlaceholders(
                        evt.Actor == enemyName
                            ? textProvider.GetCreatureTieredNarrative("criticalHit", creatureTier)
                            : textProvider.GetRandomNarrative("criticalHit"),
                        replacements);
                    triggeredNarratives.Add(narrative);
                    stateManager.IncrementNarrativeEventCount();
                }
            }

            // Critical Miss - when a critical miss occurs (natural 1 only)
            if (!evt.IsSuccess && evt.NaturalRoll == 1)
            {
                currentEventCriticalMiss = true;
                var replacements = new Dictionary<string, string> { { "name", evt.Actor } };
                string narrative = textProvider.ReplacePlaceholders(
                    evt.Actor == enemyName
                        ? textProvider.GetCreatureTieredNarrative("criticalMiss", creatureTier)
                        : textProvider.GetRandomNarrative("criticalMiss"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }

            // Environmental Action - when environment takes action
            if (!string.IsNullOrEmpty(evt.EnvironmentEffect) && !stateManager.HasEnvironmentalActionOccurred)
            {
                currentEventEnvironmental = true;
                stateManager.SetEnvironmentalActionOccurred();
                var replacements = new Dictionary<string, string> { { "effect", evt.EnvironmentEffect } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("environmentalAction"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }

            // Health Recovery - when someone heals (balance gate lives here so generation == display)
            if (evt.IsHeal && evt.HealAmount > 0 && settings.NarrativeBalance >= 0.7)
            {
                currentEventHealthRecovery = true;
                var replacements = new Dictionary<string, string> { { "name", evt.Target } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("healthRecovery"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            // Health Lead Change - when someone gains or loses health advantage
            int countBefore = triggeredNarratives.Count;
            triggerEvaluator.AddHealthLeadNarratives(evt, triggeredNarratives, settings);
            if (triggeredNarratives.Count > countBefore)
                currentEventHealthLead = true;

            // Taunt System - characters and enemies taunt periodically
            countBefore = triggeredNarratives.Count;
            triggerEvaluator.AddTauntNarratives(evt, triggeredNarratives, settings);
            if (triggeredNarratives.Count > countBefore)
                currentEventTaunt = true;

            // Health Thresholds
            countBefore = triggeredNarratives.Count;
            triggerEvaluator.AddHealthThresholdNarratives(triggeredNarratives, settings);
            if (triggeredNarratives.Count > countBefore)
                currentEventHealthThreshold = true;

            // Intense Battle - when both combatants are below 50% health
            countBefore = triggeredNarratives.Count;
            triggerEvaluator.AddIntenseBattleNarrative(triggeredNarratives, settings);
            if (triggeredNarratives.Count > countBefore)
                currentEventIntenseBattle = true;

            // Good Combo - when someone gets a 3+ step combo
            if (!stateManager.HasGoodComboOccurred && evt.IsCombo && evt.ComboStep >= 2)
            {
                currentEventGoodCombo = true;
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
                currentEventDefeat = true;
                stateManager.SetPlayerDefeated();
                var replacements = new Dictionary<string, string> { { "enemy", enemyName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("playerDefeated"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            if (!stateManager.HasEnemyDefeated && finalEnemyHealth <= 0)
            {
                currentEventDefeat = true;
                stateManager.SetEnemyDefeated();
                var replacements = new Dictionary<string, string>
                {
                    { "name", enemyName },
                    { "player", playerName }
                };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetCreatureTieredNarrative("enemyDefeated", creatureTier),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            return triggeredNarratives;
        }


        /// <summary>
        /// Determines if an event is significant enough to warrant narrative display.
        /// Uses per-event flags captured during <see cref="AnalyzeEvent"/> — not the
        /// one-shot Has*Occurred state, which is already true by the time display runs.
        /// </summary>
        public bool IsSignificantEvent(BattleEvent evt, GameSettings settings)
        {
            if (currentEventFirstBlood)
                return true;
            if (currentEventDefeat)
                return true;
            if (currentEventCriticalMiss)
                return true;
            if (currentEventEnvironmental)
                return true;
            if (currentEventHealthThreshold)
                return true;
            if (currentEventIntenseBattle)
                return true;
            if (currentEventGoodCombo)
                return true;
            if (currentEventHealthLead)
                return true;
            if (currentEventCriticalHit)
                return true;
            if (currentEventTaunt)
                return true;
            if (currentEventHealthRecovery)
                return true;

            return false;
        }

        private void ResetCurrentEventDisplayFlags()
        {
            currentEventFirstBlood = false;
            currentEventDefeat = false;
            currentEventCriticalMiss = false;
            currentEventEnvironmental = false;
            currentEventHealthThreshold = false;
            currentEventIntenseBattle = false;
            currentEventGoodCombo = false;
            currentEventHealthLead = false;
            currentEventCriticalHit = false;
            currentEventTaunt = false;
            currentEventHealthRecovery = false;
        }

    }
}

