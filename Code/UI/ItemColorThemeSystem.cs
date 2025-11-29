using System.Collections.Generic;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Themes;

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
    /// Refactored to use extracted theme provider and formatter
    /// </summary>
    public static class ItemColorThemeSystem
    {
        /// <summary>
        /// Gets all color themes for an item
        /// </summary>
        public static ItemColorThemes GetItemThemes(Item item)
        {
            return ItemThemeProvider.GetItemThemes(item);
        }
        
        /// <summary>
        /// Gets color theme for rarity
        /// </summary>
        public static List<ColoredText> GetRarityTheme(string rarity)
        {
            return ItemThemeProvider.GetRarityTheme(rarity);
        }
        
        /// <summary>
        /// Gets color theme for tier
        /// Higher tiers get more vibrant colors
        /// </summary>
        public static List<ColoredText> GetTierTheme(int tier)
        {
            return ItemThemeProvider.GetTierTheme(tier);
        }
        
        /// <summary>
        /// Gets color themes for modifications
        /// </summary>
        public static Dictionary<string, List<ColoredText>> GetModificationThemes(List<Modification> modifications)
        {
            return ItemThemeProvider.GetModificationThemes(modifications);
        }
        
        /// <summary>
        /// Gets color theme for a specific modification
        /// </summary>
        public static List<ColoredText> GetModificationTheme(string modificationName, string itemRank)
        {
            return ItemThemeProvider.GetModificationTheme(modificationName, itemRank);
        }
        
        /// <summary>
        /// Gets color theme for item type
        /// </summary>
        public static List<ColoredText> GetItemTypeTheme(ItemType itemType)
        {
            return ItemThemeProvider.GetItemTypeTheme(itemType);
        }
        
        /// <summary>
        /// Gets color theme for weapon type
        /// </summary>
        public static List<ColoredText> GetWeaponTypeTheme(WeaponType weaponType)
        {
            return ItemThemeProvider.GetWeaponTypeTheme(weaponType);
        }
        
        /// <summary>
        /// Formats an item name with multiple color themes
        /// Each element (rarity, base name, modifications, stat bonuses) gets its own color
        /// </summary>
        public static List<ColoredText> FormatItemNameWithThemes(Item item, bool includeModifications = true)
        {
            return ItemThemeFormatter.FormatItemNameWithThemes(item, includeModifications);
        }
        
        /// <summary>
        /// Formats a full item display with all color themes
        /// Shows name, type, tier, and modifications with their respective colors
        /// </summary>
        public static List<List<ColoredText>> FormatFullItemDisplay(Item item)
        {
            return ItemThemeFormatter.FormatFullItemDisplay(item);
        }
        
        /// <summary>
        /// Formats modifications list with their color themes (only colors the keyword)
        /// </summary>
        public static List<ColoredText> FormatModificationsList(Item item)
        {
            return ItemThemeFormatter.FormatModificationsList(item);
        }
    }
}

