using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Represents color themes for an item's various properties
    /// </summary>
    public class ItemColorThemes
    {
        /// <summary>
        /// Color theme for the item's rarity
        /// </summary>
        public List<ColoredText> RarityTheme { get; set; } = new List<ColoredText>();
        
        /// <summary>
        /// Color theme for the item's tier
        /// </summary>
        public List<ColoredText> TierTheme { get; set; } = new List<ColoredText>();
        
        /// <summary>
        /// Color themes for each modification
        /// </summary>
        public Dictionary<string, List<ColoredText>> ModificationThemes { get; set; } = new Dictionary<string, List<ColoredText>>();
        
        /// <summary>
        /// Color theme for the item type (Weapon, Head, Chest, Feet)
        /// </summary>
        public List<ColoredText> ItemTypeTheme { get; set; } = new List<ColoredText>();
        
        /// <summary>
        /// Color theme for weapon type (Sword, Dagger, etc.) if applicable
        /// </summary>
        public List<ColoredText>? WeaponTypeTheme { get; set; } = null;
    }
    
    /// <summary>
    /// System for managing multiple color themes for items
    /// Provides color themes for rarity, tier, modifications, and item types
    /// </summary>
    public static class ItemColorThemeSystem
    {
        /// <summary>
        /// Gets all color themes for an item
        /// </summary>
        public static ItemColorThemes GetItemThemes(Item item)
        {
            var themes = new ItemColorThemes
            {
                RarityTheme = GetRarityTheme(item.Rarity),
                TierTheme = GetTierTheme(item.Tier),
                ItemTypeTheme = GetItemTypeTheme(item.Type),
                ModificationThemes = GetModificationThemes(item.Modifications)
            };
            
            // Add weapon type theme if applicable
            if (item is WeaponItem weapon)
            {
                themes.WeaponTypeTheme = GetWeaponTypeTheme(weapon.WeaponType);
            }
            
            return themes;
        }
        
        /// <summary>
        /// Gets color theme for rarity
        /// </summary>
        public static List<ColoredText> GetRarityTheme(string rarity)
        {
            var rarityLower = rarity.ToLower();
            
            // Use color templates from ColorTemplates.json where available
            return rarityLower switch
            {
                "common" => ColoredText.FromTemplate("common", rarity),
                "uncommon" => ColoredText.FromTemplate("uncommon", rarity),
                "rare" => ColoredText.FromTemplate("rare", rarity),
                "epic" => ColoredText.FromTemplate("epic", rarity),
                "legendary" => ColoredText.FromTemplate("legendary", rarity),
                "mythic" => ColoredText.FromTemplate("mythic", rarity),
                "transcendent" => ColoredText.FromTemplate("transcendent", rarity),
                _ => ColoredText.FromColor(rarity, Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color theme for tier
        /// Higher tiers get more vibrant colors
        /// </summary>
        public static List<ColoredText> GetTierTheme(int tier)
        {
            return tier switch
            {
                1 => ColoredText.FromColor($"Tier {tier}", Colors.Gray),
                2 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Green.GetColor()),
                3 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Blue.GetColor()),
                4 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Cyan.GetColor()),
                5 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Purple.GetColor()),
                6 => ColoredText.FromColor($"Tier {tier}", ColorPalette.Orange.GetColor()),
                7 => ColoredText.FromTemplate("golden", $"Tier {tier}"),
                8 => ColoredText.FromTemplate("legendary", $"Tier {tier}"),
                9 => ColoredText.FromTemplate("mythic", $"Tier {tier}"),
                >= 10 => ColoredText.FromTemplate("transcendent", $"Tier {tier}"),
                _ => ColoredText.FromColor($"Tier {tier}", Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color themes for modifications
        /// </summary>
        public static Dictionary<string, List<ColoredText>> GetModificationThemes(List<Modification> modifications)
        {
            var themes = new Dictionary<string, List<ColoredText>>();
            
            foreach (var mod in modifications)
            {
                var modNameLower = mod.Name.ToLower();
                var theme = GetModificationTheme(modNameLower, mod.ItemRank);
                themes[mod.Name] = theme;
            }
            
            return themes;
        }
        
        /// <summary>
        /// Gets color theme for a specific modification
        /// </summary>
        public static List<ColoredText> GetModificationTheme(string modificationName, string itemRank)
        {
            // Try to match by exact name first (from ColorTemplates.json)
            var modNameLower = modificationName.ToLower();
            
            // Check if there's a template for this modification
            if (ColorTemplateLibrary.HasTemplate(modNameLower))
            {
                return ColoredText.FromTemplate(modNameLower, modificationName);
            }
            
            // Fall back to rank-based coloring
            return itemRank.ToLower() switch
            {
                "common" => ColoredText.FromColor(modificationName, Colors.Gray),
                "uncommon" => ColoredText.FromColor(modificationName, ColorPalette.Green.GetColor()),
                "rare" => ColoredText.FromColor(modificationName, ColorPalette.Blue.GetColor()),
                "epic" => ColoredText.FromColor(modificationName, ColorPalette.Purple.GetColor()),
                "legendary" => ColoredText.FromTemplate("legendary", modificationName),
                "mythic" => ColoredText.FromTemplate("mythic", modificationName),
                "transcendent" => ColoredText.FromTemplate("transcendent", modificationName),
                _ => ColoredText.FromColor(modificationName, Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color theme for item type
        /// </summary>
        public static List<ColoredText> GetItemTypeTheme(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Weapon => ColoredText.FromTemplate("weapon_class", itemType.ToString()),
                ItemType.Head => ColoredText.FromTemplate("head_armor", itemType.ToString()),
                ItemType.Chest => ColoredText.FromTemplate("chest_armor", itemType.ToString()),
                ItemType.Feet => ColoredText.FromTemplate("feet_armor", itemType.ToString()),
                _ => ColoredText.FromColor(itemType.ToString(), Colors.White)
            };
        }
        
        /// <summary>
        /// Gets color theme for weapon type
        /// </summary>
        public static List<ColoredText> GetWeaponTypeTheme(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Sword => ColoredText.FromTemplate("sword_weapon", weaponType.ToString()),
                WeaponType.Dagger => ColoredText.FromTemplate("dagger_weapon", weaponType.ToString()),
                WeaponType.Mace => ColoredText.FromTemplate("mace_weapon", weaponType.ToString()),
                WeaponType.Wand => ColoredText.FromTemplate("wand_weapon", weaponType.ToString()),
                WeaponType.Staff => ColoredText.FromTemplate("arcane", weaponType.ToString()),
                WeaponType.Axe => ColoredText.FromTemplate("fiery", weaponType.ToString()),
                WeaponType.Bow => ColoredText.FromTemplate("natural", weaponType.ToString()),
                _ => ColoredText.FromColor(weaponType.ToString(), Colors.White)
            };
        }
        
        /// <summary>
        /// Formats an item name with multiple color themes
        /// Each element (rarity, base name, modifications, stat bonuses) gets its own color
        /// </summary>
        public static List<ColoredText> FormatItemNameWithThemes(Item item, bool includeModifications = true)
        {
            var builder = new ColoredTextBuilder();
            var themes = GetItemThemes(item);
            
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
                var tierColor = GetTierColorForName(item.Tier);
                builder.Add(remainingName, tierColor);
            }
            
            // 6. Add modification suffix (if found) - only color the keyword
            if (!string.IsNullOrEmpty(modificationSuffix))
            {
                builder.AddSpace();
                var (prefix, keyword) = ExtractKeyword(modificationSuffix);
                
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
                var (prefix, keyword) = ExtractKeyword(statBonusSuffix);
                
                // Add prefix in white (e.g., "of the ")
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Add(prefix, Colors.White);
                }
                
                // Color only the keyword
                var statBonusTheme = GetModificationTheme(statBonusSuffix.ToLower(), item.Rarity);
                var color = statBonusTheme.Count > 0 ? statBonusTheme[0].Color : ColorPalette.Info.GetColor();
                builder.Add(keyword, color);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Gets the color for an item name based on its tier
        /// </summary>
        private static Color GetTierColorForName(int tier)
        {
            return tier switch
            {
                1 => Colors.Gray,
                2 => ColorPalette.Green.GetColor(),
                3 => ColorPalette.Blue.GetColor(),
                4 => ColorPalette.Cyan.GetColor(),
                5 => ColorPalette.Purple.GetColor(),
                6 => ColorPalette.Orange.GetColor(),
                7 => ColorPalette.Gold.GetColor(),
                8 => ColorPalette.Orange.GetColor(), // Legendary color
                9 => ColorPalette.Purple.GetColor(), // Mythic color
                >= 10 => ColorPalette.White.GetColor(), // Transcendent color
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Gets a single color for rarity (helper method)
        /// </summary>
        private static Color GetRarityColor(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => Colors.White,
                "uncommon" => ColorPalette.Green.GetColor(),
                "rare" => ColorPalette.Blue.GetColor(),
                "epic" => ColorPalette.Purple.GetColor(),
                "legendary" => ColorPalette.Orange.GetColor(),
                "mythic" => ColorPalette.Purple.GetColor(),
                "transcendent" => ColorPalette.White.GetColor(),
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Formats a full item display with all color themes
        /// Shows name, type, tier, and modifications with their respective colors
        /// </summary>
        public static List<List<ColoredText>> FormatFullItemDisplay(Item item)
        {
            var lines = new List<List<ColoredText>>();
            var themes = GetItemThemes(item);
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
            var themes = GetItemThemes(item);
            
            if (item.Modifications.Count == 0)
            {
                return builder.Build();
            }
            
            builder.Add("Modifications: ", Colors.Gray);
            
            for (int i = 0; i < item.Modifications.Count; i++)
            {
                var mod = item.Modifications[i];
                var (prefix, keyword) = ExtractKeyword(mod.Name);
                
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
        
        /// <summary>
        /// Extracts the keyword from a modification or stat bonus name
        /// For "of the X" patterns, returns only "X" (the keyword)
        /// For other patterns, returns the full name
        /// </summary>
        private static (string prefix, string keyword) ExtractKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
                return ("", "");
            
            // Check for "of the X" pattern
            if (name.StartsWith("of the ", StringComparison.OrdinalIgnoreCase))
            {
                string keyword = name.Substring(7); // "of the " is 7 characters
                return ("of the ", keyword);
            }
            
            // Check for "of X" pattern (without "the")
            if (name.StartsWith("of ", StringComparison.OrdinalIgnoreCase))
            {
                string keyword = name.Substring(3); // "of " is 3 characters
                return ("of ", keyword);
            }
            
            // No prefix pattern, return full name as keyword
            return ("", name);
        }
    }
}

