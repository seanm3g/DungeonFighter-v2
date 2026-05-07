using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Room completion, dungeon completion, and death screen rendering.
    /// </summary>
    public partial class DungeonRenderer
    {
        /// <summary>
        /// Renders the room completion screen.
        /// </summary>
        public void RenderRoomCompletion(int x, int y, int width, int height, Environment room, Character currentCharacter)
        {
            currentLineCount = 0;
            if (currentCharacter != null)
            {
                canvas.AddText(x + 2, y, string.Format(AsciiArtAssets.UIText.RemainingHealth,
                    currentCharacter.CurrentHealth, currentCharacter.GetEffectiveMaxHealth()),
                    AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
            }
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.RoomClearedMessage, AsciiArtAssets.Colors.Green);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.Divider, AsciiArtAssets.Colors.Green);
            currentLineCount++;
        }

        /// <summary>
        /// Renders the scrollable completion log (display buffer) and fixed footer menu.
        /// </summary>
        public void RenderDungeonCompletion(int x, int y, int width, int height, DisplayBuffer buffer)
        {
            int logHeight = System.Math.Max(1, height - DungeonCompletionRenderer.FooterReservedRows);
            var displayRenderer = new DisplayRenderer(textWriter);
            displayRenderer.Render(buffer, x, y, width, logHeight, clearContent: true);
            dungeonCompletionRenderer.RenderFooterOnly(x, y, width, height);
            currentLineCount = 0;
        }

        /// <summary>
        /// Renders the scrollable death log (display buffer) and fixed footer.
        /// </summary>
        public void RenderDeathScreen(int x, int y, int width, int height, DisplayBuffer buffer)
        {
            int logHeight = System.Math.Max(1, height - DeathScreenRenderer.FooterReservedRows);
            var displayRenderer = new DisplayRenderer(textWriter);
            displayRenderer.Render(buffer, x, y, width, logHeight, clearContent: true);
            deathScreenRenderer.RenderFooterOnly(x, y, width, height);
            currentLineCount = 0;
        }
    }
}
