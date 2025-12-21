using System;
using RPGGame;

namespace RPGGame.GameCore.Editors
{
    /// <summary>
    /// Handles action form processing and validation
    /// Extracted from ActionEditorHandler.cs ProcessFormStep() method
    /// </summary>
    public class ActionFormProcessor
    {
        private readonly Action<string> showMessage;

        public ActionFormProcessor(Action<string> showMessage)
        {
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }
        
        /// <summary>
        /// Validates that a target type is appropriate for an action type
        /// </summary>
        private bool IsValidTargetTypeForActionType(string actionType, string targetType)
        {
            return actionType switch
            {
                "Attack" => targetType == "SingleTarget" || targetType == "SelfAndTarget",
                "Spell" => targetType == "SingleTarget" || targetType == "SelfAndTarget",
                "Heal" => targetType == "Self" || targetType == "SingleTarget",
                "Buff" => targetType == "Self",
                "Debuff" => targetType == "SingleTarget",
                "Interact" => targetType == "Environment",
                "Move" => targetType == "Self",
                "UseItem" => targetType == "Self" || targetType == "SingleTarget",
                _ => targetType == "SingleTarget"
            };
        }
        
        /// <summary>
        /// Gets a message describing valid target types for an action type
        /// </summary>
        private string GetValidTargetTypesMessage(string actionType)
        {
            return actionType switch
            {
                "Attack" => "Use: SingleTarget or SelfAndTarget",
                "Spell" => "Use: SingleTarget or SelfAndTarget",
                "Heal" => "Use: Self or SingleTarget",
                "Buff" => "Use: Self",
                "Debuff" => "Use: SingleTarget",
                "Interact" => "Use: Environment",
                "Move" => "Use: Self",
                "UseItem" => "Use: Self or SingleTarget",
                _ => "Use: SingleTarget"
            };
        }

        /// <summary>
        /// Helper method to parse boolean input
        /// </summary>
        private bool ParseBoolean(string input, out bool result)
        {
            string lower = input.Trim().ToLower();
            if (lower == "true" || lower == "t" || lower == "yes" || lower == "y" || lower == "1")
            {
                result = true;
                return true;
            }
            if (lower == "false" || lower == "f" || lower == "no" || lower == "n" || lower == "0" || string.IsNullOrWhiteSpace(lower))
            {
                result = false;
                return true;
            }
            result = false;
            return false;
        }

        /// <summary>
        /// Process input for the current form step
        /// </summary>
        public bool ProcessFormStep(ActionData actionData, int currentFormStep, string input)
        {
            if (actionData == null) return false;

            switch (currentFormStep)
            {
                case 0: // Name
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        actionData.Name = input.Trim().ToUpper();
                        return true;
                    }
                    showMessage("Name cannot be empty. Please enter a name.");
                    return false;

                case 1: // Type
                    string typeLower = input.Trim().ToLower();
                    if (typeLower == "attack" || typeLower == "heal" || typeLower == "buff" || 
                        typeLower == "debuff" || typeLower == "spell" || typeLower == "interact" || 
                        typeLower == "move" || typeLower == "useitem")
                    {
                        actionData.Type = char.ToUpper(typeLower[0]) + typeLower.Substring(1);
                        return true;
                    }
                    showMessage("Invalid type. Use: Attack, Heal, Buff, Debuff, Spell, Interact, Move, or UseItem");
                    return false;

                case 2: // TargetType
                    string targetLower = input.Trim().ToLower();
                    string normalizedTargetType = targetLower == "singletarget" ? "SingleTarget" :
                                                  targetLower == "areaofeffect" ? "AreaOfEffect" :
                                                  targetLower == "selfandtarget" ? "SelfAndTarget" :
                                                  char.ToUpper(targetLower[0]) + targetLower.Substring(1);
                    
                    // Validate target type is appropriate for action type
                    if (!IsValidTargetTypeForActionType(actionData.Type, normalizedTargetType))
                    {
                        showMessage($"Invalid target type '{normalizedTargetType}' for action type '{actionData.Type}'. " + GetValidTargetTypesMessage(actionData.Type));
                        return false;
                    }
                    
                    actionData.TargetType = normalizedTargetType;
                    return true;

                case 3: // Description
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        actionData.Description = input.Trim();
                        return true;
                    }
                    showMessage("Description cannot be empty. Please enter a description.");
                    return false;

                case 4: // DamageMultiplier
                    if (double.TryParse(input.Trim(), out double damageMult))
                    {
                        actionData.DamageMultiplier = damageMult;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5).");
                    return false;

                case 5: // Length
                    if (double.TryParse(input.Trim(), out double length))
                    {
                        actionData.Length = length;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5).");
                    return false;

