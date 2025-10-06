using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles turn processing logic for player, enemy, and environment entities
    /// Extracted from CombatManager.cs to improve maintainability and separation of concerns
    /// </summary>
    public class CombatTurnProcessor
    {
        private readonly CombatStateManager stateManager;

        public CombatTurnProcessor(CombatStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Processes a single player turn
        /// </summary>
        public bool ProcessPlayerTurn(Character player, Enemy currentEnemy, Environment room)
        {
            // Check if player is stunned
            if (player.StunTurnsRemaining > 0)
            {
                if (!CombatManager.DisableCombatUIOutput)
                {
                    // Use WriteStunLine for configurable stun message handling (no indentation)
                    UIManager.WriteStunLine($"[{player.Name}] is stunned and cannot act! ({player.StunTurnsRemaining} turns remaining)");
                    // Don't add explicit blank line - let UIManager handle entity-based spacing
                }
                
                // Get the player's action speed to calculate proper stun reduction
                double playerActionSpeed = player.GetTotalAttackSpeed();
                
                // Update temp effects with action speed-based turn reduction
                player.UpdateTempEffects(playerActionSpeed / 10.0); // Normalize to turn-based system
                
                // Advance the player's turn in the action speed system based on their action speed
                var currentSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (currentSpeedSystem != null)
                {
                    currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
                }
            }
            else
            {
                // Roll first to determine what type of action to use
                int baseRoll = Dice.Roll(1, 20);
                int rollBonus = ActionUtilities.CalculateRollBonus(player, null); // Calculate base roll bonus
                int totalRoll = baseRoll + rollBonus;
                
                // Determine action type based on roll result
                Action? attemptedAction = DeterminePlayerAction(player, baseRoll, totalRoll);
                
                // Handle the different cases
                if (totalRoll < 6)
                {
                    ProcessPlayerMiss(player, currentEnemy, baseRoll, rollBonus, totalRoll);
                }
                else if (attemptedAction != null)
                {
                    ProcessPlayerAction(player, currentEnemy, room, attemptedAction, baseRoll, rollBonus, totalRoll);
                }
                else
                {
                    ProcessPlayerNoAction(player);
                }
            }
            
            // Process health regeneration for player after they act
            ProcessPlayerHealthRegeneration(player);
            
            // Return false if enemy is dead (combat should end)
            return currentEnemy.IsAlive;
        }

        /// <summary>
        /// Processes a single enemy turn
        /// </summary>
        public bool ProcessEnemyTurn(Character player, Enemy currentEnemy, Environment room)
        {
            // Check if enemy is stunned
            if (currentEnemy.StunTurnsRemaining > 0)
            {
                if (!CombatManager.DisableCombatUIOutput)
                {
                    // Use WriteStunLine for configurable stun message handling (no indentation)
                    UIManager.WriteStunLine($"[{currentEnemy.Name}] is stunned and cannot act! ({currentEnemy.StunTurnsRemaining} turns remaining)");
                    // Don't add explicit blank line - let UIManager handle entity-based spacing
                }
                
                // Get the enemy's action speed to calculate proper stun reduction
                double enemyActionSpeed = currentEnemy.GetTotalAttackSpeed();
                
                // Update temp effects with action speed-based turn reduction
                currentEnemy.UpdateTempEffects(enemyActionSpeed / 10.0); // Normalize to turn-based system
                
                // Advance the enemy's turn in the action speed system based on their action speed
                var currentSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (currentSpeedSystem != null)
                {
                    currentSpeedSystem.AdvanceEntityTurn(currentEnemy, enemyActionSpeed);
                }
            }
            else
            {
                // Use dice-based action selection for enemies (no forced action)
                var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(currentEnemy, player, null, room, stateManager.GetLastPlayerAction(), stateManager.GetCurrentBattleNarrative());
                bool textDisplayed = !string.IsNullOrEmpty(result);
                
                // Get the action that was actually used for turn counting
                var usedAction = ActionExecutor.GetLastUsedAction(currentEnemy);
                string actionName = usedAction?.Name ?? "UNKNOWN ACTION";
                
                // Record the action for turn counting
                bool newTurn = stateManager.RecordAction(currentEnemy.Name, actionName);
                if (newTurn && !CombatManager.DisableCombatUIOutput)
                {
                    UIManager.WriteSystemLine($"--- {stateManager.GetTurnInfo()} ---");
                }
                
                // Get triggered narratives and display everything together
                var battleNarrative = stateManager.GetCurrentBattleNarrative();
                if (textDisplayed && battleNarrative != null)
                {
                    var narratives = battleNarrative.GetTriggeredNarratives();
                    // Add proper indentation to status effects
                    var indentedStatusEffects = statusEffects.Select(effect => $"    {effect}").ToList();
                    TextDisplayIntegration.DisplayCombatAction(result, narratives, indentedStatusEffects, currentEnemy.Name);
                }
                
                // Update enemy's action timing in the action speed system
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    if (usedAction != null)
                    {
                        actionSpeedSystem.ExecuteAction(currentEnemy, usedAction);
                    }
                }
            }
            
            // Return false if player is dead (combat should end)
            if (!player.IsAlive)
            {
                return false;
            }
            
            // Process poison/bleed and burn damage after enemy's turn
            stateManager.ProcessDamageOverTimeEffects(player, currentEnemy);
            
            return true;
        }

        /// <summary>
        /// Processes a single environment turn
        /// </summary>
        public bool ProcessEnvironmentTurn(Character player, Enemy currentEnemy, Environment room)
        {
            if (room.ShouldEnvironmentAct())
            {
                var envAction = room.SelectAction();
                if (envAction != null)
                {
                    // Create list of all characters in the room (player and current enemy)
                    var allTargets = new List<Entity> { player, currentEnemy };
                    
                    // Use area of effect action to target all characters
                    string result = EnvironmentalActionHandler.ExecuteAreaOfEffectAction(room, allTargets, room, envAction);
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Record the environmental action for turn counting
                    bool newTurn = stateManager.RecordAction(room.Name, envAction.Name);
                    if (newTurn && !CombatManager.DisableCombatUIOutput)
                    {
                        UIManager.WriteSystemLine($"--- {stateManager.GetTurnInfo()} ---");
                    }
                    
                    // Display environmental action result
                    if (textDisplayed)
                    {
                        // Use WriteCombatLine for proper entity tracking and spacing
                        UIManager.WriteCombatLine(result);
                        
                        // Don't add explicit blank line - let UIManager handle entity-based spacing
                    }
                    
                    // Update environment's action timing in the action speed system
                    var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null)
                    {
                        actionSpeedSystem.ExecuteAction(room, envAction);
                    }
                    
                    // Return false if player is dead (combat should end)
                    if (!player.IsAlive)
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Environment doesn't want to act, but we still need to advance its turn
                // to prevent it from getting stuck in the action speed system
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    // Advance the environment's turn by a small amount to prevent infinite loops
                    actionSpeedSystem.AdvanceEntityTurn(room, 1.0);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Determines what action the player should attempt based on roll results
        /// </summary>
        private Action? DeterminePlayerAction(Character player, int baseRoll, int totalRoll)
        {
            if (baseRoll == 20) // Natural 20 - always combo + critical hit
            {
                // Use combo action for natural 20
                var comboActions = player.GetComboActions();
                if (comboActions.Count > 0)
                {
                    int actionIdx = player.ComboStep % comboActions.Count;
                    return comboActions[actionIdx];
                }
                else
                {
                    return CreateEmergencyComboAction(player);
                }
            }
            else if (totalRoll >= 14) // Combo threshold
            {
                // Use combo action
                var comboActions = player.GetComboActions();
                if (comboActions.Count > 0)
                {
                    int actionIdx = player.ComboStep % comboActions.Count;
                    return comboActions[actionIdx];
                }
                else
                {
                    // Fallback to basic attack if no combo actions available
                    return player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                }
            }
            else if (totalRoll >= 6) // Basic attack threshold
            {
                // Use basic attack
                var basicAttack = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                
                // Fallback if BASIC ATTACK is not found
                if (basicAttack == null)
                {
                    // Try to load BASIC ATTACK directly
                    basicAttack = ActionLoader.GetAction("BASIC ATTACK");
                }
                
                // Last resort: pick any Attack-type action
                if (basicAttack == null)
                {
                    basicAttack = player.ActionPool.FirstOrDefault(a => a.action.Type == ActionType.Attack || a.action.Type == ActionType.Spell).action;
                }
                
                return basicAttack;
            }
            
            return null; // Miss case
        }

        /// <summary>
        /// Creates an emergency combo action when none are available
        /// </summary>
        private Action CreateEmergencyComboAction(Character player)
        {
            DebugLogger.Log("CombatTurnProcessor", $"ERROR: No combo actions available for {player.Name} on natural 20! This should never happen.");
            
            // Try to find any combo action from the action pool
            var anyComboAction = player.ActionPool
                .Where(a => a.action.IsComboAction)
                .Select(a => a.action)
                .FirstOrDefault();
            
            if (anyComboAction != null)
            {
                DebugLogger.Log("CombatTurnProcessor", $"Found combo action {anyComboAction.Name} for {player.Name}");
                return anyComboAction;
            }
            else
            {
                // Last resort: create a combo action on the fly
                var emergencyAction = new Action(
                    name: "EMERGENCY STRIKE",
                    type: ActionType.Attack,
                    targetType: TargetType.SingleTarget,
                    baseValue: 0,
                    range: 1,
                    cooldown: 0,
                    description: "An emergency strike created when no combo actions were available",
                    comboOrder: 1,
                    damageMultiplier: 1.3,
                    length: 1.0,
                    causesBleed: false,
                    causesWeaken: false,
                    isComboAction: true
                );
                player.AddAction(emergencyAction, 1.0);
                DebugLogger.Log("CombatTurnProcessor", $"Created emergency combo action for {player.Name}");
                return emergencyAction;
            }
        }

        /// <summary>
        /// Processes a player miss
        /// </summary>
        private void ProcessPlayerMiss(Character player, Enemy currentEnemy, int baseRoll, int rollBonus, int totalRoll)
        {
            // Check for critical miss and apply penalty BEFORE formatting message
            bool isCriticalMiss = totalRoll <= 1;
            if (isCriticalMiss)
            {
                // Apply critical miss penalty - doubles action speed for next turn
                player.HasCriticalMissPenalty = true;
                player.CriticalMissPenaltyTurns = 1;
            }
            
            // Use basic attack for miss message formatting
            var basicAttack = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
            if (basicAttack != null)
            {
                string missMessage = CombatResults.FormatMissMessage(player, currentEnemy, basicAttack, baseRoll, rollBonus);
                if (!CombatManager.DisableCombatUIOutput)
                {
                    // Use TextDisplayIntegration for consistent entity tracking
                    TextDisplayIntegration.DisplayCombatAction(missMessage, new List<string>(), new List<string>(), player.Name);
                }
                
                // Record the miss as an action for turn counting
                bool newTurn = stateManager.RecordAction(player.Name, "MISS");
                if (newTurn && !CombatManager.DisableCombatUIOutput)
                {
                    UIManager.WriteSystemLine($"--- {stateManager.GetTurnInfo()} ---");
                }
                
                // Update player's action timing in the action speed system even for misses
                var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    actionSpeedSystem.ExecuteAction(player, basicAttack);
                }
            }
            else
            {
                // Fallback if no basic attack is available
                if (!CombatManager.DisableCombatUIOutput)
                {
                    // Use TextDisplayIntegration for consistent entity tracking
                    string missType = isCriticalMiss ? "CRITICAL MISS" : "misses";
                    string fallbackMessage = $"[{player.Name}] {missType}! (roll: {baseRoll} + {rollBonus} = {totalRoll})";
                    TextDisplayIntegration.DisplayCombatAction(fallbackMessage, new List<string>(), new List<string>(), player.Name);
                }
                
                // Record the miss as an action for turn counting
                bool newTurn = stateManager.RecordAction(player.Name, "MISS");
                if (newTurn && !CombatManager.DisableCombatUIOutput)
                {
                    UIManager.WriteSystemLine($"--- {stateManager.GetTurnInfo()} ---");
                }
            }
        }

        /// <summary>
        /// Processes a player action execution
        /// </summary>
        private void ProcessPlayerAction(Character player, Enemy currentEnemy, Environment room, Action attemptedAction, int baseRoll, int rollBonus, int totalRoll)
        {
            // Execute single action (not multi-attack) with speed tracking
            var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(player, currentEnemy, attemptedAction, room, stateManager.GetLastPlayerAction(), stateManager.GetCurrentBattleNarrative());
            bool textDisplayed = !string.IsNullOrEmpty(result);
            
            // Record the action for turn counting
            bool newTurn = stateManager.RecordAction(player.Name, attemptedAction.Name);
            if (newTurn && !CombatManager.DisableCombatUIOutput)
            {
                UIManager.WriteSystemLine($"--- {stateManager.GetTurnInfo()} ---");
            }
            
            // Get triggered narratives and display everything together
            var battleNarrative = stateManager.GetCurrentBattleNarrative();
            if (textDisplayed && battleNarrative != null)
            {
                var narratives = battleNarrative.GetTriggeredNarratives();
                // Add proper indentation to status effects
                var indentedStatusEffects = statusEffects.Select(effect => $"    {effect}").ToList();
                TextDisplayIntegration.DisplayCombatAction(result, narratives, indentedStatusEffects, player.Name);
            }
            
            // Update last player action for DEJA VU functionality
            stateManager.UpdateLastPlayerAction(attemptedAction);
            
            // Update player's action timing in the action speed system
            var actionSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (actionSpeedSystem != null)
            {
                actionSpeedSystem.ExecuteAction(player, attemptedAction);
            }
        }

        /// <summary>
        /// Processes when player has no actions available
        /// </summary>
        private void ProcessPlayerNoAction(Character player)
        {
            // Player has no actions available - advance their turn to prevent infinite loop
            if (!CombatManager.DisableCombatUIOutput)
            {
                UIManager.WriteLine($"[{player.Name}] has no actions available and cannot act!");
            }
            var currentSpeedSystem = stateManager.GetCurrentActionSpeedSystem();
            if (currentSpeedSystem != null)
            {
                double playerActionSpeed = player.GetTotalAttackSpeed();
                currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
            }
        }

        /// <summary>
        /// Processes health regeneration for the player
        /// </summary>
        private void ProcessPlayerHealthRegeneration(Character player)
        {
            int playerHealthRegen = player.GetEquipmentHealthRegenBonus();
            if (playerHealthRegen > 0 && player.CurrentHealth < player.GetEffectiveMaxHealth())
            {
                int oldHealth = player.CurrentHealth;
                // Use negative damage to heal (TakeDamage with negative value heals)
                player.TakeDamage(-playerHealthRegen);
                // Cap at max health
                if (player.CurrentHealth > player.GetEffectiveMaxHealth())
                {
                    player.TakeDamage(player.CurrentHealth - player.GetEffectiveMaxHealth());
                }
                int actualRegen = player.CurrentHealth - oldHealth;
                if (actualRegen > 0 && !CombatManager.DisableCombatUIOutput)
                {
                    UIManager.WriteLine($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                }
            }
        }
    }
}
