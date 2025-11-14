using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Formats item displays using the new ColoredText system
    /// Provides clean, maintainable item formatting with proper color separation
    /// </summary>
    public static class ItemDisplayColoredText
    {
        /// <summary>
        /// Rarity color mapping
        /// </summary>
        private static readonly Dictionary<string, ColorPalette> RarityColors = new Dictionary<string, ColorPalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["Common"] = ColorPalette.Common,
            ["Uncommon"] = ColorPalette.Uncommon,
            ["Rare"] = ColorPalette.Rare,
            ["Epic"] = ColorPalette.Epic,
            ["Legendary"] = ColorPalette.Legendary,
            ["Mythic"] = ColorPalette.Purple,
            ["Transcendent"] = ColorPalette.Gold
        };
        
        /// <summary>
        /// Formats a simple item name with rarity color
        /// </summary>
        public static List<ColoredText> FormatSimpleItemName(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            var rarityColor = GetRarityColor(item.Rarity);
            builder.Add(item.Name, rarityColor);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a full item name with prefixes, base name, and suffixes
        /// </summary>
        public static List<ColoredText> FormatFullItemName(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Display modifications before/after name based on naming convention
            // Prefixes typically start with certain words, suffixes start with "of "
            if (item.Modifications.Count > 0)
            {
                foreach (var mod in item.Modifications)
                {
                    // Simple heuristic: if name starts with "of ", it's a suffix
                    bool isSuffix = mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase);
                    
                    if (!isSuffix)
                    {
                        // Add prefix before item name
                        builder.Add(mod.Name, ColorPalette.Success);
                        builder.Add(" ", Colors.White);
                    }
                }
            }
            
            // Base item name with rarity color
            var rarityColor = GetRarityColor(item.Rarity);
            builder.Add(item.Name, rarityColor);
            
            // Add suffixes after name
            if (item.Modifications.Count > 0)
            {
                foreach (var mod in item.Modifications)
                {
                    bool isSuffix = mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase);
                    
                    if (isSuffix)
                    {
                        // Add suffix after item name
                        builder.Add(" ", Colors.White);
                        builder.Add(mod.Name, ColorPalette.Info);
                    }
                }
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats item with rarity tag
        /// </summary>
        public static List<ColoredText> FormatItemWithRarity(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Full item name
            var nameSegments = FormatFullItemName(item);
            builder.Add(nameSegments);
            
            // Rarity tag
            builder.Add(" [", Colors.Gray);
            var rarityColor = GetRarityColor(item.Rarity);
            builder.Add(item.Rarity, rarityColor);
            builder.Add("]", Colors.Gray);
            
            return builder.Build();
        }
        
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
                    statsLine.Add(bonuses[i]);
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
            
            // Modifications
            if (item.Modifications.Count > 0)
            {
                var modsLine = new ColoredTextBuilder();
                modsLine.Add("  Modifiers: ", ColorPalette.Info);
                
                for (int i = 0; i < item.Modifications.Count; i++)
                {
                    var mod = item.Modifications[i];
                    // Use success color for modifications since we don't have a Type property
                    var modColor = ColorPalette.Success;
                    modsLine.Add(mod.Name, modColor);
                    
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
        /// Formats an item modification
        /// </summary>
        public static List<ColoredText> FormatModification(Modification mod)
        {
            var builder = new ColoredTextBuilder();
            
            // Use heuristic: if name starts with "of ", it's a suffix (blue), otherwise prefix (green)
            bool isSuffix = mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase);
            var color = isSuffix ? ColorPalette.Info : ColorPalette.Success;
            builder.Add(mod.Name, color);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats an inventory list item
        /// </summary>
        public static List<ColoredText> FormatInventoryItem(int index, Item item)
        {
            var builder = new ColoredTextBuilder();
            
            // Index
            builder.Add($"{index}. ", Colors.Gray);
            
            // Item name with colors
            var nameSegments = FormatFullItemName(item);
            builder.Add(nameSegments);
            
            // Type and tier in brackets
            builder.Add(" [", Colors.DarkGray);
            builder.Add(item.Type.ToString(), ColorPalette.Info);
            builder.Add(" T", Colors.Gray);
            builder.Add(item.Tier.ToString(), ColorPalette.Warning);
            builder.Add("]", Colors.DarkGray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats equipped item display
        /// </summary>
        public static List<ColoredText> FormatEquippedItem(string slotName, Item? item)
        {
            var builder = new ColoredTextBuilder();
            
            // Slot name
            builder.Add(slotName, ColorPalette.Info);
            builder.Add(": ", Colors.White);
            
            if (item != null)
            {
                // Equipped item
                var itemSegments = FormatFullItemName(item);
                builder.Add(itemSegments);
                
                // Quick stats
                if (item is WeaponItem weaponItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(weaponItem.BaseDamage.ToString(), ColorPalette.Damage);
                    builder.Add(" dmg", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
                else if (item is HeadItem headItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(headItem.Armor.ToString(), ColorPalette.Success);
                    builder.Add(" armor", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
                else if (item is ChestItem bodyItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(bodyItem.Armor.ToString(), ColorPalette.Success);
                    builder.Add(" armor", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
                else if (item is FeetItem feetItem)
                {
                    builder.Add(" (", Colors.Gray);
                    builder.Add(feetItem.Armor.ToString(), ColorPalette.Success);
                    builder.Add(" armor", Colors.Gray);
                    builder.Add(")", Colors.Gray);
                }
            }
            else
            {
                // Empty slot
                builder.Add("(empty)", Colors.DarkGray);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats item comparison for equip decisions
        /// </summary>
        public static List<List<ColoredText>> FormatItemComparison(Item newItem, Item? currentItem)
        {
            var lines = new List<List<ColoredText>>();
            
            // New item header
            var newHeader = new ColoredTextBuilder();
            newHeader.Add("NEW: ", ColorPalette.Success);
            newHeader.Add(FormatItemWithRarity(newItem));
            lines.Add(newHeader.Build());
            
            // New item stats
            lines.AddRange(FormatItemStats(newItem));
            
            // Spacer
            lines.Add(new List<ColoredText>());
            
            // Current item header
            if (currentItem != null)
            {
                var currentHeader = new ColoredTextBuilder();
                currentHeader.Add("CURRENT: ", ColorPalette.Warning);
                currentHeader.Add(FormatItemWithRarity(currentItem));
                lines.Add(currentHeader.Build());
                
                // Current item stats
                lines.AddRange(FormatItemStats(currentItem));
            }
            else
            {
                var emptyLine = new ColoredTextBuilder();
                emptyLine.Add("CURRENT: ", ColorPalette.Warning);
                emptyLine.Add("(empty slot)", Colors.DarkGray);
                lines.Add(emptyLine.Build());
            }
            
            return lines;
        }
        
        /// <summary>
        /// Formats loot drop message
        /// </summary>
        public static List<ColoredText> FormatLootDrop(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            builder.Add("Found: ", ColorPalette.Success);
            var itemSegments = FormatItemWithRarity(item);
            builder.Add(itemSegments);
            builder.Add("!", Colors.White);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Gets the color for an item rarity
        /// </summary>
        private static ColorPalette GetRarityColor(string rarity)
        {
            if (RarityColors.TryGetValue(rarity, out var color))
            {
                return color;
            }
            return ColorPalette.Common; // Default to common/white
        }
        
        /// <summary>
        /// Extension method to add multiple colored text segments
        /// </summary>
        private static ColoredTextBuilder Add(this ColoredTextBuilder builder, List<ColoredText> segments)
        {
            foreach (var segment in segments)
            {
                builder.Add(segment.Text, segment.Color);
            }
            return builder;
        }
    }
}
