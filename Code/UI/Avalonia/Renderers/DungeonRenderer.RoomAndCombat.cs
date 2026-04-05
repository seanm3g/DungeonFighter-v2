using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using System;
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
        /// Renders the action-info strip at the top of the center column (combat, inventory, etc.), above the combat log.
        /// One panel per combo action, left to right; selected (current combo step) panel border is highlighted.
        /// When player is null or has no actions, strip is cleared.
        /// </summary>
        /// <param name="drawHoverDetailOverlay">When false, skips the large center tooltip (e.g. character creation narrative occupies the same cells; clearing would erase it).</param>
        public void RenderActionInfoStrip(Character? player, bool drawHoverDetailOverlay = true)
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

                // Damage % of character base (action multiplier); green/red when DAMAGE_MOD changes it
                const double damageCmpEps = 0.0001;
                bool damageDiffers = Math.Abs(info.DamageModified - info.DamageBase) > damageCmpEps;
                double damageDisplay = damageDiffers ? info.DamageModified : info.DamageBase;
                var damageColor = damageDiffers
                    ? (info.DamageModified > info.DamageBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : AsciiArtAssets.Colors.White;
                string damageLine = $"Dmg {damageDisplay:F0}%";
                if (damageLine.Length > contentW) damageLine = damageLine.Substring(0, contentW - 3) + "...";
                if (contentY < py + panelH)
                {
                    canvas.AddText(contentX, contentY, damageLine, damageColor);
                    contentY++;
                }

                // Speed % (action details): green if faster (higher %), red if slower (lower %), white when base
                double speedDisplay = info.SpeedModified != info.SpeedBase ? info.SpeedModified : info.SpeedBase;
                var speedColor = info.SpeedModified != info.SpeedBase
                    ? (info.SpeedModified > info.SpeedBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : AsciiArtAssets.Colors.White;
                string speedLine = $"Spd {speedDisplay:F0}%";
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

            if (drawHoverDetailOverlay)
                RenderActionStripTooltipOverlay(player, panelData.Count);
        }

        /// <summary>
        /// Draws a text box in the top of the framed center panel when the pointer hovers an action strip card.
        /// Clears a band of the combat log area first so text remains readable.
        /// </summary>
        private void RenderActionStripTooltipOverlay(Character? player, int panelCount)
        {
            if (player == null || panelCount <= 0)
                return;

            int hi = ActionStripHoverState.HoveredPanelIndex;
            if (hi < 0 || hi >= panelCount)
                return;

            int innerLeft = LayoutConstants.CENTER_PANEL_X + 1;
            int innerTop = LayoutConstants.CENTER_PANEL_Y + 1;
            int innerRight = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int innerW = Math.Max(8, innerRight - innerLeft + 1);

            ActionInfoStripLayout.GetPanelRect(hi, panelCount, out int px, out _, out int pw, out _);
            int boxW = Math.Min(52, innerW);
            int idealX = px + pw / 2 - boxW / 2;
            int boxX = Math.Max(innerLeft, Math.Min(idealX, innerRight - boxW + 1));

            const int maxTooltipLines = 14;
            int innerTextW = Math.Max(4, boxW - 2);
            var tipLines = CombatActionStripBuilder.BuildActionTooltipLines(player, hi, innerTextW, maxTooltipLines + 2);
            if (tipLines == null || tipLines.Count == 0)
                return;

            if (tipLines.Count > maxTooltipLines)
                tipLines = tipLines.GetRange(0, maxTooltipLines);

            int boxH = tipLines.Count + 2;
            int maxBoxBottom = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            int boxY = innerTop;
            if (boxY + boxH - 1 > maxBoxBottom)
                boxY = Math.Max(innerTop, maxBoxBottom - boxH + 1);

            canvas.ClearTextInArea(boxX, boxY, boxW, boxH);
            canvas.ClearBoxesInArea(boxX, boxY, boxW, boxH);
            canvas.AddBorder(boxX, boxY, boxW, boxH, AsciiArtAssets.Colors.Yellow);

            int tx = boxX + 1;
            int ty = boxY + 1;
            foreach (var line in tipLines)
            {
                string draw = line.Length > innerTextW ? line.Substring(0, innerTextW - 3) + "..." : line;
                canvas.AddText(tx, ty, draw, AsciiArtAssets.Colors.White);
                ty++;
            }
        }
    }
}
