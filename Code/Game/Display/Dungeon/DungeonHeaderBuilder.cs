using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Display.Dungeon
{
    /// <summary>
    /// Builds dungeon header display information
    /// </summary>
    public static class DungeonHeaderBuilder
    {
        /// <summary>
        /// Builds dungeon header lines
        /// </summary>
        public static List<string> BuildDungeonHeader(RPGGame.Dungeon dungeon)
        {
            var header = new List<string>();

            var headerText = $"===== {AsciiArtAssets.UIText.EnteringDungeonHeader} =====";
            var coloredHeader = new ColoredTextBuilder()
                .Add(headerText, ColorPalette.Gold)
                .Build();
            header.Add(ColoredTextRenderer.RenderAsMarkup(coloredHeader));

            var dungeonInfo = new ColoredTextBuilder()
                .Add("Dungeon: ", AsciiArtAssets.Colors.Gold)
                .AddRange(DungeonThemeColors.BuildThemedName(dungeon.Name, dungeon.Theme))
                .Build();
            header.Add(ColoredTextRenderer.RenderAsMarkup(dungeonInfo));
            header.Add("");

            return header;
        }
    }
}
