using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using static RPGGame.Combat.Formatting.DamageFormatter;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// Formats roll information for combat results
    /// </summary>
    public static class RollInfoFormatter
    {
        /// <summary>
        /// Formats roll information with colored text
        /// </summary>
        public static List<ColoredText> FormatRollInfoColored(
            int roll, 
            int rollBonus, 
            int rawDamage, 
            int targetDefense, 
            double actualSpeed, 
            double? comboAmplifier = null, 
            Action? action = null)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("     (", Colors.Gray);
            builder.Add("roll:", ColorPalette.Info);
            builder.AddSpace();
            builder.Add(roll.ToString(), Colors.White);
            
            if (rollBonus != 0)
            {
                if (rollBonus > 0)
                {
                    builder.Add(" + ", Colors.White);
                    builder.Add(rollBonus.ToString(), ColorPalette.Success);
                }
                else
                {
                    builder.Add(" - ", Colors.White);
                    builder.Add((-rollBonus).ToString(), ColorPalette.Error);
                }
                builder.Add(" = ", Colors.White);
                builder.Add((roll + rollBonus).ToString(), Colors.White);
            }
            
            // Attack vs Defense
            if (rawDamage > 0 || targetDefense > 0)
            {
                AddAttackVsArmor(builder, rawDamage, targetDefense);
            }
            
            // Speed information
            if (actualSpeed > 0)
            {
                AddSpeedInfo(builder, actualSpeed);
            }
            
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
    }
}

