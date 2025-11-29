using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Applications.ItemFormatting
{
    /// <summary>
    /// Formats item names with proper coloring
    /// </summary>
    public static class ItemNameFormatter
    {
        /// <summary>
        /// Formats a full item name with prefixes, base name, and suffixes
        /// Each element (prefixes, base name, mods) gets its own color
        /// </summary>
        public static List<ColoredText> FormatFullItemName(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            string fullName = item.Name;
            string remainingName = ItemNameParser.RemoveRarityPrefix(fullName);
            
            // Extract and color modification prefixes
            var (nameAfterPrefixes, prefixMods) = ItemNameParser.ExtractPrefixModifications(item, remainingName);
            foreach (var prefixMod in prefixMods)
            {
                builder.Add(prefixMod, ColorPalette.Success);
                builder.AddSpace();
            }
            
            // Parse base name and suffixes
            var parsed = ItemNameParser.ParseItemName(item, nameAfterPrefixes);
            
            // Color base item name
            if (!string.IsNullOrWhiteSpace(parsed.BaseName))
            {
                builder.Add(parsed.BaseName, Colors.White);
            }
            
            // Add all suffixes with their own colors (only color the keyword)
            foreach (var (suffixName, isStatBonus) in parsed.Suffixes)
            {
                var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(suffixName);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Color only the keyword
                if (isStatBonus)
                {
                    builder.Add(keyword, ColorPalette.Info);
                }
                else
                {
                    builder.Add(keyword, ColorPalette.Magenta);
                }
            }
            
            return builder.Build();
        }
        
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
        /// Gets the color for an item rarity
        /// </summary>
        private static ColorPalette GetRarityColor(string rarity)
        {
            var rarityColors = new Dictionary<string, ColorPalette>(StringComparer.OrdinalIgnoreCase)
            {
                ["Common"] = ColorPalette.Common,
                ["Uncommon"] = ColorPalette.Uncommon,
                ["Rare"] = ColorPalette.Rare,
                ["Epic"] = ColorPalette.Epic,
                ["Legendary"] = ColorPalette.Legendary,
                ["Mythic"] = ColorPalette.Purple,
                ["Transcendent"] = ColorPalette.Gold
            };
            
            if (rarityColors.TryGetValue(rarity, out var color))
            {
                return color;
            }
            return ColorPalette.Common;
        }
    }
}

