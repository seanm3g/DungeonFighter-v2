using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Handles action execution logic, damage application, and effect processing
    /// Refactored to focus purely on execution using shared utilities
    /// </summary>
    public static class ActionExecutor
    {
        // Flag to disable debug output during balance analysis
        public static bool DisableCombatDebugOutput = false; // Temporarily enable debug output

        // Store the last action used by each Actor
        private static readonly Dictionary<Actor, Action> _lastUsedActions = new Dictionary<Actor, Action>();

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
            var coloredStatusEffects = new List<List<ColoredText>>();
            
            var mainResult = ExecuteActionInternalColored(source, target, environment, lastPlayerAction, forcedAction, battleNarrative, coloredStatusEffects);
            
            return (mainResult, coloredStatusEffects);
        }
        
        /// <summary>
        /// Internal method that executes an action and returns ColoredText results
        /// </summary>
        private static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteActionInternalColored(Actor source, Actor target, Environment? environment, Action? lastPlayerAction, Action? forcedAction, BattleNarrative? battleNarrative, List<List<ColoredText>> coloredStatusEffects)
        {
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} executing action against {target.Name}");
            }
            
            // Use forced action if provided (for combo system), otherwise select action based on Actor type
            var selectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
            if (selectedAction == null)
            {
                var builder = new ColoredTextBuilder();
                builder.Add($"{source.Name} has no actions available.", Colors.White);
                var noActionText = builder.Build();
                return (noActionText, new List<ColoredText>());
            }
            
            // Store the action that will be used
            _lastUsedActions[source] = selectedAction;
            
            // Handle unique action chance for characters (not enemies)
            if (source is Character character && !(character is Enemy) && forcedAction == null)
            {
                selectedAction = ActionUtilities.HandleUniqueActionChance(character, selectedAction);
            }
            
            // Use the same roll that was used for action selection
            int baseRoll = ActionSelector.GetActionRoll(source);
            int rollBonus = ActionUtilities.CalculateRollBonus(source, selectedAction);
            int attackRoll = baseRoll + rollBonus;
            int cappedRoll = Math.Min(attackRoll, 20);
            
            // Check for critical miss
            bool isCriticalMiss = (baseRoll + rollBonus) <= 1;
            if (isCriticalMiss)
            {
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
                    var result = ExecuteAttackActionColored(source, target, selectedAction, baseRoll, rollBonus, battleNarrative);
                    // Apply status effects (still using string version for now, convert later)
                    var stringResults = new List<string>();
                    CombatEffectsSimplified.ApplyStatusEffects(selectedAction, source, target, stringResults);
                    // Convert status effect strings to ColoredText
                    foreach (var statusString in stringResults)
                    {
                        if (!string.IsNullOrEmpty(statusString))
                        {
                            // Parse status effect string to ColoredText
                            var statusColored = ColoredTextParser.Parse(statusString);
                            if (statusColored.Count > 0)
                            {
                                coloredStatusEffects.Add(statusColored);
                            }
                        }
                    }
                    return result;
                }
                else if (selectedAction.Type == ActionType.Heal)
                {
                    var result = ExecuteHealActionColored(source, target, selectedAction, baseRoll, rollBonus, battleNarrative);
                    // Apply status effects
                    var stringResults = new List<string>();
                    CombatEffectsSimplified.ApplyStatusEffects(selectedAction, source, target, stringResults);
                    foreach (var statusString in stringResults)
                    {
                        if (!string.IsNullOrEmpty(statusString))
                        {
                            var statusColored = ColoredTextParser.Parse(statusString);
                            if (statusColored.Count > 0)
                            {
                                coloredStatusEffects.Add(statusColored);
                            }
                        }
                    }
                    return result;
                }
                else
                {
                    // For non-damage actions
                    var (actionText, actionRollInfo) = CombatResults.FormatNonAttackActionColored(source, target, selectedAction, baseRoll, rollBonus);
                    bool isCombo = selectedAction.Name != "BASIC ATTACK";
                    ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, baseRoll + rollBonus, rollBonus, true, isCombo, 0, 0, false, battleNarrative);
                    
                    // Apply status effects
                    var stringResults = new List<string>();
                    CombatEffectsSimplified.ApplyStatusEffects(selectedAction, source, target, stringResults);
                    foreach (var statusString in stringResults)
                    {
                        if (!string.IsNullOrEmpty(statusString))
                        {
                            var statusColored = ColoredTextParser.Parse(statusString);
                            if (statusColored.Count > 0)
                            {
                                coloredStatusEffects.Add(statusColored);
                            }
                        }
                    }
                    
                    // Handle enemy roll penalty
                    if (selectedAction.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                    {
                        targetEnemy.ApplyRollPenalty(selectedAction.EnemyRollPenalty, 1);
                        var penaltyBuilder = new ColoredTextBuilder();
                        penaltyBuilder.Add("    ", Colors.White);
                        penaltyBuilder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
                        penaltyBuilder.Add(" suffers a -", Colors.White);
                        penaltyBuilder.Add(selectedAction.EnemyRollPenalty.ToString(), ColorPalette.Error);
                        penaltyBuilder.Add(" roll penalty!", Colors.White);
                        coloredStatusEffects.Add(penaltyBuilder.Build());
                    }
                    
                    // Handle combo advancement
                    if (source is Character comboCharacter && !(comboCharacter is Enemy))
                    {
                        comboCharacter.ComboStep++;
                    }
                    
                    return (actionText, actionRollInfo);
                }
            }
            else
            {
                // Miss
                if (source is Character characterMiss)
                {
                    bool isCriticalMissMiss = (baseRoll + rollBonus) <= 1;
                    characterMiss.RecordAction(false, false, isCriticalMissMiss);
                }
                
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, baseRoll + rollBonus, rollBonus, false, false, 0, 0, false, battleNarrative);
                
                var (missText, missRollInfo) = CombatResults.FormatMissMessageColored(source, target, selectedAction, baseRoll, rollBonus);
                return (missText, missRollInfo);
            }
        }
        
        /// <summary>
        /// Executes an attack action and returns ColoredText results
        /// </summary>
        private static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteAttackActionColored(Actor source, Actor target, Action selectedAction, int baseRoll, int rollBonus, BattleNarrative? battleNarrative)
        {
            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
            
            ActionUtilities.ApplyDamage(target, damage);
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {damage} damage to {target.Name} with {selectedAction.Name}");
            }
            
            // Track statistics
            if (source is Character character)
            {
                bool isCritical = totalRoll >= 20;
                bool isCriticalMiss = (baseRoll + rollBonus) <= 1;
                character.RecordAction(true, isCritical, isCriticalMiss);
                character.RecordDamageDealt(damage, isCritical);
                
                bool isComboAction = selectedAction.Name != "BASIC ATTACK";
                if (isComboAction)
                {
                    character.RecordCombo(character.ComboStep, damage);
                }
                
                if (target is Enemy enemyTarget && !enemyTarget.IsAlive && damage >= enemyTarget.GetEffectiveMaxHealth())
                {
                    character.RecordOneShotKill();
                }
            }
            
            if (target is Character targetCharacter)
            {
                targetCharacter.RecordDamageReceived(damage);
                targetCharacter.RecordHealthStatus(targetCharacter.GetHealthPercentage());
            }
            
            bool isCombo = selectedAction.Name != "BASIC ATTACK";
            bool isCriticalHit = totalRoll >= 20;
            ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, damage, totalRoll, rollBonus, true, isCombo, 0, 0, isCriticalHit, battleNarrative);
            
            var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
            
            // Handle enemy roll penalty
            if (selectedAction.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
            {
                targetEnemy.ApplyRollPenalty(selectedAction.EnemyRollPenalty, 1);
            }
            
            // Handle combo advancement
            if (source is Character comboCharacter && !(comboCharacter is Enemy))
            {
                comboCharacter.ComboStep++;
            }
            
            return (damageText, rollInfo);
        }
        
        /// <summary>
        /// Executes a heal action and returns ColoredText results
        /// </summary>
        private static (List<ColoredText> actionText, List<ColoredText> rollInfo) ExecuteHealActionColored(Actor source, Actor target, Action selectedAction, int baseRoll, int rollBonus, BattleNarrative? battleNarrative)
        {
            int healAmount = ActionUtilities.CalculateHealAmount(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            
            ActionUtilities.ApplyHealing(target, healAmount);
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} healed {target.Name} for {healAmount} health");
            }
            
            // Track statistics
            if (target is Character targetCharacter)
            {
                targetCharacter.RecordHealingReceived(healAmount);
            }
            
            bool isCombo = selectedAction.Name != "BASIC ATTACK";
            ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, totalRoll, rollBonus, true, isCombo, healAmount, 0, false, battleNarrative);
            
            var healingText = CombatResults.FormatHealingMessageColored(source, target, healAmount);
            // Create a simple roll info for healing (no attack/defense, just roll and speed)
            var rollInfoBuilder = new ColoredTextBuilder();
            rollInfoBuilder.Add("    (", Colors.Gray);
            rollInfoBuilder.Add("roll: ", ColorPalette.Info);
            rollInfoBuilder.Add(baseRoll.ToString(), Colors.White);
            if (rollBonus != 0)
            {
                if (rollBonus > 0)
                {
                    rollInfoBuilder.Add(" + ", Colors.White);
                    rollInfoBuilder.Add(rollBonus.ToString(), ColorPalette.Success);
                }
                else
                {
                    rollInfoBuilder.Add(" - ", Colors.White);
                    rollInfoBuilder.Add((-rollBonus).ToString(), ColorPalette.Error);
                }
                rollInfoBuilder.Add(" = ", Colors.White);
                rollInfoBuilder.Add(totalRoll.ToString(), Colors.White);
            }
            if (selectedAction != null && selectedAction.Length > 0)
            {
                double actualSpeed = 0;
                if (source is Character charSource)
                {
                    actualSpeed = charSource.GetTotalAttackSpeed() * selectedAction.Length;
                }
                else if (source is Enemy enemySource)
                {
                    actualSpeed = enemySource.GetTotalAttackSpeed() * selectedAction.Length;
                }
                if (actualSpeed > 0)
                {
                    rollInfoBuilder.Add(" | ", Colors.Gray);
                    rollInfoBuilder.Add("speed: ", ColorPalette.Info);
                    rollInfoBuilder.Add($"{actualSpeed:F1}s", Colors.White);
                }
            }
            rollInfoBuilder.Add(")", Colors.Gray);
            
            return (healingText, rollInfoBuilder.Build());
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
            var actionResults = new List<string>();
            return ExecuteActionInternal(source, target, environment, lastPlayerAction, forcedAction, battleNarrative, actionResults);
        }

        /// <summary>
        /// Internal method that executes an action and populates the results list
        /// </summary>
        private static string ExecuteActionInternal(Actor source, Actor target, Environment? environment, Action? lastPlayerAction, Action? forcedAction, BattleNarrative? battleNarrative, List<string> results)
        {
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} executing action against {target.Name}");
            }
            
            // Use forced action if provided (for combo system), otherwise select action based on Actor type
            var selectedAction = forcedAction ?? ActionSelector.SelectActionByEntityType(source);
            if (selectedAction == null)
            {
                return $"{source.Name} has no actions available.";
            }
            
            // Store the action that will be used
            _lastUsedActions[source] = selectedAction;
            
            // Handle unique action chance for characters (not enemies)
            // Only apply unique action chance if no forced action was provided
            if (source is Character character && !(character is Enemy) && forcedAction == null)
            {
                selectedAction = ActionUtilities.HandleUniqueActionChance(character, selectedAction);
            }
            
            // Use the same roll that was used for action selection (for both heroes and enemies)
            int baseRoll = ActionSelector.GetActionRoll(source);
            
            // Calculate roll bonus using the selected action (this may be different from selection bonus)
            int rollBonus = ActionUtilities.CalculateRollBonus(source, selectedAction);
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
                    ExecuteAttackAction(source, target, selectedAction, baseRoll, rollBonus, results, battleNarrative);
                }
                else if (selectedAction.Type == ActionType.Heal)
                {
                    ExecuteHealAction(source, target, selectedAction, baseRoll, rollBonus, results, battleNarrative);
                }
                else
                {
                    // For non-damage actions, just show the action was successful
                    var (actionText, actionRollInfo) = CombatResults.FormatNonAttackActionColored(source, target, selectedAction, baseRoll, rollBonus);
                    string actionString = ColoredTextRenderer.RenderAsPlainText(actionText) + "\n" + ColoredTextRenderer.RenderAsPlainText(actionRollInfo);
                    results.Add(actionString);
                    
                    // Create and add BattleEvent for narrative system (non-damage action)
                    bool isCombo = selectedAction.Name != "BASIC ATTACK"; // Any non-basic action counts as combo
                    ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, baseRoll + rollBonus, rollBonus, true, isCombo, 0, 0, false, battleNarrative);
                }
                
                // Apply status effects for all action types
                CombatEffectsSimplified.ApplyStatusEffects(selectedAction, source, target, results);
                
                // Apply enemy roll penalty if the action has one
                if (selectedAction.EnemyRollPenalty > 0 && target is Enemy targetEnemy)
                {
                    targetEnemy.ApplyRollPenalty(selectedAction.EnemyRollPenalty, 1); // Apply for 1 turn
                    results.Add($"    {target.Name} suffers a -{selectedAction.EnemyRollPenalty} roll penalty!");
                }
                
                // Handle combo advancement for characters
                if (source is Character comboCharacter && !(comboCharacter is Enemy))
                {
                    comboCharacter.ComboStep++;
                }
            }
            else
            {
                // Track miss statistics for player
                if (source is Character characterMiss)
                {
                    bool isCriticalMissMiss = (baseRoll + rollBonus) <= 1;
                    characterMiss.RecordAction(false, false, isCriticalMissMiss);
                }
                
                // Create and add BattleEvent for miss
                ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, baseRoll + rollBonus, rollBonus, false, false, 0, 0, false, battleNarrative);
                
                var (missText, missRollInfo) = CombatResults.FormatMissMessageColored(source, target, selectedAction, baseRoll, rollBonus);
                string missString = ColoredTextRenderer.RenderAsPlainText(missText) + "\n" + ColoredTextRenderer.RenderAsPlainText(missRollInfo);
                results.Add(missString);
            }
            
            return results.Count > 0 ? results[0] : "";
        }

        /// <summary>
        /// Executes an attack action
        /// </summary>
        private static void ExecuteAttackAction(Actor source, Actor target, Action selectedAction, int baseRoll, int rollBonus, List<string> results, BattleNarrative? battleNarrative)
        {
            // Calculate damage with Actor-specific modifiers using shared utility
            double damageMultiplier = ActionUtilities.CalculateDamageMultiplier(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            int damage = CombatCalculator.CalculateDamage(source, target, selectedAction, damageMultiplier, 1.0, rollBonus, totalRoll);
            
            // Apply damage using shared utility
            ActionUtilities.ApplyDamage(target, damage);
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} dealt {damage} damage to {target.Name} with {selectedAction.Name}");
            }
            
            // Track statistics for player actions
            if (source is Character character)
            {
                bool isCritical = totalRoll >= 20;
                bool isCriticalMiss = (baseRoll + rollBonus) <= 1;
                
                // Record action statistics
                character.RecordAction(true, isCritical, isCriticalMiss);
                character.RecordDamageDealt(damage, isCritical);
                
                // Track combo statistics
                bool isComboAction = selectedAction.Name != "BASIC ATTACK";
                if (isComboAction)
                {
                    character.RecordCombo(character.ComboStep, damage);
                }
                
                // Check for one-shot kill
                if (target is Enemy enemyTarget && !enemyTarget.IsAlive && damage >= enemyTarget.GetEffectiveMaxHealth())
                {
                    character.RecordOneShotKill();
                }
            }
            
            // Track damage received for player
            if (target is Character targetCharacter)
            {
                targetCharacter.RecordDamageReceived(damage);
                targetCharacter.RecordHealthStatus(targetCharacter.GetHealthPercentage());
            }
            
            // Create and add BattleEvent for narrative system using shared utility
            bool isCombo = selectedAction.Name != "BASIC ATTACK"; // Any non-basic attack counts as combo
            bool isCriticalHit = totalRoll >= 20; // Critical hit on natural 20 or higher
            ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, damage, totalRoll, rollBonus, true, isCombo, 0, 0, isCriticalHit, battleNarrative);
            
            // Add damage message - use the new ColoredText system, then convert to string for backward compatibility
            var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, damage, damage, selectedAction, damageMultiplier, 1.0, rollBonus, baseRoll);
            // Convert ColoredText to plain string for backward compatibility (old API)
            string damageString = ColoredTextRenderer.RenderAsPlainText(damageText) + "\n" + ColoredTextRenderer.RenderAsPlainText(rollInfo);
            results.Add(damageString);
        }

        /// <summary>
        /// Executes a heal action
        /// </summary>
        private static void ExecuteHealAction(Actor source, Actor target, Action selectedAction, int baseRoll, int rollBonus, List<string> results, BattleNarrative? battleNarrative)
        {
            // Handle healing actions using shared utility
            int healAmount = ActionUtilities.CalculateHealAmount(source, selectedAction);
            int totalRoll = baseRoll + rollBonus;
            
            // Apply healing using shared utility
            ActionUtilities.ApplyHealing(target, healAmount);
            if (!DisableCombatDebugOutput)
            {
                DebugLogger.WriteCombatDebug("ActionExecutor", $"{source.Name} healed {target.Name} for {healAmount} health with {selectedAction.Name}");
            }
            
            // Track healing statistics for player
            if (target is Character character)
            {
                character.RecordHealingReceived(healAmount);
            }
            
            // Create and add BattleEvent for narrative system using shared utility
            bool isCombo = selectedAction.Name != "BASIC ATTACK"; // Any non-basic action counts as combo
            ActionUtilities.CreateAndAddBattleEvent(source, target, selectedAction, 0, totalRoll, rollBonus, true, isCombo, 0, healAmount, false, battleNarrative);
            
            // Add healing message
            results.Add($"{source.Name} heals {target.Name} for {healAmount} health with {selectedAction.Name}");
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

