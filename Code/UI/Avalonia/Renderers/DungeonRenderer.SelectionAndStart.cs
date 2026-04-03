using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Dungeon selection and dungeon start rendering.
    /// </summary>
    public partial class DungeonRenderer
    {
        /// <summary>
        /// Renders the dungeon selection screen
        /// </summary>
        public void RenderDungeonSelection(int x, int y, int width, int height, List<Dungeon> dungeons)
        {
            if (dungeons == null)
                return;
            currentLineCount = selectionRenderer.RenderDungeonSelection(x, y, width, height, dungeons);
        }

        /// <summary>
        /// Renders the dungeon start screen using the display buffer system.
        /// All content is rendered from the display buffer for consistency.
        /// </summary>
        public void RenderDungeonStart(int x, int y, int width, int height, Dungeon dungeon, ICanvasTextManager textManager, List<string>? dungeonHeaderInfo = null)
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                var displayManager = canvasTextManager.DisplayManager;
                var buffer = displayManager.Buffer;
                var displayRenderer = new DisplayRenderer(textWriter);
                displayRenderer.Render(buffer, x, y, width, height);
            }
            else
            {
                if (textManager == null) return;
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                canvas.ClearTextInArea(x, y, width, height + 1);
                foreach (var message in displayBuffer)
                {
                    if (currentY >= y + height) break;
                    int linesRendered = textWriter.WriteLineColoredWrapped(message, textX, currentY, availableWidth);
                    currentY += linesRendered;
                }
            }
        }
    }
}
