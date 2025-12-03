using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Formats item displays using the new ColoredText system
    /// Provides clean, maintainable item formatting with proper color separation
    /// Refactored to use extracted formatters and parsers
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
            return ItemNameFormatter.FormatSimpleItemName(item);
        }
        
        /// <summary>
        /// Formats a full item name with prefixes, base name, and suffixes
        /// Each element (prefixes, base name, mods) gets its own color
        /// Note: Rarity should NOT be in the name - it's displayed separately in brackets
        /// </summary>
        public static List<ColoredText> FormatFullItemName(Item item)
        {
            return ItemNameFormatter.FormatFullItemName(item);
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
            builder.Add("[", Colors.Gray);
            var rarityText = item.Rarity?.TrimEnd() ?? "Common";
            var rarityColor = GetRarityColor(rarityText);
            // Use TrimEnd() to ensure no trailing spaces before the closing bracket
            builder.Add(rarityText, rarityColor);
            builder.Add("]", Colors.Gray);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats item stats and bonuses
        /// </summary>
        public static List<List<ColoredText>> FormatItemStats(Item item)
        {
            return ItemStatsFormatter.FormatItemStats(item);
        }
        
        /// <summary>
        /// Formats a stat bonus with appropriate color
        /// </summary>
        public static List<ColoredText> FormatStatBonus(StatBonus bonus)
        {
            return ItemStatsFormatter.FormatStatBonus(bonus);
        }
        
        /// <summary>
        /// Formats an item modification (only colors the keyword)
        /// </summary>
        public static List<ColoredText> FormatModification(Modification mod)
        {
            return ItemStatsFormatter.FormatModification(mod);
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
            builder.Add("[ ", Colors.DarkGray);
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
            return ItemComparisonFormatter.FormatItemComparison(newItem, currentItem);
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
        /// Formats loot for dungeon completion screen with rarity in brackets
        /// Format: [Rarity] ItemName (with each element colored)
        /// </summary>
        public static List<ColoredText> FormatLootForCompletion(Item item)
        {
            var result = new List<ColoredText>();
            
            // Add rarity in brackets with rarity color
            // Manually construct to avoid automatic spacing between bracket and rarity
            // Use Trim() to ensure no leading or trailing spaces before the closing bracket
            var rarityText = (item.Rarity?.Trim() ?? "Common");
            var rarityColor = GetRarityColor(rarityText);
            result.Add(new ColoredText("[", Colors.Gray));
            result.Add(new ColoredText(rarityText, rarityColor.GetColor()));
            result.Add(new ColoredText("]", Colors.Gray));
            
            // Add full item name with all colored elements
            var itemSegments = FormatFullItemName(item);
            
            // Add space between bracket and item name if needed
            if (itemSegments.Count > 0 && !string.IsNullOrEmpty(itemSegments[0].Text))
            {
                // Check if space is needed between ] and item name
                if (CombatLogSpacingManager.ShouldAddSpaceBetween("]", itemSegments[0].Text))
                {
                    result.Add(new ColoredText(" ", Colors.White));
                }
            }
            
            result.AddRange(itemSegments);
            
            return result;
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
