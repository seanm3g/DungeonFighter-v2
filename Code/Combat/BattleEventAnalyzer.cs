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
            if (evt.IsCritical && evt.IsSuccess)
            {
                var replacements = new Dictionary<string, string> { { "name", evt.Actor } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("criticalHit"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
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
            AddHealthLeadNarratives(evt, triggeredNarratives, settings);

            // Taunt System - characters and enemies taunt periodically
            AddTauntNarratives(evt, triggeredNarratives, settings);

            // Health Thresholds
            AddHealthThresholdNarratives(triggeredNarratives, settings);

            // Intense Battle - when both combatants are below 50% health
            AddIntenseBattleNarrative(triggeredNarratives, settings);

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
        /// Tracks actor actions for taunt and combo systems
        /// </summary>
        public void TrackActorAction(string actorName)
        {
            if (actorName == playerName)
            {
                stateManager.IncrementPlayerActionCount();
            }
            else if (actorName == enemyName)
            {
                stateManager.IncrementEnemyActionCount();
            }
        }

        // ===== Private Helper Methods =====

        private void AddHealthLeadNarratives(BattleEvent evt, List<string> triggeredNarratives, GameSettings settings)
        {
            // Determine current health lead
            bool playerCurrentlyLeads = finalPlayerHealth > finalEnemyHealth;
            bool enemyCurrentlyLeads = finalEnemyHealth > finalPlayerHealth;

            // Only trigger health lead change narratives for significant damage (3+ damage)
            bool significantDamage = evt.Damage >= 3;

            // Check if lead has changed
            if (playerCurrentlyLeads && !stateManager.HasPlayerHealthLead && significantDamage)
            {
                stateManager.SetPlayerHealthLead();
                var replacements = new Dictionary<string, string> { { "name", playerName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("healthLeadChange"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }
            else if (enemyCurrentlyLeads && !stateManager.HasEnemyHealthLead && significantDamage)
            {
                stateManager.SetEnemyHealthLead();
                var replacements = new Dictionary<string, string> { { "name", enemyName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("healthLeadChange"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }
        }

        private void AddTauntNarratives(BattleEvent evt, List<string> triggeredNarratives, GameSettings settings)
        {
            TrackActorAction(evt.Actor);

            // Player taunts
            if (evt.Actor == playerName && stateManager.CanPlayerTaunt)
            {
                var (shouldTaunt, tauntText) = tauntSystem.CheckPlayerTaunt(
                    stateManager.PlayerActionCount,
                    stateManager.PlayerTauntCount,
                    playerName,
                    enemyName,
                    currentLocation,
                    settings);

                if (shouldTaunt)
                {
                    triggeredNarratives.Add(tauntText);
                    stateManager.IncrementNarrativeEventCount();
                    stateManager.IncrementPlayerTauntCount();
                }
            }

            // Enemy taunts
            if (evt.Actor == enemyName && stateManager.CanEnemyTaunt)
            {
                var (shouldTaunt, tauntText) = tauntSystem.CheckEnemyTaunt(
                    stateManager.EnemyActionCount,
                    stateManager.EnemyTauntCount,
                    enemyName,
                    playerName,
                    currentLocation,
                    settings);

                if (shouldTaunt)
                {
                    triggeredNarratives.Add(tauntText);
                    stateManager.IncrementNarrativeEventCount();
                    stateManager.IncrementEnemyTauntCount();
                }
            }
        }

        private void AddHealthThresholdNarratives(List<string> triggeredNarratives, GameSettings settings)
        {
            var playerHealthPercentage = (double)finalPlayerHealth / initialPlayerHealth;
            var enemyHealthPercentage = (double)finalEnemyHealth / initialEnemyHealth;

            // Below 50% Health
            if (!stateManager.HasPlayerBelow50Percent && playerHealthPercentage < 0.5 && finalPlayerHealth > 0)
            {
                stateManager.SetPlayerBelow50Percent();
                var replacements = new Dictionary<string, string> { { "name", playerName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("below50Percent"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            if (!stateManager.HasEnemyBelow50Percent && enemyHealthPercentage < 0.5 && finalEnemyHealth > 0)
            {
                stateManager.SetEnemyBelow50Percent();
                var replacements = new Dictionary<string, string> { { "name", enemyName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("below50Percent"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            // Below 10% Health
            if (!stateManager.HasPlayerBelow10Percent && playerHealthPercentage < 0.1 && finalPlayerHealth > 0)
            {
                stateManager.SetPlayerBelow10Percent();
                var replacements = new Dictionary<string, string> { { "name", playerName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("below10Percent"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }

            if (!stateManager.HasEnemyBelow10Percent && enemyHealthPercentage < 0.1 && finalEnemyHealth > 0)
            {
                stateManager.SetEnemyBelow10Percent();
                var replacements = new Dictionary<string, string> { { "name", enemyName } };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("below10Percent"),
                    replacements);
                triggeredNarratives.Add(narrative);
            }
        }

        private void AddIntenseBattleNarrative(List<string> triggeredNarratives, GameSettings settings)
        {
            var playerHealthPercentage = (double)finalPlayerHealth / initialPlayerHealth;
            var enemyHealthPercentage = (double)finalEnemyHealth / initialEnemyHealth;

            if (!stateManager.HasIntenseBattleTriggered && playerHealthPercentage < 0.5 && enemyHealthPercentage < 0.5 && finalPlayerHealth > 0 && finalEnemyHealth > 0)
            {
                stateManager.SetIntenseBattleTriggered();
                var replacements = new Dictionary<string, string>
                {
                    { "player", playerName },
                    { "enemy", enemyName }
                };
                string narrative = textProvider.ReplacePlaceholders(
                    textProvider.GetRandomNarrative("intenseBattle"),
                    replacements);
                triggeredNarratives.Add(narrative);
                stateManager.IncrementNarrativeEventCount();
            }
        }
    }
}

