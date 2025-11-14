using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Color system for item display
    /// Provides consistent coloring for items based on rarity, type, and properties
    /// </summary>
    public static class ItemColorSystem
    {
        /// <summary>
        /// Gets the color for an item's rarity
        /// </summary>
        public static Color GetRarityColor(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => ColorPalette.Gray.GetColor(),
                "uncommon" => ColorPalette.Green.GetColor(),
                "rare" => ColorPalette.Blue.GetColor(),
                "epic" => ColorPalette.Purple.GetColor(),
                "legendary" => ColorPalette.Orange.GetColor(),
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Gets colored text for an item name based on rarity
        /// </summary>
        public static List<ColoredText> ColorItemName(string itemName, string rarity)
        {
            var color = GetRarityColor(rarity);
            return new List<ColoredText> { new ColoredText(itemName, color) };
        }
        
        /// <summary>
        /// Gets the color for item stats
        /// </summary>
        public static Color GetStatColor(string statType)
        {
            return statType.ToLower() switch
            {
                "damage" => ColorPalette.Red.GetColor(),
                "armor" => ColorPalette.Blue.GetColor(),
                "health" => ColorPalette.Green.GetColor(),
                "str" or "strength" => ColorPalette.Red.GetColor(),
                "agi" or "agility" => ColorPalette.Yellow.GetColor(),
                "tec" or "technique" => ColorPalette.Cyan.GetColor(),
                "int" or "intelligence" => ColorPalette.Magenta.GetColor(),
                _ => Colors.White
            };
        }
        
        /// <summary>
        /// Formats an item with colored text
        /// </summary>
        public static List<ColoredText> FormatItem(Item item)
        {
            var result = new List<ColoredText>();
            
            // Add item name with rarity color
            var color = GetRarityColor(item.Rarity);
            result.Add(new ColoredText(item.Name, color));
            
            return result;
        }
        
        /// <summary>
        /// Formats an item's full display with stats
        /// </summary>
        public static List<List<ColoredText>> FormatItemDetailed(Item item)
        {
            var lines = new List<List<ColoredText>>();
            
            // Item name line
            var nameLine = new List<ColoredText>
            {
                new ColoredText(item.Name, GetRarityColor(item.Rarity))
            };
            lines.Add(nameLine);
            
            // Type and tier line
            var typeLine = new List<ColoredText>
            {
                new ColoredText($"{item.Type} | Tier {item.Tier}", ColorPalette.Gray.GetColor())
            };
            lines.Add(typeLine);
            
            return lines;
        }
        
        /// <summary>
        /// Formats a simple item display (name with rarity color)
        /// </summary>
        public static List<ColoredText> FormatSimpleItemDisplay(Item item)
        {
            return ColorItemName(item.Name, item.Rarity);
        }
        
        /// <summary>
        /// Formats full item name with rarity and type
        /// </summary>
        public static List<ColoredText> FormatFullItemName(Item item)
        {
            var result = new List<ColoredText>();
            
            // Rarity indicator
            result.Add(new ColoredText("[", Colors.Gray));
            result.Add(new ColoredText(item.Rarity, GetRarityColor(item.Rarity)));
            result.Add(new ColoredText("] ", Colors.Gray));
            
            // Item name
            result.Add(new ColoredText(item.Name, GetRarityColor(item.Rarity)));
            
            return result;
        }
        
        /// <summary>
        /// Formats a stat bonus
        /// </summary>
        public static List<ColoredText> FormatStatBonus(StatBonus statBonus)
        {
            var result = new List<ColoredText>();
            
            var sign = statBonus.Value >= 0 ? "+" : "";
            var color = statBonus.Value >= 0 ? ColorPalette.Green.GetColor() : ColorPalette.Red.GetColor();
            
            result.Add(new ColoredText($"{sign}{statBonus.Value} ", color));
            result.Add(new ColoredText(statBonus.StatType, GetStatColor(statBonus.StatType)));
            
            return result;
        }
        
        /// <summary>
        /// Formats a modification
        /// </summary>
        public static List<ColoredText> FormatModification(Modification modification)
        {
            var result = new List<ColoredText>();
            
            result.Add(new ColoredText(modification.Name, ColorPalette.Orange.GetColor()));
            
            if (modification.RolledValue != 0)
            {
                var sign = modification.RolledValue >= 0 ? "+" : "";
                result.Add(new ColoredText($" ({sign}{modification.RolledValue})", ColorPalette.Gray.GetColor()));
            }
            
            return result;
        }
    }
}

