using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

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
                builder.Add(" | ", Colors.Gray);
                builder.Add("attack", ColorPalette.Info);
                builder.AddSpace();
                builder.Add(rawDamage.ToString(), Colors.White);
                builder.Add(" - ", Colors.White);
                builder.Add(targetDefense.ToString(), Colors.White);
                builder.Add(" armor", Colors.White);
            }
            
            // Speed information
            if (actualSpeed > 0)
            {
                builder.Add(" | ", Colors.Gray);
                builder.Add("speed:", ColorPalette.Info);
                builder.AddSpace();
                builder.Add($"{actualSpeed:F1}s", Colors.White);
            }
            
            if (comboAmplifier.HasValue)
            {
                if (comboAmplifier.Value > 1.0)
                {
                    builder.Add("|", Colors.Gray);
                    builder.AddSpace();
                    builder.Add("amp:", ColorPalette.Info);
                    builder.AddSpace();
                    builder.Add($"{comboAmplifier.Value:F1}x", Colors.White);
                }
                else if (action != null && action.IsComboAction)
                {
                    builder.Add("|", Colors.Gray);
                    builder.AddSpace();
                    builder.Add("amp:", ColorPalette.Info);
                    builder.AddSpace();
                    builder.Add("1.0x", Colors.White);
                }
            }
            
            builder.Add(")", Colors.Gray);
            
            return builder.Build();
        }
    }
}

