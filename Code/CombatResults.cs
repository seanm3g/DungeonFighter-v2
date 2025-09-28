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
        /// <param name="rawDamage">Raw damage before armor</param>
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
            string damageText = $"[{attacker.Name}] hits {target.Name} with {actionName} for {actualDamage} damage!";
            
            // Add critical hit indicator
            if (roll == 20)
            {
                damageText += " (CRITICAL HIT!)";
            }
            
            // Add combo amplification info
            if (comboAmplifier > 1.0)
            {
                damageText += $" (Combo x{comboAmplifier:F1})";
            }
            
            // Add roll bonus info
            if (rollBonus > 0)
            {
                damageText += $" (Roll: {roll}+{rollBonus}={roll + rollBonus})";
            }
            
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
            // Add timing information if action duration is significant
            if (actionDuration > 1.0)
            {
                return $"{originalMessage} (Action took {actionDuration:F1}s)";
            }
            
            return originalMessage;
        }

        /// <summary>
        /// Applies text display delay based on action length
        /// </summary>
        /// <param name="actionLength">Length of the action</param>
        /// <param name="isTextDisplayed">Whether text was displayed</param>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            if (isTextDisplayed && actionLength > 0.5)
            {
                // Apply delay based on action length for better readability
                int delayMs = (int)(actionLength * 1000); // Convert to milliseconds
                delayMs = Math.Min(delayMs, 2000); // Cap at 2 seconds
                Thread.Sleep(delayMs);
            }
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
            return $"[{attacker.Name}] delivers a devastating {action.Name} to {target.Name} for {damage} damage! (CRITICAL HIT!)";
        }

        /// <summary>
        /// Formats miss messages
        /// </summary>
        /// <param name="attacker">The attacking entity</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action attempted</param>
        /// <param name="roll">The attack roll</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <returns>Formatted miss message</returns>
        public static string FormatMissMessage(Entity attacker, Entity target, Action action, int roll, int rollBonus)
        {
            string rollInfo = rollBonus > 0 ? $" (Roll: {roll}+{rollBonus}={roll + rollBonus})" : $" (Roll: {roll})";
            return $"[{attacker.Name}]'s {action.Name} misses {target.Name}!{rollInfo}";
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
                return $"[{entity.Name}] has been defeated by {killer.Name}!";
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
            return $"The {environment.Name} {effect} {target.Name}!";
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
            // Execute the action using CombatActions
            string result = CombatActions.ExecuteAction(source, target, environment, lastPlayerAction);
            
            // Apply text display delay based on action length
            ApplyTextDisplayDelay(action.Length, !string.IsNullOrEmpty(result));
            
            // Format the combat message with action duration
            return FormatCombatMessage(result, action.Length);
        }
    }
}
