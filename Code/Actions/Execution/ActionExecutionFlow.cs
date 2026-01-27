using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Utils;
using RPGGame.Data;
using System.Collections.Generic;

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

            // Apply next attack stat bonus if one was set (for Follow Through and similar effects)
            // This must happen BEFORE action selection so it affects speed calculation
            if (source is Character nextAttackStatCharacter && !(nextAttackStatCharacter is Enemy))
            {
                var (nextBonus, nextStatType, nextDuration) = nextAttackStatCharacter.Effects.ConsumeNextAttackStatBonus();
                if (nextBonus != 0 && !string.IsNullOrEmpty(nextStatType))
                {
                    // Get current bonus and add to it (stacking)
                    int currentBonus = 0;
                    string statType = nextStatType.ToUpper();
                    switch (statType)
                    {
                        case "STR":
                            currentBonus = nextAttackStatCharacter.Stats.TempStrengthBonus;
                            break;
                        case "AGI":
                            currentBonus = nextAttackStatCharacter.Stats.TempAgilityBonus;
                            break;
                        case "TEC":
                            currentBonus = nextAttackStatCharacter.Stats.TempTechniqueBonus;
                            break;
                        case "INT":
                            currentBonus = nextAttackStatCharacter.Stats.TempIntelligenceBonus;
                            break;
                    }
                    
                    // Add the new bonus to the existing one
                    int newBonus = currentBonus + nextBonus;
                    int duration = nextDuration > 0 ? nextDuration : 999;
                    
                    nextAttackStatCharacter.ApplyStatBonus(newBonus, statType, duration);
                }
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

            // Apply ATTACK bonuses before roll (consumed on roll attempt, regardless of hit/miss)
            int attackBonusAccumulator = 0;
            int attackBonusHit = 0;
            int attackBonusCombo = 0;
            int attackBonusCrit = 0;
            
            if (source is Character attackBonusCharacter && !(attackBonusCharacter is Enemy))
            {
                var attackBonuses = attackBonusCharacter.Effects.GetAndConsumeAttackBonuses();
                foreach (var bonus in attackBonuses)
                {
                    switch (bonus.Type.ToUpper())
                    {
                        case "ACCURACY":
                            attackBonusAccumulator += (int)bonus.Value;
                            break;
                        case "HIT":
                            attackBonusHit += (int)bonus.Value;
                            break;
                        case "COMBO":
                            attackBonusCombo += (int)bonus.Value;
                            break;
                        case "CRIT":
                            attackBonusCrit += (int)bonus.Value;
                            break;
                    }
                }
            }
            
            // Apply roll modifications from action
            result.ModifiedBaseRoll = RollModificationManager.ApplyActionRollModifications(
                result.BaseRoll, result.SelectedAction, source, target);
            
            // Add ATTACK ACCURACY bonus to the roll
            result.ModifiedBaseRoll += attackBonusAccumulator;

            // Apply threshold overrides and adjustments
            RollModificationManager.ApplyThresholdOverrides(result.SelectedAction, source, target);
            
            // Apply ATTACK HIT, COMBO, CRIT bonuses via threshold adjustments
            if (attackBonusHit != 0 && source is Character hitBonusCharacter && !(hitBonusCharacter is Enemy))
            {
                RollModificationManager.GetThresholdManager().AdjustHitThreshold(hitBonusCharacter, -attackBonusHit);
            }
            if (attackBonusCombo != 0 && source is Character comboBonusCharacter && !(comboBonusCharacter is Enemy))
            {
                RollModificationManager.GetThresholdManager().AdjustComboThreshold(comboBonusCharacter, -attackBonusCombo);
            }
            if (attackBonusCrit != 0 && source is Character critBonusCharacter && !(critBonusCharacter is Enemy))
            {
                RollModificationManager.GetThresholdManager().AdjustCriticalHitThreshold(critBonusCharacter, -attackBonusCrit);
            }

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
            // Natural 1 always misses, regardless of bonuses
            if (result.BaseRoll == 1)
            {
                result.Hit = false;
            }
            else
            {
                result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);
            }

            if (result.Hit)
            {
                // Apply ACTION bonuses (only consumed on successful action)
                var actionBonuses = new List<ActionAttackBonusItem>();
                if (source is Character actionBonusCharacter && !(actionBonusCharacter is Enemy))
                {
                    actionBonuses = actionBonusCharacter.Effects.GetAndConsumeActionBonuses(true);
                    
                    // Apply stat bonuses from ACTION bonuses
                    foreach (var bonus in actionBonuses)
                    {
                        string bonusType = bonus.Type.ToUpper();
                        if (bonusType == "STR" || bonusType == "AGI" || bonusType == "TECH" || bonusType == "INT")
                        {
                            int currentBonus = 0;
                            switch (bonusType)
                            {
                                case "STR":
                                    currentBonus = actionBonusCharacter.Stats.TempStrengthBonus;
                                    break;
                                case "AGI":
                                    currentBonus = actionBonusCharacter.Stats.TempAgilityBonus;
                                    break;
                                case "TECH":
                                    currentBonus = actionBonusCharacter.Stats.TempTechniqueBonus;
                                    break;
                                case "INT":
                                    currentBonus = actionBonusCharacter.Stats.TempIntelligenceBonus;
                                    break;
                            }
                            
                            int newBonus = currentBonus + (int)bonus.Value;
                            int duration = 999; // Default to dungeon duration for ACTION bonuses
                            actionBonusCharacter.ApplyStatBonus(newBonus, bonusType, duration);
                        }
                    }
                }
                
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

                        // Use the already-calculated IsCritical from threshold manager instead of hardcoded check
                        ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, result.IsCritical, result.BaseRoll, battleNarrative);
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

                // Apply ACTION/ATTACK bonuses from the action that was just executed
                if (result.SelectedAction.ActionAttackBonuses != null && 
                    source is Character bonusSourceCharacter && !(bonusSourceCharacter is Enemy))
                {
                    bonusSourceCharacter.Effects.AddActionAttackBonuses(result.SelectedAction.ActionAttackBonuses);
                }
                
                // Apply status effects (with conditional support)
                CombatEffectsSimplified.ApplyStatusEffects(result.SelectedAction, source, target, result.StatusEffectMessages, hitEvent);

                // Apply enemy roll penalty if the action has one
                if (result.SelectedAction.Advanced.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(result.SelectedAction.Advanced.EnemyRollPenalty, 1);
                    result.StatusEffectMessages.Add($"    {target.Name} suffers a -{result.SelectedAction.Advanced.EnemyRollPenalty} roll penalty!");
                }

                // Apply roll bonus with duration to source if the action has one (only for characters, not enemies)
                if (result.SelectedAction.Advanced.RollBonusDuration > 0 &&
                    source is Character rollBonusCharacter && !(rollBonusCharacter is Enemy))
                {
                    // Set temporary roll bonus that will apply to the next N rolls
                    rollBonusCharacter.Effects.SetTempRollBonus(
                        result.SelectedAction.Advanced.RollBonus,
                        result.SelectedAction.Advanced.RollBonusDuration);
                }

                // Handle Follow Through: set next attack bonuses instead of applying to current attack
                if (result.SelectedAction.Name == "FOLLOW THROUGH" && 
                    source is Character followThroughCharacter && !(followThroughCharacter is Enemy))
                {
                    // Set next attack damage multiplier to 3.0 (triple damage)
                    followThroughCharacter.Effects.NextAttackDamageMultiplier = 3.0;
                    
                    // Set next attack stat bonus (slow: -1 AGI for 1 turn)
                    if (result.SelectedAction.Advanced.StatBonus != 0 && 
                        !string.IsNullOrEmpty(result.SelectedAction.Advanced.StatBonusType))
                    {
                        followThroughCharacter.Effects.NextAttackStatBonus = result.SelectedAction.Advanced.StatBonus;
                        followThroughCharacter.Effects.NextAttackStatBonusType = result.SelectedAction.Advanced.StatBonusType;
                        followThroughCharacter.Effects.NextAttackStatBonusDuration = result.SelectedAction.Advanced.StatBonusDuration > 0 
                            ? result.SelectedAction.Advanced.StatBonusDuration 
                            : 1;
                    }
                }
                else
                {
                    // Apply stat bonus to source if the action has one (only for characters, not enemies)
                    // Allow negative stat bonuses (e.g., other actions that slow)
                    if (result.SelectedAction.Advanced.StatBonus != 0 && 
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
                // Start at step 0 (initial state, first action, no bonus)
                // Step 0: Roll 14+ → go to step 1 (bonus applies at step 1)
                // Step 1: Roll 14+ → go to step 2 (bonus continues, more bonus at step 2)
                // Step 1: Roll < 14 → reset to step 0 (bonus resets, stay in combo mode)
                // Step 2+: Roll 14+ → continue to next step (bonus continues)
                // Step 2+: Roll < 14 → reset to step 0 (bonus resets, stay in combo mode)
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    int comboThreshold = GameConfiguration.Instance.RollSystem.ComboThreshold.Min; // 14
                    
                    if (comboCharacter.ComboStep == 0)
                    {
                        // At step 0 (first action), need 14+ to advance to step 1
                        if (result.AttackRoll >= comboThreshold)
                        {
                            // Advance to step 1 with routing support
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                        }
                        // If < 14, stay at step 0 (no bonus, but still in combo mode)
                    }
                    else if (comboCharacter.ComboStep == 1)
                    {
                        // At step 1 (second action, bonus applies), need 14+ to advance to step 2 (more bonus)
                        if (result.AttackRoll >= comboThreshold)
                        {
                            // Advance to step 2 with routing support
                            comboCharacter.IncrementComboStep(result.SelectedAction);
                        }
                        else
                        {
                            // Didn't get 14+, reset to step 0 (bonus resets, but stay in combo mode)
                            comboCharacter.ComboStep = 0;
                        }
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
                            // Didn't get 14+, reset to step 0 (bonus resets, but stay in combo mode)
                            comboCharacter.ComboStep = 0;
                        }
                    }
                }
            }
            else
            {
                // ACTION bonuses are NOT consumed on miss - they remain queued
                // (ATTACK bonuses were already consumed before the roll)
                
                // Publish miss event
                ActionEventPublisher.PublishActionMiss(source, target, result.SelectedAction, result.AttackRoll);

                // Track miss statistics for player
                if (source is Character characterMiss)
                {
                    ActionStatisticsTracker.RecordMissAction(characterMiss, result.BaseRoll, result.RollBonus);
                }
                
                // Reset combo step to step 0 on miss (for characters, not enemies)
                // Step 0 is the initial combo state (first action, no bonus, but ready for combo)
                if (source is Character comboCharacterMiss && !(comboCharacterMiss is Enemy))
                {
                    comboCharacterMiss.ComboStep = 0;
                }

                ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
            }

            return result;
        }
    }
}

