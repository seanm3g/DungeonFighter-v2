using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Applications.ItemFormatting
{
    /// <summary>
    /// Formats item statistics and bonuses
    /// </summary>
    public static class ItemStatsFormatter
    {
        /// <summary>
        /// Formats item stats and bonuses
        /// </summary>
        public static List<List<ColoredText>> FormatItemStats(Item item)
        {
            var lines = new List<List<ColoredText>>();
            
            // Type and Tier
            var typeLine = new ColoredTextBuilder();
            typeLine.Add("  Type: ", ColorPalette.Info);
            typeLine.Add(item.Type.ToString(), Colors.White);
            typeLine.Add(" | Tier: ", Colors.Gray);
            typeLine.Add(item.Tier.ToString(), ColorPalette.Warning);
            lines.Add(typeLine.Build());
            
            // Armor value (if applicable)
            if (item is HeadItem headArmor)
            {
                var armorLine = new ColoredTextBuilder();
                armorLine.Add("  Armor: ", ColorPalette.Info);
                armorLine.Add(headArmor.Armor.ToString(), ColorPalette.Success);
                lines.Add(armorLine.Build());
            }
            else if (item is ChestItem bodyArmor)
            {
                var armorLine = new ColoredTextBuilder();
                armorLine.Add("  Armor: ", ColorPalette.Info);
                armorLine.Add(bodyArmor.Armor.ToString(), ColorPalette.Success);
                lines.Add(armorLine.Build());
            }
            else if (item is FeetItem feetArmor)
            {
                var armorLine = new ColoredTextBuilder();
                armorLine.Add("  Armor: ", ColorPalette.Info);
                armorLine.Add(feetArmor.Armor.ToString(), ColorPalette.Success);
                lines.Add(armorLine.Build());
            }
            
            // Damage (for weapons)
            if (item is WeaponItem weapon)
            {
                var damageLine = new ColoredTextBuilder();
                damageLine.Add("  Damage: ", ColorPalette.Info);
                damageLine.Add(weapon.BaseDamage.ToString(), ColorPalette.Damage);
                lines.Add(damageLine.Build());
            }
            
            // Stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                var statsLine = new ColoredTextBuilder();
                statsLine.Add("  Stats: ", ColorPalette.Info);
                
                var bonuses = item.StatBonuses.Select(b => FormatStatBonus(b)).ToList();
                for (int i = 0; i < bonuses.Count; i++)
                {
                    statsLine.AddRange(bonuses[i]);
                    if (i < bonuses.Count - 1)
                    {
                        statsLine.Add(", ", Colors.Gray);
                    }
                }
                
                lines.Add(statsLine.Build());
            }
            
            // Action bonuses
            if (item.ActionBonuses.Count > 0)
            {
                var actionsLine = new ColoredTextBuilder();
                actionsLine.Add("  Actions: ", ColorPalette.Info);
                
                for (int i = 0; i < item.ActionBonuses.Count; i++)
                {
                    var bonus = item.ActionBonuses[i];
                    actionsLine.Add(bonus.Name, ColorPalette.Success);
                    actionsLine.Add(" +", Colors.White);
                    actionsLine.Add(bonus.Weight.ToString(), ColorPalette.Warning);
                    
                    if (i < item.ActionBonuses.Count - 1)
                    {
                        actionsLine.Add(", ", Colors.Gray);
                    }
                }
                
                lines.Add(actionsLine.Build());
            }
            
            // Modifications (only color the keyword)
            if (item.Modifications.Count > 0)
            {
                var modsLine = new ColoredTextBuilder();
                modsLine.Add("  Modifiers: ", ColorPalette.Info);
                
                for (int i = 0; i < item.Modifications.Count; i++)
                {
                    var mod = item.Modifications[i];
                    var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(mod.Name);
                    
                    // Add prefix in white (e.g., "of the ")
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        modsLine.Add(prefix, Colors.White);
                    }
                    
                    // Color only the keyword
                    bool isSuffix = mod.Name.StartsWith("of ", System.StringComparison.OrdinalIgnoreCase);
                    var modColor = isSuffix ? ColorPalette.Magenta : ColorPalette.Success;
                    modsLine.Add(keyword, modColor);
                    
                    if (i < item.Modifications.Count - 1)
                    {
                        modsLine.Add(", ", Colors.Gray);
                    }
                }
                
                lines.Add(modsLine.Build());
            }
            
            return lines;
        }
        
        /// <summary>
        /// Formats a stat bonus with appropriate color
        /// </summary>
        public static List<ColoredText> FormatStatBonus(StatBonus bonus)
        {
            var builder = new ColoredTextBuilder();
            
            var sign = bonus.Value >= 0 ? "+" : "";
            builder.Add($"{sign}{bonus.Value} ", bonus.Value >= 0 ? ColorPalette.Success : ColorPalette.Error);
            builder.Add(bonus.StatType, Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats an item modification (only colors the keyword)
        /// </summary>
        public static List<ColoredText> FormatModification(Modification mod)
        {
            var builder = new ColoredTextBuilder();
            
            var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(mod.Name);
            
            // Add prefix in white (e.g., "of the ")
            if (!string.IsNullOrEmpty(prefix))
            {
                builder.Add(prefix, Colors.White);
            }
            
            // Color only the keyword
            bool isSuffix = mod.Name.StartsWith("of ", System.StringComparison.OrdinalIgnoreCase);
            var color = isSuffix ? ColorPalette.Magenta : ColorPalette.Success;
            builder.Add(keyword, color);
            
            return builder.Build();
        }
    }
}

