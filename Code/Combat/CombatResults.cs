using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Handles combat result formatting, narrative generation, and display logic
    /// </summary>
    public static class CombatResults
    {

        /// <summary>
        /// Formats damage display with separate damage text and roll info
        /// </summary>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <param name="rawDamage">Raw damage before armor (currently same as actual damage)</param>
        /// <param name="actualDamage">Actual damage after armor</param>
        /// <param name="action">The action performed</param>
        /// <param name="comboAmplifier">Combo amplification multiplier</param>
        /// <param name="damageMultiplier">Additional damage multiplier</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <param name="roll">The attack roll</param>
        /// <returns>Tuple of (damageText, rollInfo)</returns>
        public static (string damageText, string rollInfo) FormatDamageDisplaySeparated(Actor attacker, Actor target, int rawDamage, int actualDamage, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
        {
            string actionName = action?.Name ?? "attack";
            
            // Check if this is a critical hit (total roll of 20 or higher)
            int totalRoll = roll + rollBonus;
            bool isCritical = totalRoll >= 20;
            
            // Add CRITICAL prefix to action name if it's a critical hit
            if (isCritical)
            {
                actionName = $"CRITICAL {actionName}";
            }
            
            // Determine if this is a combo action (all actions in the game are combo actions)
            bool isComboAction = action != null && action.IsComboAction;
            
            // First line: Format for combo actions
            // Using template-based coloring {{damage|number}} for proper spacing
            string damageText;
            if (isComboAction)
            {
                damageText = $"{attacker.Name} hits {target.Name} with {actionName} for {actualDamage} damage";
            }
            else
            {
                damageText = $"{attacker.Name} hits {target.Name} for {actualDamage} damage";
            }
            
            // Build the detailed roll and damage information
            var rollInfo = new List<string>();
            
            // Roll information: roll + buffs - debuffs = total (only show = if there are modifiers)
            string rollDisplay = roll.ToString();
            if (rollBonus > 0)
            {
                rollDisplay += $" + {rollBonus} = {totalRoll}";
            }
            else if (rollBonus < 0)
            {
                rollDisplay += $" - {-rollBonus} = {totalRoll}"; // Add proper spacing around minus sign
            }
            // If rollBonus is 0, don't add the = total part (totalRoll will equal roll)
            rollInfo.Add($"roll: {rollDisplay}");
            
            // Attack vs Defense information: attack X - Y defense
            int targetDefense = 0;
            if (target is Enemy targetEnemy)
            {
                // For enemies, use the Armor property directly to avoid inheritance issues
                targetDefense = targetEnemy.Armor;
            }
            else if (target is Character targetCharacter)
            {
                targetDefense = targetCharacter.GetTotalArmor();
            }
            
            // Calculate actual raw damage before armor reduction
            int actualRawDamage = CombatCalculator.CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll);
            rollInfo.Add($"attack {actualRawDamage} - {targetDefense} armor");
            
            // Speed information - calculate actual action speed
            if (action != null && action.Length > 0)
            {
                double actualSpeed = CalculateActualActionSpeed(attacker, action);
                rollInfo.Add($"speed: {actualSpeed:F1}s");
            }
            
            // Add combo info to rollInfo if present - show amp prominently
            if (comboAmplifier > 1.0)
            {
                rollInfo.Add($"amp: {comboAmplifier:F1}x");
            }
            else if (action != null && action.IsComboAction)
            {
                // Show amp:1.0x for first combo action
                rollInfo.Add("amp: 1.0x");
            }
            
            // Return the roll information as a separate string
            string rollInfoText = "     (" + string.Join(" | ", rollInfo) + ")";
            
            return (damageText, rollInfoText);
        }

        /// <summary>
        /// Checks health milestones and returns notification messages
        /// </summary>
        /// <param name="Actor">The Actor to check</param>
        /// <param name="damageDealt">Amount of damage dealt</param>
        /// <returns>List of health milestone notifications</returns>
        public static List<string> CheckHealthMilestones(Actor Actor, int damageDealt)
        {
            var notifications = new List<string>();
            
            if (Actor is Character character)
            {
                // Check for low health warnings
                double healthPercentage = (double)character.CurrentHealth / character.GetEffectiveMaxHealth();
                
                if (healthPercentage <= 0.25 && healthPercentage > 0.1)
                {
                    notifications.Add($"{character.Name} is critically wounded!");
                }
                else if (healthPercentage <= 0.1)
                {
                    notifications.Add($"{character.Name} is near death!");
                }
            }
            else if (Actor is Enemy enemy)
            {
                // Check for enemy health milestones
                double healthPercentage = (double)enemy.CurrentHealth / enemy.GetEffectiveMaxHealth();
                
                if (healthPercentage <= 0.5 && healthPercentage > 0.25)
                {
                    notifications.Add($"{enemy.Name} is badly wounded!");
                }
                else if (healthPercentage <= 0.25)
                {
                    notifications.Add($"{enemy.Name} is on the verge of defeat!");
                }
            }
            
            return notifications;
        }


        /// <summary>
        /// Executes an action with UI formatting and returns both main result and status effects as ColoredText
        /// This is the primary method - uses structured ColoredText for better reliability
        /// </summary>
        /// <param name="source">The Actor performing the action</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The specific action to execute</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A tuple containing the formatted main result as ColoredText tuple and list of status effect messages as ColoredText</returns>
        public static ((List<ColoredText> actionText, List<ColoredText> rollInfo) mainResult, List<List<ColoredText>> statusEffects) ExecuteActionWithUIAndStatusEffectsColored(Actor source, Actor target, Action? action, Environment? environment = null, Action? lastPlayerAction = null, BattleNarrative? battleNarrative = null)
        {
            // Execute the action using the ColoredText version
            return ActionExecutor.ExecuteActionWithStatusEffectsColored(source, target, environment, lastPlayerAction, action, battleNarrative);
        }
        
        /// <summary>
        /// Calculates the actual action speed by multiplying Actor base speed by action length
        /// </summary>
        /// <param name="Actor">The Actor performing the action</param>
        /// <param name="action">The action being performed</param>
        /// <returns>The calculated action speed in seconds</returns>
        private static double CalculateActualActionSpeed(Actor Actor, Action action)
        {
            // Get the Actor's base attack speed
            double baseSpeed = 0;
            if (Actor is Character character)
            {
                baseSpeed = character.GetTotalAttackSpeed();
            }
            else if (Actor is Enemy enemy)
            {
                baseSpeed = enemy.GetTotalAttackSpeed();
            }
            else if (Actor is Environment environment)
            {
                // For environments, use a default base speed
                baseSpeed = 15.0; // Same as used in CombatManager
            }
            
            // Apply critical miss penalty (doubles action speed)
            if (Actor.HasCriticalMissPenalty)
            {
                baseSpeed *= 2.0;
            }
            
            // Calculate actual action speed: base speed * action length
            return baseSpeed * action.Length;
        }
        
        // ===== NEW COLORED TEXT SYSTEM (PRIMARY API) =====
        
        /// <summary>
        /// Formats damage display using the new ColoredText system
        /// Returns both damage text and roll info as separate ColoredText lists
        /// </summary>
        public static (List<ColoredText> damageText, List<ColoredText> rollInfo) FormatDamageDisplayColored(
            Actor attacker, Actor target, int rawDamage, int actualDamage, Action? action = null, 
            double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, int multiHitCount = 1, bool isCriticalMiss = false)
        {
            return CombatResultsColoredText.FormatDamageDisplayColored(attacker, target, rawDamage, actualDamage, 
                action, comboAmplifier, damageMultiplier, rollBonus, roll, multiHitCount, isCriticalMiss);
        }
        
        /// <summary>
        /// Formats miss message using the new ColoredText system
        /// Returns both miss text and roll info as separate ColoredText lists
        /// </summary>
        public static (List<ColoredText> missText, List<ColoredText> rollInfo) FormatMissMessageColored(
            Actor attacker, Actor target, Action action, int roll, int rollBonus, int naturalRoll)
        {
            return CombatResultsColoredText.FormatMissMessageColored(attacker, target, action, roll, rollBonus, naturalRoll);
        }
        
        /// <summary>
        /// Formats non-attack action using the new ColoredText system
        /// Returns both action text and roll info as separate ColoredText lists
        /// </summary>
        public static (List<ColoredText> actionText, List<ColoredText> rollInfo) FormatNonAttackActionColored(
            Actor source, Actor target, Action action, int roll, int rollBonus)
        {
            return CombatResultsColoredText.FormatNonAttackActionColored(source, target, action, roll, rollBonus);
        }
        
        /// <summary>
        /// Formats health milestone using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthMilestoneColored(Actor Actor, double healthPercentage)
        {
            return CombatResultsColoredText.FormatHealthMilestoneColored(Actor, healthPercentage);
        }
        
        /// <summary>
        /// Formats block message using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBlockMessageColored(Actor defender, Actor attacker, int damageBlocked)
        {
            return CombatResultsColoredText.FormatBlockMessageColored(defender, attacker, damageBlocked);
        }
        
        /// <summary>
        /// Formats dodge message using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatDodgeMessageColored(Actor defender, Actor attacker)
        {
            return CombatResultsColoredText.FormatDodgeMessageColored(defender, attacker);
        }
        
        /// <summary>
        /// Formats status effect using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatStatusEffectColored(Actor target, string effectName, bool isApplied, int? duration = null, int? stackCount = null)
        {
            return CombatResultsColoredText.FormatStatusEffectColored(target, effectName, isApplied, duration, stackCount);
        }
        
        /// <summary>
        /// Formats healing message using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealingMessageColored(Actor healer, Actor target, int healAmount)
        {
            return CombatResultsColoredText.FormatHealingMessageColored(healer, target, healAmount);
        }
        
        /// <summary>
        /// Formats victory message using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatVictoryMessageColored(Actor victor, Actor defeated)
        {
            return CombatResultsColoredText.FormatVictoryMessageColored(victor, defeated);
        }
        
        /// <summary>
        /// Formats defeat message using the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatDefeatMessageColored(Actor victor, Actor defeated)
        {
            return CombatResultsColoredText.FormatDefeatMessageColored(victor, defeated);
        }
    }
}


