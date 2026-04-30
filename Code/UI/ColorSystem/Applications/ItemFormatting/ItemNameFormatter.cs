using System;
using System.Collections.Generic;
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
        /// True when rarity is missing or Common (inventory and loot treat missing rarity as Common).
        /// </summary>
        public static bool IsCommonRarity(string? rarity) =>
            string.IsNullOrWhiteSpace(rarity) ||
            rarity.Trim().Equals("Common", StringComparison.OrdinalIgnoreCase);

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
            
            AddBaseNameWithTheming(builder, parsed.BaseName, item);
            
            // Add all suffixes with their own unique colors (only color the keyword)
            foreach (var (suffixName, isStatBonus) in parsed.Suffixes)
            {
                // Add space before each suffix (after base name or previous suffix keyword)
                builder.AddSpace();
                
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
        /// Colors the base name: equipment archetype uses item-type theme; leading words use material / template hints
        /// when they are not already emitted as prefix modifications (e.g. name baked as "GLASS BROGUES").
        /// </summary>
        private static void AddBaseNameWithTheming(ColoredTextBuilder builder, string baseName, Item item)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                return;

            string normalized = baseName.Trim();
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return;

            if (words.Length == 1)
            {
                builder.Add(words[0], GetBaseEquipmentCoreColor(item));
                return;
            }

            string rarity = item.Rarity ?? "Common";
            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0)
                    builder.AddSpace();
                if (i == words.Length - 1)
                    builder.Add(words[i], GetBaseEquipmentCoreColor(item));
                else
                    builder.Add(words[i], TryGetLeadingBaseNameWordColor(words[i], rarity));
            }
        }

        private static Color GetBaseEquipmentCoreColor(Item item)
        {
            var theme = ItemThemeProvider.GetItemTypeTheme(item.Type);
            if (theme != null && theme.Count > 0)
                return theme[0].Color;
            return ItemThemeProvider.GetTierColorForName(item.Tier);
        }

        private static Color TryGetLeadingBaseNameWordColor(string word, string itemRarity)
        {
            string lower = word.ToLowerInvariant();
            if (ColorTemplateLibrary.HasTemplate(lower))
            {
                var t = ItemThemeProvider.GetModificationTheme(lower, itemRarity);
                if (t != null && t.Count > 0)
                    return t[0].Color;
            }

            if (TryGetMaterialPaletteColor(word, out Color fromMaterial))
                return fromMaterial;

            var modTheme = ItemThemeProvider.GetModificationTheme(lower, itemRarity);
            if (modTheme != null && modTheme.Count > 0)
                return modTheme[0].Color;

            return Colors.White;
        }

        private static bool TryGetMaterialPaletteColor(string word, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(word))
                return false;

            string u = word.ToUpperInvariant();
            Color? c = u switch
            {
                "GLASS" => ColorPalette.Cyan.GetColor(),
                "CRYSTAL" => ColorPalette.Cyan.GetColor(),
                "OBSIDIAN" => ColorPalette.DarkBlue.GetColor(),
                "BONE" => ColorPalette.LightGray.GetColor(),
                "BRONZE" => ColorPalette.Bronze.GetColor(),
                "WILLOW" => ColorPalette.DarkGreen.GetColor(),
                "STEEL" => ColorPalette.Silver.GetColor(),
                "GOLD" => ColorPalette.Gold.GetColor(),
                "SILVER" => ColorPalette.Silver.GetColor(),
                "MITHRIL" => ColorPalette.Cyan.GetColor(),
                "DAMASCUS" => ColorPalette.Orange.GetColor(),
                "SHADOW" => ColorPalette.Purple.GetColor(),
                "STONE" => ColorPalette.Gray.GetColor(),
                "UNKNOWN" => ColorPalette.Purple.GetColor(),
                "STRANGE" => ColorPalette.Magenta.GetColor(),
                "CELESTIAL" => ColorPalette.Gold.GetColor(),
                _ => (Color?)null
            };
            if (!c.HasValue)
                return false;
            color = c.Value;
            return true;
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
            string rarityKey = string.IsNullOrWhiteSpace(item.Rarity) ? "Common" : item.Rarity.Trim();
            builder.Add(item.Name ?? "(unnamed item)", ItemThemeProvider.GetRarityColor(rarityKey));
            return builder.Build();
        }
    }
}

