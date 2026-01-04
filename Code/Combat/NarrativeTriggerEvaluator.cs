using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Evaluates when narrative triggers should fire based on battle events and health thresholds.
    /// Extracted from BattleEventAnalyzer to reduce size and improve Single Responsibility Principle compliance.
    /// </summary>
    public class NarrativeTriggerEvaluator
    {
        private readonly NarrativeTextProvider textProvider;
        private readonly NarrativeStateManager stateManager;
        private readonly TauntSystem tauntSystem;
        private readonly string playerName;
        private readonly string enemyName;
        private readonly string currentLocation;
        private readonly int initialPlayerHealth;
        private readonly int initialEnemyHealth;
        private int finalPlayerHealth;
        private int finalEnemyHealth;
        
        public NarrativeTriggerEvaluator(
            NarrativeTextProvider textProvider,
            NarrativeStateManager stateManager,
            TauntSystem tauntSystem,
            string playerName,
            string enemyName,
            string currentLocation,
            int initialPlayerHealth,
            int initialEnemyHealth)
        {
            this.textProvider = textProvider;
            this.stateManager = stateManager;
            this.tauntSystem = tauntSystem;
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
        /// Adds health lead change narratives if applicable
        /// </summary>
        public void AddHealthLeadNarratives(BattleEvent evt, List<string> triggeredNarratives, GameSettings settings)
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
        
        /// <summary>
        /// Adds taunt narratives if applicable
        /// </summary>
        public void AddTauntNarratives(BattleEvent evt, List<string> triggeredNarratives, GameSettings settings)
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
        
        /// <summary>
        /// Adds health threshold narratives if applicable
        /// </summary>
        public void AddHealthThresholdNarratives(List<string> triggeredNarratives, GameSettings settings)
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
        
        /// <summary>
        /// Adds intense battle narrative if applicable
        /// </summary>
        public void AddIntenseBattleNarrative(List<string> triggeredNarratives, GameSettings settings)
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
        
        /// <summary>
        /// Tracks actor actions for taunt and combo systems
        /// </summary>
        private void TrackActorAction(string actorName)
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
    }
}