                case 6: // Cooldown
                    if (int.TryParse(input.Trim(), out int cooldown))
                    {
                        actionData.Cooldown = cooldown;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer.");
                    return false;

                case 7: // CausesBleed
                    if (ParseBoolean(input, out bool causesBleed))
                    {
                        actionData.CausesBleed = causesBleed;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 8: // CausesWeaken
                    if (ParseBoolean(input, out bool causesWeaken))
                    {
                        actionData.CausesWeaken = causesWeaken;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 9: // CausesSlow
                    if (ParseBoolean(input, out bool causesSlow))
                    {
                        actionData.CausesSlow = causesSlow;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 10: // CausesPoison
                    if (ParseBoolean(input, out bool causesPoison))
                    {
                        actionData.CausesPoison = causesPoison;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 11: // CausesBurn
                    if (ParseBoolean(input, out bool causesBurn))
                    {
                        actionData.CausesBurn = causesBurn;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 12: // CausesStun
                    // Note: ActionData doesn't have CausesStun property, but Action class does
                    // This will be handled when Action is created from ActionData
                    // For now, we'll accept the input but it won't be saved to ActionData
                    if (ParseBoolean(input, out bool causesStun))
                    {
                        // Store in description or skip - this is a limitation of ActionData
                        // The Action class will parse this from description if needed
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 13: // IsComboAction
                    if (ParseBoolean(input, out bool isComboAction))
                    {
                        actionData.IsComboAction = isComboAction;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 14: // ComboOrder
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.ComboOrder = -1;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int comboOrder))
                    {
                        actionData.ComboOrder = comboOrder;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer (-1 = not in combo, or leave empty).");
                    return false;

                case 15: // ComboBonusAmount
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.ComboBonusAmount = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int comboBonusAmount))
                    {
                        actionData.ComboBonusAmount = comboBonusAmount;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer or leave empty for 0.");
                    return false;

                case 16: // ComboBonusDuration
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.ComboBonusDuration = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int comboBonusDuration))
                    {
                        actionData.ComboBonusDuration = comboBonusDuration;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer or leave empty for 0.");
                    return false;

                case 17: // RollBonus
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.RollBonus = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int rollBonus))
                    {
                        actionData.RollBonus = rollBonus;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer or leave empty for 0.");
                    return false;

                case 18: // StatBonus
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.StatBonus = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int statBonus))
                    {
                        actionData.StatBonus = statBonus;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer or leave empty for 0.");
                    return false;

                case 19: // StatBonusType
                    string statType = input.Trim();
                    if (string.IsNullOrWhiteSpace(statType))
                    {
                        actionData.StatBonusType = "";
                        return true;
                    }
                    string statTypeLower = statType.ToLower();
                    if (statTypeLower == "strength" || statTypeLower == "agility" || 
                        statTypeLower == "technique" || statTypeLower == "intelligence")
                    {
                        actionData.StatBonusType = char.ToUpper(statTypeLower[0]) + statTypeLower.Substring(1);
                        return true;
                    }
                    showMessage("Invalid stat type. Use: Strength, Agility, Technique, Intelligence, or empty string.");
                    return false;

                case 20: // StatBonusDuration
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.StatBonusDuration = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int statBonusDuration))
                    {
                        actionData.StatBonusDuration = statBonusDuration;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer (-1 = permanent, or leave empty for 0).");
                    return false;

                case 21: // MultiHitCount
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.MultiHitCount = 1;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int multiHitCount) && multiHitCount >= 1)
                    {
                        actionData.MultiHitCount = multiHitCount;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer >= 1, or leave empty for 1.");
                    return false;

                case 22: // SelfDamagePercent
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.SelfDamagePercent = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int selfDamagePercent))
                    {
                        actionData.SelfDamagePercent = selfDamagePercent;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer (0-100), or leave empty for 0.");
                    return false;

                case 23: // SkipNextTurn
                    if (ParseBoolean(input, out bool skipNextTurn))
                    {
                        actionData.SkipNextTurn = skipNextTurn;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 24: // RepeatLastAction
                    if (ParseBoolean(input, out bool repeatLastAction))
                    {
                        actionData.RepeatLastAction = repeatLastAction;
                        return true;
                    }
                    showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
                    return false;

                case 25: // EnemyRollPenalty
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.EnemyRollPenalty = 0;
                        return true;
                    }
                    if (int.TryParse(input.Trim(), out int enemyRollPenalty))
                    {
                        actionData.EnemyRollPenalty = enemyRollPenalty;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer or leave empty for 0.");
                    return false;

                case 26: // HealthThreshold
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.HealthThreshold = 0.0;
                        return true;
                    }
                    if (double.TryParse(input.Trim(), out double healthThreshold) && healthThreshold >= 0.0 && healthThreshold <= 1.0)
                    {
                        actionData.HealthThreshold = healthThreshold;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid decimal between 0.0 and 1.0, or leave empty for 0.0.");
                    return false;

                case 27: // ConditionalDamageMultiplier
                    if (string.IsNullOrWhiteSpace(input.Trim()))
                    {
                        actionData.ConditionalDamageMultiplier = 1.0;
                        return true;
                    }
                    if (double.TryParse(input.Trim(), out double conditionalDamageMultiplier))
                    {
                        actionData.ConditionalDamageMultiplier = conditionalDamageMultiplier;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5), or leave empty for 1.0.");
                    return false;

                case 28: // Tags
                    // Allow empty input for tags
                    string tagsInput = input.Trim();
                    if (string.IsNullOrWhiteSpace(tagsInput))
                    {
                        actionData.Tags = new List<string>();
                        return true;
                    }
                    // Parse comma-separated tags
                    var tags = new List<string>();
                    foreach (string tag in tagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimmedTag = tag.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedTag))
                        {
                            tags.Add(trimmedTag);
                        }
                    }
                    actionData.Tags = tags;
                    return true;

                default:
                    return false;
            }
        }
    }
}

