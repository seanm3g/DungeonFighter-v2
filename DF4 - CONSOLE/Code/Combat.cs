using System;
using System.Threading;

namespace RPGGame
{
    public static class Combat
    {
        private static BattleNarrative? currentBattleNarrative;
        private static Action? lastPlayerAction = null; // Track the last action for DEJA VU

        public static void StartBattleNarrative(string playerName, string enemyName, string locationName, int playerHealth, int enemyHealth)
        {
            currentBattleNarrative = new BattleNarrative(playerName, enemyName, locationName, playerHealth, enemyHealth);
            lastPlayerAction = null; // Reset last action for new battle
        }

        public static void EndBattleNarrative()
        {
            if (currentBattleNarrative != null)
            {
                currentBattleNarrative = null;
            }
        }

        public static BattleNarrative? GetCurrentBattleNarrative()
        {
            return currentBattleNarrative;
        }

        /// <summary>
        /// Executes a combat action from the source entity to the target entity.
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The entity receiving the action</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <returns>A string describing the result of the action (or empty if using narrative mode)</returns>
        public static string ExecuteAction(Entity source, Entity target, Environment? environment = null)
        {
            // Only run combo logic for the player (not for enemies or other entities)
            if (source is Character player && !(player is Enemy))
            {
                // Get the current action to check for roll bonuses
                var comboActions = player.GetComboActions();
                int actionIndex = player.ComboStep % comboActions.Count;
                var currentAction = comboActions.Count > 0 ? comboActions[actionIndex] : null;
                
                // Apply roll bonus from the action
                int rollBonus = currentAction?.RollBonus ?? 0;
                
                // Implement the 1d20 attack system with bonus
                int baseRoll = Dice.Roll(1, 20);
                int attackRoll = baseRoll + rollBonus;
                
                if (attackRoll >= 1 && attackRoll <= 5)
                {
                    // Miss - combo resets
                    player.ResetCombo();
                    string rollText = rollBonus != 0 ? $"{baseRoll} + {rollBonus} = {attackRoll}" : attackRoll.ToString();
                    return $"[{player.Name}] attempts to attack but misses. (Rolled {rollText})";
                }
                else if (attackRoll >= 6 && attackRoll <= 13)
                {
                    // Basic attack - combo resets
                    player.ResetCombo();
                    int basicDamage = CalculateDamage(player, target, null, 1.0, 1.0, rollBonus);
                    if (target is Character basicTargetChar)
                    {
                        basicTargetChar.TakeDamage(basicDamage);
                    }
                    string rollText = rollBonus != 0 ? $"{baseRoll} + {rollBonus} = {attackRoll}" : attackRoll.ToString();
                    return $"[{player.Name}] uses [Basic Attack] on [{target.Name}]: deals {basicDamage} damage. (Rolled {rollText})";
                }
                else if (attackRoll >= 14 && attackRoll <= 20)
                {
                    // Move in combo
                    if (comboActions.Count == 0)
                    {
                        // No combo actions available, do basic attack instead
                        int fallbackDamage = CalculateDamage(player, target, null, 1.0, 1.0, rollBonus);
                        if (target is Character fallbackTargetChar)
                        {
                            fallbackTargetChar.TakeDamage(fallbackDamage);
                        }
                        string fallbackRollText = rollBonus != 0 ? $"{baseRoll} + {rollBonus} = {attackRoll}" : attackRoll.ToString();
                        return $"[{player.Name}] uses [Basic Attack] on [{target.Name}]: deals {fallbackDamage} damage. (Rolled {fallbackRollText}, no combo actions available)";
                    }
                    
                    // Execute combo action
                    var action = comboActions[actionIndex];
                    player.ComboStep++;
                    
                    // Handle DEJA VU - repeat the last action
                    if (action.Name == "DEJA VU" && action.RepeatLastAction)
                    {
                        if (lastPlayerAction != null)
                        {
                            // Execute the last action instead
                            int dejaVuDamage = CalculateDamage(player, target, lastPlayerAction, 1.0, 1.0, rollBonus);
                            if (target is Character dejaVuTargetChar)
                            {
                                dejaVuTargetChar.TakeDamage(dejaVuDamage);
                            }
                            string dejaVuRollText = rollBonus != 0 ? $"{baseRoll} + {rollBonus} = {attackRoll}" : attackRoll.ToString();
                            return $"[{player.Name}] uses [DEJA VU] to repeat [{lastPlayerAction.Name}] on [{target.Name}]: deals {dejaVuDamage} damage. (Rolled {dejaVuRollText}, combo step {actionIndex + 1})";
                        }
                        else
                        {
                            // No previous action to repeat, do basic damage
                            int fallbackDamage = CalculateDamage(player, target, action, 1.0, 1.0, rollBonus);
                            if (target is Character fallbackTargetChar)
                            {
                                fallbackTargetChar.TakeDamage(fallbackDamage);
                            }
                            string dejaVuFallbackRollText = rollBonus != 0 ? $"{baseRoll} + {rollBonus} = {attackRoll}" : attackRoll.ToString();
                            return $"[{player.Name}] uses [DEJA VU] but has no previous action to repeat on [{target.Name}]: deals {fallbackDamage} damage. (Rolled {dejaVuFallbackRollText}, combo step {actionIndex + 1})";
                        }
                    }
                    
                    // Store this action as the last action for future DEJA VU
                    lastPlayerAction = action;
                    
                    int comboDamage = CalculateDamage(player, target, action, 1.0, 1.0, rollBonus);
                    if (target is Character comboTargetChar)
                    {
                        comboTargetChar.TakeDamage(comboDamage);
                    }
                    string comboRollText = rollBonus != 0 ? $"{baseRoll} + {rollBonus} = {attackRoll}" : attackRoll.ToString();
                    return $"[{player.Name}] uses [{action.Name}] on [{target.Name}]: deals {comboDamage} damage. (Rolled {comboRollText}, combo step {actionIndex + 1})";
                }
                
                // Fallback (should never reach here)
                return $"[{player.Name}] attempts to attack but something went wrong. (Rolled {attackRoll})";
            }
            
            // For non-player entities (enemies, etc.), use the existing system
            var selectedAction = source.SelectAction();
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }

