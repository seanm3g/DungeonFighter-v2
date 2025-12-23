using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Utils;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Handles the core action execution flow
    /// Manages action selection, roll calculation, hit/miss determination, and damage/healing application
    /// </summary>
    internal static class ActionExecutionFlow
    {
        /// <summary>
        /// Executes the core action execution sequence
        /// </summary>
        public static ActionExecutionResult Execute(
            Actor source,
            Actor target,
            Environment? environment,
            Action? lastPlayerAction,
            Action? forcedAction,
            BattleNarrative? battleNarrative,
            System.Collections.Generic.Dictionary<Actor, Action> lastUsedActions,
            System.Collections.Generic.Dictionary<Actor, bool> lastCriticalMissStatus)
        {
            var result = new ActionExecutionResult();

            if (!ActionExecutor.DisableCombatDebugOutput)
            {
            }

            // Use forced action if provided (for combo system), otherwise select action based on Actor type
            result.SelectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
            if (result.SelectedAction == null)
            {
                return result;
            }

            // Store the action that will be used
            lastUsedActions[source] = result.SelectedAction;

            // Use the same roll that was used for action selection
            result.BaseRoll = ActionSelector.GetActionRoll(source);

            // Handle unique action chance for characters (not enemies)
            // BASIC ATTACK removed - all actions are now combo actions
            if (source is Character character && !(character is Enemy) && forcedAction == null)
            {
                result.SelectedAction = ActionUtilities.HandleUniqueActionChance(character, result.SelectedAction);
            }

            // Apply roll modifications from action
            result.ModifiedBaseRoll = RollModificationManager.ApplyActionRollModifications(
                result.BaseRoll, result.SelectedAction, source, target);

            // Apply threshold overrides and adjustments
            RollModificationManager.ApplyThresholdOverrides(result.SelectedAction, source, target);

            result.RollBonus = ActionUtilities.CalculateRollBonus(source, result.SelectedAction);
            result.AttackRoll = result.ModifiedBaseRoll + result.RollBonus;

            // Check for critical miss using threshold manager
            int criticalMissThreshold = RollModificationManager.GetThresholdManager().GetCriticalMissThreshold(source);
            result.IsCriticalMiss = result.BaseRoll <= criticalMissThreshold;
            if (result.IsCriticalMiss)
            {
                source.HasCriticalMissPenalty = true;
                source.CriticalMissPenaltyTurns = 1;
            }

            // Store critical miss status for this action
            lastCriticalMissStatus[source] = result.IsCriticalMiss;

            // Determine if this is a combo or critical
            // IsCombo should be based on whether the roll was >= combo threshold (14), not the action name
            result.IsCombo = result.AttackRoll >= RollModificationManager.GetThresholdManager().GetComboThreshold(source);
            result.IsCritical = result.AttackRoll >= RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);

            // Publish action executed event for conditional triggers
            var actionEvent = ActionEventPublisher.PublishActionExecuted(
                source, target, result.SelectedAction, result.AttackRoll, result.IsCombo, result.IsCritical);

            // Check for hit
            result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);

            if (result.Hit)
            {
                // Publish hit event
                var hitEvent = ActionEventPublisher.PublishActionHit(
                    source, target, result.SelectedAction, result.AttackRoll, result.IsCombo, result.IsCritical);

                // Apply damage for Attack-type and Spell-type actions
                if (result.SelectedAction.Type == ActionType.Attack || result.SelectedAction.Type == ActionType.Spell)
                {
                    double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, result.SelectedAction);
                    int totalRoll = result.ModifiedBaseRoll + result.RollBonus;

                    // Check for multi-hit attacks
                    int multiHitCount = result.SelectedAction.Advanced.MultiHitCount;

                    // If multi-hit, process multiple hits; otherwise single hit
                    if (multiHitCount > 1)
                    {
                        result.Damage = MultiHitProcessor.ProcessMultiHit(
                            source, target, result.SelectedAction, damageMultiplier, totalRoll,
                            result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll, battleNarrative);
                    }
                    else
                    {
                        // Single hit (original behavior)
                        result.Damage = CombatCalculator.CalculateDamage(source, target, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, totalRoll);
                        
                        // Handle SelfAndTarget - apply damage to both self and enemy
                        if (result.SelectedAction.Target == TargetType.SelfAndTarget)
                        {
                            // Apply damage to the enemy target
                            ActionUtilities.ApplyDamage(target, result.Damage);
                            
                            // Apply damage to self (source)
                            ActionUtilities.ApplyDamage(source, result.Damage);
                            
                            if (!ActionExecutor.DisableCombatDebugOutput)
                            {
                                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {result.Damage} damage to both {target.Name} and themselves with {result.SelectedAction.Name}");
                            }
                        }
                        else
                        {
                            // Normal single target behavior
                            ActionUtilities.ApplyDamage(target, result.Damage);
                        }

                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }

                        // Track statistics
                        if (source is Character sourceCharacter)
                        {
                            ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, result.BaseRoll, result.RollBonus, result.Damage, result.SelectedAction, target as Enemy);
                        }
                        if (target is Character targetCharacter)
                        {
                            ActionStatisticsTracker.RecordDamageReceived(targetCharacter, result.Damage);
                        }
                        
                        // Track self damage if SelfAndTarget
                        if (result.SelectedAction.Target == TargetType.SelfAndTarget && source is Character selfCharacter)
                        {
                            ActionStatisticsTracker.RecordDamageReceived(selfCharacter, result.Damage);
                        }

                        bool isCriticalHit = totalRoll >= 20;
                        ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, isCriticalHit, result.BaseRoll, battleNarrative);
                    }
                }
                else if (result.SelectedAction.Type == ActionType.Heal)
                {
                    result.HealAmount = ActionUtilities.CalculateHealAmount(source, result.SelectedAction);
                    ActionUtilities.ApplyHealing(target, result.HealAmount);

                    if (!ActionExecutor.DisableCombatDebugOutput)
                    {
                    }

                    if (target is Character targetCharacterHeal)
                    {
                        ActionStatisticsTracker.RecordHealingReceived(targetCharacterHeal, result.HealAmount);
                    }

                    int totalRoll = result.BaseRoll + result.RollBonus;
                    ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, totalRoll, result.RollBonus, true, result.IsCombo, 0, result.HealAmount, false, result.BaseRoll, battleNarrative);
                }
                else
                {
                    // For non-damage actions
                    ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, 0, false, result.BaseRoll, battleNarrative);
                }

                // Apply status effects (with conditional support)
                CombatEffectsSimplified.ApplyStatusEffects(result.SelectedAction, source, target, result.StatusEffectMessages, hitEvent);

                // Apply enemy roll penalty if the action has one
                if (result.SelectedAction.Advanced.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(result.SelectedAction.Advanced.EnemyRollPenalty, 1);
                    result.StatusEffectMessages.Add($"    {target.Name} suffers a -{result.SelectedAction.Advanced.EnemyRollPenalty} roll penalty!");
                }

                // Apply stat bonus to source if the action has one (only for characters, not enemies)
                if (result.SelectedAction.Advanced.StatBonus > 0 && 
                    !string.IsNullOrEmpty(result.SelectedAction.Advanced.StatBonusType) &&
                    source is Character statBonusCharacter && !(statBonusCharacter is Enemy))
                {
                    // Get current bonus and add to it (stacking)
                    int currentBonus = 0;
                    string statType = result.SelectedAction.Advanced.StatBonusType.ToUpper();
                    switch (statType)
                    {
                        case "STR":
                            currentBonus = statBonusCharacter.Stats.TempStrengthBonus;
                            break;
                        case "AGI":
                            currentBonus = statBonusCharacter.Stats.TempAgilityBonus;
                            break;
                        case "TEC":
                            currentBonus = statBonusCharacter.Stats.TempTechniqueBonus;
                            break;
                        case "INT":
                            currentBonus = statBonusCharacter.Stats.TempIntelligenceBonus;
                            break;
                    }
                    
                    // Add the new bonus to the existing one
                    int newBonus = currentBonus + result.SelectedAction.Advanced.StatBonus;
                    int duration = result.SelectedAction.Advanced.StatBonusDuration > 0 
                        ? result.SelectedAction.Advanced.StatBonusDuration 
                        : 999; // Default to dungeon duration if not specified
                    
                    statBonusCharacter.ApplyStatBonus(newBonus, statType, duration);
                    
                    // Stat bonus message is now handled by ActionStatusEffectApplier.ApplyStatBonusColored
                }

                // Check for enemy death and HP thresholds
                if (target is Enemy enemyTarget)
                {
                    // Check for death
                    if (enemyTarget.CurrentHealth <= 0)
                    {
                        ActionEventPublisher.PublishEnemyDeath(source, target, result.SelectedAction, result.Damage);
                    }
                    else
                    {
                        // Check for HP thresholds (50%, 25%, 10%)
                        double healthPercentage = (double)enemyTarget.CurrentHealth / enemyTarget.MaxHealth;
                        if (healthPercentage <= 0.5 && healthPercentage > 0.25)
                        {
                            ActionEventPublisher.PublishHealthThreshold(source, target, result.SelectedAction, 0.5);
                        }
                        else if (healthPercentage <= 0.25 && healthPercentage > 0.1)
                        {
                            ActionEventPublisher.PublishHealthThreshold(source, target, result.SelectedAction, 0.25);
                        }
                        else if (healthPercentage <= 0.1)
                        {
                            ActionEventPublisher.PublishHealthThreshold(source, target, result.SelectedAction, 0.1);
                        }
                    }
                }

                // Handle combo advancement for characters based on roll value
                // Start at step 1 (initial state, no bonus)
                // Step 1: Roll 14+ → go to step 2 (bonus applies)
                // Step 1: Roll < 14 → stay at step 1 (no bonus, still in combo mode)
                // Step 2+: Roll 14+ → continue to next step (bonus continues)
                // Step 2+: Roll < 14 → reset to step 1 (bonus resets, stay in combo mode)
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    int comboThreshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min; // 14
                    
                    if (comboCharacter.ComboStep == 1)
                    {
                        // At step 1, need 14+ to advance to step 2 (where bonus starts)
                        if (result.AttackRoll >= comboThreshold)
                        {
                            // Advance to step 2 with routing support
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                        }
                        // If < 14, stay at step 1 (no bonus, but still in combo mode)
                    }
                    else if (comboCharacter.ComboStep >= 2)
                    {
                        // At step 2 or higher, need 14+ to continue combo
                        if (result.AttackRoll >= comboThreshold)
                        {
                            // Combo continues - increment with routing support
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                        }
                        else
                        {
                            // Didn't get 14+, reset to step 1 (bonus resets, but stay in combo mode)
                            comboCharacter.ComboStep = 1;
                        }
                    }
                    else if (comboCharacter.ComboStep == 0)
                    {
                        // If somehow at step 0, initialize to step 1
                        comboCharacter.ComboStep = 1;
                    }
                }
            }
            else
            {
                // Publish miss event
                ActionEventPublisher.PublishActionMiss(source, target, result.SelectedAction, result.AttackRoll);

                // Track miss statistics for player
                if (source is Character characterMiss)
                {
                    ActionStatisticsTracker.RecordMissAction(characterMiss, result.BaseRoll, result.RollBonus);
                }
                
                // Reset combo step to step 1 on miss (for characters, not enemies)
                // Step 1 is the initial combo state (no bonus, but ready for combo)
                if (source is Character comboCharacterMiss && !(comboCharacterMiss is Enemy))
                {
                    comboCharacterMiss.ComboStep = 1;
                }

                ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
            }

            return result;
        }
    }
}

