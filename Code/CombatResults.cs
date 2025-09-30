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
            
            // Check if this is a critical hit (total roll of 20)
            int totalRoll = roll + rollBonus;
            bool isCritical = totalRoll == 20;
            
            // Add CRITICAL prefix to action name if it's a critical hit
            if (isCritical)
            {
                actionName = $"CRITICAL {actionName}";
            }
            
            string damageText = $"[{attacker.Name}] hits [{target.Name}] with {actionName} for {actualDamage} damage";
            
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
                rollDisplay += $" {rollBonus} = {totalRoll}"; // Already includes the minus sign
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
        /// Gets detailed armor breakdown for damage calculation
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="actualDamage">The actual damage dealt</param>
        /// <returns>Armor breakdown string</returns>
        public static string GetArmorBreakdown(Entity attacker, Entity target, int actualDamage)
        {
            int targetArmor = 0;
            if (target is Character targetCharacter)
            {
                targetArmor = targetCharacter.GetTotalArmor();
            }
            else if (target is Enemy targetEnemy)
            {
                targetArmor = targetEnemy.GetTotalArmor();
            }
            
            if (targetArmor > 0)
            {
                return $" (Armor reduced damage by {targetArmor})";
            }
            return "";
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
        /// Applies text display delay based on action length
        /// NOTE: This method is deprecated - delays are now handled by UIManager to prevent accumulation
        /// </summary>
        /// <param name="actionLength">Length of the action</param>
        /// <param name="isTextDisplayed">Whether text was displayed</param>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            // Delays are now handled by UIManager.WriteCombatLine() to prevent accumulation
            // This method is kept for compatibility but does nothing
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
        /// Gets and clears pending health notifications
        /// </summary>
        /// <returns>List of pending health notifications</returns>
        public static List<string> GetAndClearPendingHealthNotifications()
        {
            // This would integrate with a health notification system
            // For now, return empty list
            return new List<string>();
        }

        /// <summary>
        /// Formats status effect application messages
        /// </summary>
        /// <param name="target">The target entity</param>
        /// <param name="effectType">Type of effect applied</param>
        /// <param name="duration">Duration of the effect</param>
        /// <returns>Formatted effect message</returns>
        public static string FormatStatusEffectMessage(Entity target, string effectType, int duration)
        {
            return $"[{target.Name}] is {effectType.ToLower()} for {duration} turns!";
        }

        /// <summary>
        /// Formats critical hit messages
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="damage">Damage dealt</param>
        /// <param name="action">The action performed</param>
        /// <returns>Formatted critical hit message</returns>
        public static string FormatCriticalHitMessage(Entity attacker, Entity target, int damage, Action action)
        {
            return $"[{attacker.Name}] delivers a devastating {action.Name} to [{target.Name}] for {damage} damage! (CRITICAL HIT!)";
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
                rollDisplay += $" {rollBonus} = {totalRoll}"; // Already includes the minus sign
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
            string missText = $"[{attacker.Name}] misses [{target.Name}]";
            
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
                rollDisplay += $" {rollBonus} = {totalRoll}"; // Already includes the minus sign
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
        /// Formats death messages
        /// </summary>
        /// <param name="entity">The entity that died</param>
        /// <param name="killer">The entity that killed it (optional)</param>
        /// <returns>Formatted death message</returns>
        public static string FormatDeathMessage(Entity entity, Entity? killer = null)
        {
            if (killer != null)
            {
                return $"[{entity.Name}] has been defeated by [{killer.Name}]!";
            }
            else
            {
                return $"[{entity.Name}] has been defeated!";
            }
        }

        /// <summary>
        /// Formats combo progression messages
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="action">The action performed</param>
        /// <param name="comboStep">Current combo step</param>
        /// <param name="totalSteps">Total combo steps</param>
        /// <returns>Formatted combo message</returns>
        public static string FormatComboMessage(Character player, Action action, int comboStep, int totalSteps)
        {
            return $"[{player.Name}] executes {action.Name} (Combo {comboStep + 1}/{totalSteps})";
        }

        /// <summary>
        /// Formats unique action messages
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="action">The unique action performed</param>
        /// <returns>Formatted unique action message</returns>
        public static string FormatUniqueActionMessage(Character player, Action action)
        {
            return $"[{player.Name}] channels unique power and uses [{action.Name}]!";
        }

        /// <summary>
        /// Formats environmental effect messages
        /// </summary>
        /// <param name="environment">The environment</param>
        /// <param name="target">The target entity</param>
        /// <param name="effect">The environmental effect</param>
        /// <returns>Formatted environmental effect message</returns>
        public static string FormatEnvironmentalEffectMessage(Environment environment, Entity target, string effect)
        {
            return $"The [{environment.Name}] {effect} [{target.Name}]!";
        }

        /// <summary>
        /// Formats damage over time messages
        /// </summary>
        /// <param name="entity">The entity taking damage</param>
        /// <param name="damage">Amount of damage</param>
        /// <param name="effectType">Type of damage over time effect</param>
        /// <returns>Formatted damage over time message</returns>
        public static string FormatDamageOverTimeMessage(Entity entity, int damage, string effectType)
        {
            return $"[{entity.Name}] takes {damage} {effectType} damage!";
        }

        /// <summary>
        /// Formats resistance/immunity messages
        /// </summary>
        /// <param name="entity">The entity with resistance/immunity</param>
        /// <param name="effectType">Type of effect resisted</param>
        /// <param name="isImmune">Whether the entity is immune (true) or resistant (false)</param>
        /// <returns>Formatted resistance/immunity message</returns>
        public static string FormatResistanceMessage(Entity entity, string effectType, bool isImmune = false)
        {
            string resistanceType = isImmune ? "immune to" : "resistant to";
            return $"[{entity.Name}] is {resistanceType} {effectType}!";
        }

        /// <summary>
        /// Executes an action with UI formatting (delays and message formatting)
        /// This is for when you need the action result formatted for display
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The specific action to execute</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <returns>A formatted string describing the result</returns>
        public static string ExecuteActionWithUI(Entity source, Entity target, Action action, Environment? environment = null, Action? lastPlayerAction = null)
        {
            // Execute the action using CombatActions with the specific action
            string result = CombatActions.ExecuteAction(source, target, environment, lastPlayerAction, action);
            
            // Delays are now handled by UIManager.WriteCombatLine() to prevent accumulation
            
            // Format the combat message with action duration
            return FormatCombatMessage(result, action.Length);
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
            
            // Calculate actual action speed: base speed * action length
            return baseSpeed * action.Length;
        }
    }
}
