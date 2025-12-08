using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Combat.Outcomes;
using RPGGame.Actions.Conditional;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Result of action execution - contains all execution data
    /// </summary>
    internal class ActionExecutionResult
    {
        public Action? SelectedAction { get; set; }
        public int BaseRoll { get; set; }
        public int ModifiedBaseRoll { get; set; }
        public int RollBonus { get; set; }
        public int AttackRoll { get; set; }
        public bool IsCriticalMiss { get; set; }
        public bool IsCombo { get; set; }
        public bool IsCritical { get; set; }
        public bool Hit { get; set; }
        public int Damage { get; set; }
        public int HealAmount { get; set; }
        public List<string> StatusEffectMessages { get; set; } = new List<string>();
        public List<List<ColoredText>> ColoredStatusEffects { get; set; } = new List<List<ColoredText>>();
    }

    /// <summary>
    /// Handles action execution logic, damage application, and effect processing
    /// Refactored to focus purely on orchestration using extracted execution components
    /// </summary>
    public static class ActionExecutor
    {
        // Flag to disable debug output during balance analysis
        public static bool DisableCombatDebugOutput = false; // Temporarily enable debug output

        // Store the last action used by each Actor
        private static readonly Dictionary<Actor, Action> _lastUsedActions = new Dictionary<Actor, Action>();

        /// <summary>
        /// Core execution logic shared between string and ColoredText methods
        /// </summary>
        private static ActionExecutionResult ExecuteActionCore(Actor source, Actor target, Environment? environment, Action? lastPlayerAction, Action? forcedAction, BattleNarrative? battleNarrative)
        {
            var result = new ActionExecutionResult();
            
            if (!DisableCombatDebugOutput)
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
            _lastUsedActions[source] = result.SelectedAction;
            
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
            
            // Check for critical miss (use modified roll)
            result.IsCriticalMiss = (result.ModifiedBaseRoll + result.RollBonus) <= 1;
            if (result.IsCriticalMiss)
            {
                source.HasCriticalMissPenalty = true;
                source.CriticalMissPenaltyTurns = 1;
            }
            
            // Determine if this is a combo or critical
            result.IsCombo = result.SelectedAction.Name != "BASIC ATTACK";
            result.IsCritical = result.AttackRoll >= RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);
            
            // Publish action executed event for conditional triggers
            var actionEvent = new CombatEvent(CombatEventType.ActionExecuted, source)
            {
                Target = target,
                Action = result.SelectedAction,
                RollValue = result.AttackRoll,
                IsCombo = result.IsCombo,
                IsCritical = result.IsCritical
            };
            CombatEventBus.Instance.Publish(actionEvent);
            
            // Check for hit
            result.Hit = CombatCalculator.CalculateHit(source, target, result.RollBonus, result.AttackRoll);
            
            if (result.Hit)
            {
                // Publish hit event
                var hitEvent = new CombatEvent(CombatEventType.ActionHit, source)
                {
                    Target = target,
                    Action = result.SelectedAction,
                    RollValue = result.AttackRoll,
                    IsCombo = result.IsCombo,
                    IsCritical = result.IsCritical
                };
                CombatEventBus.Instance.Publish(hitEvent);
                
                // Apply damage for Attack-type and Spell-type actions
                if (result.SelectedAction.Type == ActionType.Attack || result.SelectedAction.Type == ActionType.Spell)
                {
                    double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, result.SelectedAction);
                    int totalRoll = result.ModifiedBaseRoll + result.RollBonus;
                    result.Damage = CombatCalculator.CalculateDamage(source, target, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, totalRoll);
                    ActionUtilities.ApplyDamage(target, result.Damage);
                    
                    if (!DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {result.Damage} damage to {target.Name} with {result.SelectedAction.Name}");
                    }
                    
                    // Track statistics
                    if (source is Character sourceCharacter)
                    {
                        ActionStatisticsTracker.RecordAttackAction(sourceCharacter, totalRoll, result.ModifiedBaseRoll, result.RollBonus, result.Damage, result.SelectedAction, target as Enemy);
                    }
                    if (target is Character targetCharacter)
                    {
                        ActionStatisticsTracker.RecordDamageReceived(targetCharacter, result.Damage);
                    }
                    
                    bool isCriticalHit = totalRoll >= 20;
                    ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, result.Damage, totalRoll, result.RollBonus, true, result.IsCombo, 0, 0, isCriticalHit, battleNarrative);
                }
                else if (result.SelectedAction.Type == ActionType.Heal)
                {
                    result.HealAmount = ActionUtilities.CalculateHealAmount(source, result.SelectedAction);
                    ActionUtilities.ApplyHealing(target, result.HealAmount);
                    
                    if (!DisableCombatDebugOutput)
                    {
                        DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} healed {target.Name} for {result.HealAmount} health with {result.SelectedAction.Name}");
                    }
                    
                    if (target is Character targetCharacterHeal)
                    {
                        ActionStatisticsTracker.RecordHealingReceived(targetCharacterHeal, result.HealAmount);
                    }
                    
                    int totalRoll = result.BaseRoll + result.RollBonus;
                    ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, totalRoll, result.RollBonus, true, result.IsCombo, 0, result.HealAmount, false, battleNarrative);
                }
                else
                {
                    // For non-damage actions
                    ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, 0, false, battleNarrative);
                }
                
                // Apply status effects (with conditional support)
                CombatEffectsSimplified.ApplyStatusEffects(result.SelectedAction, source, target, result.StatusEffectMessages, hitEvent);
                
                // Apply enemy roll penalty if the action has one
                if (result.SelectedAction.Advanced.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(result.SelectedAction.Advanced.EnemyRollPenalty, 1);
                    result.StatusEffectMessages.Add($"    {target.Name} suffers a -{result.SelectedAction.Advanced.EnemyRollPenalty} roll penalty!");
                }
                
                // Check for enemy death and HP thresholds
                if (target is Enemy enemyTarget)
                {
                    // Check for death
                    if (enemyTarget.CurrentHealth <= 0)
                    {
                        var deathEvent = new CombatEvent(CombatEventType.EnemyDied, source)
                        {
                            Target = target,
                            Action = result.SelectedAction,
                            Damage = result.Damage
                        };
                        CombatEventBus.Instance.Publish(deathEvent);
                        OutcomeHandlerRegistry.Instance.ProcessOutcomes(result.SelectedAction, deathEvent, source, target);
                    }
                    else
                    {
                        // Check for HP thresholds (50%, 25%, 10%)
                        double healthPercentage = (double)enemyTarget.CurrentHealth / enemyTarget.MaxHealth;
                        if (healthPercentage <= 0.5 && healthPercentage > 0.25)
                        {
                            var thresholdEvent = new CombatEvent(CombatEventType.EnemyHealthThreshold, source)
                            {
                                Target = target,
                                Action = result.SelectedAction,
                                HealthPercentage = 0.5
                            };
                            CombatEventBus.Instance.Publish(thresholdEvent);
                            OutcomeHandlerRegistry.Instance.ProcessOutcomes(result.SelectedAction, thresholdEvent, source, target);
                        }
                        else if (healthPercentage <= 0.25 && healthPercentage > 0.1)
                        {
                            var thresholdEvent = new CombatEvent(CombatEventType.EnemyHealthThreshold, source)
                            {
                                Target = target,
                                Action = result.SelectedAction,
                                HealthPercentage = 0.25
                            };
                            CombatEventBus.Instance.Publish(thresholdEvent);
                            OutcomeHandlerRegistry.Instance.ProcessOutcomes(result.SelectedAction, thresholdEvent, source, target);
                        }
                        else if (healthPercentage <= 0.1)
                        {
                            var thresholdEvent = new CombatEvent(CombatEventType.EnemyHealthThreshold, source)
                            {
                                Target = target,
                                Action = result.SelectedAction,
                                HealthPercentage = 0.1
                            };
                            CombatEventBus.Instance.Publish(thresholdEvent);
                            OutcomeHandlerRegistry.Instance.ProcessOutcomes(result.SelectedAction, thresholdEvent, source, target);
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
                var missEvent = new CombatEvent(CombatEventType.ActionMiss, source)
                {
                    Target = target,
                    Action = result.SelectedAction,
                    RollValue = result.AttackRoll,
                    IsMiss = true
                };
                CombatEventBus.Instance.Publish(missEvent);
                
                // Track miss statistics for player
                if (source is Character characterMiss)
                {
                    ActionStatisticsTracker.RecordMissAction(characterMiss, result.ModifiedBaseRoll, result.RollBonus);
                }
                
                ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, battleNarrative);
            }
            
            return result;
        }

        /// <summary>
        /// Formats execution result as ColoredText
        /// </summary>
        private static (List<ColoredText> actionText, List<ColoredText> rollInfo) FormatAsColoredText(ActionExecutionResult result, Actor source, Actor target)
        {
            if (result.SelectedAction == null)
            {
                var builder = new ColoredTextBuilder();
                builder.Add($"{source.Name} has no actions available.", Colors.White);
                var noActionText = builder.Build();
                return (noActionText, new List<ColoredText>());
            }
            
            if (result.Hit)
            {
                if (result.SelectedAction.Type == ActionType.Attack || result.SelectedAction.Type == ActionType.Spell)
                {
                    double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, result.SelectedAction);
                    var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, result.Damage, result.Damage, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, result.ModifiedBaseRoll);
                    return (damageText, rollInfo);
                }
                else if (result.SelectedAction.Type == ActionType.Heal)
                {
                    // Format healing message (healing already applied in ExecuteActionCore)
                    var healingText = CombatResults.FormatHealingMessageColored(source, target, result.HealAmount);
                    
                    // Create roll info for healing
                    var rollInfoBuilder = new ColoredTextBuilder();
                    rollInfoBuilder.Add("    (", Colors.Gray);
                    rollInfoBuilder.Add("roll:", ColorPalette.Info);
                    rollInfoBuilder.AddSpace();
                    rollInfoBuilder.Add(result.BaseRoll.ToString(), Colors.White);
                    
                    if (result.RollBonus != 0)
                    {
                        if (result.RollBonus > 0)
                        {
                            rollInfoBuilder.Add(" + ", Colors.White);
                            rollInfoBuilder.Add(result.RollBonus.ToString(), ColorPalette.Success);
                        }
                        else
                        {
                            rollInfoBuilder.Add(" - ", Colors.White);
                            rollInfoBuilder.Add((-result.RollBonus).ToString(), ColorPalette.Error);
                        }
                        rollInfoBuilder.Add(" = ", Colors.White);
                        rollInfoBuilder.Add((result.BaseRoll + result.RollBonus).ToString(), Colors.White);
                    }
                    
                    if (result.SelectedAction.Length > 0)
                    {
                        double actualSpeed = 0;
                        if (source is Character charSource)
                        {
                            actualSpeed = charSource.GetTotalAttackSpeed() * result.SelectedAction.Length;
                        }
                        else if (source is Enemy enemySource)
                        {
                            actualSpeed = enemySource.GetTotalAttackSpeed() * result.SelectedAction.Length;
                        }
                        
                        if (actualSpeed > 0)
                        {
                            rollInfoBuilder.Add("|", Colors.Gray);
                            rollInfoBuilder.AddSpace();
                            rollInfoBuilder.Add("speed: ", ColorPalette.Info);
                            rollInfoBuilder.Add($"{actualSpeed:F1}s", Colors.White);
                        }
                    }
                    
                    rollInfoBuilder.Add(")", Colors.Gray);
                    
                    return (healingText, rollInfoBuilder.Build());
                }
                else
                {
                    var (actionText, actionRollInfo) = CombatResults.FormatNonAttackActionColored(source, target, result.SelectedAction, result.ModifiedBaseRoll, result.RollBonus);
                    return (actionText, actionRollInfo);
                }
            }
            else
            {
                var (missText, missRollInfo) = CombatResults.FormatMissMessageColored(source, target, result.SelectedAction, result.ModifiedBaseRoll, result.RollBonus);
                return (missText, missRollInfo);
            }
        }

        /// <summary>
        /// Formats execution result as string
        /// </summary>
        private static string FormatAsString(ActionExecutionResult result, Actor source, Actor target)
        {
            if (result.SelectedAction == null)
            {
                return $"{source.Name} has no actions available.";
            }
            
            var results = new List<string>();
            
            if (result.Hit)
            {
                if (result.SelectedAction.Type == ActionType.Attack || result.SelectedAction.Type == ActionType.Spell)
                {
                    double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, result.SelectedAction);
                    var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, result.Damage, result.Damage, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, result.ModifiedBaseRoll);
                    string damageString = ColoredTextRenderer.RenderAsMarkup(damageText) + "\n" + ColoredTextRenderer.RenderAsMarkup(rollInfo);
                    results.Add(damageString);
                }
                else if (result.SelectedAction.Type == ActionType.Heal)
                {
                    results.Add($"{source.Name} heals {target.Name} for {result.HealAmount} health with {result.SelectedAction.Name}");
                }
                else
                {
                    var (actionText, actionRollInfo) = CombatResults.FormatNonAttackActionColored(source, target, result.SelectedAction, result.ModifiedBaseRoll, result.RollBonus);
                    string actionString = ColoredTextRenderer.RenderAsMarkup(actionText) + "\n" + ColoredTextRenderer.RenderAsMarkup(actionRollInfo);
                    results.Add(actionString);
                }
                
                // Add status effect messages
                results.AddRange(result.StatusEffectMessages);
            }
            else
            {
                var (missText, missRollInfo) = CombatResults.FormatMissMessageColored(source, target, result.SelectedAction, result.ModifiedBaseRoll, result.RollBonus);
                string missString = ColoredTextRenderer.RenderAsMarkup(missText) + "\n" + ColoredTextRenderer.RenderAsMarkup(missRollInfo);
                results.Add(missString);
            }
            
            return results.Count > 0 ? results[0] : "";
        }

        /// <summary>
        /// Executes a single action with all its effects and returns both main result and status effects as ColoredText
        /// This is the primary method - uses structured ColoredText for better reliability
        /// </summary>
        /// <param name="source">The Actor performing the action</param>
        /// <param name="target">The target Actor</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <param name="forcedAction">Forced action for combo system</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A tuple containing the main result as ColoredText tuple (actionText, rollInfo) and list of status effect messages as ColoredText</returns>
        public static ((List<ColoredText> actionText, List<ColoredText> rollInfo) mainResult, List<List<ColoredText>> statusEffects) ExecuteActionWithStatusEffectsColored(Actor source, Actor target, Environment? environment = null, Action? lastPlayerAction = null, Action? forcedAction = null, BattleNarrative? battleNarrative = null)
        {
            var result = ExecuteActionCore(source, target, environment, lastPlayerAction, forcedAction, battleNarrative);
            var coloredStatusEffects = new List<List<ColoredText>>();
            
            // Apply status effects as ColoredText
            if (result.SelectedAction != null && result.Hit)
            {
                ActionStatusEffectApplier.ApplyStatusEffectsColored(result.SelectedAction, source, target, coloredStatusEffects);
                ActionStatusEffectApplier.ApplyEnemyRollPenaltyColored(result.SelectedAction, target, coloredStatusEffects);
            }
            
            var mainResult = FormatAsColoredText(result, source, target);
            return (mainResult, coloredStatusEffects);
        }
        
        /// <summary>
        /// Internal method that executes an action and returns ColoredText results
        /// </summary>
        private static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteActionInternalColored(Actor source, Actor target, Environment? environment, Action? lastPlayerAction, Action? forcedAction, BattleNarrative? battleNarrative, List<List<ColoredText>> coloredStatusEffects)
        {
            var result = ExecuteActionCore(source, target, environment, lastPlayerAction, forcedAction, battleNarrative);
            
            // Apply status effects as ColoredText
            if (result.SelectedAction != null && result.Hit)
            {
                ActionStatusEffectApplier.ApplyStatusEffectsColored(result.SelectedAction, source, target, coloredStatusEffects);
                ActionStatusEffectApplier.ApplyEnemyRollPenaltyColored(result.SelectedAction, target, coloredStatusEffects);
            }
            
            return FormatAsColoredText(result, source, target);
        }
        
        
        /// <summary>
        /// Executes a single action with all its effects
        /// </summary>
        /// <param name="source">The Actor performing the action</param>
        /// <param name="target">The target Actor</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <param name="forcedAction">Forced action for combo system</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteAction(Actor source, Actor target, Environment? environment = null, Action? lastPlayerAction = null, Action? forcedAction = null, BattleNarrative? battleNarrative = null)
        {
            var result = ExecuteActionCore(source, target, environment, lastPlayerAction, forcedAction, battleNarrative);
            return FormatAsString(result, source, target);
        }


        /// <summary>
        /// Gets the last action used by an Actor
        /// </summary>
        public static Action? GetLastUsedAction(Actor source)
        {
            _lastUsedActions.TryGetValue(source, out Action? action);
            return action;
        }

        /// <summary>
        /// Executes multiple attacks per turn based on the source's attack speed
        /// </summary>
        /// <param name="source">The Actor performing the attacks</param>
        /// <param name="target">The Actor receiving the attacks</param>
        /// <param name="environment">The environment affecting the attacks</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A string describing the results of all attacks</returns>
        public static string ExecuteMultiAttack(Actor source, Actor target, Environment? environment = null, BattleNarrative? battleNarrative = null)
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
    }
}

