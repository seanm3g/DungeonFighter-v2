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
                    if (targetLower == "self" || targetLower == "singletarget" || 
                        targetLower == "areaofeffect" || targetLower == "environment")
                    {
                        actionData.TargetType = targetLower == "singletarget" ? "SingleTarget" :
                                                  targetLower == "areaofeffect" ? "AreaOfEffect" :
                                                  char.ToUpper(targetLower[0]) + targetLower.Substring(1);
                        return true;
                    }
                    showMessage("Invalid target type. Use: Self, SingleTarget, AreaOfEffect, or Environment");
                    return false;

                case 3: // BaseValue
                    if (int.TryParse(input.Trim(), out int baseValue))
                    {
                        actionData.BaseValue = baseValue;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer.");
                    return false;

                case 4: // Description
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        actionData.Description = input.Trim();
                        return true;
                    }
                    showMessage("Description cannot be empty. Please enter a description.");
                    return false;

                case 5: // DamageMultiplier
                    if (double.TryParse(input.Trim(), out double damageMult))
                    {
                        actionData.DamageMultiplier = damageMult;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5).");
                    return false;

                case 6: // Length
                    if (double.TryParse(input.Trim(), out double length))
                    {
                        actionData.Length = length;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid decimal (e.g., 1.0 or 1.5).");
                    return false;

                case 7: // Cooldown
                    if (int.TryParse(input.Trim(), out int cooldown))
                    {
                        actionData.Cooldown = cooldown;
                        return true;
                    }
                    showMessage("Invalid number. Please enter a valid integer.");
                    return false;

                default:
                    return false;
            }
        }
    }
}

