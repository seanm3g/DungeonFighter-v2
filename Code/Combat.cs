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
        /// Executes multiple attacks per turn based on the source's attack speed
        /// </summary>
        /// <param name="source">The entity performing the attacks</param>
        /// <param name="target">The entity receiving the attacks</param>
        /// <param name="environment">The environment affecting the attacks</param>
        /// <returns>A string describing the results of all attacks</returns>
        public static string ExecuteMultiAttack(Entity source, Entity target, Environment? environment = null)
        {
            if (source is Character character)
            {
                int attacksPerTurn = character.GetAttacksPerTurn();
                var results = new List<string>();
                
                for (int i = 0; i < attacksPerTurn; i++)
                {
                    // Check if target is alive (only Character and Enemy have IsAlive property)
                    bool isAlive = true;
                    if (target is Character targetCharacter)
                        isAlive = targetCharacter.IsAlive;
                    else if (target is Enemy targetEnemy)
                        isAlive = targetEnemy.IsAlive;
                    
                    if (!isAlive) break; // Stop if target is dead
                    
                    string result = ExecuteAction(source, target, environment);
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
                return ExecuteAction(source, target, environment);
            }
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
                int actionIndex = comboActions.Count > 0 ? player.ComboStep % comboActions.Count : 0;
                var currentAction = comboActions.Count > 0 ? comboActions[actionIndex] : null;
                
                // Apply roll bonus from the action and Intelligence
                int actionRollBonus = currentAction?.RollBonus ?? 0;
                
                // Check for different combo scaling types
                if (currentAction != null)
                {
                    if (currentAction.Tags.Contains("comboScaling"))
                    {
                        // Scale with combo length (METEOR, SHADOW STRIKE)
                        actionRollBonus = player.ComboSequence.Count;
                    }
                    else if (currentAction.Tags.Contains("comboStepScaling"))
                    {
                        // Scale with combo step position (HEROIC STRIKE)
                        actionRollBonus = (player.ComboStep % player.ComboSequence.Count) + 1;
                    }
                    else if (currentAction.Tags.Contains("comboAmplificationScaling"))
                    {
                        // Scale with combo amplification (BERSERKER RAGE)
                        actionRollBonus = (int)(player.GetComboAmplifier() * 2); // +2 per amplification level
                    }
                }
                
                int intelligenceRollBonus = player.GetIntelligenceRollBonus();
                int totalRollBonus = actionRollBonus + intelligenceRollBonus;
                
                // Implement the 1d20 attack system with bonus
                int baseRoll = Dice.Roll(1, 20);
                int attackRoll = baseRoll + totalRollBonus;
                
                // Cap attack roll at 20 for critical hit calculation
                int cappedRoll = Math.Min(attackRoll, 20);
                
                var rollTuning = TuningConfig.Instance;
                
                if (cappedRoll >= rollTuning.RollSystem.MissThreshold.Min && cappedRoll <= rollTuning.RollSystem.MissThreshold.Max)
                {
                    // Miss - combo resets
                    player.ResetCombo();
                    string rollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                    return $"[{player.Name}] attempts to attack but misses. (Rolled {rollText})";
                }
                else if (cappedRoll >= rollTuning.RollSystem.BasicAttackThreshold.Min && cappedRoll <= rollTuning.RollSystem.BasicAttackThreshold.Max)
                {
                    // Basic attack - combo resets
                    player.ResetCombo();
                    int basicDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll);
                    if (target is Character basicTargetChar)
                    {
                        basicTargetChar.TakeDamage(basicDamage);
                    }
                    string rollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                    return $"[{player.Name}] uses [Basic Attack] on [{target.Name}]: deals {basicDamage} damage. (Rolled {rollText})";
                }
                else if (cappedRoll >= rollTuning.RollSystem.ComboThreshold.Min && cappedRoll <= rollTuning.RollSystem.ComboThreshold.Max)
                {
                    // Check if this is a critical hit (original roll >= 20)
                    if (attackRoll >= rollTuning.RollSystem.CriticalThreshold)
                    {
                        // Critical hit - execute combo action with critical damage
                        if (comboActions.Count == 0)
                        {
                            // No combo actions available, do critical basic attack
                            int criticalDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll);
                            if (target is Character criticalTargetChar)
                            {
                                criticalTargetChar.TakeDamage(criticalDamage);
                            }
                            string criticalRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            return $"[{player.Name}] uses [Critical Basic Attack] on [{target.Name}]: deals {criticalDamage} damage. (Rolled {criticalRollText}, CRITICAL HIT!)";
                        }
                        
                        // Execute critical combo action
                        var criticalAction = comboActions[actionIndex];
                        player.ComboStep++;
                        
                        // Calculate exponential combo amplification: amp^comboStep
                        double criticalBaseComboAmp = player.GetComboAmplifier();
                        double criticalExponentialComboAmp = Math.Pow(criticalBaseComboAmp, actionIndex + 1);
                        
                        int criticalComboDamage = CalculateDamage(player, target, criticalAction, criticalExponentialComboAmp, 1.0, totalRollBonus, attackRoll);
                        if (target is Character criticalComboTargetChar)
                        {
                            criticalComboTargetChar.TakeDamage(criticalComboDamage);
                        }
                        string criticalComboRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                        return $"[{player.Name}] uses [Critical {criticalAction.Name}] on [{target.Name}]: deals {criticalComboDamage} damage. (Rolled {criticalComboRollText}, combo step {actionIndex + 1}, {criticalExponentialComboAmp:F2}x amplification, CRITICAL HIT!)";
                    }
                    
                    // Move in combo (non-critical)
                    if (comboActions.Count == 0)
                    {
                        // No combo actions available, do basic attack instead
                        int fallbackDamage = CalculateDamage(player, target, null, 1.0, 1.0, totalRollBonus, attackRoll);
                        if (target is Character fallbackTargetChar)
                        {
                            fallbackTargetChar.TakeDamage(fallbackDamage);
                        }
                        string fallbackRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
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
                            // Calculate exponential combo amplification for DEJA VU: amp^comboStep
                            double dejaVuBaseAmp = player.GetComboAmplifier();
                            double dejaVuExponentialAmp = Math.Pow(dejaVuBaseAmp, actionIndex + 1);
                            
                            // Execute the last action instead
                            int dejaVuDamage = CalculateDamage(player, target, lastPlayerAction, dejaVuExponentialAmp, 1.0, totalRollBonus, attackRoll);
                            if (target is Character dejaVuTargetChar)
                            {
                                dejaVuTargetChar.TakeDamage(dejaVuDamage);
                            }
                            string dejaVuRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            return $"[{player.Name}] uses [DEJA VU] to repeat [{lastPlayerAction.Name}] on [{target.Name}]: deals {dejaVuDamage} damage. (Rolled {dejaVuRollText}, combo step {actionIndex + 1}, {dejaVuExponentialAmp:F2}x amplification)";
                        }
                        else
                        {
                            // Calculate exponential combo amplification for fallback: amp^comboStep
                            double fallbackBaseAmp = player.GetComboAmplifier();
                            double fallbackExponentialAmp = Math.Pow(fallbackBaseAmp, actionIndex + 1);
                            
                            // No previous action to repeat, do basic damage
                            int fallbackDamage = CalculateDamage(player, target, action, fallbackExponentialAmp, 1.0, totalRollBonus, attackRoll);
                            if (target is Character fallbackTargetChar)
                            {
                                fallbackTargetChar.TakeDamage(fallbackDamage);
                            }
                            string dejaVuFallbackRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                            return $"[{player.Name}] uses [DEJA VU] but has no previous action to repeat on [{target.Name}]: deals {fallbackDamage} damage. (Rolled {dejaVuFallbackRollText}, combo step {actionIndex + 1}, {fallbackExponentialAmp:F2}x amplification)";
                        }
                    }
                    
                    // Store this action as the last action for future DEJA VU
                    lastPlayerAction = action;
                    
                    // Apply enemy roll penalty if this action has one
                    if (action.EnemyRollPenalty > 0)
                    {
                        player.EnemyRollPenalty = action.EnemyRollPenalty;
                        player.EnemyRollPenaltyTurns = 1; // Apply to next enemy action
                    }
                    
                    // Calculate exponential combo amplification: amp^comboStep
                    double baseComboAmp = player.GetComboAmplifier();
                    double exponentialComboAmp = Math.Pow(baseComboAmp, actionIndex + 1);
                    
                    int comboDamage = CalculateDamage(player, target, action, exponentialComboAmp, 1.0, totalRollBonus, attackRoll);
                    if (target is Character comboTargetChar)
                    {
                        comboTargetChar.TakeDamage(comboDamage);
                    }
                    string comboRollText = totalRollBonus != 0 ? $"{baseRoll} + {totalRollBonus} = {attackRoll}" : attackRoll.ToString();
                    return $"[{player.Name}] uses [{action.Name}] on [{target.Name}]: deals {comboDamage} damage. (Rolled {comboRollText}, combo step {actionIndex + 1}, {exponentialComboAmp:F2}x amplification)";
                }
                
                // This should never be reached with capped roll logic
                return $"[{player.Name}] attempts to attack but something went wrong. (Rolled {attackRoll})";
            }
            
            // For non-player entities (enemies, etc.), use the same 1d20 system
            var selectedAction = source.SelectAction();
            if (selectedAction == null)
            {
                return $"[{source.Name}] has no actions available.";
            }

            // Apply roll bonus from the action
            int enemyRollBonus = selectedAction.RollBonus;
            
            // Apply enemy roll penalty from player actions (like Arcane Shield)
            if (target is Character playerTarget && playerTarget.EnemyRollPenaltyTurns > 0)
            {
                enemyRollBonus -= playerTarget.EnemyRollPenalty;
                playerTarget.EnemyRollPenaltyTurns--;
                if (playerTarget.EnemyRollPenaltyTurns <= 0)
                {
                    playerTarget.EnemyRollPenalty = 0;
                }
            }
            
            // Implement the 1d20 attack system with bonus (same as player)
            int enemyBaseRoll = Dice.Roll(1, 20);
            int enemyAttackRoll = enemyBaseRoll + enemyRollBonus;
            
            // Cap attack roll at 20 for critical hit calculation
            int enemyCappedRoll = Math.Min(enemyAttackRoll, 20);
            
            var enemyTuning = TuningConfig.Instance;
            
            if (enemyCappedRoll >= enemyTuning.RollSystem.MissThreshold.Min && enemyCappedRoll <= enemyTuning.RollSystem.MissThreshold.Max)
            {
                // Miss
                string rollText = enemyRollBonus != 0 ? $"{enemyBaseRoll} + {enemyRollBonus} = {enemyAttackRoll}" : enemyAttackRoll.ToString();
                return $"[{source.Name}] attempts to attack but misses. (Rolled {rollText})";
            }
            else if (enemyCappedRoll >= enemyTuning.RollSystem.BasicAttackThreshold.Min && enemyCappedRoll <= enemyTuning.RollSystem.BasicAttackThreshold.Max)
            {
                // Basic attack
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
                string rollText = enemyRollBonus != 0 ? $"{enemyBaseRoll} + {enemyRollBonus} = {enemyAttackRoll}" : enemyAttackRoll.ToString();
                
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
                }
                
                return $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: deals {finalEffect} damage{envMsg}. (Rolled {rollText})";
            }
            else if (enemyCappedRoll >= enemyTuning.RollSystem.ComboThreshold.Min && enemyCappedRoll <= enemyTuning.RollSystem.ComboThreshold.Max)
            {
                // Successful attack (14+ threshold)
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
                string rollText = enemyRollBonus != 0 ? $"{enemyBaseRoll} + {enemyRollBonus} = {enemyAttackRoll}" : enemyAttackRoll.ToString();
                
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
                
                return $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: deals {finalEffect} damage{envMsg}. (Rolled {rollText})";
            }
            
            // This should never be reached with capped roll logic
            return $"[{source.Name}] attempts to attack but something went wrong. (Rolled {enemyAttackRoll})";
        }

        /// <summary>
        /// Unified damage calculation system for both players and enemies
        /// </summary>
        public static int CalculateDamage(Entity attacker, Entity target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0)
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
                strength = character.GetEffectiveStrength();
                // Find highest attribute (STR, AGI, TEC, INT)
                highestAttribute = Math.Max(Math.Max(character.GetEffectiveStrength(), character.GetEffectiveAgility()), 
                                          Math.Max(character.GetEffectiveTechnique(), character.GetEffectiveIntelligence()));
            }
            else if (attacker is Enemy enemy)
            {
                weaponDamage = 0; // Enemies don't have weapons, just base damage
                strength = enemy.Strength;
                highestAttribute = strength; // For enemies, use strength as highest
            }
            
            // Calculate base damage: STR + highest attribute + weapon damage + equipment damage bonus
            int baseDamage = strength + highestAttribute + weaponDamage;
            
            // Add equipment damage bonuses for characters
            if (attacker is Character)
            {
                baseDamage += ((Character)attacker).GetEquipmentDamageBonus();
            }
            
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
            
            // Calculate final damage with configurable minimum
            var tuning = TuningConfig.Instance;
            int finalDamage = Math.Max(tuning.Combat.MinimumDamage, baseDamage - armor);
            
            // Critical hit based on tuning config
            if (roll >= tuning.Combat.CriticalHitThreshold)
            {
                finalDamage = (int)(finalDamage * tuning.Combat.CriticalHitMultiplier);
            }
            
            return finalDamage;
        }

        /// <summary>
        /// Applies intelligent delay system for text display
        /// </summary>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            var tuning = TuningConfig.Instance;
            if (tuning.UI.EnableTextDelays && isTextDisplayed)
            {
                // Calculate delay based on action length and combat speed using tuning config
                double baseDelayMs = actionLength * tuning.UI.BaseDelayPerAction;
                double adjustedDelayMs = baseDelayMs / tuning.UI.CombatSpeedMultiplier;
                int finalDelayMs = Math.Max(tuning.UI.MinimumDelay, (int)adjustedDelayMs);
                
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