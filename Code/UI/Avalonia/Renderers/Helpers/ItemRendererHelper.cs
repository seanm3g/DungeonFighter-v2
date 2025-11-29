using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Helper methods for rendering items with colored text
    /// </summary>
    public static class ItemRendererHelper
    {
        /// <summary>
        /// Renders an item name with colored text support
        /// </summary>
        public static void RenderItemName(ColoredTextWriter textWriter, GameCanvasControl canvas, 
            int x, int y, int itemIndex, Item item, bool useColoredText = true)
        {
            if (useColoredText)
            {
                var displayBuilder = new ColoredTextBuilder();
                displayBuilder.Add($"[{itemIndex + 1}] ", Colors.White);
                var itemNameSegments = ItemDisplayColoredText.FormatFullItemName(item);
                displayBuilder.AddRange(itemNameSegments);
                textWriter.RenderSegments(displayBuilder.Build(), x, y);
            }
            else
            {
                string coloredItemName = ItemDisplayFormatter.GetColoredItemName(item);
                string displayLine = $"[{itemIndex + 1}] {coloredItemName}";
                var coloredSegments = ColoredTextParser.Parse(displayLine);
                if (coloredSegments != null && coloredSegments.Count > 0)
                {
                    textWriter.RenderSegments(coloredSegments, x, y);
                }
                else
                {
                    canvas.AddText(x, y, $"[{itemIndex + 1}] {item.Name}", AsciiArtAssets.Colors.White);
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

