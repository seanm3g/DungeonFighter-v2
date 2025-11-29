using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Formats item statistics for display
    /// </summary>
    public static class ItemStatFormatter
    {
        /// <summary>
        /// Gets formatted item stats for display as a list (one stat per line)
        /// </summary>
        public static List<string> GetItemStats(Item item, Character character)
        {
            var stats = new List<string>();
            
            if (item is WeaponItem weapon)
            {
                stats.Add($"Damage: {weapon.GetTotalDamage()}");
                stats.Add($"Speed: {weapon.GetTotalAttackSpeed():F1}s");
            }
            else if (item is HeadItem headItem)
            {
                stats.Add($"Armor: +{headItem.GetTotalArmor()}");
            }
            else if (item is FeetItem feetItem)
            {
                stats.Add($"Armor: +{feetItem.GetTotalArmor()}");
            }
            else if (item is ChestItem chestItem)
            {
                stats.Add($"Armor: +{chestItem.GetTotalArmor()}");
            }
            
            if (item.StatBonuses.Count > 0)
            {
                foreach (var bonus in item.StatBonuses)
                {
                    string formattedValue = bonus.StatType switch
                    {
                        "AttackSpeed" => $"+{bonus.Value:F3} AttackSpeed",
                        _ => $"+{bonus.Value} {bonus.StatType}"
                    };
                    
                    if (!string.IsNullOrEmpty(bonus.Name))
                        stats.Add($"{bonus.Name}: {formattedValue}");
                    else
                        stats.Add(formattedValue);
                }
            }
            
            return stats;
        }

        /// <summary>
        /// Formats a stat line string into colored text segments
        /// </summary>
        public static List<ColoredText> FormatStatLine(string stat)
        {
            var builder = new ColoredTextBuilder();
            builder.Add("    ", Colors.White);
            
            if (stat.StartsWith("Armor: +"))
            {
                var parts = stat.Split(new[] { ": +" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Armor: +", ColorPalette.Info);
                    builder.Add(parts[1], ColorPalette.Success);
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Damage: "))
            {
                var parts = stat.Split(new[] { ": " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Damage: ", ColorPalette.Info);
                    builder.Add(parts[1], ColorPalette.Damage);
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.StartsWith("Speed: "))
            {
                var parts = stat.Split(new[] { ": " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    builder.Add("Speed: ", ColorPalette.Info);
                    builder.Add(parts[1], Colors.White);
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else if (stat.Contains("+") && stat.Contains(" "))
            {
                var plusIndex = stat.IndexOf("+");
                if (plusIndex > 0)
                {
                    builder.Add(stat.Substring(0, plusIndex), ColorPalette.Info);
                    var rest = stat.Substring(plusIndex);
                    var spaceIndex = rest.IndexOf(" ");
                    if (spaceIndex > 0)
                    {
                        builder.Add(rest.Substring(0, spaceIndex + 1), ColorPalette.Success);
                        builder.Add(rest.Substring(spaceIndex + 1), Colors.White);
                    }
                    else
                    {
                        builder.Add(rest, ColorPalette.Success);
                    }
                }
                else
                {
                    builder.Add(stat, Colors.White);
                }
            }
            else
            {
                builder.Add(stat, Colors.White);
            }
            
            return builder.Build();
        }
    }
}

