using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles combat action execution, combo logic, and action selection
    /// </summary>
    public static class CombatActions
    {
        // Store the last action selection roll for consistency
        private static readonly Dictionary<Entity, int> _lastActionSelectionRolls = new Dictionary<Entity, int>();
        private static readonly Dictionary<Entity, Action> _lastUsedActions = new Dictionary<Entity, Action>();
        
        // Flag to disable debug output during balance analysis
        public static bool DisableCombatDebugOutput = false;
        /// <summary>
        /// Executes a single action with all its effects and returns both main result and status effects
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <param name="forcedAction">Forced action for combo system</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A tuple containing the main result string and list of status effect messages</returns>
        public static (string mainResult, List<string> statusEffects) ExecuteActionWithStatusEffects(Entity source, Entity target, Environment? environment = null, Action? lastPlayerAction = null, Action? forcedAction = null, BattleNarrative? battleNarrative = null)
        {
            var actionResults = new List<string>();
            string mainResult = ExecuteActionInternal(source, target, environment, lastPlayerAction, forcedAction, battleNarrative, actionResults);
            
            // Separate main result from status effects
            var statusEffects = new List<string>();
            if (actionResults.Count > 1)
            {
                // First result is the main action result, rest are status effects
                statusEffects = actionResults.Skip(1).ToList();
            }
            
            return (mainResult, statusEffects);
        }

        /// <summary>
        /// Executes a single action with all its effects
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <param name="forcedAction">Forced action for combo system</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteAction(Entity source, Entity target, Environment? environment = null, Action? lastPlayerAction = null, Action? forcedAction = null, BattleNarrative? battleNarrative = null)
        {
            var actionResults = new List<string>();
            return ExecuteActionInternal(source, target, environment, lastPlayerAction, forcedAction, battleNarrative, actionResults);
        }

        /// <summary>
        /// Internal method that executes an action and populates the results list
        /// </summary>
        private static string ExecuteActionInternal(Entity source, Entity target, Environment? environment, Action? lastPlayerAction, Action? forcedAction, BattleNarrative? battleNarrative, List<string> results)
        {
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("CombatActions", $"{source.Name} executing action against {target.Name}");
            }
            
            // Use forced action if provided (for combo system), otherwise select action based on entity type
            var selectedAction = forcedAction ?? SelectActionByEntityType(source);
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }
            
            // Store the action that will be used
            _lastUsedActions[source] = selectedAction;
            
            // Handle unique action chance for characters (not enemies)
            // Only apply unique action chance if no forced action was provided
            if (source is Character character && !(character is Enemy) && forcedAction == null)
            {
                selectedAction = HandleUniqueActionChance(character, selectedAction);
            }
            
            // Calculate roll bonus based on entity type and action
            int rollBonus = CalculateRollBonus(source, selectedAction);
            
            // Use the same roll that was used for action selection (for both heroes and enemies)
            int baseRoll = GetActionRoll(source);
            int attackRoll = baseRoll + rollBonus;
            int cappedRoll = Math.Min(attackRoll, 20);
            
            // Check for critical miss (total roll <= 1)
            bool isCriticalMiss = (baseRoll + rollBonus) <= 1;
            if (isCriticalMiss)
            {
                // Apply critical miss penalty - doubles action speed for next turn
                source.HasCriticalMissPenalty = true;
                source.CriticalMissPenaltyTurns = 1;
            }
            
            // Check for hit
            bool hit = CombatCalculator.CalculateHit(source, target, rollBonus, attackRoll);
            
            if (hit)
            {
                // Apply damage for Attack-type and Spell-type actions
                if (selectedAction.Type == ActionType.Attack || selectedAction.Type == ActionType.Spell)
                {
                    // Calculate damage with entity-specific modifiers
                    double damageMultiplier = CalculateDamageMultiplier(source, selectedAction);
                    int totalRoll = baseRoll + rollBonus;
                    int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
                    
                    // Apply damage
                    ApplyDamage(target, damage);
                    if (!DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("CombatActions", $"{source.Name} dealt {damage} damage to {target.Name} with {selectedAction.Name}");
                    }
                    
                    // Create and add BattleEvent for narrative system
                    bool isCritical = totalRoll >= 20; // Critical hit on natural 20 or higher
                    bool isCombo = selectedAction.Name != "BASIC ATTACK"; // Any non-basic attack counts as combo
                    CreateAndAddBattleEvent(source, target, selectedAction, damage, totalRoll, rollBonus, true, isCombo, 0, 0, isCritical, battleNarrative);
                    
                    // Add damage message
                    results.Add(CombatResults.FormatDamageDisplay(source, target, damage, damage, selectedAction, 1.0, damageMultiplier, rollBonus, baseRoll));
                }
                else if (selectedAction.Type == ActionType.Heal)
                {
                    // Handle healing actions
                    int healAmount = CalculateHealAmount(source, selectedAction);
                    int totalRoll = baseRoll + rollBonus;
                    
                    // Apply healing
                    ApplyHealing(target, healAmount);
                    if (!DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("CombatActions", $"{source.Name} healed {target.Name} for {healAmount} health with {selectedAction.Name}");
                    }
                    
                    // Create and add BattleEvent for narrative system (healing action)
                    bool isCombo = selectedAction.Name != "BASIC ATTACK"; // Any non-basic action counts as combo
                    CreateAndAddBattleEvent(source, target, selectedAction, 0, totalRoll, rollBonus, true, isCombo, 0, healAmount, false, battleNarrative);
                    
                    // Add healing message
                    results.Add($"[{source.Name}] heals [{target.Name}] for {healAmount} health with {selectedAction.Name}");
                }
                else
                {
                    // For non-damage actions, just show the action was successful
                    results.Add(CombatResults.FormatNonAttackAction(source, target, selectedAction, baseRoll, rollBonus));
                    
                    // Create and add BattleEvent for narrative system (non-damage action)
                    bool isCombo = selectedAction.Name != "BASIC ATTACK"; // Any non-basic action counts as combo
                    CreateAndAddBattleEvent(source, target, selectedAction, 0, baseRoll + rollBonus, rollBonus, true, isCombo, 0, 0, false, battleNarrative);
                }
                
                // Apply status effects for all action types
                CombatEffects.ApplyStatusEffects(selectedAction, source, target, results);
                
                // Apply enemy roll penalty if the action has one
                if (selectedAction.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(selectedAction.EnemyRollPenalty, 1); // Apply for 1 turn
                    results.Add($"[{target.Name}] suffers a -{selectedAction.EnemyRollPenalty} roll penalty!");
                }
                
                // Handle combo advancement for characters
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    comboCharacter.ComboStep++;
                }
            }
            else
            {
                results.Add(CombatResults.FormatMissMessage(source, target, selectedAction, baseRoll, rollBonus));
            }
            
            return results.Count > 0 ? results[0] : "";
        }

        /// <summary>
        /// Selects an action based on entity type - heroes use roll-based logic, enemies use random selection
        /// </summary>
        /// <param name="source">The entity selecting the action</param>
        /// <returns>The selected action or null if no action available</returns>
        private static Action? SelectActionByEntityType(Entity source)
        {
            // Heroes/Characters use advanced roll-based system with combos
            if (source is Character character && !(character is Enemy))
            {
                return SelectActionBasedOnRoll(source);
            }
            // Enemies use simple random probability-based selection
            else
            {
                return SelectEnemyActionBasedOnRoll(source);
            }
        }

        /// <summary>
        /// Selects an action based on dice roll logic (6+ = BASIC ATTACK, 14+ = COMBO) - for heroes only
        /// </summary>
        /// <param name="source">The entity selecting the action</param>
        /// <returns>The selected action or null if no action available</returns>
        private static Action? SelectActionBasedOnRoll(Entity source)
        {
            if (source.ActionPool.Count == 0)
                return null;

            // Check if entity is stunned
            if (source.IsStunned)
                return null;

            // Roll first to determine what type of action to use
            int baseRoll = Dice.Roll(1, 20);
            int rollBonus = CalculateRollBonus(source, null); // Calculate base roll bonus
            int totalRoll = baseRoll + rollBonus;
            
            // Store the roll for use in the main execution
            _lastActionSelectionRolls[source] = baseRoll;
            
            // Determine action type based on roll result
            Action? selectedAction = null;
            
            if (baseRoll == 20) // Natural 20 - always combo + critical hit
            {
                // Use combo action for natural 20
                var comboActions = GetComboActions(source);
                if (comboActions.Count > 0)
                {
                    int actionIdx = GetComboStep(source) % comboActions.Count;
                    selectedAction = comboActions[actionIdx];
                }
                else
                {
                    // This should never happen - combo actions should always be available
                    // If we reach here, there's a bug in the combo initialization
                    DebugLogger.Log("CombatActions", $"ERROR: No combo actions available for {source.Name} on natural 20! This should never happen.");
                    
                    // Try to find any combo action from the action pool
                    var anyComboAction = source.ActionPool
                        .Where(a => a.action.IsComboAction)
                        .Select(a => a.action)
                        .FirstOrDefault();
                    
                    if (anyComboAction != null)
                    {
                        selectedAction = anyComboAction;
                        DebugLogger.Log("CombatActions", $"Found combo action {anyComboAction.Name} for {source.Name}");
                    }
                    else
                    {
                        // Last resort: create a combo action on the fly
                        selectedAction = new Action(
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
                        source.AddAction(selectedAction, 1.0);
                        DebugLogger.Log("CombatActions", $"Created emergency combo action for {source.Name}");
                    }
                }
            }
            else if (totalRoll >= 14) // Combo threshold (14-20) - FIXED: Use totalRoll instead of baseRoll
            {
                // Use combo action
                var comboActions = GetComboActions(source);
                if (GameConfiguration.IsDebugEnabled)
                {
                    if (!DisableCombatDebugOutput)
                    {
                    }
                }
                if (comboActions.Count > 0)
                {
                    int actionIdx = GetComboStep(source) % comboActions.Count;
                    selectedAction = comboActions[actionIdx];
                }
                else
                {
                    // Fallback to basic attack if no combo actions available
                    var basicAttackEntry = source.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK");
                    selectedAction = basicAttackEntry.action;
                    
                    // Debug: Log when falling back to basic attack for high rolls
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!DisableCombatDebugOutput)
                        {
                        }
                    }
                }
            }
            else if (totalRoll >= 6) // Basic attack threshold (6-13)
            {
                // Use basic attack
                var basicAttackEntry = source.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK");
                selectedAction = basicAttackEntry.action;
                
                // CRITICAL FALLBACK: If BASIC ATTACK is not found, create it immediately
                if (selectedAction == null)
                {
                    // Try to load BASIC ATTACK directly from JSON
                    var basicLoadedAction = ActionLoader.GetAction("BASIC ATTACK");
                    if (basicLoadedAction != null)
                    {
                        selectedAction = basicLoadedAction;
                        // Add it to the action pool for future use
                        source.AddAction(basicLoadedAction, 1.0);
                        DebugLogger.Log("CombatActions", $"CRITICAL: Added missing BASIC ATTACK to {source.Name}'s action pool during combat");
                    }
                    else
                    {
                        // Final fallback: create BASIC ATTACK
                        selectedAction = new Action(
                            name: "BASIC ATTACK",
                            type: ActionType.Attack,
                            targetType: TargetType.SingleTarget,
                            baseValue: 0, // Damage comes from STR + weapon
                            range: 1,
                            cooldown: 0,
                            description: "A standard physical attack using STR + weapon damage",
                            comboOrder: 0,
                            damageMultiplier: 1.0,
                            length: 1.0,
                            causesBleed: false,
                            causesWeaken: false,
                            isComboAction: false
                        );
                        source.AddAction(selectedAction, 1.0);
                        DebugLogger.Log("CombatActions", $"CRITICAL: Created and added BASIC ATTACK to {source.Name}'s action pool during combat");
                    }
                }
            }
            // If totalRoll < 6, still attempt basic attack (will likely miss) - FIXED: Always attempt an action
            if (totalRoll < 6)
            {
                var basicAttackEntry = source.ActionPool.FirstOrDefault(a => a.action.Name == "BASIC ATTACK");
                if (basicAttackEntry.action != null)
                {
                    selectedAction = basicAttackEntry.action;
                }
                else
                {
                    // Try to load BASIC ATTACK directly
                    var basicLoadedAction = ActionLoader.GetAction("BASIC ATTACK");
                    if (basicLoadedAction != null)
                    {
                        selectedAction = basicLoadedAction;
                        // Add it to the action pool for future use
                        source.AddAction(basicLoadedAction, 1.0);
                        DebugLogger.Log("CombatActions", $"Added missing BASIC ATTACK to {source.Name}'s action pool during combat");
                    }
                    else
                    {
                        // Final fallback: create BASIC ATTACK
                        selectedAction = new Action(
                            name: "BASIC ATTACK",
                            type: ActionType.Attack,
                            targetType: TargetType.SingleTarget,
                            baseValue: 0, // Damage comes from STR + weapon
                            range: 1,
                            cooldown: 0,
                            description: "A standard physical attack using STR + weapon damage",
                            comboOrder: 0,
                            damageMultiplier: 1.0,
                            length: 1.0,
                            causesBleed: false,
                            causesWeaken: false,
                            isComboAction: false
                        );
                        source.AddAction(selectedAction, 1.0);
                        DebugLogger.Log("CombatActions", $"Created and added BASIC ATTACK to {source.Name}'s action pool during combat");
                    }
                }
            }
            
            return selectedAction;
        }

        /// <summary>
        /// Selects an enemy action based on roll thresholds:
        ///  - 6-13: BASIC ATTACK (with robust fallbacks)
        ///  - 14-20: Prefer combo action, else fallback to weighted selection
        ///  - <6: no action (treated as miss/no action)
        /// </summary>
        public static Action? SelectEnemyActionBasedOnRoll(Entity source)
        {
            if (source.ActionPool.Count == 0)
                return null;

            // Enemies can be stunned too
            if (source.IsStunned)
                return null;

            int baseRoll = Dice.Roll(1, 20);
            int rollBonus = 0; // Enemies generally have no base roll bonus for selection
            int totalRoll = baseRoll + rollBonus;
            
            // Store the roll for use in hit calculation (same as heroes)
            _lastActionSelectionRolls[source] = baseRoll;

            // 20 or 14-19: prefer combo actions - FIXED: Use totalRoll consistently
            if (baseRoll == 20 || totalRoll >= 14)
            {
                var comboActions = source.ActionPool.Where(a => a.action.IsComboAction).Select(a => a.action).ToList();
                if (comboActions.Count > 0)
                {
                    int idx = comboActions.Count > 1 ? Dice.Roll(1, comboActions.Count) - 1 : 0;
                    return comboActions[idx];
                }
                // Fallback to weighted selection
                return source.SelectAction();
            }

            // 6-13: BASIC ATTACK
            if (totalRoll >= 6)
            {
                // Try to find BASIC ATTACK in pool (case-insensitive)
                var basicInPool = source.ActionPool
                    .Select(a => a.action)
                    .FirstOrDefault(a => string.Equals(a.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));
                if (basicInPool != null)
                {
                    return basicInPool;
                }

                // Try to load BASIC ATTACK directly
                var basicLoaded = ActionLoader.GetAction("BASIC ATTACK");
                if (basicLoaded != null)
                {
                    // Add it to the action pool for future use
                    source.AddAction(basicLoaded, 1.0);
                    DebugLogger.Log("CombatActions", $"Added missing BASIC ATTACK to {source.Name}'s action pool during combat");
                    return basicLoaded;
                }

                // CRITICAL FALLBACK: Create BASIC ATTACK if not found
                var fallbackBasicAttack = new Action(
                    name: "BASIC ATTACK",
                    type: ActionType.Attack,
                    targetType: TargetType.SingleTarget,
                    baseValue: 0, // Damage comes from STR + weapon
                    range: 1,
                    cooldown: 0,
                    description: "A standard physical attack using STR + weapon damage",
                    comboOrder: 0,
                    damageMultiplier: 1.0,
                    length: 1.0,
                    causesBleed: false,
                    causesWeaken: false,
                    isComboAction: false
                );
                source.AddAction(fallbackBasicAttack, 1.0);
                DebugLogger.Log("CombatActions", $"Created and added BASIC ATTACK to {source.Name}'s action pool during combat");
                return fallbackBasicAttack;
            }

            // <6: treat as basic attack attempt (will likely miss) - FIXED: Enemies should always attempt an action
            var basicInPoolEnemy = source.ActionPool
                .Select(a => a.action)
                .FirstOrDefault(a => string.Equals(a.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));
            if (basicInPoolEnemy != null)
            {
                return basicInPoolEnemy;
            }

            // Try to load BASIC ATTACK directly
            var basicLoadedEnemy = ActionLoader.GetAction("BASIC ATTACK");
            if (basicLoadedEnemy != null)
            {
                // Add it to the action pool for future use
                source.AddAction(basicLoadedEnemy, 1.0);
                DebugLogger.Log("CombatActions", $"Added missing BASIC ATTACK to {source.Name}'s action pool during combat");
                return basicLoadedEnemy;
            }

            // CRITICAL FALLBACK: Create BASIC ATTACK if not found
            var fallbackBasicAttackEnemy = new Action(
                name: "BASIC ATTACK",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0, // Damage comes from STR + weapon
                range: 1,
                cooldown: 0,
                description: "A standard physical attack using STR + weapon damage",
                comboOrder: 0,
                damageMultiplier: 1.0,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                isComboAction: false
            );
            source.AddAction(fallbackBasicAttackEnemy, 1.0);
            DebugLogger.Log("CombatActions", $"Created and added BASIC ATTACK to {source.Name}'s action pool during combat");
            return fallbackBasicAttackEnemy;
        }
        
        /// <summary>
        /// Gets combo actions for an entity
        /// </summary>
        private static List<Action> GetComboActions(Entity source)
        {
            if (source is Character character)
            {
                return character.GetComboActions();
            }
            else
            {
                // For enemies, get combo actions from ActionPool
                return source.ActionPool.Where(a => a.action.IsComboAction).Select(a => a.action).ToList();
            }
        }
        
        /// <summary>
        /// Gets the current combo step for an entity
        /// </summary>
        private static int GetComboStep(Entity source)
        {
            if (source is Character character)
            {
                return character.ComboStep;
            }
            else
            {
                return 0; // Enemies don't have combo steps
            }
        }
        
        /// <summary>
        /// Gets the action roll for an entity - uses stored roll for both heroes and enemies
        /// </summary>
        private static int GetActionRoll(Entity source)
        {
            // Both heroes and enemies use the stored roll from action selection
            if (_lastActionSelectionRolls.TryGetValue(source, out int roll))
            {
                return roll;
            }
            else
            {
                // Fallback to a new roll if not found (shouldn't happen in normal flow)
                return Dice.Roll(1, 20);
            }
        }

        /// <summary>
        /// Gets the last action used by an entity
        /// </summary>
        public static Action? GetLastUsedAction(Entity source)
        {
            _lastUsedActions.TryGetValue(source, out Action? action);
            return action;
        }

        /// <summary>
        /// Handles unique action chance for characters
        /// </summary>
        private static Action HandleUniqueActionChance(Character character, Action selectedAction)
        {
            double uniqueActionChance = character.GetModificationUniqueActionChance();
            if (uniqueActionChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < uniqueActionChance)
                {
                    var availableUniqueActions = character.GetAvailableUniqueActions();
                    if (availableUniqueActions.Count > 0)
                    {
                        int randomIndex = availableUniqueActions.Count > 1 ? Dice.Roll(1, availableUniqueActions.Count) - 1 : 0;
                        selectedAction = availableUniqueActions[randomIndex];
                        // Use TextDisplayIntegration for consistent entity tracking
                        string uniqueActionMessage = $"[{character.Name}] channels unique power and uses [{selectedAction.Name}]!";
                        TextDisplayIntegration.DisplayCombatAction(uniqueActionMessage, new List<string>(), new List<string>(), character.Name);
                    }
                }
            }
            return selectedAction;
        }
        
        /// <summary>
        /// Calculates roll bonus based on entity type and action
        /// </summary>
        public static int CalculateRollBonus(Entity source, Action? action)
        {
            int rollBonus = 0;
            
            if (source is Character character)
            {
                // Character-specific roll bonuses
                rollBonus += character.GetIntelligenceRollBonus();
                rollBonus += character.GetModificationRollBonus();
                rollBonus += character.GetEquipmentRollBonus();
                
                // Action-specific roll bonus
                if (action != null)
                {
                    rollBonus += action.RollBonus;
                    
                    // Combo scaling bonuses
                    if (action.Tags.Contains("comboScaling"))
                    {
                        rollBonus += character.ComboSequence.Count;
                    }
                    else if (action.Tags.Contains("comboStepScaling"))
                    {
                        rollBonus += (character.ComboStep % character.ComboSequence.Count) + 1;
                    }
                    else if (action.Tags.Contains("comboAmplificationScaling"))
                    {
                        var combatBalance = GameConfiguration.Instance.CombatBalance;
                        rollBonus += (int)(character.GetComboAmplifier() * combatBalance.RollDamageMultipliers.ComboAmplificationScalingMultiplier);
                    }
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemy-specific roll bonuses (same as heroes)
                rollBonus += enemy.GetIntelligenceRollBonus();
                
                if (action != null)
                {
                    rollBonus += action.RollBonus;
                }
            }
            
            // Apply roll penalty
            rollBonus -= source.RollPenalty;
            
            return rollBonus;
        }
        
        /// <summary>
        /// Calculates damage multiplier based on entity type and action
        /// </summary>
        private static double CalculateDamageMultiplier(Entity source, Action action)
        {
            if (source is Character character)
            {
                // Only apply combo amplification to combo actions, and only after the first one
                if (action.IsComboAction && character.ComboStep > 0)
                {
                    return character.GetCurrentComboAmplification();
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemies also get combo amplification (same as heroes)
                if (action.IsComboAction && enemy.ComboStep > 0)
                {
                    return enemy.GetCurrentComboAmplification();
                }
            }
            return 1.0;
        }
        
        /// <summary>
        /// Applies damage to target entity
        /// </summary>
        private static void ApplyDamage(Entity target, int damage)
        {
            if (target is Character targetCharacter)
            {
                targetCharacter.TakeDamage(damage);
            }
            else if (target is Enemy targetEnemy)
            {
                targetEnemy.TakeDamage(damage);
            }
        }


        /// <summary>
        /// Executes multiple attacks per turn based on the source's attack speed
        /// </summary>
        /// <param name="source">The entity performing the attacks</param>
        /// <param name="target">The entity receiving the attacks</param>
        /// <param name="environment">The environment affecting the attacks</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the results of all attacks</returns>
        public static string ExecuteMultiAttack(Entity source, Entity target, Environment? environment = null, BattleNarrative? battleNarrative = null)
        {
            if (source is Character character)
            {
                int attacksPerTurn = character.GetAttacksPerTurn();
                var results = new List<string>();
                
                for (int i = 0; i < attacksPerTurn; i++)
                {
                    // Check if target is alive
                    bool isAlive = true;
                    if (target is Character targetCharacter)
                        isAlive = targetCharacter.CurrentHealth > 0;
                    else if (target is Enemy targetEnemy)
                        isAlive = targetEnemy.CurrentHealth > 0;
                    
                    if (!isAlive) break; // Stop if target is dead
                    
                    string result = ExecuteAction(source, target, environment, null, null, battleNarrative);
                    if (!string.IsNullOrEmpty(result))
                    {
                        results.Add(result);
                    }
                }
                
                return string.Join("\n", results);
            }
            else
            {
                // For non-character entities, just execute one action
                return ExecuteAction(source, target, environment, null, null, battleNarrative);
            }
        }

        /// <summary>
        /// Executes an area of effect action
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">The action to perform (optional)</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the results</returns>
        public static string ExecuteAreaOfEffectAction(Entity source, List<Entity> targets, Environment? environment = null, Action? selectedAction = null, BattleNarrative? battleNarrative = null)
        {
            var results = new List<string>();
            
            // Get the action to use
            var action = selectedAction ?? source.SelectAction();
            if (action == null)
            {
                return $"[{source.Name}] has no actions available.";
            }
            
            // For environmental actions, use special duration-based system
            if (source is Environment)
            {
                return ExecuteEnvironmentalAction(source, targets, action, environment);
            }
            
            // Execute the action on each target (for non-environmental actions)
            foreach (var target in targets)
            {
                bool isAlive = false;
                if (target is Character targetCharacter)
                    isAlive = targetCharacter.CurrentHealth > 0;
                else if (target is Enemy targetEnemy)
                    isAlive = targetEnemy.CurrentHealth > 0;
                
                if (isAlive)
                {
                    string result = ExecuteAction(source, target, environment, null, null, battleNarrative);
                    if (!string.IsNullOrEmpty(result))
                    {
                        results.Add(result);
                    }
                }
            }
            
            return string.Join("\n", results);
        }

        /// <summary>
        /// Executes an environmental action with duration-based effects
        /// </summary>
        /// <param name="source">The environment performing the action</param>
        /// <param name="targets">List of target entities</param>
        /// <param name="action">The action to perform</param>
        /// <param name="environment">The environment context</param>
        /// <returns>A string describing the results</returns>
        private static string ExecuteEnvironmentalAction(Entity source, List<Entity> targets, Action action, Environment? environment = null)
        {
            // Get list of alive targets
            var aliveTargets = new List<Entity>();
            foreach (var target in targets)
            {
                bool isAlive = false;
                if (target is Character targetCharacter)
                    isAlive = targetCharacter.CurrentHealth > 0;
                else if (target is Enemy targetEnemy)
                    isAlive = targetEnemy.CurrentHealth > 0;
                
                if (isAlive)
                {
                    aliveTargets.Add(target);
                }
            }
            
            if (aliveTargets.Count == 0)
            {
                return $"[{source.Name}]'s {action.Name} has no effect!";
            }
            
            // Roll separately for each target and track which ones are affected
            var affectedTargets = new List<(Entity target, int duration)>();
            
            foreach (var target in aliveTargets)
            {
                // Roll 2d2-2 to determine duration (0-2 turns) for this specific target
                int duration = Dice.Roll(1, 2) + Dice.Roll(1, 2) - 2;
                
                // If duration is 0, the effect is not applied to this target
                if (duration > 0)
                {
                    ApplyEnvironmentalEffectSilent(source, target, action, duration);
                    affectedTargets.Add((target, duration));
                }
            }
            
            // If no targets were affected, show no effect message
            if (affectedTargets.Count == 0)
            {
                return $"[{source.Name}]'s {action.Name} has no effect!";
            }
            
            // Format the message based on number of affected targets
            if (affectedTargets.Count == 1)
            {
                // Single target affected - use the original format
                var (target, duration) = affectedTargets[0];
                return FormatEnvironmentalEffectMessage(source, target, action, duration);
            }
            else
            {
                // Multiple targets affected - format as area of effect with individual results
                var targetNames = GetUniqueTargetNames(aliveTargets);
                var result = $"[{source.Name}] uses [{action.Name}] on {targetNames}!";
                
                // Add individual effect messages for each affected target
                foreach (var (target, duration) in affectedTargets)
                {
                    string displayName = GetDisplayName(target);
                    string effectMessage = GetEnvironmentalEffectMessage(action, duration);
                    result += $"\n    {displayName} affected by {effectMessage}";
                }
                
                return result;
            }
        }

        /// <summary>
        /// Applies environmental effects to a target without adding messages to results list
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        private static void ApplyEnvironmentalEffectSilent(Entity source, Entity target, Action action, int duration)
        {
            // Apply effects based on action type and properties
            if (action.CausesBleed)
            {
                target.ApplyPoison(2, duration, true); // 2 damage per turn, bleeding type
            }
            else if (action.CausesWeaken)
            {
                target.ApplyWeaken(duration);
            }
            else if (action.CausesSlow)
            {
                // Apply slow effect - for characters, use the character-specific method
                if (target is Character character)
                {
                    character.ApplySlow(0.5, duration); // 50% speed reduction
                }
                // For enemies, we can't easily apply slow without modifying the base class
            }
            else if (action.CausesPoison)
            {
                target.ApplyPoison(2, duration); // 2 damage per turn for the duration
            }
            else if (action.CausesStun)
            {
                target.IsStunned = true;
                target.StunTurnsRemaining = duration;
            }
            else if (action.CausesBurn)
            {
                target.ApplyBurn(3, duration); // 3 damage per turn for the duration
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                
                // Apply damage
                ApplyDamage(target, damage);
            }
        }

        /// <summary>
        /// Formats a single environmental effect message
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Formatted message</returns>
        private static string FormatEnvironmentalEffectMessage(Entity source, Entity target, Action action, int duration)
        {
            string displayName = GetDisplayName(target);
            if (action.CausesBleed)
            {
                return $"[{source.Name}] uses [{action.Name}] on {displayName}!\n    {displayName} is bleeding for {duration} turns!";
            }
            else if (action.CausesWeaken)
            {
                return $"[{source.Name}] uses [{action.Name}] on {displayName}!\n    {displayName} is weakened for {duration} turns!";
            }
            else if (action.CausesSlow)
            {
                return $"[{source.Name}] uses [{action.Name}] on {displayName}!\n    {displayName} is slowed for {duration} turns!";
            }
            else if (action.CausesPoison)
            {
                return $"[{source.Name}] uses [{action.Name}] on {displayName}!\n    {displayName} is poisoned for {duration} turns!";
            }
            else if (action.CausesStun)
            {
                return $"[{source.Name}] uses [{action.Name}] on {displayName}!\n    {displayName} is stunned for {duration} turns!";
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                return CombatResults.FormatDamageDisplay(source, target, damage, damage, action, 1.0, damageMultiplier, 0, 0);
            }
            else
            {
                // Generic environmental effect
                return $"[{source.Name}] uses [{action.Name}] on {displayName}!\n    Effect lasts for {duration} turns!";
            }
        }

        /// <summary>
        /// Gets unique target names for environmental messages, ensuring HERO and ENEMY are shown correctly
        /// </summary>
        /// <param name="targets">List of target entities</param>
        /// <returns>Comma-separated list of unique target names</returns>
        private static string GetUniqueTargetNames(List<Entity> targets)
        {
            var uniqueNames = new HashSet<string>();
            
            foreach (var target in targets)
            {
                string displayName = GetDisplayName(target);
                uniqueNames.Add(displayName);
            }
            
            return string.Join(" and ", uniqueNames);
        }

        /// <summary>
        /// Gets the display name for an entity in environmental messages
        /// </summary>
        /// <param name="entity">The entity to get display name for</param>
        /// <returns>Display name with entity's actual name</returns>
        private static string GetDisplayName(Entity entity)
        {
            return $"[{entity.Name}]";
        }

        /// <summary>
        /// Gets the environmental effect message for area-of-effect actions
        /// </summary>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Effect message</returns>
        private static string GetEnvironmentalEffectMessage(Action action, int duration)
        {
            if (action.CausesBleed)
            {
                return $"BLEED for {duration} turns";
            }
            else if (action.CausesWeaken)
            {
                return $"WEAKEN for {duration} turns";
            }
            else if (action.CausesSlow)
            {
                return $"SLOW for {duration} turns";
            }
            else if (action.CausesPoison)
            {
                return $"POISON for {duration} turns";
            }
            else if (action.CausesStun)
            {
                return $"STUN for {duration} turns";
            }
            else
            {
                return $"EFFECT for {duration} turns";
            }
        }

        /// <summary>
        /// Applies environmental effects to a target
        /// </summary>
        /// <param name="source">The environment source</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <param name="results">List to add result messages to</param>
        private static void ApplyEnvironmentalEffect(Entity source, Entity target, Action action, int duration, List<string> results)
        {
            // Apply effects based on action type and properties
            if (action.CausesBleed)
            {
                target.ApplyPoison(2, duration, true); // 2 damage per turn, bleeding type
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is bleeding for {duration} turns!");
            }
            else if (action.CausesWeaken)
            {
                target.ApplyWeaken(duration);
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is weakened for {duration} turns!");
            }
            else if (action.CausesSlow)
            {
                // Apply slow effect - for characters, use the character-specific method
                if (target is Character character)
                {
                    character.ApplySlow(0.5, duration); // 50% speed reduction
                }
                else
                {
                    // For enemies, we can't easily apply slow without modifying the base class
                    // For now, just show the message
                }
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is slowed for {duration} turns!");
            }
            else if (action.CausesPoison)
            {
                target.ApplyPoison(2, duration); // 2 damage per turn for the duration
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is poisoned for {duration} turns!");
            }
            else if (action.CausesStun)
            {
                target.IsStunned = true;
                target.StunTurnsRemaining = duration;
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    [{target.Name}] is stunned for {duration} turns!");
            }
            else if (action.Type == ActionType.Attack)
            {
                // For environmental attacks, calculate damage normally
                double damageMultiplier = CalculateDamageMultiplier(source, action);
                int damage = CombatCalculator.CalculateDamage(source, target, action, damageMultiplier, 1.0, 0, 0);
                
                // Apply damage
                ApplyDamage(target, damage);
                
                // Add damage message
                results.Add(CombatResults.FormatDamageDisplay(source, target, damage, damage, action, 1.0, damageMultiplier, 0, 0));
            }
            else
            {
                // Generic environmental effect
                results.Add($"[{source.Name}] uses [{action.Name}] on [{target.Name}]!\n    Effect lasts for {duration} turns!");
            }
        }


        /// <summary>
        /// Handles divine reroll functionality
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="baseRoll">The original roll</param>
        /// <param name="totalRollBonus">Total roll bonus</param>
        /// <returns>New roll result and whether reroll was used</returns>
        public static (int newRoll, bool rerollUsed) HandleDivineReroll(Character player, int baseRoll, int totalRollBonus)
        {
            // For now, return the original roll without divine reroll functionality
            // This would need to be implemented based on the actual divine reroll system
            return (baseRoll, false);
        }

        /// <summary>
        /// Creates and adds a BattleEvent to the current battle narrative
        /// </summary>
        /// <param name="source">The acting entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action performed</param>
        /// <param name="damage">Damage dealt (0 for non-damage actions)</param>
        /// <param name="totalRoll">Total roll including bonuses</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <param name="isSuccess">Whether the action was successful</param>
        /// <param name="isCombo">Whether this is part of a combo</param>
        /// <param name="comboStep">Current combo step (0 if not a combo)</param>
        /// <param name="healAmount">Amount healed (0 if not a heal)</param>
        /// <param name="isCritical">Whether this was a critical hit</param>
        /// <param name="battleNarrative">The battle narrative to add the event to (optional)</param>
        private static void CreateAndAddBattleEvent(Entity source, Entity target, Action action, int damage, int totalRoll, int rollBonus, bool isSuccess, bool isCombo, int comboStep, int healAmount, bool isCritical, BattleNarrative? battleNarrative)
        {
            try
            {
                if (battleNarrative == null)
                {
                    return; // No active battle narrative
                }

                // Create the battle event
                var battleEvent = new BattleEvent
                {
                    Actor = source.Name,
                    Target = target.Name,
                    Action = action.Name,
                    Damage = damage,
                    IsSuccess = isSuccess,
                    IsCombo = isCombo,
                    ComboStep = comboStep,
                    IsHeal = healAmount > 0,
                    HealAmount = healAmount,
                    Roll = totalRoll - rollBonus, // Base roll without bonuses
                    Difficulty = 0, // Action doesn't have Difficulty property, use 0
                    IsCritical = isCritical,
                    ActorHealthBefore = GetEntityHealth(source),
                    TargetHealthBefore = GetEntityHealth(target),
                    ActorHealthAfter = GetEntityHealth(source),
                    TargetHealthAfter = GetEntityHealth(target) - damage + healAmount
                };

                // Add the event to the narrative
                battleNarrative.AddEvent(battleEvent);
            }
            catch (Exception ex)
            {
                // Log error but don't break combat
                if (!DisableCombatDebugOutput)
                {
                    DebugLogger.WriteCombatDebug("CombatActions", $"Error creating BattleEvent: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the current health of an entity
        /// </summary>
        /// <param name="entity">The entity to get health from</param>
        /// <returns>Current health value</returns>
        private static int GetEntityHealth(Entity entity)
        {
            return entity switch
            {
                Enemy enemy => enemy.CurrentHealth,
                Character character => character.CurrentHealth,
                _ => 0
            };
        }

        /// <summary>
        /// Calculates the amount of healing for a healing action
        /// </summary>
        /// <param name="source">The entity performing the healing</param>
        /// <param name="action">The healing action</param>
        /// <returns>Amount of healing to apply</returns>
        private static int CalculateHealAmount(Entity source, Action action)
        {
            // Base healing from action properties
            int baseHeal = action.HealAmount;
            
            // Add technique-based healing for characters
            if (source is Character character)
            {
                baseHeal += character.Technique;
            }
            else if (source is Enemy enemy)
            {
                // For enemies, use a simple calculation based on their stats
                baseHeal += enemy.Technique;
            }
            
            return Math.Max(1, baseHeal); // Ensure at least 1 healing
        }

        /// <summary>
        /// Applies healing to a target entity
        /// </summary>
        /// <param name="target">The entity to heal</param>
        /// <param name="amount">Amount of healing to apply</param>
        private static void ApplyHealing(Entity target, int amount)
        {
            if (target is Character character)
            {
                character.Heal(amount);
            }
            else if (target is Enemy enemy)
            {
                // For enemies, we need to add a Heal method or use direct health modification
                enemy.CurrentHealth = Math.Min(enemy.MaxHealth, enemy.CurrentHealth + amount);
            }
        }
    }
}

