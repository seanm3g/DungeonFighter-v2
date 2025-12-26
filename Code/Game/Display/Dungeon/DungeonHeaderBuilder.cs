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
                .Add(headerText, ColorPalette.White)
                .Build();
            header.Add(ColoredTextRenderer.RenderAsMarkup(coloredHeader));

            char themeColorCode = DungeonThemeColors.GetThemeColorCode(dungeon.Theme);
            var dungeonNameColor = GetColorFromThemeCode(themeColorCode);

            var dungeonInfo = new ColoredTextBuilder()
                .Add("Dungeon: ", AsciiArtAssets.Colors.Gold)
                .Add(dungeon.Name, dungeonNameColor)
                .Build();
            header.Add(ColoredTextRenderer.RenderAsMarkup(dungeonInfo));
            header.Add("");

            return header;
        }
        
        private static ColorPalette GetColorFromThemeCode(char themeCode)
        {
            return themeCode switch
            {
                'R' => ColorPalette.Error,
                'G' => ColorPalette.Success,
                'B' => ColorPalette.Info,
                'Y' => ColorPalette.Warning,
                _ => ColorPalette.White
            };
        }
    }
}
