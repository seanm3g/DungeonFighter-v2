using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Actions.RollModification;
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
        /// Plain-text roll value for log footers, including 2d20 luck/unluck when applicable.
        /// </summary>
        public static string FormatRollValuePlain(int roll, MultiDiceRollDetail detail = default)
        {
            if (!detail.HasLuckDetail)
                return roll.ToString();

            return detail.Mode switch
            {
                MultiDiceLuckMode.Advantage =>
                    $"2d20 luck {detail.HighDie}/{detail.LowDie} → {roll}",
                MultiDiceLuckMode.Disadvantage =>
                    $"2d20 unluck {detail.HighDie}/{detail.LowDie} → {roll}",
                MultiDiceLuckMode.Cancelled =>
                    $"{roll} (luck cancel)",
                _ => roll.ToString()
            };
        }

        /// <summary>
        /// Appends the d20 (or 2d20 luck/unluck) segment after the <c>roll:</c> label.
        /// </summary>
        public static void AppendRollValue(ColoredTextBuilder builder, int roll, MultiDiceRollDetail detail = default)
        {
            if (!detail.HasLuckDetail)
            {
                builder.Add(roll.ToString(), Colors.White);
                return;
            }

            switch (detail.Mode)
            {
                case MultiDiceLuckMode.Advantage:
                    builder.Add("2d20", Colors.White);
                    builder.AddSpace();
                    builder.Add("luck", ColorPalette.Success);
                    builder.AddSpace();
                    builder.Add($"{detail.HighDie}/{detail.LowDie}", Colors.White);
                    builder.Add(" → ", Colors.White);
                    builder.Add(roll.ToString(), Colors.White);
                    break;
                case MultiDiceLuckMode.Disadvantage:
                    builder.Add("2d20", Colors.White);
                    builder.AddSpace();
                    builder.Add("unluck", ColorPalette.Error);
                    builder.AddSpace();
                    builder.Add($"{detail.HighDie}/{detail.LowDie}", Colors.White);
                    builder.Add(" → ", Colors.White);
                    builder.Add(roll.ToString(), Colors.White);
                    break;
                case MultiDiceLuckMode.Cancelled:
                    builder.Add(roll.ToString(), Colors.White);
                    builder.Add(" (luck cancel)", Colors.Gray);
                    break;
                default:
                    builder.Add(roll.ToString(), Colors.White);
                    break;
            }
        }

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
            Action? action = null,
            bool targetUsesArmorPool = false,
            MultiDiceRollDetail multiDiceDetail = default)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("     (", Colors.Gray);
            builder.Add("roll:", ColorPalette.Info);
            builder.AddSpace();
            AppendRollValue(builder, roll, multiDiceDetail);
            
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
                if (targetUsesArmorPool)
                    AddAttackWithArmorPool(builder, rawDamage, targetDefense);
                else
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
