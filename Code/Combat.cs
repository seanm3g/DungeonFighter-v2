using System;
using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Legacy Combat class - now delegates to specialized combat components
    /// This class is kept for backward compatibility
    /// </summary>
    public static class Combat
    {
        // All methods now delegate to specialized components:
        // - CombatActions: Action execution and combo logic
        // - CombatCalculator: Damage calculations and hit/miss logic
        // - CombatEffects: Status effects processing
        // - CombatResults: Result formatting and narrative generation

        /// <summary>
        /// Executes multiple attacks per turn based on the source's attack speed
        /// </summary>
        public static string ExecuteMultiAttack(Entity source, Entity target, Environment? environment = null)
        {
            return CombatActions.ExecuteMultiAttack(source, target, environment);
        }

        /// <summary>
        /// Handles Divine reroll functionality for failed attacks
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="baseRoll">The original dice roll</param>
        /// <param name="totalRollBonus">Total roll bonus</param>
        /// <returns>New roll result if reroll was used, otherwise original roll</returns>
        private static (int newRoll, bool rerollUsed) HandleDivineReroll(Character player, int baseRoll, int totalRollBonus)
        {
            // Check if player has Divine reroll charges available
            if (player.GetRemainingRerollCharges() > 0)
            {
                // Use a Divine reroll charge
                if (player.UseRerollCharge())
                {
                    // Roll a new d20 and apply bonuses
                    int newBaseRoll = Dice.Roll(1, 20);
                    int newAttackRoll = newBaseRoll + totalRollBonus;
                    int newCappedRoll = Math.Min(newAttackRoll, 20);
                    
                    CombatLogger.Log($"[{player.Name}] uses Divine Reroll! ({player.GetRemainingRerollCharges()} charges remaining)");
                    
                    return (newCappedRoll, true);
                }
            }
            
            // No reroll available or used
            return (baseRoll + totalRollBonus, false);
        }

        /// <summary>
        /// Executes a combat action from the source entity to the target entity.
        /// Now delegates to CombatActions for unified action execution.
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The entity receiving the action</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="lastPlayerAction">The last player action for DEJA VU functionality</param>
        /// <returns>A string describing the result of the action (or empty if using narrative mode)</returns>
        public static string ExecuteAction(Entity source, Entity target, Environment? environment = null, Action? lastPlayerAction = null)
        {
            // Delegate to CombatActions for unified action execution
            return CombatActions.ExecuteAction(source, target, environment, lastPlayerAction);
        }

        /// <summary>
        /// Determines the appropriate effect type for actions that cause damage over time
        /// </summary>
        private static string GetEffectType(Action action)
        {
            string actionName = action.Name.ToLower();
            
            // Check for poison-based actions
            if (actionName.Contains("poison") || actionName.Contains("venom") || actionName.Contains("bite"))
            {
                return "poisoned";
            }
            
            // Check for fire/heat-based actions
            if (actionName.Contains("acid") || actionName.Contains("lava") || actionName.Contains("fire") || 
                actionName.Contains("flame") || actionName.Contains("burn") || actionName.Contains("heat"))
            {
                return "burning";
            }
            
            // Default to bleeding for other causes
            return "bleeding";
        }

        /// <summary>
        /// Unified damage calculation system for both players and enemies
        /// Now delegates to CombatCalculator
        /// </summary>
        public static int CalculateDamage(Entity attacker, Entity target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true)
        {
            return CombatCalculator.CalculateDamage(attacker, target, action, comboAmplifier, damageMultiplier, rollBonus, roll, showWeakenedMessage);
        }

        /// <summary>
        /// Applies intelligent delay system for text display
        /// Now delegates to CombatResults
        /// </summary>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            CombatResults.ApplyTextDisplayDelay(actionLength, isTextDisplayed);
        }

        /// <summary>
        /// Formats damage display - returns just the final damage for main line
        /// Now delegates to CombatResults
        /// </summary>
        public static string FormatDamageDisplay(Entity attacker, Entity target, int rawDamage, int actualDamage, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
        {
            return CombatResults.FormatDamageDisplay(attacker, target, rawDamage, actualDamage, action, comboAmplifier, damageMultiplier, rollBonus, roll);
        }

        /// <summary>
        /// Gets armor breakdown for inline display
        /// Now delegates to CombatResults
        /// </summary>
        public static string GetArmorBreakdown(Entity attacker, Entity target, int actualDamage)
        {
            return CombatResults.GetArmorBreakdown(attacker, target, actualDamage);
        }


        /// <summary>
        /// Applies environmental debuffs (weaken, slow) with proper turn calculation and 0-turn handling
        /// Now delegates to CombatEffects
        /// </summary>
        /// <param name="source">The entity applying the debuff</param>
        /// <param name="target">The entity receiving the debuff</param>
        /// <param name="action">The action causing the debuff</param>
        /// <param name="debuffType">Type of debuff ("weaken" or "slow")</param>
        /// <param name="results">List to add result messages to</param>
        /// <returns>True if debuff was applied, false if 0 turns</returns>
        private static bool ApplyEnvironmentalDebuff(Entity source, Entity target, Action action, string debuffType, List<string> results)
        {
            return CombatEffects.ApplyEnvironmentalDebuff(source, target, action, debuffType, results);
        }


        /// <summary>
        /// Checks and applies bleed chance from modifications after damage is dealt
        /// Now delegates to CombatEffects
        /// </summary>
        public static void CheckAndApplyBleedChance(Character attacker, Entity target)
        {
            var results = new List<string>();
            CombatEffects.CheckAndApplyBleedChance(attacker, target, results);
        }


        /// <summary>
        /// Executes an area of effect action from the source entity to all targets in the room
        /// Now delegates to CombatActions
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="targets">List of all targets in the room</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <param name="selectedAction">Optional pre-selected action to use instead of selecting a new one</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteAreaOfEffectAction(Entity source, List<Entity> targets, Environment? environment = null, Action? selectedAction = null)
        {
            return CombatActions.ExecuteAreaOfEffectAction(source, targets, environment, selectedAction);
        }

        
        /// <summary>
        /// Formats combat messages to separate action from roll details
        /// </summary>
        /// <param name="originalMessage">The original combat message</param>
        /// <param name="actionDuration">The duration of the action</param>
        /// <returns>Formatted message with roll details on separate line</returns>
        private static string FormatCombatMessage(string originalMessage, double actionDuration)
        {
            // Split the message into lines to handle status effects and roll details
            string[] lines = originalMessage.Split('\n');
            string mainMessage = lines[0];
            string? statusEffectMessage = null;
            string? existingRollDetails = null;
            
            // Check if there are additional lines with roll details or status effects
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("(") && line.EndsWith(")"))
                {
                    // This is roll details - extract content without parentheses
                    existingRollDetails = line.Substring(1, line.Length - 2);
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    // This is a status effect message
                    statusEffectMessage = line;
                }
            }
            
            // Look for patterns like "(Rolled X, combo step Y, amplification, CRITICAL HIT)" in main message
            var match = System.Text.RegularExpressions.Regex.Match(mainMessage, @"^(.*?)\s*\((.*)\)$");
            
            string rollDetails = "";
            if (match.Success)
            {
                // Extract roll details from main message
                rollDetails = match.Groups[2].Value.Trim();
                mainMessage = match.Groups[1].Value.Trim();
            }
            
            // Use existing roll details if found, otherwise use main message roll details
            string finalRollDetails = !string.IsNullOrEmpty(existingRollDetails) ? existingRollDetails : rollDetails;
            
            // Add duration to roll details if present
            if (actionDuration > 0.0)
            {
                if (!string.IsNullOrEmpty(finalRollDetails))
                {
                    finalRollDetails += $" | Duration: {actionDuration:F2}s";
                }
                else
                {
                    finalRollDetails = $"Duration: {actionDuration:F2}s";
                }
            }
            
            // Build the result with roll details on indented line
            string result = mainMessage;
            if (!string.IsNullOrEmpty(finalRollDetails))
            {
                result += $"\n        ({finalRollDetails})";
            }
            
            // Add status effect message indented if present
            if (!string.IsNullOrEmpty(statusEffectMessage))
            {
                result += $"\n        {statusEffectMessage}";
            }
            
            return result;
        }
        
        
        /// <summary>
        /// Parses the roll result from a combat result string
        /// </summary>
        /// <param name="resultString">The combat result string</param>
        /// <returns>The roll result, or 0 if not found</returns>
        private static int ParseRollResultFromString(string resultString)
        {
            // Look for patterns like "Rolled 16 + 4 = 20" or "Rolled 5"
            var match = System.Text.RegularExpressions.Regex.Match(resultString, @"Rolled\s+(\d+)(?:\s*\+\s*\d+\s*=\s*(\d+))?");
            if (match.Success)
            {
                // If there's a total (like "16 + 4 = 20"), use the total
                if (match.Groups[2].Success)
                {
                    return int.Parse(match.Groups[2].Value);
                }
                // Otherwise use the base roll
                else
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            return 0; // Default to 0 if no roll found
        }

        // Note: GetNextEntityToAct and IsEntityReady methods moved to CombatManager
    }
}