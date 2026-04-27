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
        /// Shows at least <see cref="LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT"/> panels (empty placeholders when the combo is shorter or empty);
        /// selected (current combo step) panel border is highlighted when the sequence is non-empty.
        /// When player is null, strip is cleared.
        /// </summary>
        /// <param name="drawHoverDetailOverlay">When false, skips the large center tooltip (e.g. character creation narrative occupies the same cells; clearing would erase it).</param>
        public void RenderActionInfoStrip(Character? player, bool drawHoverDetailOverlay = true)
        {
            int stripX = LayoutConstants.ACTION_INFO_X;
            int stripY = LayoutConstants.ACTION_INFO_Y;
            int stripW = LayoutConstants.ACTION_INFO_WIDTH;
            int stripH = LayoutConstants.ACTION_INFO_HEIGHT;
            canvas.ClearTextInArea(stripX, stripY, stripW, stripH);
            canvas.ClearBoxesInArea(stripX, stripY, stripW, stripH);

            if (player == null)
                return;

            var panelData = CombatActionStripBuilder.BuildPanelData(player);
            var comboForStrip = player.GetComboActions();
            int filled = panelData.Count;
            int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(filled);
            int selectedIndex = filled > 0 ? player.ComboStep % filled : -1;

            for (int i = 0; i < displayCount; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, displayCount, out int px, out int py, out int pw, out int panelH);
                bool isEmptySlot = i >= filled;
                bool isSelected = !isEmptySlot && i == selectedIndex;
                var borderColor = isSelected
                    ? AsciiArtAssets.Colors.Gold
                    : (isEmptySlot ? AsciiArtAssets.Colors.DarkGray : AsciiArtAssets.Colors.Cyan);
                canvas.AddBorder(px, py, pw, panelH, borderColor);

                if (isEmptySlot)
                    continue;

                int contentX = px + 1;
                int contentY = py + 1;
                int contentW = Math.Max(0, pw - 2);
                var info = panelData[i];
                string name = string.IsNullOrEmpty(info.Name) ? "?" : (info.Name.Length > contentW ? info.Name.Substring(0, contentW - 3) + "..." : info.Name);
                var action = comboForStrip[i];
                var nameColor = WeaponRequiredComboAction.IsRequiredBasicForEquippedWeapon(player, action)
                    ? AsciiArtAssets.Colors.Red
                    : AsciiArtAssets.Colors.White;
                canvas.AddText(contentX, contentY, name, nameColor);
                contentY++;

                // Damage % of character base (action multiplier); green/red when DAMAGE_MOD changes it
                const double damageCmpEps = 0.0001;
                bool damageDiffers = Math.Abs(info.DamageModified - info.DamageBase) > damageCmpEps;
                double damageDisplay = damageDiffers ? info.DamageModified : info.DamageBase;
                var damageColor = damageDiffers
                    ? (info.DamageModified > info.DamageBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : AsciiArtAssets.Colors.White;
                string damageLine = CombatActionStripBuilder.FormatSwingDamageLine(info.EffectiveMultiHitCount, damageDisplay);
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

                // Positive roll bonus (easier hit): match tooltip "Accuracy: +N" on the compact card
                if (contentY < py + panelH && info.AccuracyRollBonus > 0)
                {
                    string accLine = $"Acc +{info.AccuracyRollBonus}";
                    if (accLine.Length > contentW) accLine = accLine.Substring(0, contentW - 3) + "...";
                    canvas.AddText(contentX, contentY, accLine, AsciiArtAssets.Colors.Green);
                    contentY++;
                }

                // Thresholds: only when action has non-zero adjustments
                if (contentY < py + panelH && !string.IsNullOrEmpty(info.ThresholdText))
                {
                    string thresholdLine = info.ThresholdText.Length > contentW ? info.ThresholdText.Substring(0, contentW - 3) + "..." : info.ThresholdText;
                    canvas.AddText(contentX, contentY, thresholdLine, AsciiArtAssets.Colors.Cyan);
                }
            }

            if (drawHoverDetailOverlay)
                RenderActionStripTooltipOverlay(player, filled, displayCount);
        }

        /// <summary>
        /// Draws a text box in the top of the framed center panel when the pointer hovers an action strip card.
        /// Clears a band of the combat log area first so text remains readable.
        /// </summary>
        private void RenderActionStripTooltipOverlay(Character? player, int filledPanelCount, int displaySlotCount)
        {
            if (player == null)
                return;

            // Action-pool hover tooltip is drawn in RightPanelRenderer after pool rows (correct z-order).
            if (RightPanelActionHoverState.HoveredPoolIndex >= 0)
                return;

            int innerLeft = LayoutConstants.CENTER_PANEL_X + 1;
            int innerTop = LayoutConstants.CENTER_PANEL_Y + 1;
            int innerRight = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int innerW = Math.Max(8, innerRight - innerLeft + 1);
            const int maxTooltipLines = 14;
            int boxW = Math.Min(52, innerW);
            int innerTextW = Math.Max(4, boxW - 2);

            int rpSeq = RightPanelActionHoverState.HoveredSequenceIndex;

            List<string>? tipLines = null;
            int? anchorCenterX = null;

            if (LeftPanelHoverState.IsActive)
                tipLines = LeftPanelTooltipBuilder.BuildLines(player, LeftPanelHoverState.Value, innerTextW, maxTooltipLines + 2);

            if (tipLines == null || tipLines.Count == 0)
            {
                // Action-strip hover must win over right-panel sequence hover so panels 2+ use the correct
                // anchor and tooltip (overlay fill/text) for the card under the pointer.
                int hiStrip = ActionStripHoverState.HoveredPanelIndex;
                if (filledPanelCount > 0 && displaySlotCount > 0 && hiStrip >= 0 && hiStrip < filledPanelCount)
                {
                    ActionInfoStripLayout.GetPanelRect(hiStrip, displaySlotCount, out int px, out _, out int pw, out _);
                    anchorCenterX = px + pw / 2;
                    tipLines = CombatActionStripBuilder.BuildActionTooltipLines(player, hiStrip, innerTextW, maxTooltipLines + 2);
                }
                else if (rpSeq >= 0)
                {
                    var combo = player.GetComboActions();
                    if (rpSeq < combo.Count)
                    {
                        tipLines = CombatActionStripBuilder.BuildActionTooltipLines(player, rpSeq, innerTextW, maxTooltipLines + 2);
                        if (filledPanelCount > 0 && displaySlotCount > 0 && rpSeq < filledPanelCount)
                        {
                            ActionInfoStripLayout.GetPanelRect(rpSeq, displaySlotCount, out int px, out _, out int pw, out _);
                            anchorCenterX = px + pw / 2;
                        }
                    }
                }
            }

            if (tipLines == null || tipLines.Count == 0)
            {
                HoverTooltipDrawing.ClearInnerCenterPanelTooltipOverlay(canvas);
                return;
            }

            if (tipLines.Count > maxTooltipLines)
                tipLines = tipLines.GetRange(0, maxTooltipLines);

            int boxWFinal = Math.Min(52, innerW);
            int idealX = anchorCenterX.HasValue
                ? anchorCenterX.Value - boxWFinal / 2
                : innerLeft + Math.Max(0, (innerW - boxWFinal) / 2);
            int boxX = Math.Max(innerLeft, Math.Min(idealX, innerRight - boxWFinal + 1));
            int innerTextWDraw = Math.Max(4, boxWFinal - 2);

            int boxH = tipLines.Count + 2;
            int maxBoxBottom = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            int boxY = innerTop;
            if (boxY + boxH - 1 > maxBoxBottom)
                boxY = Math.Max(innerTop, maxBoxBottom - boxH + 1);

            int innerRightInclusive = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            HoverTooltipDrawing.DrawFramedPanel(canvas, boxX, boxY, boxWFinal, boxH, innerLeft, innerTop, innerRightInclusive, maxBoxBottom);

            int tx = boxX + 1;
            int ty = boxY + 1;
            foreach (var line in tipLines)
            {
                string draw = line.Length > innerTextWDraw ? line.Substring(0, innerTextWDraw - 3) + "..." : line;
                canvas.AddOverlayText(tx, ty, draw, AsciiArtAssets.Colors.White);
                ty++;
            }
        }
    }
}
