namespace RPGGame
{
    /// <summary>
    /// Manages combat loop logic including turn-based combat, effect processing, and combat state
    /// </summary>
    public class CombatManager
    {
        // Combat state management (moved from Combat.cs)
        private BattleNarrative? currentBattleNarrative;
        private Action? lastPlayerAction = null; // Track the last action for DEJA VU
        private ActionSpeedSystem? currentActionSpeedSystem = null;
        private BattleHealthTracker? currentHealthTracker = null;

        /// <summary>
        /// Starts battle narrative and initializes combat state
        /// </summary>
        public void StartBattleNarrative(string playerName, string enemyName, string locationName, int playerHealth, int enemyHealth)
        {
            currentBattleNarrative = new BattleNarrative(playerName, enemyName, locationName, playerHealth, enemyHealth);
            lastPlayerAction = null; // Reset last action for new battle
            CombatLogger.ResetForNewBattle(); // Reset entity tracking for new battle
            currentActionSpeedSystem = new ActionSpeedSystem();
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
            currentActionSpeedSystem = null;
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
            return currentActionSpeedSystem;
        }

        /// <summary>
        /// Initializes combat entities in the action speed system
        /// </summary>
        public void InitializeCombatEntities(Character player, Enemy enemy, Environment? environment = null)
        {
            if (currentActionSpeedSystem == null) return;

            // New system: Use the attack speed directly as base speed
            double playerAttackSpeed = player.GetTotalAttackSpeed();
            double enemyAttackSpeed = enemy.GetTotalAttackSpeed();
            
            // For the new system, we use the attack speed directly as the base speed
            // This will be multiplied by action length in ExecuteAction
            currentActionSpeedSystem.AddEntity(player, playerAttackSpeed);
            currentActionSpeedSystem.AddEntity(enemy, enemyAttackSpeed);

            // Add environment to action speed system with a slow base speed (longer cooldowns)
            if (environment != null && environment.IsHostile)
            {
                double environmentBaseSpeed = 15.0; // Very slow - environment acts infrequently
                currentActionSpeedSystem.AddEntity(environment, environmentBaseSpeed);
            }
            
            // Initialize health tracker for battle participants
            var participants = new List<Entity> { player, enemy };
            if (environment != null && environment.IsHostile)
            {
                participants.Add(environment);
            }
            currentHealthTracker = new BattleHealthTracker();
            currentHealthTracker.InitializeBattle(participants);
        }

        /// <summary>
        /// Gets the next entity that should act based on action speed
        /// </summary>
        public Entity? GetNextEntityToAct()
        {
            return currentActionSpeedSystem?.GetNextEntityToAct();
        }

        /// <summary>
        /// Updates the last player action for DEJA VU functionality
        /// </summary>
        public void UpdateLastPlayerAction(Action action)
        {
            lastPlayerAction = action;
        }

        /// <summary>
        /// Gets the last player action for DEJA VU functionality
        /// </summary>
        public Action? GetLastPlayerAction()
        {
            return lastPlayerAction;
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
                CombatLogger.Log($"[{player.Name}] is stunned and cannot act! ({player.StunTurnsRemaining} turns remaining)");
                // Update temp effects (including reducing stun and weaken turns) even when stunned
                player.UpdateTempEffects(1.0); // 1.0 represents one turn
                // Advance the player's turn in the action speed system based on their action speed
                var currentSpeedSystem = GetCurrentActionSpeedSystem();
                if (currentSpeedSystem != null)
                {
                    // Use the player's actual action speed for turn duration
                    double playerActionSpeed = player.GetTotalAttackSpeed();
                    currentSpeedSystem.AdvanceEntityTurn(player, playerActionSpeed);
                }
            }
            else
            {
                // Always recalculate comboActions and actionIdx after any combo reset
                var comboActions = player.GetComboActions();
                int actionIdx = 0; // Always start at 0 after a reset
                if (comboActions.Count > 0)
                    actionIdx = player.ComboStep % comboActions.Count;
                // The action that will be attempted this turn
                var attemptedAction = comboActions.Count > 0 ? comboActions[actionIdx] : null;

                // Execute single action (not multi-attack) with speed tracking
                if (attemptedAction != null)
                {
                    string result = CombatResults.ExecuteActionWithUI(player, currentEnemy, attemptedAction, room, GetLastPlayerAction());
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Update last player action for DEJA VU functionality
                    UpdateLastPlayerAction(attemptedAction);
                    
                    // Show individual action messages with consistent delay
                    if (textDisplayed)
                    {
                        CombatLogger.Log(result);
                    }
                }
                else
                {
                    // Player has no actions available - advance their turn to prevent infinite loop
                    CombatLogger.Log($"[{player.Name}] has no actions available and cannot act!");
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
                CombatLogger.Log($"[{currentEnemy.Name}] is stunned and cannot act! ({currentEnemy.StunTurnsRemaining} turns remaining)");
                // Update temp effects (including reducing stun and weaken turns) even when stunned
                currentEnemy.UpdateTempEffects(1.0); // 1.0 represents one turn
                // Advance the enemy's turn in the action speed system based on their action speed
                var currentSpeedSystem = GetCurrentActionSpeedSystem();
                if (currentSpeedSystem != null)
                {
                    // Use the enemy's actual action speed for turn duration
                    double enemyActionSpeed = currentEnemy.GetTotalAttackSpeed();
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
                    
                    // Show individual action messages with consistent delay
                    if (textDisplayed)
                    {
                        CombatLogger.Log(result);
                    }
                }
            }
            
            // Return false if player is dead (combat should end)
            if (!player.IsAlive)
            {
                return false;
            }
            
            // Process poison/bleed and burn damage after enemy's turn
            ProcessDamageOverTimeEffects(player, currentEnemy);
            
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
                    string result = Combat.ExecuteAreaOfEffectAction(room, allTargets, room, envAction);
                    bool textDisplayed = !string.IsNullOrEmpty(result);
                    
                    // Show individual action messages
                    if (textDisplayed)
                    {
                        CombatLogger.Log(result);
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
                    CombatLogger.Log($"[{player.Name}] regenerates {actualRegen} health ({player.CurrentHealth}/{player.GetEffectiveMaxHealth()})");
                }
            }
        }

