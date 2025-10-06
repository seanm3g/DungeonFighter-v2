using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages combat loop logic including turn-based combat, effect processing, and combat state
    /// </summary>
    public class CombatManager
    {
        // Flag to disable UI output during balance analysis
        public static bool DisableCombatUIOutput = false;
        
        // Combat state management (moved from Combat.cs)
        private BattleNarrative? currentBattleNarrative;
        private TurnManager turnManager = new TurnManager();
        private Entity? lastActingEntity = null;

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
                if (settings.EnableNarrativeEvents && !DisableCombatUIOutput)
                {
                    string summary = currentBattleNarrative.GenerateInformationalSummary();
                    if (!string.IsNullOrEmpty(summary))
                    {
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
                if (settings.EnableNarrativeEvents && !DisableCombatUIOutput)
                {
                    string summary = currentBattleNarrative.GenerateInformationalSummary();
                    if (!string.IsNullOrEmpty(summary))
                    {
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
        /// Handles entity change detection and adds blank lines when the acting entity changes
        /// </summary>
        /// <param name="currentEntity">The entity that is about to act</param>
        private void HandleEntityChange(Entity currentEntity)
        {
            // Blank lines are now handled by UIManager.WriteCombatLine() to prevent duplication
            
            // Update the last acting entity
            lastActingEntity = currentEntity;
        }

        /// <summary>
        /// Runs the main combat loop between player and enemy
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="currentEnemy">The current enemy</param>
        /// <param name="room">The current room/environment</param>
        /// <returns>True if combat completed successfully, false if player died</returns>
        public bool RunCombat(Character player, Enemy currentEnemy, Environment room)
        {
            DebugLogger.WriteCombatDebug("CombatManager", $"Starting combat: {player.Name} vs {currentEnemy.Name} in {room.Name}");
            
            // Start battle narrative and initialize action speed system
            StartBattleNarrative(player.Name, currentEnemy.Name, room.Name, player.CurrentHealth, currentEnemy.CurrentHealth);
            InitializeCombatEntities(player, currentEnemy, room);
            
            // Reset game time AFTER initializing combat entities to avoid timing conflicts
            GameTicker.Instance.Reset();
            
            // Reset environment action count for new fight
            room.ResetForNewFight();

            // Combat Loop with action speed system
            while (player.IsAlive && currentEnemy.IsAlive)
            {
                // Get the next entity that should act based on action speed
                Entity? nextEntity = GetNextEntityToAct();
                
                if (nextEntity == null)
                {
                    // No entities ready, advance time to the next entity's action time
                    var currentSpeedSystem = GetCurrentActionSpeedSystem();
                    if (currentSpeedSystem != null)
                    {
                        // Find the next entity that will be ready and advance time to that point
                        var nextReadyTime = currentSpeedSystem.GetNextReadyTime();
                        if (nextReadyTime > 0)
                        {
                            double timeToAdvance = nextReadyTime - currentSpeedSystem.GetCurrentTime();
                            if (timeToAdvance > 0)
                            {
                                currentSpeedSystem.AdvanceTime(timeToAdvance);
                            }
                            else
                            {
                                // Fallback: advance time slightly
                                currentSpeedSystem.AdvanceTime(0.1);
                            }
                        }
                        else
                        {
                            // No entities ready and no next ready time - this shouldn't happen
                            Console.WriteLine("ERROR: No entities ready and no next ready time. Breaking combat loop.");
                            break;
                        }
                    }
                    else
                    {
                        // ActionSpeedSystem is null - this shouldn't happen
                        break;
                    }
                    continue;
                }

                // Handle entity change detection and add blank lines when needed
                HandleEntityChange(nextEntity);

                // Player acts
                if (nextEntity == player && player.IsAlive)
                {
                    if (!ProcessPlayerTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                }
                // Enemy acts
                else if (nextEntity == currentEnemy && currentEnemy.IsAlive)
                {
                    if (!ProcessEnemyTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                }
                // Environment acts
                else if (nextEntity == room && room.IsHostile && room.ActionPool.Count > 0)
                {
                    if (!ProcessEnvironmentTurn(player, currentEnemy, room))
                    {
                        break; // Combat ended
                    }
                }
            }
            
            // End the battle narrative with final health values
            EndBattleNarrative(player, currentEnemy);
            
            DebugLogger.WriteCombatDebug("CombatManager", $"Combat ended: {player.Name} {(player.IsAlive ? "survived" : "died")} vs {currentEnemy.Name}");
            
            // Return true if player survived, false if player died
            return player.IsAlive;
        }

        /// <summary>
        /// Processes a single player turn
        /// </summary>
        private bool ProcessPlayerTurn(Character player, Enemy currentEnemy, Environment room)
        {
                // Check if player is stunned
            if (player.StunTurnsRemaining > 0)
            {
                if (!DisableCombatUIOutput)
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
                var currentSpeedSystem = GetCurrentActionSpeedSystem();
                if (currentSpeedSystem != null)
                {
                    currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
                }
            }
            else
            {
                // Roll first to determine what type of action to use
                int baseRoll = Dice.Roll(1, 20);
                int rollBonus = CombatActions.CalculateRollBonus(player, null); // Calculate base roll bonus
                int totalRoll = baseRoll + rollBonus;
                
                
                // Determine action type based on roll result
                Action? attemptedAction = null;
                if (baseRoll == 20) // Natural 20 - always combo + critical hit
                {
                    // Use combo action for natural 20
                    var comboActions = player.GetComboActions();
                    if (comboActions.Count > 0)
                    {
                        int actionIdx = player.ComboStep % comboActions.Count;
                        attemptedAction = comboActions[actionIdx];
                    }
                    else
                    {
                        // This should never happen - combo actions should always be available
                        // If we reach here, there's a bug in the combo initialization
                        DebugLogger.Log("CombatManager", $"ERROR: No combo actions available for {player.Name} on natural 20! This should never happen.");
                        
                        // Try to find any combo action from the action pool
                        var anyComboAction = player.ActionPool
                            .Where(a => a.action.IsComboAction)
                            .Select(a => a.action)
                            .FirstOrDefault();
                        
                        if (anyComboAction != null)
                        {
                            attemptedAction = anyComboAction;
                            DebugLogger.Log("CombatManager", $"Found combo action {anyComboAction.Name} for {player.Name}");
                        }
                        else
                        {
                            // Last resort: create a combo action on the fly
                            attemptedAction = new Action(
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
                            player.AddAction(attemptedAction, 1.0);
                            DebugLogger.Log("CombatManager", $"Created emergency combo action for {player.Name}");
                        }
                    }
                }
                else if (totalRoll >= 14) // Combo threshold
                {
                    // Use combo action
                    var comboActions = player.GetComboActions();
                    if (comboActions.Count > 0)
                    {
                        int actionIdx = player.ComboStep % comboActions.Count;
                        attemptedAction = comboActions[actionIdx];
                    }
                    else
                    {
                        // Fallback to basic attack if no combo actions available
                        attemptedAction = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                    }
                }
                else if (totalRoll >= 6) // Basic attack threshold
                {
                    // Use basic attack
                    attemptedAction = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                    
                    // Fallback if BASIC ATTACK is not found
                    if (attemptedAction == null)
                    {
                        // Try to load BASIC ATTACK directly
                        attemptedAction = ActionLoader.GetAction("BASIC ATTACK");
                    }
                    
                    // Last resort: pick any Attack-type action
                    if (attemptedAction == null)
                    {
                        attemptedAction = player.ActionPool.FirstOrDefault(a => a.action.Type == ActionType.Attack || a.action.Type == ActionType.Spell).action;
                    }
                }
                // Handle the different cases
                if (totalRoll < 6)
                {
                    // Miss - no action executed
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
                        if (!DisableCombatUIOutput)
                        {
                            // Use TextDisplayIntegration for consistent entity tracking
                            TextDisplayIntegration.DisplayCombatAction(missMessage, new List<string>(), new List<string>(), player.Name);
                        }
                        
                        // Update player's action timing in the action speed system even for misses
                        var actionSpeedSystem = GetCurrentActionSpeedSystem();
                        if (actionSpeedSystem != null)
                        {
                            actionSpeedSystem.ExecuteAction(player, basicAttack);
                        }
                    }
                    else
                    {
                        // Fallback if no basic attack is available
                        if (!DisableCombatUIOutput)
                        {
                            // Use TextDisplayIntegration for consistent entity tracking
                            string missType = isCriticalMiss ? "CRITICAL MISS" : "misses";
                            string fallbackMessage = $"[{player.Name}] {missType}! (roll: {baseRoll} + {rollBonus} = {totalRoll})";
                            TextDisplayIntegration.DisplayCombatAction(fallbackMessage, new List<string>(), new List<string>(), player.Name);
                        }
                    }
                }
                else if (attemptedAction != null)
                {
                    // Execute single action (not multi-attack) with speed tracking
                    var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(player, currentEnemy, attemptedAction, room, GetLastPlayerAction(), currentBattleNarrative);
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Get triggered narratives and display everything together
                    if (textDisplayed && currentBattleNarrative != null)
                    {
                        var narratives = currentBattleNarrative.GetTriggeredNarratives();
                        // Add proper indentation to status effects
                        var indentedStatusEffects = statusEffects.Select(effect => $"    {effect}").ToList();
                        TextDisplayIntegration.DisplayCombatAction(result, narratives, indentedStatusEffects, player.Name);
                    }
                    
                    // Update last player action for DEJA VU functionality
                    UpdateLastPlayerAction(attemptedAction);
                    
                    // Update player's action timing in the action speed system
                    var actionSpeedSystem = GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null)
                    {
                        actionSpeedSystem.ExecuteAction(player, attemptedAction);
                    }
                    
                    // Display is now handled by TextDisplayIntegration.DisplayCombatAction above
                }
                else
                {
                    // Player has no actions available - advance their turn to prevent infinite loop
                    if (!DisableCombatUIOutput)
                    {
                        UIManager.WriteLine($"[{player.Name}] has no actions available and cannot act!");
                    }
                    var currentSpeedSystem = GetCurrentActionSpeedSystem();
                    if (currentSpeedSystem != null)
                    {
                        double playerActionSpeed = player.GetTotalAttackSpeed();
                        currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
                    }
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
        private bool ProcessEnemyTurn(Character player, Enemy currentEnemy, Environment room)
        {
            // Check if enemy is stunned
            if (currentEnemy.StunTurnsRemaining > 0)
            {
                if (!DisableCombatUIOutput)
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
                var currentSpeedSystem = GetCurrentActionSpeedSystem();
                if (currentSpeedSystem != null)
                {
                    currentSpeedSystem.AdvanceEntityTurn(currentEnemy, enemyActionSpeed);
                }
            }
            else
            {
                // Use dice-based action selection for enemies (no forced action)
                var (result, statusEffects) = CombatResults.ExecuteActionWithUIAndStatusEffects(currentEnemy, player, null, room, GetLastPlayerAction(), currentBattleNarrative);
                bool textDisplayed = !string.IsNullOrEmpty(result);
                
                // Get triggered narratives and display everything together
                if (textDisplayed && currentBattleNarrative != null)
                {
                    var narratives = currentBattleNarrative.GetTriggeredNarratives();
                    // Add proper indentation to status effects
                    var indentedStatusEffects = statusEffects.Select(effect => $"    {effect}").ToList();
                    TextDisplayIntegration.DisplayCombatAction(result, narratives, indentedStatusEffects, currentEnemy.Name);
                }
                
                // Update enemy's action timing in the action speed system
                var actionSpeedSystem = GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    // Get the action that was actually used for timing purposes
                    var usedAction = CombatActions.GetLastUsedAction(currentEnemy);
                    if (usedAction != null)
                    {
                        actionSpeedSystem.ExecuteAction(currentEnemy, usedAction);
                    }
                }
                
                // Display is now handled by TextDisplayIntegration.DisplayCombatAction above
            }
            
            // Return false if player is dead (combat should end)
            if (!player.IsAlive)
            {
                return false;
            }
            
            // Process poison/bleed and burn damage after enemy's turn
            turnManager.ProcessDamageOverTimeEffects(player, currentEnemy);
            
            return true;
        }

        /// <summary>
        /// Processes a single environment turn
        /// </summary>
        private bool ProcessEnvironmentTurn(Character player, Enemy currentEnemy, Environment room)
        {
            if (room.ShouldEnvironmentAct())
            {
                var envAction = room.SelectAction();
                if (envAction != null)
                {
                    // Create list of all characters in the room (player and current enemy)
                    var allTargets = new List<Entity> { player, currentEnemy };
                    
                    // Use area of effect action to target all characters
                    string result = CombatActions.ExecuteAreaOfEffectAction(room, allTargets, room, envAction);
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Display environmental action result
                    if (textDisplayed)
                    {
                        // Use WriteCombatLine for proper entity tracking and spacing
                        UIManager.WriteCombatLine(result);
                        
                        // Don't add explicit blank line - let UIManager handle entity-based spacing
                    }
                    
                    // Update environment's action timing in the action speed system
                    var actionSpeedSystem = GetCurrentActionSpeedSystem();
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
                var actionSpeedSystem = GetCurrentActionSpeedSystem();
                if (actionSpeedSystem != null)
                {
                    // Advance the environment's turn by a small amount to prevent infinite loops
                    actionSpeedSystem.AdvanceEntityTurn(room, 1.0);
                }
            }
            
            return true;
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
                if (actualRegen > 0 && !DisableCombatUIOutput)
                {
                    UIManager.WriteLine($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                }
            }
        }

    }
}
