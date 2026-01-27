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
using RPGGame.Combat.Formatting;

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
        
        // Store the critical miss status for the last action used by each Actor
        private static readonly Dictionary<Actor, bool> _lastCriticalMissStatus = new Dictionary<Actor, bool>();

        /// <summary>
        /// Core execution logic shared between string and ColoredText methods
        /// </summary>
        private static ActionExecutionResult ExecuteActionCore(Actor source, Actor target, Environment? environment, Action? lastPlayerAction, Action? forcedAction, BattleNarrative? battleNarrative)
        {
            return ActionExecutionFlow.Execute(
                source, target, environment, lastPlayerAction, forcedAction, battleNarrative,
                _lastUsedActions, _lastCriticalMissStatus);
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
                    // Get multi-hit count for display formatting
                    int multiHitCount = result.SelectedAction.Advanced.MultiHitCount;
                    var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, result.Damage, result.Damage, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, result.ModifiedBaseRoll, multiHitCount, result.IsCriticalMiss);
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
                        double actualSpeed = ActionSpeedCalculator.CalculateActualActionSpeed(source, result.SelectedAction, result.IsCriticalMiss);
                        
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
                var (missText, missRollInfo) = CombatResults.FormatMissMessageColored(source, target, result.SelectedAction, result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll);
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
                    // Get multi-hit count for display formatting
                    int multiHitCount = result.SelectedAction.Advanced.MultiHitCount;
                    var (damageText, rollInfo) = CombatResults.FormatDamageDisplayColored(source, target, result.Damage, result.Damage, result.SelectedAction, damageMultiplier, 1.0, result.RollBonus, result.ModifiedBaseRoll, multiHitCount);
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
                var (missText, missRollInfo) = CombatResults.FormatMissMessageColored(source, target, result.SelectedAction, result.ModifiedBaseRoll, result.RollBonus, result.BaseRoll);
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
                ActionStatusEffectApplier.ApplyStatBonusColored(result.SelectedAction, source, coloredStatusEffects);
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
                ActionStatusEffectApplier.ApplyStatBonusColored(result.SelectedAction, source, coloredStatusEffects);
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
        /// Gets whether the last action used by an Actor was a critical miss
        /// </summary>
        public static bool GetLastCriticalMissStatus(Actor source)
        {
            _lastCriticalMissStatus.TryGetValue(source, out bool isCriticalMiss);
            return isCriticalMiss;
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

