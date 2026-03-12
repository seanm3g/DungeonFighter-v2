using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Room entry, enemy encounter, and combat screen rendering.
    /// </summary>
    public partial class DungeonRenderer
    {
        /// <summary>
        /// Prepares the room entry screen (pure reactive mode). No-op; CenterPanelDisplayManager handles rendering.
        /// </summary>
        public void RenderRoomEntry(int x, int y, int width, int height, Environment room, ICanvasTextManager textManager, int? startFromBufferIndex = null)
        {
            // Pure reactive mode: CenterPanelDisplayManager renders when buffer changes.
        }

        /// <summary>
        /// Renders the enemy encounter screen using the display buffer system.
        /// </summary>
        public void RenderEnemyEncounter(int x, int y, int width, int height, Enemy enemy, ICanvasTextManager textManager, List<string>? dungeonContext = null)
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

        /// <summary>
        /// Renders the complete combat screen by rendering the display buffer.
        /// Uses GetDisplayManagerForCharacter(player) so the correct character's buffer is shown.
        /// </summary>
        public void RenderCombatScreen(int x, int y, int width, int height, Dungeon? dungeon, Environment? room, Enemy enemy, ICanvasTextManager textManager, Character? player = null, List<string>? dungeonContext = null)
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                var displayManager = canvasTextManager.GetDisplayManagerForCharacter(player);
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

        /// <summary>
        /// Renders the action-info strip below the center panel (combat only).
        /// One panel per combo action, left to right; selected (current combo step) panel border is highlighted.
        /// When player is null or has no actions, strip is cleared.
        /// </summary>
        public void RenderActionInfoStrip(Character? player)
        {
            int stripX = LayoutConstants.ACTION_INFO_X;
            int stripY = LayoutConstants.ACTION_INFO_Y;
            int stripW = LayoutConstants.ACTION_INFO_WIDTH;
            int stripH = LayoutConstants.ACTION_INFO_HEIGHT;
            canvas.ClearTextInArea(stripX, stripY, stripW, stripH);

            var panelData = player != null ? CombatActionStripBuilder.BuildPanelData(player) : new List<ActionPanelInfo>();
            if (panelData.Count == 0)
                return;

            int selectedIndex = player != null && panelData.Count > 0
                ? player.ComboStep % panelData.Count
                : 0;
            int panelWidth = stripW / panelData.Count;
            int remainder = stripW % panelData.Count;

            for (int i = 0; i < panelData.Count; i++)
            {
                int pw = panelWidth + (i == panelData.Count - 1 ? remainder : 0);
                int px = stripX + i * panelWidth;
                int py = stripY;
                bool isSelected = i == selectedIndex;
                var borderColor = isSelected ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.Cyan;
                canvas.AddBorder(px, py, pw, stripH, borderColor);

                int contentX = px + 1;
                int contentY = py + 1;
                int contentW = Math.Max(0, pw - 2);
                var info = panelData[i];
                string name = string.IsNullOrEmpty(info.Name) ? "?" : (info.Name.Length > contentW ? info.Name.Substring(0, contentW - 3) + "..." : info.Name);
                canvas.AddText(contentX, contentY, name, AsciiArtAssets.Colors.White);
                contentY++;
                string accLine = $"Acc {info.Acc:+0;-0;0}";
                if (info.BonusFromPrev != 0)
                    accLine += $" Prev:{info.BonusFromPrev:+0;-0;0}";
                if (accLine.Length > contentW) accLine = accLine.Substring(0, contentW - 3) + "...";
                if (contentY < py + stripH)
                {
                    canvas.AddText(contentX, contentY, accLine, AsciiArtAssets.Colors.White);
                    contentY++;
                }
                if (contentY < py + stripH && !string.IsNullOrEmpty(info.Causes))
                {
                    string causes = info.Causes.Length > contentW ? info.Causes.Substring(0, contentW - 3) + "..." : info.Causes;
                    canvas.AddText(contentX, contentY, causes, AsciiArtAssets.Colors.Gray);
                }
            }
        }
    }
}
