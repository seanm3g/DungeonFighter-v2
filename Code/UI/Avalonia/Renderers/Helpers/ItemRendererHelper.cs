using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.ColorSystem.Applications.ItemFormatting;
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
                ItemType.Legs => "Legs",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }

        /// <summary>
        /// True when <paramref name="character"/> has the item in a context where attribute requirements
        /// matter (bag display, comparison previews) but does not currently meet them. Used to surface
        /// a red category bracket on inventory rows so the player can see at a glance which items their
        /// effective STR/AGI/TEC/INT block from equipping.
        /// </summary>
        public static bool IsEquipBlockedForCharacter(Item item, Character? character)
        {
            if (item == null || character == null)
                return false;
            return item.GetEquipBlockedReason(character) != null;
        }

        /// <summary>
        /// Renders an item name with colored text support. When <paramref name="character"/> is provided
        /// and the item fails its <see cref="Item.GetEquipBlockedReason"/> check, the category bracket
        /// (e.g. <c>[Head]</c> / <c>[Mace]</c>) is drawn in red so the player can see at a glance that
        /// the item is not equippable with their current effective stats.
        /// </summary>
        public static void RenderItemName(ColoredTextWriter textWriter, GameCanvasControl canvas, 
            int x, int y, int itemIndex, Item item, bool useColoredText = true, Character? character = null)
        {
            if (useColoredText)
            {
                var segments = BuildItemNameSegments(itemIndex, item, character);
                textWriter.RenderSegments(segments, x, y);
            }
            else
            {
                string slotName = GetSlotName(item);
                string rarity = item.Rarity?.Trim() ?? "Common";
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

        internal static List<ColoredText> BuildItemNameSegments(int itemIndex, Item item, Character? character)
        {
            string slotName = GetSlotName(item);
            string rarity = item.Rarity?.Trim() ?? "Common";
            var rarityColor = ItemThemeProvider.GetRarityColor(rarity);
            // Gray brackets/slot so rarity and item name colors read clearly (Common name uses type/material theming).
            var metaColor = Colors.Gray;
            bool slotBlocked = IsEquipBlockedForCharacter(item, character);
            var slotColor = slotBlocked ? Colors.Red : metaColor;

            var displayBuilder = new ColoredTextBuilder();
            if (itemIndex >= 0)
                displayBuilder.Add($"[{itemIndex + 1}] ", Colors.White);
            displayBuilder.Add("[", metaColor);
            displayBuilder.Add(rarity, rarityColor);
            displayBuilder.Add("] ", metaColor);
            displayBuilder.Add($"[{slotName}] ", slotColor);

            // Full name: prefix / base / suffix segments (same as armor). Rarity stays in [brackets] only.
            displayBuilder.AddRange(ItemDisplayColoredText.FormatFullItemName(item));

            return displayBuilder.Build();
        }

        /// <summary>
        /// Renders item stats with colored text support.
        /// </summary>
        /// <param name="weaponSpeedBaseline">When set (e.g. equip comparison), used with <paramref name="displayedItem"/> for relative speed/damage colors.</param>
        /// <param name="armorComparisonBaseline">Other armor piece for equip comparison coloring (higher armor green, lower red).</param>
        public static void RenderItemStats(ColoredTextWriter textWriter, GameCanvasControl canvas,
            int x, int y, List<string> itemStats, ref int currentY, ref int lineCount, bool useColoredText = true,
            Item? displayedItem = null, WeaponItem? weaponSpeedBaseline = null, Item? armorComparisonBaseline = null)
        {
            if (itemStats.Count == 0) return;

            foreach (var stat in itemStats)
            {
                if (useColoredText)
                {
                    var statSegments = ItemStatFormatter.FormatStatLine(stat, displayedItem, weaponSpeedBaseline, armorComparisonBaseline);
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

