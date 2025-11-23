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
            builder.Add(attacker.Name, attacker is Character ? ColorPalette.Player : ColorPalette.Enemy);
            builder.Add(" hits ", Colors.White);
            
            // Target name
            builder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
            
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
                actualSpeed = CalculateActualActionSpeed(attacker, action);
            }
            
            var rollInfo = FormatRollInfoColored(roll, rollBonus, actualRawDamage, targetDefense, actualSpeed, comboAmplifier, action);
            
            return (damageText, rollInfo);
        }
        
        /// <summary>
        /// Helper method to calculate actual action speed
        /// </summary>
        private static double CalculateActualActionSpeed(Actor actor, Action action)
        {
            double baseSpeed = 0;
            if (actor is Character character)
            {
                baseSpeed = character.GetTotalAttackSpeed();
            }
            else if (actor is Enemy enemy)
            {
                baseSpeed = enemy.GetTotalAttackSpeed();
            }
            else if (actor is Environment environment)
            {
                baseSpeed = 15.0;
            }
            
            if (actor.HasCriticalMissPenalty)
            {
                baseSpeed *= 2.0;
            }
            
            return baseSpeed * action.Length;
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
            builder.Add("attack ", ColorPalette.Info);
            builder.Add(attack.ToString(), Colors.White);
            builder.Add(" - ", Colors.White);
            builder.Add(defense.ToString(), Colors.White);
            builder.Add(" armor", Colors.White);
            
            // Speed information
            if (actualSpeed > 0)
            {
                builder.Add(" | ", Colors.Gray);
                builder.Add("speed: ", ColorPalette.Info);
                builder.Add($"{actualSpeed:F1}s", Colors.White);
            }
            
            // Combo amplifier information
            if (comboAmplifier.HasValue)
            {
                if (comboAmplifier.Value > 1.0)
                {
                    builder.Add(" | ", Colors.Gray);
                    builder.Add("amp: ", ColorPalette.Info);
                    builder.Add($"{comboAmplifier.Value:F1}x", Colors.White);
                }
                else if (action != null && action.IsComboAction)
                {
                    builder.Add(" | ", Colors.Gray);
                    builder.Add("amp: ", ColorPalette.Info);
                    builder.Add("1.0x", Colors.White);
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
            int rollBonus)
        {
            var builder = new ColoredTextBuilder();
            
            int totalRoll = roll + rollBonus;
            bool isCriticalMiss = totalRoll <= 1;
            
            // Attacker name
            builder.Add(attacker.Name, attacker is Character ? ColorPalette.Player : ColorPalette.Enemy);
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
            builder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
            
            var missText = builder.Build();
            
            // Calculate roll info
            double actualSpeed = 0;
            if (action != null && action.Length > 0)
            {
                actualSpeed = CalculateActualActionSpeed(attacker, action);
            }
            
            var rollInfo = FormatRollInfoColored(roll, rollBonus, 0, 0, actualSpeed);
            
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
            builder.Add(source.Name, source is Character ? ColorPalette.Player : ColorPalette.Enemy);
            builder.Add(" uses ", Colors.White);
            
            // Action name
            builder.Add(action.Name, ColorPalette.Success);
            
            builder.Add(" on ", Colors.White);
            
            // Target name
            builder.Add(target.Name, target is Character ? ColorPalette.Player : ColorPalette.Enemy);
            
            var actionText = builder.Build();
            
            // Calculate roll info
            double actualSpeed = 0;
            if (action != null && action.Length > 0)
            {
                actualSpeed = CalculateActualActionSpeed(source, action);
            }
            
            var rollInfo = FormatRollInfoColored(roll, rollBonus, 0, 0, actualSpeed);
            
            return (actionText, rollInfo);
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


