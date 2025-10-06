using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Handles combat result formatting, narrative generation, and display logic
    /// </summary>
    public static class CombatResults
    {

        /// <summary>
        /// Formats damage display with detailed breakdown
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="rawDamage">Raw damage before armor (currently same as actual damage)</param>
        /// <param name="actualDamage">Actual damage after armor</param>
        /// <param name="action">The action performed</param>
        /// <param name="comboAmplifier">Combo amplification multiplier</param>
        /// <param name="damageMultiplier">Additional damage multiplier</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <param name="roll">The attack roll</param>
        /// <returns>Formatted damage display string</returns>
        public static string FormatDamageDisplay(Entity attacker, Entity target, int rawDamage, int actualDamage, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
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
            
            // Determine if this is a combo action (anything that's not BASIC ATTACK)
            bool isComboAction = actionName != "BASIC ATTACK" && actionName != "CRITICAL BASIC ATTACK";
            
            // First line: Different format for basic attacks vs combo actions
            string damageText;
            if (isComboAction)
            {
                damageText = $"[{attacker.Name}] hits [{target.Name}] with {actionName} for {actualDamage} damage";
            }
            else
            {
                damageText = $"[{attacker.Name}] hits [{target.Name}] for {actualDamage} damage";
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
            
            // Add combo info to rollInfo if present
            
            if (comboAmplifier > 1.0)
            {
                rollInfo.Add($"Combo x{comboAmplifier:F1}");
            }
            
            // Add the detailed information on the next line with indentation and parentheses
            damageText += "\n    (" + string.Join(" | ", rollInfo) + ")";
            
            return damageText;
        }

        /// <summary>
        /// Formats damage display with separate damage text and roll info
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="rawDamage">Raw damage before armor (currently same as actual damage)</param>
        /// <param name="actualDamage">Actual damage after armor</param>
        /// <param name="action">The action performed</param>
        /// <param name="comboAmplifier">Combo amplification multiplier</param>
        /// <param name="damageMultiplier">Additional damage multiplier</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <param name="roll">The attack roll</param>
        /// <returns>Tuple of (damageText, rollInfo)</returns>
        public static (string damageText, string rollInfo) FormatDamageDisplaySeparated(Entity attacker, Entity target, int rawDamage, int actualDamage, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
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
            
            // Determine if this is a combo action (anything that's not BASIC ATTACK)
            bool isComboAction = actionName != "BASIC ATTACK" && actionName != "CRITICAL BASIC ATTACK";
            
            // First line: Different format for basic attacks vs combo actions
            string damageText;
            if (isComboAction)
            {
                damageText = $"[{attacker.Name}] hits [{target.Name}] with {actionName} for {actualDamage} damage";
            }
            else
            {
                damageText = $"[{attacker.Name}] hits [{target.Name}] for {actualDamage} damage";
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
            
            // Add combo info to rollInfo if present
            if (comboAmplifier > 1.0)
            {
                rollInfo.Add($"Combo x{comboAmplifier:F1}");
            }
            
            // Return the roll information as a separate string
            string rollInfoText = "    (" + string.Join(" | ", rollInfo) + ")";
            
            return (damageText, rollInfoText);
        }


        /// <summary>
        /// Formats combat message with action duration timing
        /// </summary>
        /// <param name="originalMessage">The original combat message</param>
        /// <param name="actionDuration">Duration of the action in seconds</param>
        /// <returns>Formatted message with timing</returns>
        public static string FormatCombatMessage(string originalMessage, double actionDuration)
        {
            // Action duration is now handled in FormatDamageDisplay, so just return the original message
            return originalMessage;
        }

        /// <summary>
        /// Checks health milestones and returns notification messages
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="damageDealt">Amount of damage dealt</param>
        /// <returns>List of health milestone notifications</returns>
        public static List<string> CheckHealthMilestones(Entity entity, int damageDealt)
        {
            var notifications = new List<string>();
            
            if (entity is Character character)
            {
                // Check for low health warnings
                double healthPercentage = (double)character.CurrentHealth / character.GetEffectiveMaxHealth();
                
                if (healthPercentage <= 0.25 && healthPercentage > 0.1)
                {
                    notifications.Add($"[{character.Name}] is critically wounded!");
                }
                else if (healthPercentage <= 0.1)
                {
                    notifications.Add($"[{character.Name}] is near death!");
                }
            }
            else if (entity is Enemy enemy)
            {
                // Check for enemy health milestones
                double healthPercentage = (double)enemy.CurrentHealth / enemy.GetEffectiveMaxHealth();
                
                if (healthPercentage <= 0.5 && healthPercentage > 0.25)
                {
                    notifications.Add($"[{enemy.Name}] is badly wounded!");
                }
                else if (healthPercentage <= 0.25)
                {
                    notifications.Add($"[{enemy.Name}] is on the verge of defeat!");
                }
            }
            
            return notifications;
        }

        /// <summary>
        /// Formats non-attack action messages (buffs, debuffs, etc.)
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action performed</param>
        /// <param name="roll">The action roll</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <returns>Formatted non-attack action message</returns>
        public static string FormatNonAttackAction(Entity source, Entity target, Action action, int roll, int rollBonus)
        {
            string actionText = $"[{source.Name}] uses [{action.Name}] on [{target.Name}]";
            
            // Build the detailed roll information
            var rollInfo = new List<string>();
            
            // Roll information: roll + buffs - debuffs = total (only show = if there are modifiers)
            string rollDisplay = roll.ToString();
            int totalRoll = roll + rollBonus;
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
            
            // Speed information - calculate actual action speed
            if (action != null && action.Length > 0)
            {
                double actualSpeed = CalculateActualActionSpeed(source, action);
                rollInfo.Add($"speed: {actualSpeed:F1}s");
            }
            
            // Add the detailed information on the next line with indentation and parentheses
            actionText += "\n    (" + string.Join(" | ", rollInfo) + ")";
            
            return actionText;
        }

        /// <summary>
        /// Formats miss messages with detailed roll information
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action attempted</param>
        /// <param name="roll">The attack roll</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <returns>Formatted miss message</returns>
        public static string FormatMissMessage(Entity attacker, Entity target, Action action, int roll, int rollBonus)
        {
            // Check if this is a critical miss (total roll <= 1)
            int totalRoll = roll + rollBonus;
            bool isCriticalMiss = totalRoll <= 1;
            
            string missText = isCriticalMiss ? 
                $"[{attacker.Name}] CRITICAL MISS on [{target.Name}]" : 
                $"[{attacker.Name}] misses [{target.Name}]";
            
            // Build the detailed roll information
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
            
            // Speed information - calculate actual action speed
            if (action != null && action.Length > 0)
            {
                double actualSpeed = CalculateActualActionSpeed(attacker, action);
                rollInfo.Add($"speed: {actualSpeed:F1}s");
            }
            
            // Add the detailed information on the next line with indentation and parentheses
            missText += "\n    (" + string.Join(" | ", rollInfo) + ")";
            
            return missText;
        }

        /// <summary>
        /// Executes an action with UI formatting and returns both main result and status effects
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The specific action to execute</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <param name="battleNarrative">The battle narrative to add events to</param>
        /// <returns>A tuple containing the formatted main result and list of status effect messages</returns>
        public static (string mainResult, List<string> statusEffects) ExecuteActionWithUIAndStatusEffects(Entity source, Entity target, Action? action, Environment? environment = null, Action? lastPlayerAction = null, BattleNarrative? battleNarrative = null)
        {
            // Execute the action using CombatActions with the specific action
            var (mainResult, statusEffects) = ActionExecutor.ExecuteActionWithStatusEffects(source, target, environment, lastPlayerAction, action, battleNarrative);
            
            // Format the combat message with action duration
            string formattedResult = FormatCombatMessage(mainResult, action?.Length ?? 1.0);
            
            return (formattedResult, statusEffects);
        }

        /// <summary>
        /// Calculates the actual action speed by multiplying entity base speed by action length
        /// </summary>
        /// <param name="entity">The entity performing the action</param>
        /// <param name="action">The action being performed</param>
        /// <returns>The calculated action speed in seconds</returns>
        private static double CalculateActualActionSpeed(Entity entity, Action action)
        {
            // Get the entity's base attack speed
            double baseSpeed = 0;
            if (entity is Character character)
            {
                baseSpeed = character.GetTotalAttackSpeed();
            }
            else if (entity is Enemy enemy)
            {
                baseSpeed = enemy.GetTotalAttackSpeed();
            }
            else if (entity is Environment environment)
            {
                // For environments, use a default base speed
                baseSpeed = 15.0; // Same as used in CombatManager
            }
            
            // Apply critical miss penalty (doubles action speed)
            if (entity.HasCriticalMissPenalty)
            {
                baseSpeed *= 2.0;
            }
            
            // Calculate actual action speed: base speed * action length
            return baseSpeed * action.Length;
        }
    }
}
