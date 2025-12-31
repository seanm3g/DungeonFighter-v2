using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Helper methods for rendering items with colored text
    /// </summary>
    public static class ItemRendererHelper
    {
        /// <summary>
        /// Gets the slot name for an item, showing weapon class for weapons
        /// </summary>
        private static string GetSlotName(Item item)
        {
            if (item is WeaponItem weaponItem)
            {
                return weaponItem.WeaponType.ToString();
            }
            
            return item.Type switch
            {
                ItemType.Head => "Head",
                ItemType.Chest => "Body",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }

        /// <summary>
        /// Renders an item name with colored text support
        /// </summary>
        public static void RenderItemName(ColoredTextWriter textWriter, GameCanvasControl canvas, 
            int x, int y, int itemIndex, Item item, bool useColoredText = true)
        {
            string slotName = GetSlotName(item);
            string rarity = item.Rarity?.Trim() ?? "Common";
            var rarityColor = ItemThemeProvider.GetRarityColor(rarity);
            
            if (useColoredText)
            {
                var displayBuilder = new ColoredTextBuilder();
                // Only add index if it's >= 0 (negative values indicate no index should be shown)
                if (itemIndex >= 0)
                {
                    displayBuilder.Add($"[{itemIndex + 1}] ", Colors.White);
                }
                displayBuilder.Add("[", Colors.Gray);
                displayBuilder.Add(rarity, rarityColor);
                displayBuilder.Add("] ", Colors.Gray);
                displayBuilder.Add($"[{slotName}] ", Colors.Gray);
                var itemNameSegments = ItemDisplayColoredText.FormatFullItemName(item);
                displayBuilder.AddRange(itemNameSegments);
                textWriter.RenderSegments(displayBuilder.Build(), x, y);
            }
            else
            {
                string coloredItemName = ItemDisplayFormatter.GetColoredItemName(item);
                string indexPrefix = itemIndex >= 0 ? $"[{itemIndex + 1}] " : "";
                string displayLine = $"{indexPrefix}[{rarity}] [{slotName}] {coloredItemName}";
                var coloredSegments = ColoredTextParser.Parse(displayLine);
                if (coloredSegments != null && coloredSegments.Count > 0)
                {
                    textWriter.RenderSegments(coloredSegments, x, y);
                }
                else
                {
                    canvas.AddText(x, y, $"{indexPrefix}[{rarity}] [{slotName}] {item.Name}", AsciiArtAssets.Colors.White);
                }
            }
        }

        /// <summary>
        /// Renders item stats with colored text support
        /// </summary>
        public static void RenderItemStats(ColoredTextWriter textWriter, GameCanvasControl canvas,
            int x, int y, List<string> itemStats, ref int currentY, ref int lineCount, bool useColoredText = true)
        {
            if (itemStats.Count == 0) return;

            foreach (var stat in itemStats)
            {
                if (useColoredText)
                {
                    var statSegments = ItemStatFormatter.FormatStatLine(stat);
                    textWriter.RenderSegments(statSegments, x, currentY);
                }
                else
                {
                    var statSegments = ColoredTextParser.Parse($"    {stat}");
                    if (statSegments != null && statSegments.Count > 0)
                    {
                        textWriter.RenderSegments(statSegments, x, currentY);
                    }
                    else
                    {
                        canvas.AddText(x, currentY, $"    {stat}", AsciiArtAssets.Colors.White);
                    }
                }
                currentY++;
                lineCount++;
            }
        }
    }
}

