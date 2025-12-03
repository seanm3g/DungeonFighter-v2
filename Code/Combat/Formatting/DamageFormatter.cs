using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Formats damage display messages
    /// Provides centralized formatting for combat messages with consistent spacing
    /// </summary>
    public static class DamageFormatter
    {
        /// <summary>
        /// Adds "for [amount] [unit]" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddForAmountUnit(ColoredTextBuilder builder, string amount, ColorPalette amountColor, string unit, ColorPalette unitColor)
        {
            builder.AddSpace();
            builder.Add("for", Colors.White);
            builder.AddSpace();
            builder.Add(amount, amountColor);
            builder.AddSpace();
            builder.Add(unit, unitColor);
        }
        
        /// <summary>
        /// Adds "for [amount] [unit]" pattern to a ColoredTextBuilder with proper spacing (Color overload for white unit)
        /// </summary>
        public static void AddForAmountUnit(ColoredTextBuilder builder, string amount, ColorPalette amountColor, string unit, Avalonia.Media.Color unitColor)
        {
            builder.AddSpace();
            builder.Add("for", Colors.White);
            builder.AddSpace();
            builder.Add(amount, amountColor);
            builder.AddSpace();
            builder.Add(unit, unitColor);
        }
        
        /// <summary>
        /// Adds "with [action]" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddWithAction(ColoredTextBuilder builder, string actionName, ColorPalette actionColor)
        {
            builder.AddSpace();
            builder.Add("with", Colors.White);
            builder.AddSpace();
            builder.Add(actionName, actionColor);
        }
        
        /// <summary>
        /// Adds "uses [action]" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddUsesAction(ColoredTextBuilder builder, string actionName, ColorPalette actionColor)
        {
            builder.AddSpace();
            builder.Add("uses", Colors.White);
            builder.AddSpace();
            builder.Add(actionName, actionColor);
        }
        
        /// <summary>
        /// Adds "hits [target]" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddHitsTarget(ColoredTextBuilder builder, string targetName, ColorPalette targetColor)
        {
            builder.AddSpace();
            builder.Add("hits", Colors.White);
            builder.AddSpace();
            builder.Add(targetName, targetColor);
        }
        
        /// <summary>
        /// Adds "attack X - Y armor" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddAttackVsArmor(ColoredTextBuilder builder, int attack, int armor)
        {
            builder.Add(" | ", Colors.Gray);
            builder.Add("attack", ColorPalette.Info);
            builder.AddSpace();
            builder.Add(attack.ToString(), Colors.White);
            builder.Add(" - ", Colors.White);
            builder.Add(armor.ToString(), Colors.White);
            builder.Add(" armor", Colors.White);
        }
        
        /// <summary>
        /// Adds "speed: X.Xs" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddSpeedInfo(ColoredTextBuilder builder, double speed)
        {
            builder.Add(" | ", Colors.Gray);
            builder.Add("speed:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add($"{speed:F1}s", Colors.White);
        }
        
        /// <summary>
        /// Adds "amp: X.Xx" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddAmpInfo(ColoredTextBuilder builder, double amp)
        {
            builder.Add(" | ", Colors.Gray);
            builder.Add("amp:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add($"{amp:F1}x", Colors.White);
        }
        
        /// <summary>
        /// Adds "takes X damage from [effect]" pattern to a ColoredTextBuilder with proper spacing
        /// </summary>
        public static void AddTakesDamageFrom(ColoredTextBuilder builder, int damage, string effectName, ColorPalette effectColor)
        {
            builder.AddSpace();
            builder.Add("takes", Colors.White);
            builder.AddSpace();
            builder.Add(damage.ToString(), ColorPalette.Damage);
            builder.AddSpace();
            builder.Add("damage", Colors.White);
            builder.AddSpace();
            builder.Add("from", Colors.White);
            builder.AddSpace();
            builder.Add(effectName, effectColor);
        }
        
        /// <summary>
        /// Adds "[Actor] takes X [damageType] damage" pattern to a ColoredTextBuilder with proper spacing
        /// Used for poison/bleed damage over time messages
        /// </summary>
        public static void AddBracketedActorTakesDamage(ColoredTextBuilder builder, string actorName, ColorPalette actorColor, int damage, string damageType)
        {
            // Add opening bracket
            builder.Add("[", Colors.White);
            // Add actor name (no space before, bracket handles it)
            builder.Add(actorName, actorColor);
            // Add closing bracket (no space after, spacing manager handles it)
            builder.Add("]", Colors.White);
            // Add "takes" with proper spacing
            builder.AddSpace();
            builder.Add("takes", Colors.White);
            // Add damage amount with proper spacing
            builder.AddSpace();
            builder.Add(damage.ToString(), ColorPalette.Damage);
            // Add damage type with proper spacing
            builder.AddSpace();
            builder.Add(damageType, Colors.White);
            // Add "damage" with proper spacing
            builder.AddSpace();
            builder.Add("damage", Colors.White);
        }
        
        /// <summary>
        /// Adds "[Actor] is no longer [effect]!" pattern to a ColoredTextBuilder with proper spacing
        /// Used for status effect end messages (e.g., "no longer poisoned", "no longer burning")
        /// </summary>
        public static void AddBracketedActorNoLongerAffected(ColoredTextBuilder builder, string actorName, ColorPalette actorColor, string effectName, ColorPalette effectColor)
        {
            // Add indentation (4 spaces)
            builder.Add("    (", Colors.White);
            // Add opening bracket
            builder.Add("[", Colors.White);
            // Add actor name
            builder.Add(actorName, actorColor);
            // Add closing bracket
            builder.Add("]", Colors.White);
            // Add "is no longer" with proper spacing
            builder.AddSpace();
            builder.Add("is", Colors.White);
            builder.AddSpace();
            builder.Add("no", Colors.White);
            builder.AddSpace();
            builder.Add("longer", Colors.White);
            builder.AddSpace();
            // Add effect name
            builder.Add(effectName, effectColor);
            // Add exclamation mark and closing parenthesis
            builder.Add("!)", Colors.White);
        }
        
        /// <summary>
        /// Adds "(effect: X stacks remain)" pattern to a ColoredTextBuilder with proper spacing
        /// Used for status effect stack count messages
        /// </summary>
        public static void AddEffectStacksRemain(ColoredTextBuilder builder, string effectName, ColorPalette effectColor, int stacks)
        {
            // Add indentation (4 spaces)
            builder.Add("    (", Colors.White);
            // Add effect name
            builder.Add(effectName, effectColor);
            // Add colon
            builder.Add(":", Colors.White);
            builder.AddSpace();
            // Add stack count
            builder.Add(stacks.ToString(), Colors.White);
            builder.AddSpace();
            // Add "stacks remain"
            builder.Add("stacks remain", Colors.White);
            // Add closing parenthesis
            builder.Add(")", Colors.White);
        }
        
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
            
            // Target name with "hits" verb
            AddHitsTarget(builder, target.Name, target is Character ? ColorPalette.Gold : ColorPalette.Enemy);
            
            // Action name for combo actions
            if (isComboAction)
            {
                AddWithAction(builder, actionName, isCritical ? ColorPalette.Critical : ColorPalette.Warning);
            }
            
            // Damage amount
            AddForAmountUnit(builder, actualDamage.ToString(), isCritical ? ColorPalette.Critical : ColorPalette.Damage, "damage", Colors.White);
            
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

