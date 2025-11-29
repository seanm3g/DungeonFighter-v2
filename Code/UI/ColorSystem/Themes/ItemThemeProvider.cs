using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Themes
{
    /// <summary>
    /// Provides color themes for item properties (rarity, tier, modifications, item types)
    /// </summary>
    public static class ItemThemeProvider
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
        /// Gets the color for an item name based on its tier
        /// </summary>
        public static Color GetTierColorForName(int tier)
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
        public static Color GetRarityColor(string rarity)
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
    }
}

