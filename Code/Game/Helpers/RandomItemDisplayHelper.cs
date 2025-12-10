namespace RPGGame
{
    using System.Collections.Generic;
    using Avalonia.Media;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Helper class for displaying randomly generated items in a formatted way.
    /// </summary>
    public static class RandomItemDisplayHelper
    {
        /// <summary>
        /// Displays a list of items with proper formatting including colored names, stats, and bonuses.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator for displaying items</param>
        /// <param name="items">The list of items to display</param>
        /// <param name="character">The character for stat comparison (can be null)</param>
        public static void DisplayItems(CanvasUICoordinator canvasUI, List<Item> items, Character? character = null)
        {
            if (items == null || items.Count == 0)
            {
                canvasUI.WriteLine("Error: No items to display.", UIMessageType.System);
                return;
            }

            canvasUI.WriteLine($"Generated {items.Count} random items:", UIMessageType.System);
            canvasUI.WriteBlankLine();

            // Display each item with proper formatting
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                DisplayItem(canvasUI, item, i + 1, character ?? new Character());
            }

            canvasUI.WriteBlankLine();
            canvasUI.WriteLine("=== Item Generation Complete ===", UIMessageType.System);
            canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
        }

        /// <summary>
        /// Displays a single item with proper formatting.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator for displaying the item</param>
        /// <param name="item">The item to display</param>
        /// <param name="itemNumber">The number to display (1-based index)</param>
        /// <param name="character">The character for stat comparison</param>
        private static void DisplayItem(CanvasUICoordinator canvasUI, Item item, int itemNumber, Character character)
        {
            // Display item number and name with proper colored text
            string displayType = ItemDisplayFormatter.GetDisplayType(item);
            var coloredNameSegments = ItemDisplayFormatter.GetColoredFullItemNameNew(item);

            // Build the colored text line: "1. (Head) [colored item name]"
            var itemLineBuilder = new ColoredTextBuilder();
            itemLineBuilder.Add($"{itemNumber}. ({displayType}) ", Colors.White);
            itemLineBuilder.AddRange(coloredNameSegments);
            canvasUI.WriteLineColoredSegments(itemLineBuilder.Build(), UIMessageType.System);

            // Display item stats
            string itemStats = ItemDisplayFormatter.GetItemStatsDisplay(item, character);
            if (!string.IsNullOrEmpty(itemStats))
            {
                canvasUI.WriteLine($"   {itemStats}", UIMessageType.System);
            }

            // Display bonuses if any
            if (item.StatBonuses.Count > 0 || item.ActionBonuses.Count > 0 || item.Modifications.Count > 0)
            {
                // Format bonuses using the formatter (it handles indentation internally)
                ItemDisplayFormatter.FormatItemBonusesWithColor(item, (line) =>
                {
                    canvasUI.WriteLine(line, UIMessageType.System);
                });
            }

            canvasUI.WriteBlankLine();
        }
    }
}

