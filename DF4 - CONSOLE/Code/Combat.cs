namespace RPGGame
{
    public class Combat
    {
        private static BattleNarrative? currentBattleNarrative;

        /// <summary>
        /// Starts a new battle narrative session
        /// </summary>
        public static void StartBattleNarrative(string playerName, string enemyName, string environmentName = "", int playerHealth = 0, int enemyHealth = 0)
        {
            currentBattleNarrative = new BattleNarrative(playerName, enemyName, environmentName, playerHealth, enemyHealth);
        }

        /// <summary>
        /// Checks if we're currently in narrative mode
        /// </summary>
        public static bool IsInNarrativeMode()
        {
            return currentBattleNarrative != null;
        }

        /// <summary>
        /// Adds a battle event to the current narrative
        /// </summary>
        public static void AddBattleEvent(BattleEvent evt)
        {
            if (currentBattleNarrative != null)
            {
                currentBattleNarrative.AddEvent(evt);
            }
        }

        /// <summary>
        /// Ends the current battle narrative and returns the description
        /// </summary>
        public static string EndBattleNarrative()
        {
            if (currentBattleNarrative == null)
                return "No battle narrative to end.";
            
            currentBattleNarrative.EndBattle();
            string narrative = currentBattleNarrative.GenerateNarrative();
            currentBattleNarrative = null;
            return narrative;
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
                player.UpdateComboBonus();
                var comboActions = player.GetComboActions();
                if (comboActions.Count == 0)
                {
                    if (currentBattleNarrative != null)
                    {
                        var evt = new BattleEvent
                        {
                            Actor = player.Name,
                            Target = target.Name,
                            Action = "No Action",
                            IsSuccess = false
                        };
                        currentBattleNarrative.AddEvent(evt);
                        return "";
                    }
                    string msg = "No combo actions available.";
                    return msg;
                }
                // Track last action index to prevent repeats
                if (player is IComboMemory comboMemory)
                {
                    // Prevent repeating the same action unless combo just reset
                    int currentActionIdx = player.ComboStep % comboActions.Count;
                    if (comboMemory.LastComboActionIdx == currentActionIdx && player.ComboStep != 0)
                    {
                        // Skip to next action if repeat detected
                        currentActionIdx = (currentActionIdx + 1) % comboActions.Count;
                        player.ComboStep = currentActionIdx;
                    }
                    var action = comboActions[currentActionIdx];
                    int rollBonus = player.ComboBonus;
                    int tempBonus = player.ConsumeTempComboBonus();
                    
                    // Use new dice mechanics
                    DiceResult diceResult;
                    if (player.ComboModeActive)
                    {
                        // In combo mode, use combo continue mechanics
                        diceResult = Dice.RollComboContinue(rollBonus + tempBonus);
                    }
                    else
                    {
                        // Not in combo mode, use normal combo action mechanics
                        diceResult = Dice.RollComboAction(rollBonus + tempBonus);
                    }
                    
                    string rollMsg = $"(Rolled {diceResult.Roll - rollBonus - tempBonus} + {rollBonus} + {tempBonus} = {diceResult.Roll}, {diceResult.Description})";
                    
                    // Handle combo mode activation
                    if (diceResult.ComboTriggered)
                    {
                        player.ActivateComboMode();
                    }
                    
                    if (diceResult.Success)
                    {
                        player.ComboAmplifier = Math.Pow(comboStepExponent, player.ComboStep);
                        int baseEffect = action.BaseValue > 0 ? action.BaseValue : 1;
                        double effect = baseEffect * action.DamageMultiplier * player.ComboAmplifier;
                        double effectBeforeEnv = effect;
                        string envMsg = "";
                        if (environment != null)
                        {
                            effect = environment.ApplyPassiveEffect(effect);
                            if (effect != effectBeforeEnv && environment.PassiveEffectType != PassiveEffectType.None)
                            {
                                envMsg = $" (Environment passive: {environment.PassiveEffectType} applied, final value: {effect:0.##})";
                            }
                        }
                        int finalEffect = (int)effect;
                        
                        if (currentBattleNarrative != null)
                        {
                            var evt = new BattleEvent
                            {
                                Actor = player.Name,
                                Target = target.Name,
                                Action = action.Name,
                                Damage = 0,
                                IsSuccess = true,
                                IsCombo = true,
                                ComboStep = player.ComboStep,
                                CausesBleed = action.CausesBleed,
                                CausesWeaken = action.CausesWeaken,
                                ComboAmplifier = player.ComboAmplifier,
                                EnvironmentEffect = envMsg
                            };

                            if (action.Type == ActionType.Attack)
                            {
                                var settings = GameSettings.Instance;
                                finalEffect = CalculateDamage(player, target, action, player.ComboAmplifier, settings.PlayerDamageMultiplier);
                                if (target is Character c) c.TakeDamage(finalEffect);
                                evt.Damage = finalEffect;
                            }
                            else if (action.Type == ActionType.Debuff)
                            {
                                if (action.DamageMultiplier > 0)
                                {
                                    var settings = GameSettings.Instance;
                                    finalEffect = CalculateDamage(player, target, action, player.ComboAmplifier, settings.PlayerDamageMultiplier);
                                    if (target is Character c) c.TakeDamage(finalEffect);
                                    evt.Damage = finalEffect;
                                }
                                if (action.ComboBonusAmount > 0 && action.ComboBonusDuration > 0)
                                {
                                    player.SetTempComboBonus(action.ComboBonusAmount, action.ComboBonusDuration);
                                }
                            }

                            currentBattleNarrative.AddEvent(evt);
                            comboMemory.LastComboActionIdx = currentActionIdx;
                            player.ComboStep++;
                            if (player.ComboStep >= comboActions.Count)
                            {
                                // Combo complete - deactivate combo mode and reset
                                player.DeactivateComboMode();
                                player.ComboStep = 0;
                                player.ComboAmplifier = 1.0;
                                comboMemory.LastComboActionIdx = -1;
                            }
                            
                            // Check narrative balance setting - if 0, show action messages
                            var narrativeSettings = GameSettings.Instance;
                            if (narrativeSettings.NarrativeBalance <= 0.0)
                            {
                                string actionResult = $"[{player.Name}] uses [{action.Name}] on [{target.Name}]: ";
                                if (action.Type == ActionType.Attack)
                                {
                                    actionResult += $"deals {finalEffect} damage{envMsg}.";
                                }
                                else if (action.Type == ActionType.Debuff)
                                {
                                    if (action.DamageMultiplier > 0)
                                    {
                                        actionResult += $"deals {finalEffect} damage. ";
                                    }
                                    actionResult += $"applies debuff.";
                                    if (action.ComboBonusAmount > 0 && action.ComboBonusDuration > 0)
                                    {
                                        actionResult += $" ({action.Name}: +{action.ComboBonusAmount} to next {action.ComboBonusDuration} combo rolls!)";
                                    }
                                }
                                if (action.CausesBleed)
                                    actionResult += " (Bleed effect applied!)";
                                if (action.CausesWeaken)
                                    actionResult += " (Weaken effect applied!)";
                                actionResult += $" {rollMsg}";
                                if (player.ComboStep == 0)
                                {
                                    actionResult += " Combo complete! Resetting combo.";
                                }
                                else if (player.ComboModeActive)
                                {
                                    actionResult += " Combo mode active!";
                                }
                                return actionResult;
                            }
                            return "";
                        }

                        // Fallback to old message format if not using narrative mode
                        string result = $"[Player] uses [{action.Name}] on [{target.Name}]: ";
                        if (action.Type == ActionType.Attack)
                        {
                            int weaponDamage = 0;
                            if (player.Weapon is WeaponItem weapon)
                                weaponDamage = weapon.Damage;
                            int armor = 0;
                            if (target is Character targetChar)
                            {
                                if (targetChar.Head is HeadItem head) armor += head.Armor;
                                if (targetChar.Body is ChestItem body) armor += body.Armor;
                                if (targetChar.Feet is FeetItem feet) armor += feet.Armor;
                            }
                            int strength = player.Strength;
                            var settings = GameSettings.Instance;
                            double raw = (weaponDamage + strength) * action.DamageMultiplier * player.ComboAmplifier * settings.PlayerDamageMultiplier;
                            finalEffect = Math.Max(0, (int)raw - armor);
                            if (target is Character c) c.TakeDamage(finalEffect);
                            result += $"deals {finalEffect} damage (raw: {raw:0.##}, armor: {armor}){envMsg}.";
                        }
                        else if (action.Type == ActionType.Debuff)
                        {
                            if (action.DamageMultiplier > 0)
                            {
                                int weaponDamage = 0;
                                if (player.Weapon is WeaponItem weapon)
                                    weaponDamage = weapon.Damage;
                                int armor = 0;
                                if (target is Character targetChar)
                                {
                                    if (targetChar.Head is HeadItem head) armor += head.Armor;
                                    if (targetChar.Body is ChestItem body) armor += body.Armor;
                                    if (targetChar.Feet is FeetItem feet) armor += feet.Armor;
                                }
                                int strength = player.Strength;
                                double raw = (weaponDamage + strength) * action.DamageMultiplier * player.ComboAmplifier;
                                finalEffect = Math.Max(0, (int)raw - armor);
                                if (target is Character c) c.TakeDamage(finalEffect);
                                result += $"deals {finalEffect} damage (raw: {raw:0.##}, armor: {armor}). ";
                            }
                            result += $"applies debuff.";
                            if (action.ComboBonusAmount > 0 && action.ComboBonusDuration > 0)
                            {
                                player.SetTempComboBonus(action.ComboBonusAmount, action.ComboBonusDuration);
                                result += $" ({action.Name}: +{action.ComboBonusAmount} to next {action.ComboBonusDuration} combo rolls!)";
                            }
                        }
                        if (action.CausesBleed)
                            result += " (Bleed effect applied!)";
                        if (action.CausesWeaken)
                            result += " (Weaken effect applied!)";
                        result += $" {rollMsg}";
                        comboMemory.LastComboActionIdx = currentActionIdx;
                        player.ComboStep++;
                        if (player.ComboStep >= comboActions.Count)
                        {
                            result += " Combo complete! Resetting combo.";
                            player.DeactivateComboMode();
                            player.ComboStep = 0;
                            player.ComboAmplifier = 1.0;
                            comboMemory.LastComboActionIdx = -1;
                        }
                        else if (player.ComboModeActive)
                        {
                            result += " Combo mode active!";
                        }
                        return result;
                    }
                    else
                    {
                        // Handle failure - deactivate combo mode and reset combo
                        player.DeactivateComboMode();
                        
                        if (currentBattleNarrative != null)
                        {
                            var evt = new BattleEvent
                            {
                                Actor = player.Name,
                                Target = target.Name,
                                Action = action.Name,
                                IsSuccess = false,
                                IsCombo = true,
                                ComboStep = player.ComboStep
                            };
                            currentBattleNarrative.AddEvent(evt);
                            player.ResetCombo();
                            
                            // Check narrative balance setting - if 0, show action messages
                            var narrativeSettings = GameSettings.Instance;
                            if (narrativeSettings.NarrativeBalance <= 0.0)
                            {
                                string failureMsg = $"[{player.Name}] uses [{action.Name}] but fails. {rollMsg} No action performed. Combo resets.";
                                return failureMsg;
                            }
                            return "";
                        }
                        string failMsg = $"[{player.Name}] uses [{action.Name}] but fails. {rollMsg} No action performed. Combo resets.";
                        player.ResetCombo();
                        return failMsg;
                    }
                }
                // fallback for non-IComboMemory characters (should not happen)
                var fallbackAction = comboActions[player.ComboStep % comboActions.Count];
                int fallbackRollBonus = player.ComboBonus;
                int fallbackTempBonus = player.ConsumeTempComboBonus();
                
                // Use new dice mechanics for fallback
                DiceResult fallbackDiceResult;
                if (player.ComboModeActive)
                {
                    fallbackDiceResult = Dice.RollComboContinue(fallbackRollBonus + fallbackTempBonus);
                }
                else
                {
                    fallbackDiceResult = Dice.RollComboAction(fallbackRollBonus + fallbackTempBonus);
                }
                
                string fallbackRollMsg = $"(Rolled {fallbackDiceResult.Roll - fallbackRollBonus - fallbackTempBonus} + {fallbackRollBonus} + {fallbackTempBonus} = {fallbackDiceResult.Roll}, {fallbackDiceResult.Description})";
                
                // Handle combo mode activation
                if (fallbackDiceResult.ComboTriggered)
                {
                    player.ActivateComboMode();
                }
                
                if (fallbackDiceResult.Success)
                {
                    player.ComboAmplifier = Math.Pow(comboStepExponent, player.ComboStep);
                    int baseEffect = fallbackAction.BaseValue > 0 ? fallbackAction.BaseValue : 1;
                    double effect = baseEffect * fallbackAction.DamageMultiplier * player.ComboAmplifier;
                    double effectBeforeEnv = effect;
                    string envMsg = "";
                    if (environment != null)
                    {
                        effect = environment.ApplyPassiveEffect(effect);
                        if (effect != effectBeforeEnv && environment.PassiveEffectType != PassiveEffectType.None)
                        {
                            envMsg = $" (Environment passive: {environment.PassiveEffectType} applied, final value: {effect:0.##})";
                        }
                    }
                    int finalEffect = (int)effect;
                    
                    if (currentBattleNarrative != null)
                    {
                        var evt = new BattleEvent
                        {
                            Actor = player.Name,
                            Target = target.Name,
                            Action = fallbackAction.Name,
                            Damage = 0,
                            IsSuccess = true,
                            IsCombo = true,
                            ComboStep = player.ComboStep,
                            CausesBleed = fallbackAction.CausesBleed,
                            CausesWeaken = fallbackAction.CausesWeaken,
                            ComboAmplifier = player.ComboAmplifier,
                            EnvironmentEffect = envMsg
                        };

                        if (fallbackAction.Type == ActionType.Attack)
                        {
                            finalEffect = CalculateDamage(player, target, fallbackAction, player.ComboAmplifier);
                            if (target is Character c) c.TakeDamage(finalEffect);
                            evt.Damage = finalEffect;
                        }
                        else if (fallbackAction.Type == ActionType.Debuff)
                        {
                            if (fallbackAction.DamageMultiplier > 0)
                            {
                                finalEffect = CalculateDamage(player, target, fallbackAction, player.ComboAmplifier);
                                if (target is Character c) c.TakeDamage(finalEffect);
                                evt.Damage = finalEffect;
                            }
                            if (fallbackAction.ComboBonusAmount > 0 && fallbackAction.ComboBonusDuration > 0)
                            {
                                player.SetTempComboBonus(fallbackAction.ComboBonusAmount, fallbackAction.ComboBonusDuration);
                            }
                        }

                        currentBattleNarrative.AddEvent(evt);
                        player.ComboStep++;
                        if (player.ComboStep >= comboActions.Count)
                        {
                            // Combo complete - deactivate combo mode and reset
                            player.DeactivateComboMode();
                            player.ComboStep = 0;
                            player.ComboAmplifier = 1.0;
                        }
                        return "";
                    }

                    // Fallback to old message format
                    string result = $"[Player] uses [{fallbackAction.Name}] on [{target.Name}]: ";
                    if (fallbackAction.Type == ActionType.Attack)
                    {
                        finalEffect = CalculateDamage(player, target, fallbackAction, player.ComboAmplifier);
                        if (target is Character c) c.TakeDamage(finalEffect);
                        result += $"deals {finalEffect} damage{envMsg}.";
                    }
                    else if (fallbackAction.Type == ActionType.Debuff)
                    {
                        if (fallbackAction.DamageMultiplier > 0)
                        {
                            finalEffect = CalculateDamage(player, target, fallbackAction, player.ComboAmplifier);
                            if (target is Character c) c.TakeDamage(finalEffect);
                            result += $"deals {finalEffect} damage. ";
                        }
                        result += $"applies debuff.";
                        if (fallbackAction.ComboBonusAmount > 0 && fallbackAction.ComboBonusDuration > 0)
                        {
                            player.SetTempComboBonus(fallbackAction.ComboBonusAmount, fallbackAction.ComboBonusDuration);
                            result += $" ({fallbackAction.Name}: +{fallbackAction.ComboBonusAmount} to next {fallbackAction.ComboBonusDuration} combo rolls!)";
                        }
                    }
                    if (fallbackAction.CausesBleed)
                        result += " (Bleed effect applied!)";
                    if (fallbackAction.CausesWeaken)
                        result += " (Weaken effect applied!)";
                    result += $" {fallbackRollMsg}";
                    player.ComboStep++;
                    if (player.ComboStep >= comboActions.Count)
                    {
                        result += " Combo complete! Resetting combo.";
                        player.DeactivateComboMode();
                        player.ComboStep = 0;
                        player.ComboAmplifier = 1.0;
                    }
                    else if (player.ComboModeActive)
                    {
                        result += " Combo mode active!";
                    }
                    return result;
                }
                else
                {
                    // Handle fallback failure - deactivate combo mode and reset combo
                    player.DeactivateComboMode();
                    
                    if (currentBattleNarrative != null)
                    {
                        var evt = new BattleEvent
                        {
                            Actor = player.Name,
                            Target = target.Name,
                            Action = fallbackAction.Name,
                            IsSuccess = false,
                            IsCombo = true,
                            ComboStep = player.ComboStep
                        };
                        currentBattleNarrative.AddEvent(evt);
                        player.ResetCombo();
                        return "";
                    }
                    string failMsg = $"[{player.Name}] uses [{fallbackAction.Name}] but fails. {fallbackRollMsg} No action performed. Combo resets.";
                    player.ResetCombo();
                    return failMsg;
                }
            }
            // Regular action logic for enemies and other entities
            var selectedAction = source.SelectAction();
            if (selectedAction == null)
            {
                if (currentBattleNarrative != null)
                {
                    var evt = new BattleEvent
                    {
                        Actor = source.Name,
                        Target = target.Name,
                        Action = "No Action",
                        IsSuccess = false
                    };
                    currentBattleNarrative.AddEvent(evt);
                    return "";
                }
                return $"{source.Name} has no available actions!";
            }
            if (selectedAction.IsOnCooldown)
            {
                selectedAction.UpdateCooldown();
                if (currentBattleNarrative != null)
                {
                    var evt = new BattleEvent
                    {
                        Actor = source.Name,
                        Target = target.Name,
                        Action = selectedAction.Name,
                        IsSuccess = false
                    };
                    currentBattleNarrative.AddEvent(evt);
                    return "";
                }
                return $"{source.Name}'s {selectedAction.Name} is on cooldown! ({selectedAction.CurrentCooldown} turns remaining)";
            }
            double selectedEffect = 0;
            double effectBeforeEnv2 = 0;
            string envMsg2 = "";
            if (source is Character sourceCharacter)
            {
                selectedEffect = selectedAction.CalculateEffect(sourceCharacter, target as Character);
                effectBeforeEnv2 = selectedEffect;
                if (environment != null)
                {
                    selectedEffect = environment.ApplyPassiveEffect(selectedEffect);
                    if (selectedEffect != effectBeforeEnv2 && environment.PassiveEffectType != PassiveEffectType.None)
                    {
                        envMsg2 = $" (Environment passive: {environment.PassiveEffectType} applied, final value: {selectedEffect:0.##})";
                    }
                }
            }
            else if (source is Environment sourceEnvironment)
            {
                // Environment actions use their base value and theme-based scaling
                selectedEffect = selectedAction.BaseValue;
                // Apply theme-based scaling for environment actions
                switch (sourceEnvironment.Theme)
                {
                    case "Lava":
                        selectedEffect = (int)(selectedEffect * 1.5); // Lava is more dangerous
                        break;
                    case "Crypt":
                        selectedEffect = (int)(selectedEffect * 1.2); // Crypt has supernatural effects
                        break;
                    case "Forest":
                        selectedEffect = (int)(selectedEffect * 1.1); // Forest has natural hazards
                        break;
                    default:
                        selectedEffect = (int)(selectedEffect * 1.0); // Default scaling
                        break;
                }
                effectBeforeEnv2 = selectedEffect;
                if (environment != null)
                {
                    selectedEffect = environment.ApplyPassiveEffect(selectedEffect);
                    if (selectedEffect != effectBeforeEnv2 && environment.PassiveEffectType != PassiveEffectType.None)
                    {
                        envMsg2 = $" (Environment passive: {environment.PassiveEffectType} applied, final value: {selectedEffect:0.##})";
                    }
                }
            }
            else
            {
                return $"{source.Name} is not a valid character for this action!";
            }

            if (currentBattleNarrative != null)
            {
                var evt = new BattleEvent
                {
                    Actor = source.Name,
                    Target = target.Name,
                    Action = selectedAction.Name,
                    Damage = 0,
                    IsSuccess = true,
                    IsCombo = false,
                    EnvironmentEffect = envMsg2
                };

                switch (selectedAction.Type)
                {
                    case ActionType.Attack:
                        if (target is Character character)
                        {
                            character.TakeDamage((int)selectedEffect);
                            evt.Damage = (int)selectedEffect;
                        }
                        break;
                    case ActionType.Heal:
                        if (target is Character healTarget)
                        {
                            healTarget.Heal((int)selectedEffect);
                            evt.IsHeal = true;
                            evt.HealAmount = (int)selectedEffect;
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

                currentBattleNarrative.AddEvent(evt);
                if (selectedAction.Cooldown > 0)
                    selectedAction.ResetCooldown();
                return "";
            }

            // Fallback to old message format if not using narrative mode
            string selectedResult = $"[{source.Name}] uses [{selectedAction.Name}] on [{target.Name}]: ";
            switch (selectedAction.Type)
            {
                case ActionType.Attack:
                    if (target is Character character)
                    {
                        character.TakeDamage((int)selectedEffect);
                        selectedResult += $"deals {(int)selectedEffect} damage{envMsg2}.";
                    }
                    break;
                case ActionType.Heal:
                    if (target is Character healTarget)
                    {
                        healTarget.Heal((int)selectedEffect);
                        selectedResult += $"heals {(int)selectedEffect} HP.";
                    }
                    break;
                case ActionType.Buff:
                    selectedResult += $"applies buff.";
                    break;
                case ActionType.Debuff:
                    selectedResult += $"applies debuff.";
                    break;
                case ActionType.Interact:
                    selectedResult += $"interacts with environment.";
                    break;
                case ActionType.Move:
                    selectedResult += $"moves.";
                    break;
                case ActionType.UseItem:
                    selectedResult += $"uses item.";
                    break;
                default:
                    selectedResult += $"performs action.";
                    break;
            }
            if (selectedAction.Cooldown > 0)
                selectedAction.ResetCooldown();
            return selectedResult;
        }

        /// <summary>
        /// Unified damage calculation system for both players and enemies
        /// </summary>
        public static int CalculateDamage(Entity attacker, Entity target, Action action, double comboAmplifier = 1.0, double damageMultiplier = 1.0)
        {
            // Get attacker's weapon damage and stats
            int weaponDamage = 0;
            int strength = 0;
            int agility = 0;
            int technique = 0;
            
            if (attacker is Character character)
            {
                if (character.Weapon is WeaponItem weapon)
                    weaponDamage = weapon.Damage;
                strength = character.Strength;
                agility = character.Agility;
                technique = character.Technique;
            }
            else if (attacker is Enemy enemy)
            {
                // Enemies can have "virtual weapons" based on their level and stats
                weaponDamage = enemy.Level; // Base weapon damage scales with level
                strength = enemy.Strength;
                agility = enemy.Agility;
                technique = enemy.Technique;
            }
            
            // Calculate base damage based on action type
            double baseDamage = 0;
            switch (action.Type)
            {
                case ActionType.Attack:
                    baseDamage = weaponDamage + strength;
                    break;
                case ActionType.Debuff:
                    baseDamage = weaponDamage + (agility / 2); // Debuffs use agility
                    break;
                case ActionType.Heal:
                    baseDamage = technique; // Healing uses technique
                    break;
                default:
                    baseDamage = action.BaseValue; // Fallback to action base value
                    break;
            }
            
            // Apply multipliers
            double rawDamage = baseDamage * action.DamageMultiplier * comboAmplifier * damageMultiplier;
            
            // Calculate target's armor
            int armor = 0;
            if (target is Character targetChar)
            {
                if (targetChar.Head is HeadItem head) armor += head.Armor;
                if (targetChar.Body is ChestItem body) armor += body.Armor;
                if (targetChar.Feet is FeetItem feet) armor += feet.Armor;
            }
            else if (target is Enemy targetEnemy)
            {
                // Enemies get natural armor based on their level and primary attribute
                armor = targetEnemy.Level / 2; // Base armor from level
                switch (targetEnemy.PrimaryAttribute)
                {
                    case PrimaryAttribute.Strength:
                        armor += targetEnemy.Strength / 4; // Strength-based enemies are tankier
                        break;
                    case PrimaryAttribute.Agility:
                        armor += targetEnemy.Agility / 6; // Agility-based enemies have some dodge/armor
                        break;
                    case PrimaryAttribute.Technique:
                        armor += targetEnemy.Technique / 8; // Technique-based enemies are less tanky
                        break;
                }
            }
            
            // Apply armor reduction
            int finalDamage = Math.Max(0, (int)rawDamage - armor);
            
            return finalDamage;
        }

        private static string ApplyEffect(Action action, Entity source, Entity target, int effect)
        {
            switch (action.Type)
            {
                case ActionType.Attack:
                    if (target is Character character)
                    {
                        character.TakeDamage(effect);
                        return $"{source.Name} used {action.Name} on {target.Name} for {effect} damage!";
                    }
                    break;

                case ActionType.Heal:
                    if (target is Character healTarget)
                    {
                        healTarget.Heal(effect);
                        return $"{source.Name} used {action.Name} on {target.Name} to heal {effect} health!";
                    }
                    break;

                case ActionType.Buff:
                    // Buff effects would be implemented here
                    return $"{source.Name} used {action.Name} on {target.Name} to apply a buff!";

                case ActionType.Debuff:
                    // Debuff effects would be implemented here
                    return $"{source.Name} used {action.Name} on {target.Name} to apply a debuff!";

                case ActionType.Interact:
                    return $"{source.Name} used {action.Name} to interact with {target.Name}!";

                case ActionType.Move:
                    return $"{source.Name} used {action.Name} to move!";

                case ActionType.UseItem:
                    return $"{source.Name} used {action.Name} to use an item!";
            }

            return $"{source.Name} used {action.Name} on {target.Name}!";
        }

        // Combo state tracking (for demo: static, but should be per-character in a full implementation)
        // private static int playerComboStep = 0;
        // private static double playerComboAmplifier = 1.0;
        // private static int playerComboBonus = 0; // TODO: Add loot integration
        public static double comboStepExponent = 0.85; // Adjustable combo step exponent
        
        /// <summary>
        /// Applies a delay only when text is being displayed, based on game settings
        /// </summary>
        /// <param name="actionLength">The length of the action (used to calculate delay)</param>
        /// <param name="isTextDisplayed">Whether text was actually displayed to the user</param>
        public static void ApplyTextDisplayDelay(double actionLength, bool isTextDisplayed)
        {
            var settings = GameSettings.Instance;
            
            // Only apply delays if the setting is enabled AND text was actually displayed
            if (settings.EnableTextDisplayDelays && isTextDisplayed)
            {
                // Calculate delay based on action length and combat speed
                double baseDelayMs = actionLength * 400; // 400ms per action length unit
                double adjustedDelayMs = baseDelayMs / settings.CombatSpeed;
                int finalDelayMs = Math.Max(50, (int)adjustedDelayMs); // Minimum 50ms delay
                
                Thread.Sleep(finalDelayMs);
            }
        }
    }
} 