            int baseEffect = selectedAction.BaseValue > 0 ? selectedAction.BaseValue : 1;
            double effect = baseEffect * selectedAction.DamageMultiplier;
            double effectBeforeEnv = effect;
            string envMsg = "";
            if (environment != null)
            {
                effect = environment.ApplyPassiveEffect(effect);
                if (effect != effectBeforeEnv)
                {
                    envMsg = $" (Environment modified: {effectBeforeEnv:F1} → {effect:F1})";
                }
            }
            
            int finalEffect = (int)Math.Round(effect);

            // Apply the action effect
            switch (selectedAction.Type)
            {
                case ActionType.Attack:
                    if (target is Character character)
                    {
                        character.TakeDamage(finalEffect);
                    }
                    break;
                case ActionType.Heal:
                    if (target is Character healTarget)
                    {
                        healTarget.Heal(finalEffect);
                    }
                    break;
                case ActionType.Buff:
                    // Buff effects would be implemented here
                    break;
                case ActionType.Debuff:
                    // Debuff effects would be implemented here
                    break;
                case ActionType.Interact:
                    // Interaction effects would be implemented here
                    break;
                case ActionType.Move:
                    // Movement effects would be implemented here
                    break;
                case ActionType.UseItem:
                    // Item use effects would be implemented here
                    break;
            }

            if (selectedAction.Cooldown > 0)
                selectedAction.ResetCooldown();
                
            string result = $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: ";
            switch (selectedAction.Type)
            {
                case ActionType.Attack:
                    result += $"deals {finalEffect} damage{envMsg}.";
                    break;
                case ActionType.Heal:
                    result += $"heals {finalEffect} health{envMsg}.";
                    break;
                case ActionType.Buff:
                    result += $"applies {selectedAction.Name} effect{envMsg}.";
                    break;
                case ActionType.Debuff:
                    result += $"applies {selectedAction.Name} effect{envMsg}.";
                    break;
                case ActionType.Interact:
                    result += $"interacts with {target.Name}{envMsg}.";
                    break;
                case ActionType.Move:
                    result += $"moves{envMsg}.";
                    break;
                case ActionType.UseItem:
                    result += $"uses an item{envMsg}.";
                    break;
            }
            
            return result;
        }

