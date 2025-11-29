using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.ColorSystem.Applications.ItemFormatting
{
    /// <summary>
    /// Formats item comparisons for equip decisions
    /// </summary>
    public static class ItemComparisonFormatter
    {
        /// <summary>
        /// Formats item comparison for equip decisions
        /// </summary>
        public static List<List<ColoredText>> FormatItemComparison(Item newItem, Item? currentItem)
        {
            var lines = new List<List<ColoredText>>();
            
            // New item header
            var newHeader = new ColoredTextBuilder();
            newHeader.Add("NEW: ", ColorPalette.Success);
            // Format item with rarity - delegate to ItemDisplayColoredText for now (will be refactored)
            var newItemName = ItemDisplayColoredText.FormatItemWithRarity(newItem);
            newHeader.AddRange(newItemName);
            lines.Add(newHeader.Build());
            
            // New item stats
            lines.AddRange(ItemStatsFormatter.FormatItemStats(newItem));
            
            // Spacer
            lines.Add(new List<ColoredText>());
            
            // Current item header
            if (currentItem != null)
            {
                var currentHeader = new ColoredTextBuilder();
                currentHeader.Add("CURRENT: ", ColorPalette.Warning);
                // Format item with rarity - delegate to ItemDisplayColoredText for now (will be refactored)
                var currentItemName = ItemDisplayColoredText.FormatItemWithRarity(currentItem);
                currentHeader.AddRange(currentItemName);
                lines.Add(currentHeader.Build());
                
                // Current item stats
                lines.AddRange(ItemStatsFormatter.FormatItemStats(currentItem));
            }
            else
            {
                var emptyLine = new ColoredTextBuilder();
                emptyLine.Add("CURRENT: ", ColorPalette.Warning);
                emptyLine.Add("(empty slot)", Colors.DarkGray);
                lines.Add(emptyLine.Build());
            }
            
            return lines;
        }
    }
}

