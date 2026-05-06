using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;

namespace RPGGame.UI.ColorSystem.Themes
{
    /// <summary>
    /// Item display lines using shared themes; composed names delegate to <see cref="ItemNameFormatter"/>.
    /// </summary>
    public static class ItemThemeFormatter
    {
        /// <summary>
        /// Formats an item name — delegates to <see cref="ItemNameFormatter"/> for one parsing/color pipeline.
        /// </summary>
        public static List<ColoredText> FormatItemNameWithThemes(Item item, bool includeModifications = true)
        {
            if (!includeModifications)
                return ItemNameFormatter.FormatSimpleItemName(item);
            return ItemNameFormatter.FormatFullItemName(item);
        }
        
        /// <summary>
        /// Formats a full item display with all color themes
        /// Shows name, type, tier, and modifications with their respective colors
        /// </summary>
        public static List<List<ColoredText>> FormatFullItemDisplay(Item item)
        {
            var lines = new List<List<ColoredText>>();
            var themes = ItemThemeProvider.GetItemThemes(item);
            
            var nameLine = FormatItemNameWithThemes(item, includeModifications: true);
            lines.Add(nameLine);
            
            var infoBuilder = new ColoredTextBuilder();
            foreach (var segment in themes.ItemTypeTheme)
            {
                infoBuilder.Add(segment);
            }
            
            if (item is WeaponItem && themes.WeaponTypeTheme != null)
            {
                infoBuilder.Add(" (");
                foreach (var segment in themes.WeaponTypeTheme)
                {
                    infoBuilder.Add(segment);
                }
                infoBuilder.Add(")");
            }
            
            infoBuilder.Add(" | ");
            foreach (var segment in themes.TierTheme)
            {
                infoBuilder.Add(segment);
            }
            
            lines.Add(infoBuilder.Build());
            
            return lines;
        }
        
        /// <summary>
        /// Formats modifications list with their color themes (keyword phrases use affix rank themes).
        /// </summary>
        public static List<ColoredText> FormatModificationsList(Item item)
        {
            var builder = new ColoredTextBuilder();
            
            if (item.Modifications.Count == 0)
                return builder.Build();
            
            builder.Add("Modifications: ", ColorPalette.Gray.GetColor());
            
            for (int i = 0; i < item.Modifications.Count; i++)
            {
                var mod = item.Modifications[i];
                var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(mod.Name);
                
                if (!string.IsNullOrEmpty(prefix))
                    builder.Add(prefix, ItemThemeProvider.AffixConnectorColor);

                string rank = string.IsNullOrWhiteSpace(mod.ItemRank) ? (item.Rarity ?? "Common") : mod.ItemRank;
                if (!string.IsNullOrEmpty(keyword))
                    builder.AddRange(ItemThemeProvider.GetModificationSuffixKeywordTheme(keyword, rank));
                else
                    builder.AddRange(ItemThemeProvider.GetModificationTheme(mod.Name.ToLowerInvariant(), rank));

                if (i < item.Modifications.Count - 1)
                    builder.Add(", ", ColorPalette.Gray.GetColor());
            }
            
            return builder.Build();
        }
    }
}
