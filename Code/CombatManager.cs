using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages combat loop logic including turn-based combat, effect processing, and combat state
    /// </summary>
    public class CombatManager
    {
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
            
            // End the battle narrative
            EndBattleNarrative();
            
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
                UIManager.WriteLine($"[{player.Name}] is stunned and cannot act! ({player.StunTurnsRemaining} turns remaining)");
                
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
                
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"DEBUG [CombatManager]: {player.Name} rolled {baseRoll} + {rollBonus} = {totalRoll}");
                }
                
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
                        // Fallback to basic attack if no combo actions available
                        attemptedAction = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                    }
                }
                else if (totalRoll >= 14) // Combo threshold
                {
                    // Use combo action
                    var comboActions = player.GetComboActions();
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        UIManager.WriteSystemLine($"DEBUG [CombatManager]: {player.Name} has {comboActions.Count} combo actions: {string.Join(", ", comboActions.Select(a => a.Name))}");
                    }
                    if (comboActions.Count > 0)
                    {
                        int actionIdx = player.ComboStep % comboActions.Count;
                        attemptedAction = comboActions[actionIdx];
                        if (GameConfiguration.IsDebugEnabled)
                        {
                            UIManager.WriteSystemLine($"DEBUG [CombatManager]: Selected combo action: {attemptedAction.Name} (index {actionIdx})");
                        }
                    }
                    else
                    {
                        // Fallback to basic attack if no combo actions available
                        attemptedAction = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                        if (GameConfiguration.IsDebugEnabled)
                        {
                            UIManager.WriteSystemLine($"DEBUG [CombatManager]: No combo actions available for {player.Name} (roll {totalRoll}), falling back to BASIC ATTACK");
                        }
                    }
                }
                else if (totalRoll >= 6) // Basic attack threshold
                {
                    // Use basic attack
                    attemptedAction = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                }
                // Handle the different cases
                if (totalRoll < 6)
                {
                    // Miss - no action executed
                    // Use basic attack for miss message formatting
                    var basicAttack = player.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK").action;
                    if (basicAttack != null)
                    {
                        string missMessage = CombatResults.FormatMissMessage(player, currentEnemy, basicAttack, baseRoll, rollBonus);
                        UIManager.WriteCombatLine(missMessage);
                        
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
                        UIManager.WriteCombatLine($"[{player.Name}] misses! (roll: {baseRoll} + {rollBonus} = {totalRoll})");
                    }
                }
                else if (attemptedAction != null)
                {
                    // Execute single action (not multi-attack) with speed tracking
                    string result = CombatResults.ExecuteActionWithUI(player, currentEnemy, attemptedAction, room, GetLastPlayerAction());
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Update last player action for DEJA VU functionality
                    UpdateLastPlayerAction(attemptedAction);
                    
                    // Update player's action timing in the action speed system
                    var actionSpeedSystem = GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null)
                    {
                        actionSpeedSystem.ExecuteAction(player, attemptedAction);
                    }
                    
                    // Show individual action messages with consistent delay
                    if (textDisplayed)
                    {
                        // Split multi-line results and display as a group (no delays between lines)
                        string[] lines = result.Split('\n');
                        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                        if (nonEmptyLines.Length > 0)
                        {
                            UIManager.WriteGroup(nonEmptyLines);
                        }
                    }
                }
                else
                {
                    // Player has no actions available - advance their turn to prevent infinite loop
                    UIManager.WriteLine($"[{player.Name}] has no actions available and cannot act!");
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
                UIManager.WriteLine($"[{currentEnemy.Name}] is stunned and cannot act! ({currentEnemy.StunTurnsRemaining} turns remaining)");
                
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
                var enemyAction = currentEnemy.SelectAction();
                if (enemyAction != null)
                {
                    string result = CombatResults.ExecuteActionWithUI(currentEnemy, player, enemyAction, room, GetLastPlayerAction());
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Update enemy's action timing in the action speed system
                    var actionSpeedSystem = GetCurrentActionSpeedSystem();
                    if (actionSpeedSystem != null)
                    {
                        actionSpeedSystem.ExecuteAction(currentEnemy, enemyAction);
                    }
                    
                    // Show individual action messages with consistent delay
                    if (textDisplayed)
                    {
                        // Split multi-line results and display as a group (no delays between lines)
                        string[] lines = result.Split('\n');
                        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                        if (nonEmptyLines.Length > 0)
                        {
                            UIManager.WriteGroup(nonEmptyLines);
                        }
                    }
                }
                else
                {
                    // Enemy has no actions available - advance their turn to prevent infinite loop
                    UIManager.WriteLine($"[{currentEnemy.Name}] has no actions available and cannot act!");
                    var currentSpeedSystem = GetCurrentActionSpeedSystem();
                    if (currentSpeedSystem != null)
                    {
                        double enemyActionSpeed = currentEnemy.GetTotalAttackSpeed();
                        currentSpeedSystem.AdvanceEntityTurn(currentEnemy, enemyActionSpeed);
                    }
                }
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
                    
                    // Show individual action messages
                    if (textDisplayed)
                    {
                        // Split multi-line results and display as a group (no delays between lines)
                        string[] lines = result.Split('\n');
                        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                        if (nonEmptyLines.Length > 0)
                        {
                            UIManager.WriteGroup(nonEmptyLines);
                        }
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
                if (actualRegen > 0)
                {
                    UIManager.WriteLine($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                }
            }
        }

    }
}
