using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Manages combat state including battle narrative, turn tracking, and Actor management
    /// Extracted from CombatManager.cs to improve maintainability and separation of concerns
    /// </summary>
    public class CombatStateManager
    {
        private BattleNarrative? currentBattleNarrative;
        private TurnManager turnManager;
        private FunMomentTracker? funMomentTracker;

        public CombatStateManager()
        {
            turnManager = new TurnManager();
        }

        /// <summary>
        /// Starts battle narrative and initializes combat state
        /// </summary>
        public void StartBattleNarrative(string playerName, string enemyName, string locationName, int playerHealth, int enemyHealth)
        {
            // Clear previous battle's narrative before starting a new one
            currentBattleNarrative = null;

            currentBattleNarrative = new BattleNarrative(playerName, enemyName, locationName, playerHealth, enemyHealth);
            
            // Initialize fun moment tracker
            funMomentTracker = new FunMomentTracker();
            funMomentTracker.InitializeCombat(playerName, enemyName, playerHealth, enemyHealth);
            currentBattleNarrative.SetFunMomentTracker(funMomentTracker);
            
            UIManager.ResetForNewBattle(); // Reset Actor tracking for new battle
            TextDisplayIntegration.ResetForNewBattle(); // Reset new text display system
            turnManager.InitializeBattle();
            // Actor tracking is now handled by BlockDisplayManager
        }

        /// <summary>
        /// Ends battle narrative and cleans up combat state
        /// NOTE: Does NOT null the narrative - it's kept for post-battle metrics calculation
        /// The narrative will be cleared when StartBattleNarrative is called for the next battle
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
                        // Use system block for combat summary
                        BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse(summary));
                    }
                }

                // NOTE: We do NOT null currentBattleNarrative here
                // It needs to remain available for post-battle metrics calculation
            }
            turnManager.EndBattle();
        }

        /// <summary>
        /// Ends the battle narrative with final health values from actual entities
        /// NOTE: Does NOT null the narrative - it's kept for post-battle metrics calculation
        /// The narrative will be cleared when StartBattleNarrative is called for the next battle
        /// </summary>
        public void EndBattleNarrative(Character player, Enemy enemy)
        {
            if (currentBattleNarrative != null)
            {
                // Finalize fun moment tracking
                if (funMomentTracker != null)
                {
                    funMomentTracker.FinalizeCombat(player.IsAlive, player.CurrentHealth, player.GetEffectiveMaxHealth());
                }

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
                        // Use system block for combat summary
                        BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse(summary));
                    }
                }

                // NOTE: We do NOT null currentBattleNarrative here
                // It needs to remain available for post-battle metrics calculation
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
        public void InitializeCombatEntities(Character player, Enemy enemy, Environment? environment = null, bool playerGetsFirstAttack = false, bool enemyGetsFirstAttack = false)
        {
            var actionSpeedSystem = GetCurrentActionSpeedSystem();
            if (actionSpeedSystem == null) 
            {
                var errorText = CombatFlowColoredText.FormatSystemErrorColored("ActionSpeedSystem is null during InitializeCombatEntities!");
                BlockDisplayManager.DisplaySystemBlock(errorText);
                return;
            }

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
            
            // Handle forced turn order from exploration mechanics
            if (playerGetsFirstAttack)
            {
                // Set player's NextActionTime to 0.0, enemy's to 1.0
                actionSpeedSystem.SetEntityActionTime(player, 0.0);
                actionSpeedSystem.SetEntityActionTime(enemy, 1.0);
            }
            else if (enemyGetsFirstAttack)
            {
                // Set enemy's NextActionTime to 0.0, player's to 1.0
                actionSpeedSystem.SetEntityActionTime(enemy, 0.0);
                actionSpeedSystem.SetEntityActionTime(player, 1.0);
            }
            // Otherwise, use normal speed-based turn order (both start at currentTime)
            
            // Initialize health tracker for battle participants
            var participants = new List<Actor> { player, enemy };
            if (environment != null && environment.IsHostile)
            {
                participants.Add(environment);
            }
            turnManager.InitializeHealthTracker(participants);
        }

        /// <summary>
        /// Gets the next Actor that should act based on action speed
        /// </summary>
        public Actor? GetNextEntityToAct()
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
            bool newTurn = turnManager.RecordAction(entityName, actionName);
            
            // Notify fun moment tracker when a new turn starts
            if (newTurn && funMomentTracker != null)
            {
                funMomentTracker.EndTurn();
            }
            
            return newTurn;
        }

        /// <summary>
        /// Handles Actor change detection and adds blank lines when the acting Actor changes
        /// </summary>
        /// <param name="currentEntity">The Actor that is about to act</param>
        public void HandleEntityChange(Actor currentEntity)
        {
            // Actor tracking and blank lines are now handled by BlockDisplayManager
        }

        /// <summary>
        /// Processes damage over time effects for all entities
        /// </summary>
        public void ProcessDamageOverTimeEffects(Character player, Enemy enemy)
        {
            turnManager.ProcessDamageOverTimeEffects(player, enemy);
        }

        /// <summary>
        /// Gets the total number of actions performed in the current battle
        /// </summary>
        public int GetTotalActionCount()
        {
            return turnManager.GetTotalActionCount();
        }

        /// <summary>
        /// Gets the current turn number (turns increment every 10 actions)
        /// </summary>
        public int GetCurrentTurn()
        {
            return turnManager.GetCurrentTurn();
        }

        /// <summary>
        /// Gets the fun moment tracker for this combat
        /// </summary>
        public FunMomentTracker? GetFunMomentTracker()
        {
            return funMomentTracker;
        }
    }
}



