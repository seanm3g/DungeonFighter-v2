using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;

namespace RPGGame.UI.ColorSystem.Themes
{
    /// <summary>
    /// Formats item names and displays using color themes
    /// </summary>
    public static class ItemThemeFormatter
    {
        /// <summary>
        /// Formats an item name with multiple color themes
        /// Each element (rarity, base name, modifications, stat bonuses) gets its own color
        /// </summary>
        public static List<ColoredText> FormatItemNameWithThemes(Item item, bool includeModifications = true)
        {
            var builder = new ColoredTextBuilder();
            var themes = ItemThemeProvider.GetItemThemes(item);
            
            string fullName = item.Name;
            string remainingName = fullName;
            
            // 1. Extract and color rarity (if present at start of name)
            if (!string.IsNullOrEmpty(item.Rarity) && 
                remainingName.StartsWith(item.Rarity, StringComparison.OrdinalIgnoreCase))
            {
                // Add rarity with its theme
                foreach (var segment in themes.RarityTheme)
                {
                    builder.Add(segment);
                }
                builder.AddSpace();
                
                // Remove rarity from remaining name
                remainingName = remainingName.Substring(item.Rarity.Length).TrimStart();
            }
            
            // 2. Extract and color modification prefixes (not starting with "of ")
            // Process in order they appear in the name
            if (includeModifications)
            {
                bool foundMod = true;
                while (foundMod && !string.IsNullOrWhiteSpace(remainingName))
                {
                    foundMod = false;
                    foreach (var mod in item.Modifications)
                    {
                        if (!mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase) &&
                            remainingName.StartsWith(mod.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            // Check if it's at word boundary
                            int modLength = mod.Name.Length;
                            if (remainingName.Length == modLength || 
                                remainingName[modLength] == ' ')
                            {
                                if (themes.ModificationThemes.TryGetValue(mod.Name, out var modTheme))
                                {
                                    foreach (var segment in modTheme)
                                    {
                                        builder.Add(segment);
                                    }
                                    builder.AddSpace();
                                }
                                
                                // Remove modification from remaining name
                                remainingName = remainingName.Substring(modLength).TrimStart();
                                foundMod = true;
                                break; // Restart loop to check for more modifications
                            }
                        }
                    }
                }
            }
            
            // 3. Extract suffixes from the end (stat bonuses and modification suffixes)
            // Stat bonuses take priority over modification suffixes
            string statBonusSuffix = "";
            string modificationSuffix = "";
            
            if (includeModifications && item.StatBonuses.Count > 0)
            {
                // Try to match stat bonus names from the end first
                foreach (var statBonus in item.StatBonuses)
                {
                    if (!string.IsNullOrEmpty(statBonus.Name) &&
                        remainingName.EndsWith(statBonus.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if it's at word boundary
                        int bonusLength = statBonus.Name.Length;
                        int startIndex = remainingName.Length - bonusLength;
                        if (startIndex == 0 || remainingName[startIndex - 1] == ' ')
                        {
                            statBonusSuffix = statBonus.Name;
                            remainingName = remainingName.Substring(0, startIndex).TrimEnd();
                            break;
                        }
                    }
                }
            }
            
            // Only check for modification suffixes if no stat bonus was found
            if (string.IsNullOrEmpty(statBonusSuffix) && includeModifications)
            {
                foreach (var mod in item.Modifications)
                {
                    if (mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase) &&
                        remainingName.EndsWith(mod.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if it's at word boundary
                        int modLength = mod.Name.Length;
                        int startIndex = remainingName.Length - modLength;
                        if (startIndex == 0 || remainingName[startIndex - 1] == ' ')
                        {
                            modificationSuffix = mod.Name;
                            remainingName = remainingName.Substring(0, startIndex).TrimEnd();
                            break;
                        }
                    }
                }
            }
            
            // 5. Color base item name by tier (what's left)
            if (!string.IsNullOrWhiteSpace(remainingName))
            {
                var tierColor = ItemThemeProvider.GetTierColorForName(item.Tier);
                builder.Add(remainingName, tierColor);
            }
            
            // 6. Add modification suffix (if found) - only color the keyword
            if (!string.IsNullOrEmpty(modificationSuffix))
            {
                builder.AddSpace();
                var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(modificationSuffix);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Color only the keyword
                if (themes.ModificationThemes.TryGetValue(modificationSuffix, out var modTheme))
                {
                    // Get the color from the first segment of the theme
                    var color = modTheme.Count > 0 ? modTheme[0].Color : ColorPalette.Magenta.GetColor();
                    builder.Add(keyword, color);
                }
                else
                {
                    builder.Add(keyword, ColorPalette.Magenta.GetColor());
                }
            }
            
            // 7. Add stat bonus suffix (if found) - only color the keyword
            if (!string.IsNullOrEmpty(statBonusSuffix))
            {
                builder.AddSpace();
                var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(statBonusSuffix);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Color only the keyword
                var statBonusTheme = ItemThemeProvider.GetModificationTheme(statBonusSuffix.ToLower(), item.Rarity);
                var color = statBonusTheme.Count > 0 ? statBonusTheme[0].Color : ColorPalette.Info.GetColor();
                builder.Add(keyword, color);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Formats a full item display with all color themes
        /// Shows name, type, tier, and modifications with their respective colors
        /// </summary>
        public static List<List<ColoredText>> FormatFullItemDisplay(Item item)
        {
            var lines = new List<List<ColoredText>>();
            var themes = ItemThemeProvider.GetItemThemes(item);
            var builder = new ColoredTextBuilder();
            
            // Line 1: Item name with rarity and modifications
            var nameLine = FormatItemNameWithThemes(item, includeModifications: true);
            lines.Add(nameLine);
            
            // Line 2: Type, Tier, and Weapon Type (if applicable)
            var infoBuilder = new ColoredTextBuilder();
            // Add each segment of item type theme
            foreach (var segment in themes.ItemTypeTheme)
            {
                infoBuilder.Add(segment);
            }
            
            if (item is WeaponItem weapon && themes.WeaponTypeTheme != null)
            {
                infoBuilder.Add(" (");
                // Add each segment of weapon type theme
                foreach (var segment in themes.WeaponTypeTheme)
                {
                    infoBuilder.Add(segment);
                }
                infoBuilder.Add(")");
            }
            
            infoBuilder.Add(" | ");
            // Add each segment of tier theme
            foreach (var segment in themes.TierTheme)
            {
                infoBuilder.Add(segment);
            }
            
            lines.Add(infoBuilder.Build());
            
            return lines;
        }
        
        /// <summary>
        /// Formats modifications list with their color themes (only colors the keyword)
        /// </summary>
        public static List<ColoredText> FormatModificationsList(Item item)
        {
            var builder = new ColoredTextBuilder();
            var themes = ItemThemeProvider.GetItemThemes(item);
            
            if (item.Modifications.Count == 0)
            {
                return builder.Build();
            }
            
            builder.Add("Modifications: ", Colors.Gray);
            
            for (int i = 0; i < item.Modifications.Count; i++)
            {
                var mod = item.Modifications[i];
                var (prefix, keyword) = ItemKeywordExtractor.ExtractKeyword(mod.Name);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Try to get theme for the full name, but only apply color to keyword
                if (themes.ModificationThemes.TryGetValue(mod.Name, out var modTheme))
                {
                    // Get the color from the first segment of the theme (or use default)
                    var color = modTheme.Count > 0 ? modTheme[0].Color : Colors.White;
                    builder.Add(keyword, color);
                }
                else
                {
                    // No theme found, use default color based on suffix/prefix
                    bool isSuffix = mod.Name.StartsWith("of ", StringComparison.OrdinalIgnoreCase);
                    var color = isSuffix ? ColorPalette.Magenta.GetColor() : ColorPalette.Success.GetColor();
                    builder.Add(keyword, color);
                }
                
                if (i < item.Modifications.Count - 1)
                {
                    builder.Add(", ", Colors.Gray);
                }
            }
            
            return builder.Build();
        }
    }
}

