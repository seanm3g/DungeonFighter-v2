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
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} executing action against {target.Name}");
            }

            // Use forced action if provided (for combo system), otherwise select action based on Actor type
            result.SelectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
            if (result.SelectedAction == null)
            {
                return result;
            }

            // Store the action that will be used
            lastUsedActions[source] = result.SelectedAction;

            // Handle unique action chance for characters (not enemies)
            if (source is Character character && !(character is Enemy) && forcedAction == null)
            {
                result.SelectedAction = ActionUtilities.HandleUniqueActionChance(character, result.SelectedAction);
            }

            // Use the same roll that was used for action selection
            result.BaseRoll = ActionSelector.GetActionRoll(source);

            // Apply roll modifications from action
            result.ModifiedBaseRoll = RollModificationManager.ApplyActionRollModifications(
                result.BaseRoll, result.SelectedAction, source, target);

            // Apply threshold overrides
            RollModificationManager.ApplyThresholdOverrides(result.SelectedAction, source);

            result.RollBonus = ActionUtilities.CalculateRollBonus(source, result.SelectedAction);
            result.AttackRoll = result.ModifiedBaseRoll + result.RollBonus;

            // Check for critical miss (natural 1 only)
            result.IsCriticalMiss = result.BaseRoll == 1;
            if (result.IsCriticalMiss)
            {
                source.HasCriticalMissPenalty = true;
                source.CriticalMissPenaltyTurns = 1;
            }

            // Store critical miss status for this action
            lastCriticalMissStatus[source] = result.IsCriticalMiss;

            // Determine if this is a combo or critical
            result.IsCombo = result.SelectedAction.Name != "BASIC ATTACK";
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
                        ActionUtilities.ApplyDamage(target, result.Damage);

                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                            DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {result.Damage} damage to {target.Name} with {result.SelectedAction.Name}");
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
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} healed {target.Name} for {result.HealAmount} health with {result.SelectedAction.Name}");
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
                    
                    string statName = statType switch
                    {
                        "STR" => "Strength",
                        "AGI" => "Agility",
                        "TEC" => "Technique",
                        "INT" => "Intelligence",
                        _ => statType
                    };
                    
                    result.StatusEffectMessages.Add($"    {source.Name} gains +{result.SelectedAction.Advanced.StatBonus} {statName} (dungeon duration)!");
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

                // Handle combo advancement for characters (with routing support)
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    comboCharacter.IncrementComboStep(result.SelectedAction);
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
                
                // Reset combo step to beginning of action queue on miss (for characters, not enemies)
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

