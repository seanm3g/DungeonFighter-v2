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
                ItemType.Consumable => "Use",
                _ => "Item"
            };
        }

        /// <summary>
        /// True when <paramref name="character"/> has the item in a context where attribute requirements
        /// matter (bag display, comparison previews) but does not currently meet them. Used to draw the
        /// whole item row (name line, stats, supporting text) in red so the player can see at a glance
        /// which gear their effective STR/AGI/TEC/INT block from equipping.
        /// </summary>
        public static bool IsEquipBlockedForCharacter(Item item, Character? character)
        {
            if (item == null || character == null)
                return false;
            return item.GetEquipBlockedReason(character) != null;
        }

        /// <summary>
        /// Gets the currently equipped same-slot armor item used for inventory armor comparison colors.
        /// </summary>
        public static Item? GetArmorComparisonBaseline(Character? character, Item? displayedItem)
        {
            if (character == null || displayedItem == null)
                return null;

            return displayedItem switch
            {
                HeadItem => character.Head,
                ChestItem => character.Body,
                LegsItem => character.Legs,
                FeetItem => character.Feet,
                _ => null
            };
        }

        /// <summary>
        /// Renders an item name with colored text support. When <paramref name="character"/> is provided
        /// and the item fails its <see cref="Item.GetEquipBlockedReason"/> check, the entire name line
        /// (index, rarity bracket, slot bracket, and full item name) is drawn in red.
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
                    if (IsEquipBlockedForCharacter(item, character))
                        coloredSegments = RecolorAllSegments(coloredSegments, Colors.Red);
                    textWriter.RenderSegments(coloredSegments, x, y);
                }
                else
                {
                    var fallbackColor = IsEquipBlockedForCharacter(item, character)
                        ? AsciiArtAssets.Colors.Red
                        : AsciiArtAssets.Colors.White;
                    canvas.AddText(x, y, $"{indexPrefix}[{rarity}] [{slotName}] {item.Name}", fallbackColor);
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

            var displayBuilder = new ColoredTextBuilder();
            if (itemIndex >= 0)
                displayBuilder.Add($"[{itemIndex + 1}] ", Colors.White);
            displayBuilder.Add("[", metaColor);
            displayBuilder.Add(rarity, rarityColor);
            displayBuilder.Add("] ", metaColor);
            displayBuilder.Add($"[{slotName}] ", metaColor);

            // Full name: prefix / base / suffix segments (same as armor). Rarity stays in [brackets] only.
            displayBuilder.AddRange(ItemDisplayColoredText.FormatFullItemName(item));

            var built = displayBuilder.Build();
            return slotBlocked ? RecolorAllSegments(built, Colors.Red) : built;
        }

        private static List<ColoredText> RecolorAllSegments(List<ColoredText> segments, Color color)
        {
            if (segments == null || segments.Count == 0)
                return segments ?? new List<ColoredText>();

            var list = new List<ColoredText>(segments.Count);
            foreach (var s in segments)
            {
                if (s == null)
                    continue;
                list.Add(new ColoredText(s.Text ?? "", color));
            }

            return list;
        }

        /// <summary>
        /// Renders item stats with colored text support.
        /// </summary>
        /// <param name="weaponSpeedBaseline">When set (e.g. equip comparison), used with <paramref name="displayedItem"/> for relative speed/damage colors.</param>
        /// <param name="armorComparisonBaseline">Other armor piece for equip comparison coloring (higher armor green, lower red).</param>
        /// <param name="characterForEquipRequirements">When set with <paramref name="displayedItem"/>, stat lines are drawn all red if attribute requirements are not met.</param>
        public static void RenderItemStats(ColoredTextWriter textWriter, GameCanvasControl canvas,
            int x, int y, List<string> itemStats, ref int currentY, ref int lineCount, bool useColoredText = true,
            Item? displayedItem = null, WeaponItem? weaponSpeedBaseline = null, Item? armorComparisonBaseline = null,
            Character? characterForEquipRequirements = null)
        {
            if (itemStats.Count == 0) return;

            bool statsAllRed = displayedItem != null
                && IsEquipBlockedForCharacter(displayedItem, characterForEquipRequirements);

            foreach (var stat in itemStats)
            {
                if (useColoredText)
                {
                    var statSegments = ItemStatFormatter.FormatStatLine(stat, displayedItem, weaponSpeedBaseline, armorComparisonBaseline);
                    if (statsAllRed)
                        statSegments = RecolorAllSegments(statSegments, Colors.Red);
                    textWriter.RenderSegments(statSegments, x, currentY);
                }
                else
                {
                    var statSegments = ColoredTextParser.Parse($"    {stat}");
                    if (statSegments != null && statSegments.Count > 0)
                    {
                        if (statsAllRed)
                            statSegments = RecolorAllSegments(statSegments, Colors.Red);
                        textWriter.RenderSegments(statSegments, x, currentY);
                    }
                    else
                    {
                        var lineColor = statsAllRed ? AsciiArtAssets.Colors.Red : AsciiArtAssets.Colors.White;
                        canvas.AddText(x, currentY, $"    {stat}", lineColor);
                    }
                }
                currentY++;
                lineCount++;
            }
        }
    }
}

