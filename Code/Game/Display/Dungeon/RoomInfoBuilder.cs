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

            // Get room name color based on theme (single color, not template)
            char themeColorCode = DungeonThemeColors.GetThemeColorCode(room.Theme);
            var roomNameColor = GetColorFromThemeCode(themeColorCode);
            
            var roomNameInfo = new ColoredTextBuilder()
                .Add("Room: ", AsciiArtAssets.Colors.Gold)
                .Add(room.Name, roomNameColor)
                .Build();
            info.Add(ColoredTextRenderer.RenderAsMarkup(roomNameInfo));
            info.Add(""); // Blank line after room name

            // Separate buffer rows so BufferStorage's 152-char truncation cannot clip
            // concatenated Rooms.json + flavor text. Theme is resolved from the room
            // itself (not dungeon Theme). A single flavor line is always appended under
            // the Rooms.json description: roomContexts for {biome}/{roomType} when that
            // bank exists, otherwise locationDescriptions.
            if (!string.IsNullOrWhiteSpace(room.Description))
            {
                info.Add(RenderWhiteLine(room.Description));
            }

            string flavorTheme = FlavorLocationResolver.ResolveLocationTheme(room);
            string roomType = FlavorLocationResolver.ResolveRoomType(room);
            string flavorLine = FlavorText.HasRoomContext(flavorTheme, roomType)
                ? FlavorText.GenerateRoomContext(flavorTheme, roomType)
                : FlavorText.GenerateLocationDescription(flavorTheme);
            if (!string.IsNullOrWhiteSpace(flavorLine))
            {
                info.Add(RenderWhiteLine(flavorLine));
            }

            // Note: No trailing blank line - spacing system handles transitions

            return info;
        }

        private static string RenderWhiteLine(string text)
        {
            var colored = new ColoredTextBuilder()
                .Add(text, ColorPalette.White)
                .Build();
            return ColoredTextRenderer.RenderAsMarkup(colored);
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
