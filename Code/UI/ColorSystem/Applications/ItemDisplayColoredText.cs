using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;
using RPGGame.UI.ColorSystem.Themes;

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
            // Use TrimEnd() to ensure no trailing spaces before the closing bracket
            builder.Add(rarityText, ItemThemeProvider.GetRarityColor(rarityText));
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
        /// Formats item comparison for equip decisions
        /// </summary>
        public static List<List<ColoredText>> FormatItemComparison(Item newItem, Item? currentItem)
        {
            return ItemComparisonFormatter.FormatItemComparison(newItem, currentItem);
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
            var rarityColor = ItemThemeProvider.GetRarityColor(rarityText);
            result.Add(new ColoredText("[", Colors.Gray));
            result.Add(new ColoredText(rarityText, rarityColor));
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