        /// <summary>
        /// Unified damage calculation system for both players and enemies
        /// </summary>
        public static int CalculateDamage(Entity attacker, Entity target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0)
        {
            // Get attacker's weapon damage and stats
            int weaponDamage = 0;
            int strength = 0;
            int highestAttribute = 0;
            
            if (attacker is Character character)
            {
                if (character.Weapon is WeaponItem weapon)
                {
                    weaponDamage = weapon.GetTotalDamage();
                }
                strength = character.Strength;
                // Find highest attribute (STR, AGI, TEC, INT)
                highestAttribute = Math.Max(Math.Max(character.Strength, character.Agility), 
                                          Math.Max(character.Technique, character.Intelligence));
            }
            else if (attacker is Enemy enemy)
            {
                weaponDamage = 0; // Enemies don't have weapons, just base damage
                strength = enemy.Strength;
                highestAttribute = strength; // For enemies, use strength as highest
            }
            
            // Calculate base damage: STR + highest attribute + weapon damage
            int baseDamage = strength + highestAttribute + weaponDamage;
            
            // Apply roll bonus to damage (roll bonus affects base damage)
            baseDamage += rollBonus;
            
            // Apply action multiplier if provided
            if (action != null)
            {
                baseDamage = (int)(baseDamage * action.DamageMultiplier);
            }
            
            // Apply combo amplifier
            baseDamage = (int)(baseDamage * comboAmplifier);
            
            // Apply damage multiplier
            baseDamage = (int)(baseDamage * damageMultiplier);
            
            // Get target's armor
            int armor = 0;
            if (target is Character targetChar)
            {
                if (targetChar.Head is HeadItem head) armor += head.GetTotalArmor();
                if (targetChar.Body is ChestItem chest) armor += chest.GetTotalArmor();
                if (targetChar.Feet is FeetItem feet) armor += feet.GetTotalArmor();
            }
            else if (target is Enemy targetEnemy)
            {
                armor = targetEnemy.Armor; // Use enemy's scaled armor value
            }
            
            // Calculate final damage (minimum 1)
            int finalDamage = Math.Max(1, baseDamage - armor);
            
            return finalDamage;
        }

        /// <summary>
        /// Applies intelligent delay system for text display
        /// </summary>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            var settings = GameSettings.Instance;
            if (settings.EnableTextDisplayDelays && isTextDisplayed)
            {
                // Calculate delay based on action length and combat speed
                double baseDelayMs = actionLength * 400; // 400ms per action length unit
                double adjustedDelayMs = baseDelayMs / settings.CombatSpeed;
                int finalDelayMs = Math.Max(50, (int)adjustedDelayMs); // Minimum 50ms delay
                
                Thread.Sleep(finalDelayMs);
            }
        }

        /// <summary>
        /// Check if we're in narrative mode
        /// </summary>
        public static bool IsInNarrativeMode()
        {
            return currentBattleNarrative != null;
        }

        /// <summary>
        /// Add a battle event to the current narrative
        /// </summary>
        public static void AddBattleEvent(BattleEvent battleEvent)
        {
            if (currentBattleNarrative != null)
            {
                // BattleNarrative doesn't have AddEvent method, so we'll just ignore for now
                // This maintains compatibility with existing code
            }
        }

        /// <summary>
        /// Executes an area of effect action from the source entity to all targets in the room
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="targets">List of all targets in the room</param>
        /// <param name="environment">The environment affecting the action</param>
        /// <returns>A string describing the result of the action</returns>
        public static string ExecuteAreaOfEffectAction(Entity source, List<Entity> targets, Environment? environment = null)
        {
            var selectedAction = source.SelectAction();
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }

            int baseEffect = selectedAction.BaseValue > 0 ? selectedAction.BaseValue : 1;
            double effect = baseEffect * selectedAction.DamageMultiplier;
            double effectBeforeEnv = effect;
            string envMsg = "";
            if (environment != null)
            {
                effect = environment.ApplyPassiveEffect(effect);
                if (effect != effectBeforeEnv)
                {
                    envMsg = $" (Environment modified: {effectBeforeEnv:F1} → {effect:F1})";
                }
            }
            
            int finalEffect = (int)Math.Round(effect);
            var results = new List<string>();

            // Apply the action effect to all targets
            foreach (var target in targets)
            {
                // Check if target is alive (only Character and Enemy have IsAlive property)
                bool isAlive = true;
                if (target is Character character)
                    isAlive = character.IsAlive;
                else if (target is Enemy enemy)
                    isAlive = enemy.IsAlive;
                
                if (!isAlive) continue; // Skip dead targets

                switch (selectedAction.Type)
                {
                    case ActionType.Attack:
                        if (target is Character attackTarget)
                        {
                            attackTarget.TakeDamage(finalEffect);
                            results.Add($"[{target.Name}] takes {finalEffect} damage");
                        }
                        break;
                    case ActionType.Heal:
                        if (target is Character healTarget)
                        {
                            healTarget.Heal(finalEffect);
                            results.Add($"[{target.Name}] heals {finalEffect} health");
                        }
                        break;
                    case ActionType.Debuff:
                        // Debuff effects would be implemented here
                        results.Add($"[{target.Name}] is affected by {selectedAction.Name}");
                        break;
                    case ActionType.Buff:
                        // Buff effects would be implemented here
                        results.Add($"[{target.Name}] is buffed by {selectedAction.Name}");
                        break;
                }
            }

            if (selectedAction.Cooldown > 0)
                selectedAction.ResetCooldown();

            string result = $"[{source.Name}] uses [{selectedAction.Name}]: ";
            if (results.Count > 0)
            {
                result += string.Join(", ", results) + envMsg + ".";
            }
            else
            {
                result += "no effect.";
            }
            
            return result;
        }
    }
}