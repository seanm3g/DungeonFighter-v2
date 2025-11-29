using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Formats damage display messages
    /// </summary>
    public static class DamageFormatter
    {
        /// <summary>
        /// Formats damage display with the new ColoredText system
        /// Returns both the main damage text and roll info as separate ColoredText lists
        /// </summary>
        public static (List<ColoredText> damageText, List<ColoredText> rollInfo) FormatDamageDisplayColored(
            Actor attacker, 
            Actor target, 
            int rawDamage, 
            int actualDamage, 
            Action? action = null, 
            double comboAmplifier = 1.0, 
            double damageMultiplier = 1.0, 
            int rollBonus = 0, 
            int roll = 0)
        {
            var builder = new ColoredTextBuilder();
            
            string actionName = action?.Name ?? "attack";
            
            // Check if this is a critical hit
            int totalRoll = roll + rollBonus;
            bool isCritical = totalRoll >= 20;
            
            if (isCritical)
            {
                actionName = $"CRITICAL {actionName}";
            }
            
            // Determine if this is a combo action
            bool isComboAction = actionName != "BASIC ATTACK" && actionName != "CRITICAL BASIC ATTACK";
            
            // Attacker name
            builder.Add(attacker.Name, attacker is Character ? ColorPalette.Gold : ColorPalette.Enemy);
            builder.AddSpace();
            builder.Add("hits", Colors.White);
            builder.AddSpace();
            
            // Target name
            builder.Add(target.Name, target is Character ? ColorPalette.Gold : ColorPalette.Enemy);
            
            // Action name for combo actions
            if (isComboAction)
            {
                builder.Add("with", Colors.White);
                builder.Add(actionName, isCritical ? ColorPalette.Critical : ColorPalette.Warning);
            }
            
            // Damage amount
            builder.Add("for", Colors.White);
            builder.Add(actualDamage.ToString(), isCritical ? ColorPalette.Critical : ColorPalette.Damage);
            builder.Add("damage", Colors.White);
            
            var damageText = builder.Build();
            
            // Calculate roll info
            int targetDefense = 0;
            if (target is Enemy targetEnemy)
            {
                targetDefense = targetEnemy.Armor;
            }
            else if (target is Character targetCharacter)
            {
                targetDefense = targetCharacter.GetTotalArmor();
            }
            
            int actualRawDamage = CombatCalculator.CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll);
            
            double actualSpeed = 0;
            if (action != null && action.Length > 0)
            {
                actualSpeed = ActionSpeedCalculator.CalculateActualActionSpeed(attacker, action);
            }
            
            var rollInfo = RollInfoFormatter.FormatRollInfoColored(roll, rollBonus, actualRawDamage, targetDefense, actualSpeed, comboAmplifier, action);
            
            return (damageText, rollInfo);
        }
    }
}

