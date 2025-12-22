using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame.GameCore.Editors.Processors
{
    /// <summary>
    /// Processors for individual action form fields
    /// </summary>
    public static class ActionFieldProcessors
    {
        public static bool ProcessName(ActionData actionData, string input, Action<string> showMessage)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                actionData.Name = input.Trim().ToUpper();
                return true;
            }
            showMessage("Name cannot be empty. Please enter a name.");
            return false;
        }

        public static bool ProcessType(ActionData actionData, string input, Action<string> showMessage)
        {
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
        }

        public static bool ProcessTargetType(ActionData actionData, string input, Action<string> showMessage)
        {
            string targetLower = input.Trim().ToLower();
            string normalizedTargetType = targetLower == "singletarget" ? "SingleTarget" :
                                          targetLower == "areaofeffect" ? "AreaOfEffect" :
                                          targetLower == "selfandtarget" ? "SelfAndTarget" :
                                          char.ToUpper(targetLower[0]) + targetLower.Substring(1);
            
            if (!IsValidTargetTypeForActionType(actionData.Type, normalizedTargetType))
            {
                showMessage($"Invalid target type '{normalizedTargetType}' for action type '{actionData.Type}'. " + GetValidTargetTypesMessage(actionData.Type));
                return false;
            }
            
            actionData.TargetType = normalizedTargetType;
            return true;
        }

        public static bool ProcessDescription(ActionData actionData, string input, Action<string> showMessage)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                actionData.Description = input.Trim();
                return true;
            }
            showMessage("Description cannot be empty. Please enter a description.");
            return false;
        }

        public static bool ProcessDoubleField(ActionData actionData, string input, Action<string> showMessage, string fieldName, Action<ActionData, double> setter, bool allowEmpty = false, double emptyValue = 0.0)
        {
            if (string.IsNullOrWhiteSpace(input.Trim()) && allowEmpty)
            {
                setter(actionData, emptyValue);
                return true;
            }
            if (double.TryParse(input.Trim(), out double value))
            {
                setter(actionData, value);
                return true;
            }
            showMessage($"Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5){(allowEmpty ? " or leave empty for " + emptyValue : "")}.");
            return false;
        }

        public static bool ProcessIntField(ActionData actionData, string input, Action<string> showMessage, string fieldName, Action<ActionData, int> setter, bool allowEmpty = false, int emptyValue = 0)
        {
            if (string.IsNullOrWhiteSpace(input.Trim()) && allowEmpty)
            {
                setter(actionData, emptyValue);
                return true;
            }
            if (int.TryParse(input.Trim(), out int value))
            {
                setter(actionData, value);
                return true;
            }
            showMessage($"Invalid number. Please enter a valid integer{(allowEmpty ? " or leave empty for " + emptyValue : "")}.");
            return false;
        }

        public static bool ProcessBooleanField(ActionData actionData, string input, Action<string> showMessage, Action<ActionData, bool> setter)
        {
            if (ParseBoolean(input, out bool value))
            {
                setter(actionData, value);
                return true;
            }
            showMessage("Invalid boolean. Use: true/false, yes/no, y/n, or 1/0");
            return false;
        }

        public static bool ProcessStatBonusType(ActionData actionData, string input, Action<string> showMessage)
        {
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
        }

        public static bool ProcessHealthThreshold(ActionData actionData, string input, Action<string> showMessage)
        {
            if (string.IsNullOrWhiteSpace(input.Trim()))
            {
                actionData.HealthThreshold = 0.0;
                return true;
            }
            if (double.TryParse(input.Trim(), out double value) && value >= 0.0 && value <= 1.0)
            {
                actionData.HealthThreshold = value;
                return true;
            }
            showMessage("Invalid number. Please enter a valid decimal between 0.0 and 1.0, or leave empty for 0.0.");
            return false;
        }

        public static bool ProcessTags(ActionData actionData, string input, Action<string> showMessage)
        {
            string tagsInput = input.Trim();
            if (string.IsNullOrWhiteSpace(tagsInput))
            {
                actionData.Tags = new List<string>();
                return true;
            }
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
        }

        private static bool ParseBoolean(string input, out bool result)
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

        private static bool IsValidTargetTypeForActionType(string actionType, string targetType)
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

        private static string GetValidTargetTypesMessage(string actionType)
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
    }
}

