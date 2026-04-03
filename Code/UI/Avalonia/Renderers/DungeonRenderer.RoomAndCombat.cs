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
        /// Renders the action-info strip below the center panel (combat, inventory, etc.).
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
            int count = panelData.Count;

            for (int i = 0; i < panelData.Count; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, count, out int px, out int py, out int pw, out int panelH);
                bool isSelected = i == selectedIndex;
                var borderColor = isSelected ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.Cyan;
                canvas.AddBorder(px, py, pw, panelH, borderColor);

                int contentX = px + 1;
                int contentY = py + 1;
                int contentW = Math.Max(0, pw - 2);
                var info = panelData[i];
                string name = string.IsNullOrEmpty(info.Name) ? "?" : (info.Name.Length > contentW ? info.Name.Substring(0, contentW - 3) + "..." : info.Name);
                canvas.AddText(contentX, contentY, name, AsciiArtAssets.Colors.White);
                contentY++;

                // Damage: green if modified up, red if modified down, white when base
                int damageDisplay = info.DamageModified != info.DamageBase ? info.DamageModified : info.DamageBase;
                var damageColor = info.DamageModified != info.DamageBase
                    ? (info.DamageModified > info.DamageBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : AsciiArtAssets.Colors.White;
                string damageLine = $"Dmg {damageDisplay}";
                if (damageLine.Length > contentW) damageLine = damageLine.Substring(0, contentW - 3) + "...";
                if (contentY < py + panelH)
                {
                    canvas.AddText(contentX, contentY, damageLine, damageColor);
                    contentY++;
                }

                // Attack speed: green if faster (lower time), red if slower (higher time), white when base
                double speedDisplay = info.SpeedModified != info.SpeedBase ? info.SpeedModified : info.SpeedBase;
                var speedColor = info.SpeedModified != info.SpeedBase
                    ? (info.SpeedModified < info.SpeedBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : AsciiArtAssets.Colors.White;
                string speedLine = $"Spd {speedDisplay:F1}s";
                if (speedLine.Length > contentW) speedLine = speedLine.Substring(0, contentW - 3) + "...";
                if (contentY < py + panelH)
                {
                    canvas.AddText(contentX, contentY, speedLine, speedColor);
                    contentY++;
                }

                // Thresholds: only when action has non-zero adjustments
                if (contentY < py + panelH && !string.IsNullOrEmpty(info.ThresholdText))
                {
                    string thresholdLine = info.ThresholdText.Length > contentW ? info.ThresholdText.Substring(0, contentW - 3) + "..." : info.ThresholdText;
                    canvas.AddText(contentX, contentY, thresholdLine, AsciiArtAssets.Colors.Cyan);
                }
            }

            // Active modifiers section (bottom 2 lines of strip)
            var modifierLines = player != null ? CombatActionStripBuilder.BuildActiveModifierLines(player) : new List<string>();
            int modY = stripY + stripH - 2;
            int modX = stripX + 1;
            int modW = Math.Max(0, stripW - 2);
            for (int m = 0; m < modifierLines.Count && m < 2; m++)
            {
                string line = modifierLines[m];
                if (line.Length > modW) line = line.Substring(0, modW - 3) + "...";
                canvas.AddText(modX, modY + m, line, AsciiArtAssets.Colors.Gold);
            }
        }
    }
}
