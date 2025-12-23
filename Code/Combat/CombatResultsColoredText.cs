using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Combat.Formatting;
using RPGGame.UI.ColorSystem;
using static RPGGame.Combat.Formatting.DamageFormatter;

namespace RPGGame
{
    /// <summary>
    /// Enhanced combat result formatting using the new ColoredText system
    /// Provides cleaner, more maintainable colored combat messages
    /// Refactored to use extracted formatters
    /// 
    /// SPACING STANDARDIZATION:
    /// Uses ColoredTextBuilder which automatically handles spacing via CombatLogSpacingManager.
    /// Manual spaces in operators (e.g., " + ", " - ", " | ") are intentional for display.
    /// See Documentation/05-Systems/COMBAT_LOG_SPACING_STANDARD.md for spacing guidelines.
    /// </summary>
    public static class CombatResultsColoredText
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
            return DamageFormatter.FormatDamageDisplayColored(attacker, target, rawDamage, actualDamage, action, comboAmplifier, damageMultiplier, rollBonus, roll);
        }
        
        /// <summary>
        /// Formats roll information with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatRollInfoColored(
            int roll,
            int rollBonus,
            int attack,
            int defense,
            double actualSpeed,
            double? comboAmplifier = null,
            Action? action = null)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("     (", Colors.Gray);
            
            // Roll information
            builder.Add("roll:", ColorPalette.Info);
            builder.AddSpace();
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
            AddAttackVsArmor(builder, attack, defense);
            
            // Speed information
            if (actualSpeed > 0)
            {
                AddSpeedInfo(builder, actualSpeed);
            }
            
            // Combo amplifier information
            if (comboAmplifier.HasValue)
            {
                if (comboAmplifier.Value > 1.0)
                {
                    AddAmpInfo(builder, comboAmplifier.Value);
                }
                else if (action != null && action.IsComboAction)
                {
                    AddAmpInfo(builder, 1.0);
                }
            }
            
            builder.Add(")", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats miss message with the new ColoredText system
        /// Returns both the main miss text and roll info as separate ColoredText lists
        /// </summary>
        public static (List<ColoredText> missText, List<ColoredText> rollInfo) FormatMissMessageColored(
            Actor attacker, 
            Actor target, 
            Action action, 
            int roll, 
            int rollBonus,
            int naturalRoll)
        {
            var builder = new ColoredTextBuilder();
            
            int totalRoll = roll + rollBonus;
            bool isCriticalMiss = naturalRoll == 1; // Natural 1 only
            
            // Attacker name (check Enemy first since Enemy inherits from Character)
            builder.Add(attacker.Name, attacker is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            
            if (isCriticalMiss)
            {
                builder.AddSpace(); // Explicit space between attacker name and "CRITICAL"
                builder.Add("CRITICAL", ColorPalette.Critical);
                builder.AddSpace(); // Explicit space between "CRITICAL" and "MISS"
                builder.Add("MISS", ColorPalette.Miss);
            }
            else
            {
                builder.AddSpace(); // Explicit space between attacker name and "misses"
                builder.Add("misses", ColorPalette.Miss);
            }
            
            builder.AddSpace(); // Explicit space between "MISS"/"misses" and target name
            builder.Add(target.Name, target is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            
            var missText = builder.Build();
            
            // Calculate roll info
            double actualSpeed = 0;
            if (action != null && action.Length > 0)
            {
                actualSpeed = ActionSpeedCalculator.CalculateActualActionSpeed(attacker, action);
            }
            
            var rollInfo = RollInfoFormatter.FormatRollInfoColored(roll, rollBonus, 0, 0, actualSpeed, null, action);
            
            return (missText, rollInfo);
        }
        
        /// <summary>
        /// Formats non-attack action messages with the new ColoredText system
        /// Returns both the main action text and roll info as separate ColoredText lists
        /// </summary>
        public static (List<ColoredText> actionText, List<ColoredText> rollInfo) FormatNonAttackActionColored(
            Actor source, 
            Actor target, 
            Action action, 
            int roll, 
            int rollBonus)
        {
            var builder = new ColoredTextBuilder();
            
            // Source name
            builder.Add(source.Name, source is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            AddUsesAction(builder, action.Name, ColorPalette.Green);
            
            builder.AddSpace(); // Explicit space between action name and "on"
            builder.Add("on", Colors.White);
            builder.AddSpace(); // Explicit space between "on" and target name
            
            // Target name
            builder.Add(target.Name, target is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            
            var actionText = builder.Build();
            
            // Calculate roll info
            double actualSpeed = 0;
            if (action != null && action.Length > 0)
            {
                actualSpeed = ActionSpeedCalculator.CalculateActualActionSpeed(source, action);
            }
            
            var rollInfo = RollInfoFormatter.FormatRollInfoColored(roll, rollBonus, 0, 0, actualSpeed, null, action);
            
            return (actionText, rollInfo);
        }
        
        /// <summary>
        /// Formats health milestone notifications with the new ColoredText system
        /// </summary>
        public static List<ColoredText> FormatHealthMilestoneColored(Actor Actor, double healthPercentage)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add(Actor.Name, Actor is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            builder.Add("is", Colors.White);
            
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
            builder.Add("blocks", ColorPalette.Block);
            builder.Add(damageBlocked.ToString(), ColorPalette.Block);
            builder.Add("damage", Colors.White);
            builder.Add("from", Colors.White);
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
            builder.Add("dodges", ColorPalette.Dodge);
            builder.Add(attacker.Name, ColorPalette.Enemy);
            builder.Add("'s", Colors.White);
            builder.Add("attack!", Colors.White);
            
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
            
            // Add 5 spaces for indentation to match roll detail lines
            builder.Add("     ", Colors.White);
            builder.Add(target.Name, target is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            
            if (isApplied)
            {
                builder.AddSpace(); // Explicit space between target name and "is"
                builder.Add("is", Colors.White);
                builder.AddSpace(); // Explicit space between "is" and "affected"
                builder.Add("affected", ColorPalette.Warning);
                builder.AddSpace(); // Explicit space between "affected" and "by"
                builder.Add("by", Colors.White);
                builder.AddSpace(); // Explicit space between "by" and effect name
                builder.Add(effectName, ColorPalette.Error);
                
                if (stackCount.HasValue && stackCount.Value > 1)
                {
                    builder.Add($"(x{stackCount.Value})", ColorPalette.Warning);
                }
                
                if (duration.HasValue)
                {
                    builder.Add($"[{duration.Value} turns]", Colors.Gray);
                }
            }
            else
            {
                builder.AddSpace(); // Explicit space between target name and "is"
                builder.Add("is", Colors.White);
                builder.AddSpace(); // Explicit space between "is" and "no"
                builder.Add("no", Colors.White);
                builder.AddSpace(); // Explicit space between "no" and "longer"
                builder.Add("longer", Colors.White);
                builder.AddSpace(); // Explicit space between "longer" and "affected"
                builder.Add("affected", ColorPalette.Success);
                builder.AddSpace(); // Explicit space between "affected" and "by"
                builder.Add("by", Colors.White);
                builder.AddSpace(); // Explicit space between "by" and effect name
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
            builder.AddSpace();
            builder.Add("heals", Colors.White);
            builder.AddSpace();
            builder.Add(target.Name, target is Enemy ? ColorPalette.Enemy : ColorPalette.Player);
            AddForAmountUnit(builder, healAmount.ToString(), ColorPalette.Healing, "health", ColorPalette.Healing);
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
            builder.Add("has", Colors.White);
            builder.Add("defeated", Colors.White);
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
            builder.Add("has", Colors.White);
            builder.Add("been", Colors.White);
            builder.Add("defeated", Colors.White);
            builder.Add("by", Colors.White);
            builder.Add(victor.Name, ColorPalette.Enemy);
            builder.Add("!", ColorPalette.Error);
            
            return builder.Build();
        }
    }
}


