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
            if (CombatBoardLayout.IsActive && player != null)
            {
                var boardLog = (textManager as CanvasTextManager)?.GetDisplayManagerForCharacter(player).Buffer;
                RenderCombatBoard(player, enemy, boardLog, combatEnemyNamesForLogAlignment);
                return;
            }

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

        /// <summary>Text brightness on strip cards that already fired in the current combo run.</summary>
        private const double SpentCardTextBrightness = 0.45;

        /// <summary>
        /// Renders the action-info strip at the top of the center column (combat, inventory, etc.), above the combat log.
        /// Shows at least <see cref="LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT"/> panels (empty placeholders when the combo is shorter or empty);
        /// selected (next combo step) panel border is white when the sequence is non-empty; slots already fired this run of the combo stay solid white (dimmed text); other filled slots use neutral gray darkened 50%; a miss pulses red and a combo-action hit pulses gold via <see cref="RPGGame.UI.Avalonia.Feedback.HeroActionStripFeedback"/> (thicker stroke while that pulse runs). A normal hit leaves the selected card solid white.
        /// After a successful combo, the white next-border stays on the firing card until that action block finishes (<see cref="HeroActionStripFeedback.TryGetHeldSelectedIndex"/>), while the gold pulse may run on the same card.
        /// Cards with pending ACTION-cadence buffs (slot queue / bank on current step) shimmer via <see cref="RPGGame.UI.Avalonia.Feedback.ActionBonusBorderShimmer"/> (flash still overrides). Granting actions alone do not shimmer.
        /// Panels at indices ≥ <see cref="ComboSequenceMaxHelper.GetEffectiveMax(Character?)"/> use a black border so unused strip capacity matches the character’s combo slot limit.
        /// When player is null, strip is cleared.
        /// </summary>
        /// <param name="drawHoverDetailOverlay">When false, skips the large center tooltip (e.g. character creation narrative occupies the same cells; clearing would erase it).</param>
        /// <param name="damageLineMode">Slot-modified damage with combo-slot amp (always <see cref="ActionStripDamageLineMode.EffectiveWithComboAmp"/> in play).</param>
        public void RenderActionInfoStrip(Character? player, bool drawHoverDetailOverlay = true, ActionStripDamageLineMode damageLineMode = ActionStripDamageLineMode.EffectiveWithComboAmp)
        {
            int stripX = LayoutConstants.ACTION_INFO_X;
            int stripY = LayoutConstants.ACTION_INFO_Y;
            int stripW = LayoutConstants.ACTION_INFO_WIDTH;
            int stripH = LayoutConstants.ACTION_INFO_HEIGHT;
            canvas.ClearTextInArea(stripX, stripY, stripW, stripH);
            canvas.ClearBoxesInArea(stripX, stripY, stripW, stripH);

            // Between fights the log box runs down through the strip band; the hero strip only returns for the next fight.
            if (player == null || CombatBoardLayout.IsBetweenFights)
            {
                ComboTension.Track(null);
                return;
            }

            var panelData = CombatActionStripBuilder.BuildPanelData(player);
            var comboForStrip = player.GetComboActions();
            int filled = panelData.Count;
            int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(filled);
            // Live ComboStep may already point at the next slot after a successful combo;
            // hold the white next-border on the firing panel until that action block finishes.
            int selectedIndex = filled > 0 ? player.ComboStep % filled : -1;
            int liveNextIndex = selectedIndex;
            int heldFiringIndex = -1;
            if (HeroActionStripFeedback.TryGetHeldSelectedIndex(out int heldSelected)
                && heldSelected >= 0
                && heldSelected < filled)
            {
                selectedIndex = heldSelected;
                heldFiringIndex = heldSelected;
            }
            int effectiveMaxSlots = ComboSequenceMaxHelper.GetEffectiveMax(player);
            ComboTension.Track(player);
            bool anyBonusShimmer = false;
            var shimmerNow = DateTimeOffset.UtcNow;
            // Per-roll Action chance (combo or crit) from the same thresholds as the CHANCES panel, held from the opener
            // for the whole run so a landed roll only removes its own factor.
            var thresholds = DiceRollThresholdResolver.Resolve(player);
            int liveActionChancePercent = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                thresholds.EffectiveCrit, thresholds.EffectiveCombo, thresholds.EffectiveHit, thresholds.EffectiveCritMiss).ComboOrCritPercent;
            bool anyCardFiring = heldFiringIndex >= 0;
            for (int f = 0; f < filled && !anyCardFiring; f++)
                anyCardFiring = HeroActionStripFeedback.IsFlashEmphasisActive(f);
            int actionChancePercent = ComboTension.ResolveComboOddsChance(player, liveNextIndex, anyCardFiring, liveActionChancePercent);

            for (int i = 0; i < displayCount; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, displayCount, out int px, out int py, out int pw, out int panelH);
                bool isEmptySlot = i >= filled;
                var baseBorder = ActionInfoStripLayout.GetPanelBorderColor(i, filled, selectedIndex, effectiveMaxSlots);
                // Combo anticipation: fired slots keep a solid white border (text dims) so the run so far reads as one chain;
                // the finisher always reads gold so the goal is visible from the opener.
                bool isSpent = !isEmptySlot && i < effectiveMaxSlots && ComboTension.IsSpent(i, selectedIndex);
                bool isFinisherSlot = !isEmptySlot && i < effectiveMaxSlots && ComboTension.IsFinisherNext(i, filled);
                if (isFinisherSlot)
                    baseBorder = i == selectedIndex ? ComboTension.FinisherGold : ActionInfoStripLayout.DarkenRgb(ComboTension.FinisherGold, 0.45);
                else if (isSpent)
                    baseBorder = AsciiArtAssets.Colors.White;
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
                // On the board the room backdrop runs behind the strip: fill the card so it stays solid when the
                // turn scrim lifts (hover, or the hero's own turn), matching the enemy strip cards.
                if (CombatBoardLayout.IsActive)
                    AddBoardBox(px, py, pw, panelH, borderColor, BoardCardBackground, borderThick);
                else
                    canvas.AddBorder(px, py, pw, panelH, borderColor, borderThick);

                if (bonusCue && !flashActive)
                {
                    var highlight = ActionBonusBorderShimmer.GetTravelHighlightColor(shimmerNow);
                    foreach (var (hx, hy, hw, hh) in ActionBonusBorderShimmer.GetTravelHighlightRects(px, py, pw, panelH, shimmerNow))
                        canvas.AddBorder(hx, hy, hw, hh, highlight, 2);
                }

                if (isEmptySlot || i >= effectiveMaxSlots)
                    continue;

                // Odds count from the live next slot; the card that is firing (held border or flash) and fired cards drop them.
                bool isFiring = (heldFiringIndex == i) || HeroActionStripFeedback.IsFlashEmphasisActive(i);
                string? reachChance = !isSpent && !isFiring && liveNextIndex >= 0 && i >= liveNextIndex
                    ? ComboTension.FormatReachChance(ComboTension.GetReachChancePercent(i, liveNextIndex, actionChancePercent))
                    : null;
                DrawActionCardContent(player, panelData[i], comboForStrip[i], i, px, py, pw, panelH, damageLineMode,
                    textBrightness: isSpent ? SpentCardTextBrightness : 1.0, reachChanceText: reachChance);
            }

            if (anyBonusShimmer)
                ActionBonusBorderShimmer.KeepAlive();

            if (drawHoverDetailOverlay)
                RenderActionStripTooltipOverlay(player, filled, displayCount, damageLineMode);
        }

        /// <summary>
        /// Draws one action card's text (name, role, swing, description, modifiers, threshold) inside the
        /// bordered rect. Shared by the action strip and the combat board (<see cref="CombatBoardRenderer"/>).
        /// </summary>
        /// <param name="textBrightness">RGB scale for every line (below 1 dims a combo slot that already fired this run).</param>
        /// <param name="reachChanceText">Chance the combo lands this slot (<see cref="ComboTension.GetReachChancePercent"/>), right-aligned on the name row; null hides it.</param>
        internal void DrawActionCardContent(Character player, ActionPanelInfo info, Action action, int slotIndex,
            int px, int py, int pw, int panelH, ActionStripDamageLineMode damageLineMode, double textBrightness = 1.0,
            string? reachChanceText = null)
        {
            Color Shade(Color c) => textBrightness >= 1.0 ? c : ActionInfoStripLayout.DarkenRgb(c, textBrightness);
            int contentX = px + 1;
            int contentY = py + 1;
            int contentW = Math.Max(0, pw - 2);
            int panelBottomExclusive = py + panelH;
            bool showReach = !string.IsNullOrEmpty(reachChanceText) && contentW >= reachChanceText.Length + 5;
            int nameW = showReach ? contentW - reachChanceText!.Length - 1 : contentW;
            string name = string.IsNullOrEmpty(info.Name) ? "?" : (info.Name.Length > nameW ? info.Name.Substring(0, Math.Max(1, nameW - 3)) + "..." : info.Name);
            // Required weapon basic stays red; other titles carry their class / rarity color.
            var nameColor = WeaponRequiredComboAction.IsRequiredBasicForEquippedWeapon(player, action)
                ? AsciiArtAssets.Colors.Red
                : ActionNameColors.Resolve(action);
            canvas.AddText(contentX, contentY, name, Shade(nameColor));
            if (showReach)
                canvas.AddText(contentX + contentW - reachChanceText!.Length, contentY, reachChanceText, Shade(AsciiArtAssets.Colors.Cyan));
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
                canvas.AddText(contentX, contentY, text, Shade(color));
                contentY++;
            }

            foreach (var roleLine in CombatActionStripBuilder.BuildActionStripComboRoleLines(player, action))
                drawLine(roleLine, AsciiArtAssets.Colors.White);

            // Calculated damage | seconds on cards; hover tip uses % damage/speed for the same damageLineMode
            const double cmpEps = 0.0001;
            CombatActionStripBuilder.GetStripSwingDisplayPercents(in info, player, action, damageLineMode, out double damageDisplayPct, out double speedDisplayPct);
            bool damageDiffersFromIntrinsic = Math.Abs(damageDisplayPct - info.DamageBase) > cmpEps;
            bool speedDiffersFromIntrinsic = Math.Abs(speedDisplayPct - info.SpeedBase) > cmpEps;
            bool convertScalesDamage = MaterialSetController.GetConvertDamageBonus(player, action) > 0;
            var swingLineColor = convertScalesDamage
                ? AsciiArtAssets.Colors.Green
                : damageDiffersFromIntrinsic
                ? (damageDisplayPct > info.DamageBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                : speedDiffersFromIntrinsic
                    ? (speedDisplayPct > info.SpeedBase ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Red)
                    : AsciiArtAssets.Colors.White;
            string swingLine = CombatActionStripBuilder.FormatStripSwingLine(in info, player, action, damageLineMode, slotIndex);
            drawLine(swingLine, swingLineColor);
            string stanceLine = HeroDefenseHudFormatter.FormatActionBlockSuffix(action);
            if (!string.IsNullOrEmpty(stanceLine))
                drawLine(stanceLine, AsciiArtAssets.Colors.White);

            // Spreadsheet DESCRIPTION directly under damage/speed (name → swing → description).
            int rowsLeftAfterSwing = Math.Max(0, panelBottomExclusive - contentY - reserveBottomRows);
            int descBudget = Math.Min(2, rowsLeftAfterSwing);
            foreach (var descLine in CombatActionStripBuilder.BuildActionStripDescriptionLines(action, contentW, descBudget))
                drawLine(descLine, AsciiArtAssets.Colors.Gray);

            // External bonuses (skills / material / equipped) are already folded into the swing line;
            // their per-source breakdown lives in the hover tooltip only.
            int tailBudget = Math.Max(0, panelBottomExclusive - contentY - reserveBottomRows);
            var tailLines = CombatActionStripBuilder.BuildActionStripModifierTailLines(action, contentW, tailBudget, player, slotIndex);
            foreach (var tl in tailLines)
                drawLine(tl, AsciiArtAssets.Colors.White);

            if (hasThreshold && contentY < panelBottomExclusive)
            {
                string thresholdLine = info.ThresholdText!.Length > contentW ? info.ThresholdText.Substring(0, contentW - 3) + "..." : info.ThresholdText;
                canvas.AddText(contentX, contentY, thresholdLine, Shade(AsciiArtAssets.Colors.Cyan));
            }
        }

        /// <summary>
        /// When the hovered item is a bag tile or paper-doll slot, returns where its tooltip docks so it never covers
        /// item tiles or the description pane: the empty bag space below the last tile row (down to the panel bottom,
        /// over the actions list), or the description pane when that space is too short for <paramref name="boxH"/> rows.
        /// </summary>
        private static bool TryGetInventoryTooltipDock(int innerLeft, int innerRight, int maxBoxBottom,
            Func<int, int> boxHeightForWidth, out int dockX, out int dockY, out int dockW)
        {
            dockX = 0;
            dockY = 0;
            dockW = 0;
            if (!LeftPanelHoverState.TryGetTargetBounds(out int tx, out int ty, out _, out _))
                return false;
            var grid = Inventory.InventoryScreenRenderer.LastBagGridBounds;
            if (!Inventory.InventoryScreenRenderer.BoundsContain(grid, tx, ty)
                && !Inventory.InventoryScreenRenderer.BoundsContain(Inventory.InventoryScreenRenderer.LastPaperDollBounds, tx, ty))
                return false;

            int freeTop = Inventory.InventoryScreenRenderer.LastBagGridFreeTopY;
            if (grid.Width >= 12 && freeTop > 0)
            {
                int x = Math.Max(innerLeft, grid.X);
                int w = Math.Min(52, Math.Min(innerRight, grid.X + grid.Width - 1) - x + 1);
                if (w >= 12 && freeTop + boxHeightForWidth(w) - 1 <= maxBoxBottom)
                {
                    dockX = x;
                    dockY = freeTop;
                    dockW = w;
                    return true;
                }
            }

            var pane = Inventory.InventoryScreenRenderer.LastDetailPaneBounds;
            if (pane.Width < 12 || pane.Height <= 0)
                return false;
            dockX = Math.Max(innerLeft, pane.X);
            int right = Math.Min(innerRight, pane.X + pane.Width - 1);
            dockW = Math.Min(52, right - dockX + 1);
            dockY = -1;
            return dockW >= 12;
        }

        /// <summary>
        /// Draws a text box in the top of the framed center panel when the pointer hovers an action strip card.
        /// Clears a band of the combat log area first so text remains readable.
        /// </summary>
        private void RenderActionStripTooltipOverlay(Character? player, int filledPanelCount, int displaySlotCount, ActionStripDamageLineMode damageLineMode)
        {
            if (player == null)
                return;

            // Gear/bag pool hover tooltips are drawn in RightPanelRenderer after pool rows (correct z-order).
            // Must skip both: inventory strip paint runs after the right panel and would ClearInnerCenterPanelTooltipOverlay,
            // wiping the item tooltip and leaving straddling overlay fragments in the right column.
            if (RightPanelActionHoverState.HoveredPoolIndex >= 0
                || RightPanelActionHoverState.HoveredInventoryPoolIndex >= 0)
                return;

            int innerLeft = LayoutConstants.CENTER_PANEL_X + 1;
            int innerTop = LayoutConstants.CombatLogContentY;
            int innerRight = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int innerW = Math.Max(8, innerRight - innerLeft + 1);
            // Item tooltips (animal suffixes + taxon synergies) need more rows than action tips:
            // colored segments wrap after "Name —", so a tight 18-row cap clipped trigger bodies.
            // Stat/threshold/action tips carry their formulas and sources, so let them use the panel's height.
            int panelTooltipLines = Math.Max(1, LayoutConstants.CombatLogContentHeight - 2);
            int maxActionTooltipLines = Math.Max(18, Math.Min(32, panelTooltipLines));
            int maxItemTooltipLines = Math.Max(28, Math.Min(40, panelTooltipLines));
            int boxW = Math.Min(52, innerW);
            int innerTextW = Math.Max(4, boxW - 2);

            int rpSeq = RightPanelActionHoverState.HoveredSequenceIndex;

            List<string>? tipLines = null;
            List<List<ColoredText>>? coloredItemLines = null;
            int? anchorCenterX = null;
            int? idealXOverride = null;
            bool leftPanelTooltipActive = false;
            int maxTooltipLines = maxActionTooltipLines;
            int? hoverTargetY = null;

            if (LeftPanelHoverState.IsActive)
            {
                coloredItemLines = LeftPanelTooltipBuilder.BuildColoredItemLines(player, LeftPanelHoverState.Value, maxItemTooltipLines + 2);
                if (coloredItemLines.Count > 0)
                {
                    leftPanelTooltipActive = true;
                    maxTooltipLines = maxItemTooltipLines;
                }
                else
                {
                    tipLines = LeftPanelTooltipBuilder.BuildLines(player, LeftPanelHoverState.Value, innerTextW, maxItemTooltipLines + 2);
                    leftPanelTooltipActive = tipLines.Count > 0;
                    if (leftPanelTooltipActive)
                        maxTooltipLines = maxItemTooltipLines;
                }
            }

            if (!leftPanelTooltipActive && (tipLines == null || tipLines.Count == 0))
            {
                // Action-strip hover must win over right-panel sequence hover so panels 2+ use the correct
                // anchor and tooltip (overlay fill/text) for the card under the pointer.
                int hiStrip = ActionStripHoverState.HoveredPanelIndex;
                if (filledPanelCount > 0 && displaySlotCount > 0 && hiStrip >= 0 && hiStrip < filledPanelCount)
                {
                    ActionInfoStripLayout.GetPanelRect(hiStrip, displaySlotCount, out int px, out _, out int pw, out _);
                    anchorCenterX = px + pw / 2;
                    coloredItemLines = CombatActionStripBuilder.BuildColoredActionTooltipLinesForSlot(
                        player, hiStrip, maxTooltipLines + 2, damageLineMode,
                        HoverTooltipDetailState.IsAltDetailActive);
                }
                else if (rpSeq >= 0)
                {
                    var combo = player.GetComboActions();
                    if (rpSeq < combo.Count)
                    {
                        coloredItemLines = CombatActionStripBuilder.BuildColoredActionTooltipLinesForSlot(
                            player, rpSeq, maxTooltipLines + 2, damageLineMode,
                            HoverTooltipDetailState.IsAltDetailActive);
                        if (filledPanelCount > 0 && displaySlotCount > 0 && rpSeq < filledPanelCount)
                        {
                            ActionInfoStripLayout.GetPanelRect(rpSeq, displaySlotCount, out int px, out _, out int pw, out _);
                            anchorCenterX = px + pw / 2;
                        }
                    }
                }
                else if (CombatLogActionHoverState.IsActive)
                {
                    var info = CombatLogActionHoverState.InfoLines;
                    if (info != null && info.Count > 0)
                    {
                        coloredItemLines = new List<List<ColoredText>>(info);
                        maxTooltipLines = Math.Max(maxTooltipLines, 22);
                        if (CombatLogActionHoverState.TryGetTargetBounds(out int logX, out int logY, out int logW, out _))
                        {
                            hoverTargetY = logY;
                            // Span the prose hit band (full log column) so a centered narrow tip
                            // cannot leave orphaned narrative fragments on either side.
                            boxW = Math.Max(8, Math.Min(innerW, logW > 0 ? logW : innerW));
                            idealXOverride = Math.Max(innerLeft, Math.Min(logX, innerRight - boxW + 1));
                        }
                        else
                        {
                            boxW = innerW;
                            idealXOverride = innerLeft;
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

            int boxWFinal = idealXOverride.HasValue
                ? Math.Min(boxW, innerW)
                : Math.Min(
                    CombatLogActionHoverState.IsActive ? innerW : 52,
                    innerW);
            int idealX = idealXOverride ?? (anchorCenterX.HasValue
                ? anchorCenterX.Value - boxWFinal / 2
                : innerLeft + Math.Max(0, (innerW - boxWFinal) / 2));
            if (leftPanelTooltipActive &&
                LeftPanelHoverState.TryGetTargetBounds(out int targetX, out int targetY, out int targetWidth, out _))
            {
                hoverTargetY = targetY;
                idealX = HoverTooltipDrawing.GetHorizontalPositionAvoidingTarget(
                    idealX,
                    boxWFinal,
                    innerLeft,
                    innerRight,
                    targetX,
                    targetWidth);
            }
            int maxBoxBottom = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            int BoxHeightForWidth(int w) => 2 + (hasColoredItemTooltip
                ? HoverTooltipColoredDrawing.CountDisplayRows(coloredItemLines!, Math.Max(4, w - 2), maxTooltipLines)
                : tipLines!.Count);
            // Inventory grid / paper doll hover: dock below the bag tiles (else the description pane) so the tip never covers item tiles.
            int? dockedY = null;
            if (leftPanelTooltipActive
                && TryGetInventoryTooltipDock(innerLeft, innerRight, maxBoxBottom, BoxHeightForWidth, out int dockX, out int dockY, out int dockW))
            {
                boxWFinal = dockW;
                idealX = dockX;
                if (dockY >= 0)
                    dockedY = dockY;
            }
            int boxX = Math.Max(innerLeft, Math.Min(idealX, innerRight - boxWFinal + 1));
            int innerTextWDraw = Math.Max(4, boxWFinal - 2);

            int boxH = BoxHeightForWidth(boxWFinal);
            int boxY = dockedY
                ?? (hoverTargetY.HasValue
                    ? HoverTooltipDrawing.GetVerticalPositionNearTarget(
                        hoverTargetY.Value, boxH, innerTop, maxBoxBottom)
                    : innerTop);
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
