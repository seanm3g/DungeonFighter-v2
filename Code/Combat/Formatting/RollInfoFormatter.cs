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
            
            AppendComboAmpToRollInfo(builder, comboAmplifier, action);
            
            builder.Add(")", Colors.Gray);
            
            return builder.Build();
        }

        /// <summary>
        /// Appends the amp segment for roll footers. Pass the displayed swing multiplier (chain/slot mult × queued AMP_MOD).
        /// </summary>
        public static void AppendComboAmpToRollInfo(ColoredTextBuilder builder, double? comboAmplifier, Action? action)
        {
            double amp = comboAmplifier ?? 1.0;
            if (amp > 1.0001 || (action != null && action.IsComboAction))
                AddAmpInfo(builder, amp);
        }
    }
}

