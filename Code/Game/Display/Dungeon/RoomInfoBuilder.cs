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

            var roomHeaderText = AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnteringRoomHeader);
            var coloredRoomHeader = new ColoredTextBuilder()
                .Add(roomHeaderText, AsciiArtAssets.Colors.Yellow)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(coloredRoomHeader));

            var roomNumberInfo = new ColoredTextBuilder()
                .Add("Room Number: ", ColorPalette.Info)
                .Add($"{roomNumber} of {totalRooms}", ColorPalette.Warning)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(roomNumberInfo));

            // Get environment color template based on theme
            string themeTemplate = string.IsNullOrEmpty(room.Theme) 
                ? "" 
                : room.Theme.ToLower().Replace(" ", "");
            var environmentNameColored = ColorTemplateLibrary.GetTemplate(themeTemplate, room.Name);
            
            var roomNameInfo = new ColoredTextBuilder()
                .Add("Room: ", ColorPalette.White)
                .AddRange(environmentNameColored)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(roomNameInfo));
            info.Add(""); // Blank line after room name

            var roomDescription = new ColoredTextBuilder()
                .Add(room.Description, ColorPalette.White)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(roomDescription));
            info.Add("");

            return info;
        }
    }
}

