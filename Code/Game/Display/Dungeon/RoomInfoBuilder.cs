using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Display.Dungeon
{
    /// <summary>
    /// Builds room information display
    /// </summary>
    public static class RoomInfoBuilder
    {
        /// <summary>
        /// Builds room info lines
        /// </summary>
        public static List<string> BuildRoomInfo(Environment room, int roomNumber, int totalRooms)
        {
            var info = new List<string>();

            var roomHeaderText = $"===== {AsciiArtAssets.UIText.EnteringRoomHeader} =====";
            var coloredRoomHeader = new ColoredTextBuilder()
                .Add(roomHeaderText, ColorPalette.Gold)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(coloredRoomHeader));

            var roomNameInfo = new ColoredTextBuilder()
                .Add("Room: ", AsciiArtAssets.Colors.Gold)
                .AddRange(DungeonThemeColors.BuildThemedName(room.Name, room.Theme))
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(roomNameInfo));
            info.Add(""); // Blank line after room name

            var roomDescription = new ColoredTextBuilder()
                .Add(room.Description, ColorPalette.White)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(roomDescription));
            // Note: No trailing blank line - spacing system handles transitions

            return info;
        }
    }
}
