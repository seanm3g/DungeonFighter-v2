using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages combat state including battle narrative, turn tracking, and entity management
    /// Extracted from CombatManager.cs to improve maintainability and separation of concerns
    /// </summary>
    public class CombatStateManager
    {
        private BattleNarrative? currentBattleNarrative;
        private TurnManager turnManager;
        private Entity? lastActingEntity;

        public CombatStateManager()
        {
            turnManager = new TurnManager();
            lastActingEntity = null;
        }

        /// <summary>
        /// Starts battle narrative and initializes combat state
        /// </summary>
        public void StartBattleNarrative(string playerName, string enemyName, string locationName, int playerHealth, int enemyHealth)
        {
            currentBattleNarrative = new BattleNarrative(playerName, enemyName, locationName, playerHealth, enemyHealth);
            UIManager.ResetForNewBattle(); // Reset entity tracking for new battle
            TextDisplayIntegration.ResetForNewBattle(); // Reset new text display system
            turnManager.InitializeBattle();
            lastActingEntity = null; // Reset entity change tracking for new battle
        }

        /// <summary>
        /// Ends battle narrative and cleans up combat state
        /// </summary>
        public void EndBattleNarrative()
        {
            if (currentBattleNarrative != null)
            {
                // End the battle and generate narrative
                currentBattleNarrative.EndBattle();
                
                // Display only the battle summary (damage totals) since narrative events are now displayed immediately
                var settings = GameSettings.Instance;
                if (settings.EnableNarrativeEvents && !CombatManager.DisableCombatUIOutput)
                {
                    string summary = currentBattleNarrative.GenerateInformationalSummary();
                    if (!string.IsNullOrEmpty(summary))
                    {
                        // Add blank line before battle summary
                        UIManager.WriteBlankLine();
                        // Use system delay for combat summary
                        UIManager.WriteSystemLine(summary);
                    }
                }
                
                currentBattleNarrative = null;
            }
            turnManager.EndBattle();
        }

        /// <summary>
        /// Ends the battle narrative with final health values from actual entities
        /// </summary>
        public void EndBattleNarrative(Character player, Enemy enemy)
        {
            if (currentBattleNarrative != null)
            {
                // Update final health values from actual entities
                currentBattleNarrative.UpdateFinalHealth(player.CurrentHealth, enemy.CurrentHealth);
                
                // End the battle and generate narrative
                currentBattleNarrative.EndBattle();
                
                // Display only the battle summary (damage totals) since narrative events are now displayed immediately
                var settings = GameSettings.Instance;
                if (settings.EnableNarrativeEvents && !CombatManager.DisableCombatUIOutput)
                {
                    string summary = currentBattleNarrative.GenerateInformationalSummary();
                    if (!string.IsNullOrEmpty(summary))
                    {
                        // Add blank line before battle summary
                        UIManager.WriteBlankLine();
                        // Use system delay for combat summary
                        UIManager.WriteSystemLine(summary);
                    }
                }
                
                currentBattleNarrative = null;
            }
            turnManager.EndBattle();
        }

        /// <summary>
        /// Gets the current battle narrative
        /// </summary>
        public BattleNarrative? GetCurrentBattleNarrative()
        {
            return currentBattleNarrative;
        }

        /// <summary>
        /// Gets the current action speed system
        /// </summary>
        public ActionSpeedSystem? GetCurrentActionSpeedSystem()
        {
            return turnManager.GetActionSpeedSystem();
        }

        /// <summary>
        /// Initializes combat entities in the action speed system
        /// </summary>
        public void InitializeCombatEntities(Character player, Enemy enemy, Environment? environment = null)
        {
            var actionSpeedSystem = GetCurrentActionSpeedSystem();
            if (actionSpeedSystem == null) return;

            // New system: Use the attack speed directly as base speed
            double playerAttackSpeed = player.GetTotalAttackSpeed();
            double enemyAttackSpeed = enemy.GetTotalAttackSpeed();
            
            // For the new system, we use the attack speed directly as the base speed
            // This will be multiplied by action length in ExecuteAction
            actionSpeedSystem.AddEntity(player, playerAttackSpeed);
            actionSpeedSystem.AddEntity(enemy, enemyAttackSpeed);

            // Add environment to action speed system with a slow base speed (longer cooldowns)
            if (environment != null && environment.IsHostile)
            {
                double environmentBaseSpeed = 15.0; // Very slow - environment acts infrequently
                actionSpeedSystem.AddEntity(environment, environmentBaseSpeed);
            }
            
            // Initialize health tracker for battle participants
            var participants = new List<Entity> { player, enemy };
            if (environment != null && environment.IsHostile)
            {
                participants.Add(environment);
            }
            turnManager.InitializeHealthTracker(participants);
        }

        /// <summary>
        /// Gets the next entity that should act based on action speed
        /// </summary>
        public Entity? GetNextEntityToAct()
        {
            var actionSpeedSystem = GetCurrentActionSpeedSystem();
            return actionSpeedSystem?.GetNextEntityToAct();
        }

        /// <summary>
        /// Updates the last player action for DEJA VU functionality
        /// </summary>
        public void UpdateLastPlayerAction(Action action)
        {
            turnManager.UpdateLastPlayerAction(action);
        }

        /// <summary>
        /// Gets the last player action for DEJA VU functionality
        /// </summary>
        public Action? GetLastPlayerAction()
        {
            return turnManager.GetLastPlayerAction();
        }

        /// <summary>
        /// Gets the current turn information for display
        /// </summary>
        public string GetTurnInfo()
        {
            return turnManager.GetTurnInfo();
        }

        /// <summary>
        /// Records an action for turn counting
        /// </summary>
        public bool RecordAction(string entityName, string actionName)
        {
            return turnManager.RecordAction(entityName, actionName);
        }

        /// <summary>
        /// Handles entity change detection and adds blank lines when the acting entity changes
        /// </summary>
        /// <param name="currentEntity">The entity that is about to act</param>
        public void HandleEntityChange(Entity currentEntity)
        {
            // Blank lines are now handled by UIManager.WriteCombatLine() to prevent duplication
            
            // Update the last acting entity
            lastActingEntity = currentEntity;
        }

        /// <summary>
        /// Processes damage over time effects for all entities
        /// </summary>
        public void ProcessDamageOverTimeEffects(Character player, Enemy enemy)
        {
            turnManager.ProcessDamageOverTimeEffects(player, enemy);
        }
    }
}
