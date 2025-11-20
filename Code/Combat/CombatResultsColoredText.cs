using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Enhanced combat result formatting using the new ColoredText system
    /// Provides cleaner, more maintainable colored combat messages
    /// </summary>
    public static class CombatResultsColoredText
    {
        /// <summary>
        /// Formats damage display with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatDamageDisplayColored(
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
            builder.Add(attacker.Name, ColorPalette.Player);
            builder.Add(" hits ", Colors.White);
            
            // Target name
            builder.Add(target.Name, ColorPalette.Enemy);
            
            // Action name for combo actions
            if (isComboAction)
            {
                builder.Add(" with ", Colors.White);
                builder.Add(actionName, isCritical ? ColorPalette.Critical : ColorPalette.Warning);
            }
            
            // Damage amount
            builder.Add(" for ", Colors.White);
            builder.Add(actualDamage.ToString(), isCritical ? ColorPalette.Critical : ColorPalette.Damage);
            builder.Add(" damage", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats roll information with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatRollInfoColored(
            int roll,
            int rollBonus,
            int attack,
            int defense,
            double actualSpeed)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("    (", Colors.Gray);
            
            // Roll information
            builder.Add("roll: ", ColorPalette.Info);
            builder.Add(roll.ToString(), Colors.White);
            
            int totalRoll = roll + rollBonus;
            if (rollBonus > 0)
            {
                builder.Add(" + ", Colors.White);
                builder.Add(rollBonus.ToString(), ColorPalette.Success);
                builder.Add(" = ", Colors.White);
                builder.Add(totalRoll.ToString(), ColorPalette.Success);
            }
            else if (rollBonus < 0)
            {
                builder.Add(" - ", Colors.White);
                builder.Add((-rollBonus).ToString(), ColorPalette.Error);
                builder.Add(" = ", Colors.White);
                builder.Add(totalRoll.ToString(), ColorPalette.Warning);
            }
            
            // Attack vs Defense
            builder.Add(" | ", Colors.Gray);
            builder.Add("attack: ", ColorPalette.Info);
            builder.Add(attack.ToString(), Colors.White);
            builder.Add(" - ", Colors.White);
            builder.Add(defense.ToString(), Colors.White);
            builder.Add(" defense", Colors.White);
            
            // Speed information
            builder.Add(" | ", Colors.Gray);
            builder.Add("speed: ", ColorPalette.Info);
            builder.Add($"{actualSpeed:F1}s", Colors.White);
            
            builder.Add(")", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats miss message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatMissMessageColored(
            Actor attacker, 
            Actor target, 
            Action action, 
            int roll, 
            int rollBonus)
        {
            var builder = new ColoredTextBuilder();
            
            int totalRoll = roll + rollBonus;
            bool isCriticalMiss = totalRoll <= 1;
            
            // Attacker name
            builder.Add(attacker.Name, ColorPalette.Player);
            builder.Add(" ", Colors.White);
            
            if (isCriticalMiss)
            {
                builder.Add("CRITICAL ", ColorPalette.Critical);
                builder.Add("MISS", ColorPalette.Miss);
            }
            else
            {
                builder.Add("misses", ColorPalette.Miss);
            }
            
            builder.Add(" ", Colors.White);
            builder.Add(target.Name, ColorPalette.Enemy);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats non-attack action messages with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatNonAttackActionColored(
            Actor source, 
            Actor target, 
            Action action, 
            int roll, 
            int rollBonus)
        {
            var builder = new ColoredTextBuilder();
            
            // Source name
            builder.Add(source.Name, ColorPalette.Player);
            builder.Add(" uses ", Colors.White);
            
            // Action name
            builder.Add(action.Name, ColorPalette.Success);
            
            builder.Add(" on ", Colors.White);
            
            // Target name
            builder.Add(target.Name, ColorPalette.Enemy);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats health milestone notifications with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthMilestoneColored(Actor Actor, double healthPercentage)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(Actor.Name, Actor is Character ? ColorPalette.Player : ColorPalette.Enemy);
            builder.Add(" is ", Colors.White);
            
            if (healthPercentage <= 0.1)
            {
                builder.Add("near death", ColorPalette.Critical);
            }
            else if (healthPercentage <= 0.25)
            {
                if (Actor is Character)
                {
                    builder.Add("critically wounded", ColorPalette.Error);
                }
                else
                {
                    builder.Add("on the verge of defeat", ColorPalette.Warning);
                }
            }
            else if (healthPercentage <= 0.5)
            {
                builder.Add("badly wounded", ColorPalette.Warning);
            }
            
            builder.Add("!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats block message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatBlockMessageColored(
            Actor defender, 
            Actor attacker, 
            int damageBlocked)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(defender.Name, ColorPalette.Player);
            builder.Add(" ", Colors.White);
            builder.Add("blocks", ColorPalette.Block);
            builder.Add(" ", Colors.White);
            builder.Add(damageBlocked.ToString(), ColorPalette.Block);
            builder.Add(" damage from ", Colors.White);
            builder.Add(attacker.Name, ColorPalette.Enemy);
            builder.Add("!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats dodge message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatDodgeMessageColored(
            Actor defender, 
            Actor attacker)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(defender.Name, ColorPalette.Player);
            builder.Add(" ", Colors.White);
            builder.Add("dodges", ColorPalette.Dodge);
            builder.Add(" ", Colors.White);
            builder.Add(attacker.Name, ColorPalette.Enemy);
            builder.Add("'s attack!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats status effect application with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatStatusEffectColored(
            Actor target, 
            string effectName, 
            bool isApplied,
            int? duration = null,
            int? stackCount = null)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
            builder.Add(" ", Colors.White);
            
            if (isApplied)
            {
                builder.Add("is affected by", ColorPalette.Warning);
                builder.Add(" ", Colors.White);
                builder.Add(effectName, ColorPalette.Error);
                
                if (stackCount.HasValue && stackCount.Value > 1)
                {
                    builder.Add(" ", Colors.White);
                    builder.Add($"(x{stackCount.Value})", ColorPalette.Warning);
                }
                
                if (duration.HasValue)
                {
                    builder.Add(" ", Colors.White);
                    builder.Add($"[{duration.Value} turns]", Colors.Gray);
                }
            }
            else
            {
                builder.Add("is no longer affected by", ColorPalette.Success);
                builder.Add(" ", Colors.White);
                builder.Add(effectName, ColorPalette.Info);
            }
            
            builder.Add("!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats healing message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealingMessageColored(
            Actor healer, 
            Actor target, 
            int healAmount)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(healer.Name, ColorPalette.Player);
            builder.Add(" heals ", Colors.White);
            builder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
            builder.Add(" for ", Colors.White);
            builder.Add(healAmount.ToString(), ColorPalette.Healing);
            builder.Add(" health", ColorPalette.Healing);
            builder.Add("!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats combat victory message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatVictoryMessageColored(Actor victor, Actor defeated)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(victor.Name, ColorPalette.Player);
            builder.Add(" has defeated ", Colors.White);
            builder.Add(defeated.Name, ColorPalette.Enemy);
            builder.Add("!", ColorPalette.Success);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats combat defeat message with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatDefeatMessageColored(Actor victor, Actor defeated)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(defeated.Name, ColorPalette.Player);
            builder.Add(" has been defeated by ", Colors.White);
            builder.Add(victor.Name, ColorPalette.Enemy);
            builder.Add("!", ColorPalette.Error);
            
            return builder.Build();
        }
    }
}


