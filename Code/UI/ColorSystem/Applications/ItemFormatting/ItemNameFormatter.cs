using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
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
            string fallbackRank = string.IsNullOrWhiteSpace(item.Rarity) ? "Common" : item.Rarity.Trim();
            
            string fullName = item.Name ?? "(unnamed item)";
            string remainingName = ItemNameParser.RemoveRarityPrefix(fullName);
            
            var (nameAfterPrefixes, prefixMods) = ItemNameParser.ExtractPrefixModifications(item, remainingName);
            foreach (var prefixMod in prefixMods)
            {
                Modification? prefixModification = item.Modifications?.FirstOrDefault(m =>
                    m != null &&
                    !string.IsNullOrEmpty(m.Name) &&
                    string.Equals(m.Name, prefixMod, StringComparison.OrdinalIgnoreCase));

                builder.AddRange(ItemThemeProvider.GetPrefixWordTheme(prefixModification, prefixMod, fallbackRank));
                builder.AddSpace();
            }
            
            var parsed = ItemNameParser.ParseItemName(item, nameAfterPrefixes);
            
            AddBaseNameWithTheming(builder, parsed.BaseName, item);
            
            foreach (var (suffixName, isStatBonus) in parsed.Suffixes)
            {
                builder.AddSpace();
                
                var (connector, keyword) = ItemKeywordExtractor.ExtractKeyword(suffixName);
                
                if (!string.IsNullOrEmpty(connector))
                    builder.Add(connector, ItemThemeProvider.AffixConnectorColor);

                if (isStatBonus)
                {
                    StatBonus? sb = item.StatBonuses?.FirstOrDefault(b =>
                        b != null &&
                        !string.IsNullOrEmpty(b.Name) &&
                        string.Equals(b.Name, suffixName, StringComparison.OrdinalIgnoreCase));
                    string affixTier = sb?.Rarity ?? fallbackRank;
                    builder.AddRange(ItemThemeProvider.GetStatBonusKeywordTheme(keyword, affixTier));
                }
                else
                {
                    Modification? suffMod = item.Modifications?.FirstOrDefault(m =>
                        m != null &&
                        !string.IsNullOrEmpty(m.Name) &&
                        string.Equals(m.Name, suffixName, StringComparison.OrdinalIgnoreCase));
                    string affixRank = string.IsNullOrWhiteSpace(suffMod?.ItemRank) ? fallbackRank : suffMod!.ItemRank!;
                    builder.AddRange(ItemThemeProvider.GetModificationSuffixKeywordTheme(keyword, affixRank));
                }
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
                    builder.AddRange(ItemThemeProvider.GetBaseLeadingWordTheme(words[i], rarity));
            }
        }

        private static Color GetBaseEquipmentCoreColor(Item item)
        {
            var theme = ItemThemeProvider.GetItemTypeTheme(item.Type);
            if (theme != null && theme.Count > 0)
                return theme[0].Color;
            return ItemThemeProvider.GetTierColorForName(item.Tier);
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
