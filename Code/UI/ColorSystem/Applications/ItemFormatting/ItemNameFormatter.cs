using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.UI.ColorSystem.Applications.ItemFormatting
{
    /// <summary>
    /// Formats item names with proper coloring
    /// Each prefix and modifier gets its own unique color from the theme system
    /// </summary>
    public static class ItemNameFormatter
    {
        /// <summary>
        /// Formats a full item name with prefixes, base name, and suffixes
        /// Each element (prefixes, base name, mods) gets its own unique color
        /// </summary>
        public static List<ColoredText> FormatFullItemName(Item item)
        {
            if (item == null)
            {
                return new List<ColoredText> { new ColoredText("(null item)", Colors.Red) };
            }
            
            var builder = new ColoredTextBuilder();
            var themes = ItemThemeProvider.GetItemThemes(item);
            
            string fullName = item.Name ?? "(unnamed item)";
            string remainingName = ItemNameParser.RemoveRarityPrefix(fullName);
            
            // Extract and color modification prefixes - each gets its own unique color
            var (nameAfterPrefixes, prefixMods) = ItemNameParser.ExtractPrefixModifications(item, remainingName);
            foreach (var prefixMod in prefixMods)
            {
                // Get the unique color theme for this specific prefix modifier
                if (themes.ModificationThemes.TryGetValue(prefixMod, out var prefixTheme))
                {
                    // Use the first color from the theme sequence for the prefix
                    var prefixColor = prefixTheme.Count > 0 ? prefixTheme[0].Color : ColorPalette.Success.GetColor();
                    builder.Add(prefixMod, prefixColor);
                }
                else
                {
                    // Fallback: get theme directly from ItemThemeProvider
                    var fallbackTheme = ItemThemeProvider.GetModificationTheme(prefixMod.ToLower(), item.Rarity);
                    var fallbackColor = fallbackTheme.Count > 0 ? fallbackTheme[0].Color : ColorPalette.Success.GetColor();
                    builder.Add(prefixMod, fallbackColor);
                }
                builder.AddSpace();
            }
            
            // Parse base name and suffixes
            var parsed = ItemNameParser.ParseItemName(item, nameAfterPrefixes);
            
            // Color base item name
            if (!string.IsNullOrWhiteSpace(parsed.BaseName))
            {
                builder.Add(parsed.BaseName, Colors.White);
            }
            
            // Add all suffixes with their own unique colors (only color the keyword)
            foreach (var (suffixName, isStatBonus) in parsed.Suffixes)
            {
                var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(suffixName);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Color only the keyword with its unique color
                Color keywordColor;
                if (isStatBonus)
                {
                    // Stat bonus: extract keyword and use theme system to get unique color
                    // For "of the Phoenix", look up template "phoenix" (the keyword)
                    var keywordLower = keyword.ToLower();
                    var statBonusTheme = ItemThemeProvider.GetModificationTheme(keywordLower, item.Rarity);
                    keywordColor = statBonusTheme.Count > 0 ? statBonusTheme[0].Color : ColorPalette.Info.GetColor();
                }
                else
                {
                    // Modification suffix: use theme from the modifications dictionary
                    if (themes.ModificationThemes.TryGetValue(suffixName, out var modTheme))
                    {
                        keywordColor = modTheme.Count > 0 ? modTheme[0].Color : ColorPalette.Magenta.GetColor();
                    }
                    else
                    {
                        // Fallback: get theme directly from ItemThemeProvider
                        var fallbackTheme = ItemThemeProvider.GetModificationTheme(suffixName.ToLower(), item.Rarity);
                        keywordColor = fallbackTheme.Count > 0 ? fallbackTheme[0].Color : ColorPalette.Magenta.GetColor();
                    }
                }
                
                builder.Add(keyword, keywordColor);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a simple item name with rarity color
        /// </summary>
        public static List<ColoredText> FormatSimpleItemName(Item item)
        {
            if (item == null)
            {
                return new List<ColoredText> { new ColoredText("(null item)", Colors.Red) };
            }
            
            var builder = new ColoredTextBuilder();
            var rarityColor = GetRarityColor(item.Rarity ?? "Common");
            builder.Add(item.Name ?? "(unnamed item)", rarityColor);
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