        /// <summary>
        /// Processes damage over time effects (poison, bleed, burn) for both player and enemy
        /// </summary>
        private void ProcessDamageOverTimeEffects(Character player, Enemy currentEnemy)
        {
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            
            // Process poison for player
            int playerPoisonDamage = player.ProcessPoison(currentTime);
            if (playerPoisonDamage > 0)
            {
                string damageType = player.GetDamageTypeText();
                player.TakeDamage(playerPoisonDamage); // Apply the actual damage
                CombatLogger.Log($"[{player.Name}] takes {playerPoisonDamage} {damageType} damage");
                if (player.PoisonStacks > 0)
                {
                    CombatLogger.Log($"        ({damageType}: {player.PoisonStacks} stacks remain)");
                }
                else
                {
                    string effectEndMessage = damageType == "bleed" ? "bleeding" : "poisoned";
                    CombatLogger.Log($"        ([{player.Name}] is no longer {effectEndMessage}!)");
                }
            }
            
            // Process poison for enemy (only if living)
            if (currentEnemy.IsLiving)
            {
                int enemyPoisonDamage = currentEnemy.ProcessPoison(currentTime);
                if (enemyPoisonDamage > 0)
                {
                    string damageType = currentEnemy.GetDamageTypeText();
                    currentEnemy.TakeDamage(enemyPoisonDamage); // Apply the actual damage
                    CombatLogger.Log($"[{currentEnemy.Name}] takes {enemyPoisonDamage} {damageType} damage");
                    if (currentEnemy.PoisonStacks > 0)
                    {
                        CombatLogger.Log($"        ({damageType}: {currentEnemy.PoisonStacks} stacks remain)");
                    }
                    else
                    {
                        string effectEndMessage = damageType == "bleed" ? "bleeding" : "poisoned";
                        CombatLogger.Log($"        ([{currentEnemy.Name}] is no longer {effectEndMessage}!)");
                    }
                }
            }
            
            // Process burn damage for player
            int playerBurnDamage = player.ProcessBurn(currentTime);
            if (playerBurnDamage > 0)
            {
                player.TakeDamage(playerBurnDamage); // Apply the actual damage
                CombatLogger.Log($"[{player.Name}] takes {playerBurnDamage} burn damage");
                if (player.BurnStacks > 0)
                {
                    CombatLogger.Log($"        (burn: {player.BurnStacks} stacks remain)");
                }
                else
                {
                    CombatLogger.Log($"        ([{player.Name}] is no longer burning!)");
                }
            }
            
            // Process burn damage for enemy (only if living)
            if (currentEnemy.IsLiving)
            {
                int enemyBurnDamage = currentEnemy.ProcessBurn(currentTime);
                if (enemyBurnDamage > 0)
                {
                    currentEnemy.TakeDamage(enemyBurnDamage); // Apply the actual damage
                    CombatLogger.Log($"[{currentEnemy.Name}] takes {enemyBurnDamage} burn damage");
                    if (currentEnemy.BurnStacks > 0)
                    {
                        CombatLogger.Log($"        (burn: {currentEnemy.BurnStacks} stacks remain)");
                    }
                    else
                    {
                        CombatLogger.Log($"        ([{currentEnemy.Name}] is no longer burning!)");
                    }
                }
            }
        }
    }
}
