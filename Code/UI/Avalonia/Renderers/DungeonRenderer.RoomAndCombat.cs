using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
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
        public void RenderEnemyEncounter(int x, int y, int width, int height, Enemy enemy, ICanvasTextManager textManager, List<string>? dungeonContext = null, IReadOnlyList<string>? combatEnemyNamesForLogAlignment = null)
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                var displayManager = canvasTextManager.DisplayManager;
                var buffer = displayManager.Buffer;
                var displayRenderer = new DisplayRenderer(textWriter);
                displayRenderer.Render(buffer, x, y, width, height, clearContent: true, combatEnemyNamesForPrimaryLineRightAlign: combatEnemyNamesForLogAlignment, combatHeroNameForLineAlignment: null);
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
        public void RenderCombatScreen(int x, int y, int width, int height, Dungeon? dungeon, Environment? room, Enemy enemy, ICanvasTextManager textManager, Character? player = null, List<string>? dungeonContext = null, IReadOnlyList<string>? combatEnemyNamesForLogAlignment = null)
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                var displayManager = canvasTextManager.GetDisplayManagerForCharacter(player);
                var buffer = displayManager.Buffer;
                var displayRenderer = new DisplayRenderer(textWriter);
                displayRenderer.Render(buffer, x, y, width, height, clearContent: true, combatEnemyNamesForPrimaryLineRightAlign: combatEnemyNamesForLogAlignment, combatHeroNameForLineAlignment: player?.Name);
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
        /// selected (next combo step) panel border is white when the sequence is non-empty; other filled slots use neutral gray darkened 50%; brief pulsing red/green/gold border after each hero swing is handled by <see cref="RPGGame.UI.Avalonia.Feedback.HeroActionStripFeedback"/> (thicker stroke for the flashing panel during the sequence).
        /// Cards with pending ACTION-cadence buffs (slot queue / bank on current step) shimmer via <see cref="RPGGame.UI.Avalonia.Feedback.ActionBonusBorderShimmer"/> (flash still overrides). Granting actions alone do not shimmer.
        /// Panels at indices ≥ <see cref="ComboSequenceMaxHelper.GetEffectiveMax(Character?)"/> use a black border so unused strip capacity matches the character’s combo slot limit.
        /// When player is null, strip is cleared.
        /// </summary>
        /// <param name="drawHoverDetailOverlay">When false, skips the large center tooltip (e.g. character creation narrative occupies the same cells; clearing would erase it).</param>
        /// <param name="damageLineMode">Intrinsic vs slot-modified damage with combo-slot amp (global toggle; same for all panels).</param>
        public void RenderActionInfoStrip(Character? player, bool drawHoverDetailOverlay = true, ActionStripDamageLineMode damageLineMode = ActionStripDamageLineMode.EffectiveWithComboAmp)
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
            int effectiveMaxSlots = ComboSequenceMaxHelper.GetEffectiveMax(player);
            bool anyBonusShimmer = false;
            var shimmerNow = DateTimeOffset.UtcNow;

            for (int i = 0; i < displayCount; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, displayCount, out int px, out int py, out int pw, out int panelH);
                bool isEmptySlot = i >= filled;
                var baseBorder = ActionInfoStripLayout.GetPanelBorderColor(i, filled, selectedIndex, effectiveMaxSlots);
                bool flashActive = HeroActionStripFeedback.TryGetBorderOverride(i, out var flashColor);
                bool isSelected = !isEmptySlot && selectedIndex >= 0 && i == selectedIndex;
                bool bonusCue = !isEmptySlot && i < effectiveMaxSlots
                    && i < comboForStrip.Count
                    && ActionBonusBorderShimmer.SlotHasPendingBonusCue(player, i);

                Color borderColor;
                if (flashActive)
                    borderColor = flashColor;
                else if (bonusCue)
                {
                    anyBonusShimmer = true;
                    borderColor = ActionBonusBorderShimmer.GetBorderColor(isSelected, shimmerNow);
                }
                else
                    borderColor = baseBorder;

                int borderThick = HeroActionStripFeedback.IsFlashEmphasisActive(i)
                    ? HeroActionStripFeedback.FlashBorderThicknessPixels
                    : 1;
                canvas.AddBorder(px, py, pw, panelH, borderColor, borderThick);

                if (bonusCue && !flashActive)
                {
                    var highlight = ActionBonusBorderShimmer.GetTravelHighlightColor(shimmerNow);
                    foreach (var (hx, hy, hw, hh) in ActionBonusBorderShimmer.GetTravelHighlightRects(px, py, pw, panelH, shimmerNow))
                        canvas.AddBorder(hx, hy, hw, hh, highlight, 2);
                }

                if (isEmptySlot || i >= effectiveMaxSlots)
                    continue;

                int contentX = px + 1;
                int contentY = py + 1;
                int contentW = Math.Max(0, pw - 2);
                int panelBottomExclusive = py + panelH;
                var info = panelData[i];
                string name = string.IsNullOrEmpty(info.Name) ? "?" : (info.Name.Length > contentW ? info.Name.Substring(0, contentW - 3) + "..." : info.Name);
                var action = comboForStrip[i];
                var nameColor = WeaponRequiredComboAction.IsRequiredBasicForEquippedWeapon(player, action)
                    ? AsciiArtAssets.Colors.Red
                    : AsciiArtAssets.Colors.White;
                canvas.AddText(contentX, contentY, name, nameColor);
                contentY++;

                bool hasThreshold = !string.IsNullOrEmpty(info.ThresholdText);
                int reserveBottomRows = hasThreshold ? 1 : 0;

                void drawLine(string text, Color color)
                {
                    if (contentY >= panelBottomExclusive - reserveBottomRows)
                        return;
                    if (string.IsNullOrEmpty(text))
                    {
                        contentY++;
                        return;
                    }
                    if (text.Length > contentW)
                        text = text.Substring(0, Math.Max(1, contentW - 3)) + "...";
                    canvas.AddText(contentX, contentY, text, color);
                    contentY++;
                }

                foreach (var roleLine in CombatActionStripBuilder.BuildActionStripComboRoleLines(player, action))
                    drawLine(roleLine, AsciiArtAssets.Colors.White);

                // Calculated damage | seconds on cards; hover tip uses % damage/speed for the same damageLineMode
                const double cmpEps = 0.0001;
                CombatActionStripBuilder.GetStripSwingDisplayPercents(in info, player, action, damageLineMode, out double damageDisplayPct, out double speedDisplayPct);
                bool damageDiffersFromIntrinsic = Math.Abs(damageDisplayPct - info.DamageBase) > cmpEps;
                bool speedDiffersFromIntrinsic = Math.Abs(speedDisplayPct - info.SpeedBase) > cmpEps;
                var swingLineColor = damageDiffersFromIntrinsic
                    ? (damageDisplayPct > info.DamageBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : speedDiffersFromIntrinsic
                        ? (speedDisplayPct > info.SpeedBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                        : AsciiArtAssets.Colors.White;
                string swingLine = CombatActionStripBuilder.FormatStripSwingLine(in info, player, action, damageLineMode);
                drawLine(swingLine, swingLineColor);

                int tailBudget = Math.Max(0, panelBottomExclusive - contentY - reserveBottomRows);
                var tailLines = CombatActionStripBuilder.BuildActionStripModifierTailLines(action, contentW, tailBudget);
                foreach (var tl in tailLines)
                    drawLine(tl, AsciiArtAssets.Colors.White);

                if (hasThreshold && contentY < panelBottomExclusive)
                {
                    string thresholdLine = info.ThresholdText!.Length > contentW ? info.ThresholdText.Substring(0, contentW - 3) + "..." : info.ThresholdText;
                    canvas.AddText(contentX, contentY, thresholdLine, AsciiArtAssets.Colors.Cyan);
                }
            }

            if (anyBonusShimmer)
                ActionBonusBorderShimmer.KeepAlive();

            if (drawHoverDetailOverlay)
                RenderActionStripTooltipOverlay(player, filled, displayCount, damageLineMode);
        }

        /// <summary>
        /// Draws a text box in the top of the framed center panel when the pointer hovers an action strip card.
        /// Clears a band of the combat log area first so text remains readable.
        /// </summary>
        private void RenderActionStripTooltipOverlay(Character? player, int filledPanelCount, int displaySlotCount, ActionStripDamageLineMode damageLineMode)
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
            const int maxTooltipLines = 18;
            int boxW = Math.Min(52, innerW);
            int innerTextW = Math.Max(4, boxW - 2);

            int rpSeq = RightPanelActionHoverState.HoveredSequenceIndex;

            List<string>? tipLines = null;
            List<List<ColoredText>>? coloredItemLines = null;
            int? anchorCenterX = null;
            bool leftPanelTooltipActive = false;

            if (LeftPanelHoverState.IsActive)
            {
                coloredItemLines = LeftPanelTooltipBuilder.BuildColoredItemLines(player, LeftPanelHoverState.Value, maxTooltipLines + 2);
                if (coloredItemLines.Count > 0)
                    leftPanelTooltipActive = true;
                else
                {
                    tipLines = LeftPanelTooltipBuilder.BuildLines(player, LeftPanelHoverState.Value, innerTextW, maxTooltipLines + 2);
                    leftPanelTooltipActive = tipLines.Count > 0;
                }
            }

            if (tipLines == null || tipLines.Count == 0)
            {
                // Action-strip hover must win over right-panel sequence hover so panels 2+ use the correct
                // anchor and tooltip (overlay fill/text) for the card under the pointer.
                int hiStrip = ActionStripHoverState.HoveredPanelIndex;
                if (filledPanelCount > 0 && displaySlotCount > 0 && hiStrip >= 0 && hiStrip < filledPanelCount)
                {
                    ActionInfoStripLayout.GetPanelRect(hiStrip, displaySlotCount, out int px, out _, out int pw, out _);
                    anchorCenterX = px + pw / 2;
                    tipLines = CombatActionStripBuilder.BuildActionTooltipLines(player, hiStrip, innerTextW, maxTooltipLines + 2, damageLineMode);
                }
                else if (rpSeq >= 0)
                {
                    var combo = player.GetComboActions();
                    if (rpSeq < combo.Count)
                    {
                        tipLines = CombatActionStripBuilder.BuildActionTooltipLines(player, rpSeq, innerTextW, maxTooltipLines + 2, damageLineMode);
                        if (filledPanelCount > 0 && displaySlotCount > 0 && rpSeq < filledPanelCount)
                        {
                            ActionInfoStripLayout.GetPanelRect(rpSeq, displaySlotCount, out int px, out _, out int pw, out _);
                            anchorCenterX = px + pw / 2;
                        }
                    }
                }
            }

            bool hasColoredItemTooltip = coloredItemLines != null && coloredItemLines.Count > 0;
            if (!hasColoredItemTooltip && (tipLines == null || tipLines.Count == 0))
            {
                HoverTooltipDrawing.ClearInnerCenterPanelTooltipOverlay(canvas);
                return;
            }

            if (!hasColoredItemTooltip && tipLines != null && tipLines.Count > maxTooltipLines)
                tipLines = tipLines.GetRange(0, maxTooltipLines);

            int boxWFinal = Math.Min(52, innerW);
            int idealX = anchorCenterX.HasValue
                ? anchorCenterX.Value - boxWFinal / 2
                : innerLeft + Math.Max(0, (innerW - boxWFinal) / 2);
            if (leftPanelTooltipActive &&
                LeftPanelHoverState.TryGetTargetBounds(out int targetX, out _, out int targetWidth, out _))
            {
                idealX = HoverTooltipDrawing.GetHorizontalPositionAvoidingTarget(
                    idealX,
                    boxWFinal,
                    innerLeft,
                    innerRight,
                    targetX,
                    targetWidth);
            }
            int boxX = Math.Max(innerLeft, Math.Min(idealX, innerRight - boxWFinal + 1));
            int innerTextWDraw = Math.Max(4, boxWFinal - 2);

            int contentRows = hasColoredItemTooltip
                ? HoverTooltipColoredDrawing.CountDisplayRows(coloredItemLines!, innerTextWDraw, maxTooltipLines)
                : tipLines!.Count;
            int boxH = contentRows + 2;
            int maxBoxBottom = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            int boxY = innerTop;
            if (boxY + boxH - 1 > maxBoxBottom)
                boxY = Math.Max(innerTop, maxBoxBottom - boxH + 1);

            int innerRightInclusive = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            HoverTooltipDrawing.DrawFramedPanel(canvas, boxX, boxY, boxWFinal, boxH, innerLeft, innerTop, innerRightInclusive, maxBoxBottom);

            int tx = boxX + 1;
            int ty = boxY + 1;
            if (hasColoredItemTooltip)
            {
                HoverTooltipColoredDrawing.DrawColoredLines(canvas, tx, ty, coloredItemLines!, innerTextWDraw, maxTooltipLines);
            }
            else
            {
                foreach (var line in tipLines!)
                {
                    string draw = line.Length > innerTextWDraw ? line.Substring(0, innerTextWDraw - 3) + "..." : line;
                    canvas.AddOverlayText(tx, ty, draw, AsciiArtAssets.Colors.White);
                    ty++;
                }
            }
        }
    }
}
