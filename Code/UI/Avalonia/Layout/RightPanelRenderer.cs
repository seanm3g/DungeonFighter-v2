namespace RPGGame.UI.Avalonia.Layout
{
    using System;
    using System.Collections.Generic;
    using RPGGame;
    using RPGGame.ActionInteractionLab;
    using RPGGame.Combat.Calculators;
    using RPGGame.Handlers.Inventory;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Feedback;
    using RPGGame.UI.Avalonia.Managers;
    using RPGGame.UI.Avalonia.Renderers;
    using RPGGame.UI.Avalonia.Renderers.Inventory;
    using Helpers = RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Renders the right panel (location/enemy info or inventory actions)
    /// </summary>
    public class RightPanelRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly ICanvasInteractionManager? interactionManager;
        private readonly StatsPanelStateManager? stateManager;

        public RightPanelRenderer(
            GameCanvasControl canvas,
            ColoredTextWriter textWriter,
            ICanvasInteractionManager? interactionManager = null,
            StatsPanelStateManager? stateManager = null)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.interactionManager = interactionManager;
            this.stateManager = stateManager;
        }
        
        /// <summary>
        /// Renders the dungeon and enemy information panel (right side)
        /// Shows combo sequence and action pool when on inventory page or combo management, otherwise shows location/enemy info
        /// </summary>
        /// <param name="inventoryComboRightPanel">When true, always show sequence/pool (caller is inventory/combo UI). Avoids relying on title strings alone.</param>
        /// <param name="registerActionLabEnemyLevelHover">When true (Action Lab combat), register click target for enemy level line.</param>
        public void RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName, string title, Character? character, bool inventoryComboRightPanel = false, bool registerActionLabEnemyLevelHover = false)
        {
            // Clear the right panel content area before rendering to prevent text overlap.
            // RIGHT_PANEL_X shifts when EffectiveVisibleWidth changes (resize / scale-up); clearing only
            // the new inner rect can leave one column of prior-pass text (ghosting). Pad by a few columns
            // on each side — bounded so we never reach the center column (gap is only 3 cells at full width).
            const int clearPadColumns = 2;
            int contentX = LayoutConstants.RIGHT_PANEL_X + 2;
            int contentY = LayoutConstants.RIGHT_PANEL_Y + 1;
            int contentWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 4;  // Account for left and right borders
            int contentHeight = LayoutConstants.RIGHT_PANEL_HEIGHT - 2; // Account for top and bottom borders
            int minClearX = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH;
            int naiveLeft = contentX - clearPadColumns;
            int clearRightExclusive = contentX + contentWidth + clearPadColumns;
            int clearX = System.Math.Max(minClearX, naiveLeft);
            int clearW = System.Math.Max(1, clearRightExclusive - clearX);

            HubRegionPlate = null;
            canvas.ClearTextInArea(clearX, contentY, clearW, contentHeight);
            canvas.ClearProgressBarsInArea(clearX, contentY, clearW, contentHeight);
            canvas.ClearSegmentedBarsInArea(clearX, contentY, clearW, contentHeight);
            int rpX = LayoutConstants.RIGHT_PANEL_X;
            int rpY = LayoutConstants.RIGHT_PANEL_Y;
            int rpW = LayoutConstants.RIGHT_PANEL_WIDTH;
            int rpH = LayoutConstants.RIGHT_PANEL_HEIGHT;
            canvas.ClearBoxesInArea(System.Math.Max(0, rpX - 1), rpY, rpW + 2, rpH);
            
            // Main border for right panel - extends to right edge with no padding
            canvas.AddBorder(rpX, rpY, rpW, rpH, PanelJoin.Border(AsciiArtAssets.Colors.Purple));
            PanelJoin.ApplyRightPanel(canvas);
            
            int y = LayoutConstants.RIGHT_PANEL_Y + 1;
            int x = LayoutConstants.RIGHT_PANEL_X + 2;
            
            // Inventory / combo management: sequence + pool. Prefer explicit flag from CanvasRenderer; keep title fallback for compatibility.
            // The hub's Skills tab keeps the hub's right column too (read-only there: clicks belong to the inventory).
            bool hubSkillsTab = title == "SKILL TREE" && HubTabBar.IsActive;
            bool useInventoryPanel = character != null && (inventoryComboRightPanel
                || title == "INVENTORY"
                || title == "COMBO MANAGEMENT"
                || title == "ACTIONS"
                || hubSkillsTab);
            // Inventory as the hub: navigation lives in the center tabs, Dungeon Select is this right column and Main Menu /
            // Settings in the Esc menu, so the right column shows the action sequence and the pool it draws from.
            // Every hub screen (Inventory, Actions, Skills, Trade Up and their sub-prompts): the Enter Dungeon panel.
            bool hubDungeonPanel = useInventoryPanel && HubTabBar.IsActive;
            if (hubDungeonPanel)
            {
                RenderHubDungeonPanel(x, y, character!);
            }
            else if (useInventoryPanel)
            {
                RenderInventoryRightPanel(x, y, character!, interactive: !hubSkillsTab);
            }
            else
            {
                // Render location and enemy info for other pages
                int? heroLevelForLabCaption = registerActionLabEnemyLevelHover ? character?.Level : null;
                RenderLocationEnemyPanel(x, y, enemy, dungeonName, roomName, registerActionLabEnemyLevelHover, heroLevelForLabCaption);
            }
        }

        /// <summary>Game state the hub dungeon panel reads (current run, dungeons on offer); set by the canvas renderer.</summary>
        public static GameStateManager? HubStateManager { get; set; }

        /// <summary>The region whose environment plate fades in under the hub's region panel.</summary>
        public sealed record HubRegionPlateInfo(string RegionKey, string Theme);

        /// <summary>Set while the hub right panel shows the region (no dungeon in progress); null otherwise.</summary>
        public static HubRegionPlateInfo? HubRegionPlate { get; private set; }

        /// <summary>
        /// Hub right column: the dungeon in progress if there is one, otherwise the current region and the dungeons on
        /// offer (name, level, theme, foes). The whole panel is the hub's Dungeon Select button.
        /// </summary>
        private void RenderHubDungeonPanel(int x, int y, Character character)
        {
            int panelBottom = LayoutConstants.RIGHT_PANEL_Y + LayoutConstants.RIGHT_PANEL_HEIGHT - 2;
            int maxW = Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 4);
            var sm = HubStateManager;

            if (interactionManager != null)
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = LayoutConstants.RIGHT_PANEL_X + 1,
                    Y = LayoutConstants.RIGHT_PANEL_Y + 1,
                    Width = Math.Max(1, LayoutConstants.RIGHT_PANEL_WIDTH - 2),
                    Height = Math.Max(1, LayoutConstants.RIGHT_PANEL_HEIGHT - 2),
                    Type = ElementType.MenuOption,
                    Value = InventoryMenuHandler.HubDungeonSelect,
                    DisplayText = "Dungeon Select"
                });
            }

            var current = sm?.CurrentDungeon;
            if (current != null)
            {
                canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("DUNGEON"), AsciiArtAssets.Colors.Gold);
                y += 2;
                y = DrawHubDungeonEntry(x, y, maxW, panelBottom, current);
                int rooms = current.Rooms?.Count ?? 0;
                if (rooms > 0 && y <= panelBottom)
                {
                    int done = Math.Clamp(current.TraversalState?.NextContentIndex ?? 0, 0, rooms);
                    canvas.AddText(x, y, $"Rooms: {done}/{rooms}", AsciiArtAssets.Colors.White);
                    y++;
                }
                string? roomName = sm!.CurrentRoom?.Name;
                if (!string.IsNullOrEmpty(roomName) && y + 1 <= panelBottom)
                {
                    canvas.AddText(x, y, "Room:", AsciiArtAssets.Colors.Gray);
                    y++;
                    canvas.AddText(x, y, RightPanelContentText.EllipsizeToPanelWidth(roomName), AsciiArtAssets.Colors.Yellow);
                    y++;
                }
                return;
            }

            // "ENTER DUNGEON" call to action: big centered title, the region, then the dungeons on offer, the whole
            // block centered vertically over the region plate.
            var region = new TravelRegionCatalog().GetRegionForCharacter(character);
            var offered = new List<Dungeon>();
            if (sm?.AvailableDungeons != null)
                foreach (var d in sm.AvailableDungeons)
                    if (d != null && !GameConstants.IsDungeonSelectionUtilityOption(d.Name))
                        offered.Add(d);

            int innerLeft = LayoutConstants.RIGHT_PANEL_X + 1;
            int innerW = Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 2);
            int panelTop = LayoutConstants.RIGHT_PANEL_Y + 1;
            var callColor = AsciiArtAssets.Colors.DarkRed;

            double titleScale = Math.Clamp(Math.Floor(maxW * 2.0 / "DUNGEON".Length) / 2.0, 1.0, 3.0);
            int titleRows = (int)Math.Ceiling(titleScale);
            const double labelScale = 1.5;
            const int labelRows = 2;
            int entryX = x + 2;
            int entryW = Math.Max(8, maxW - 4);
            bool hasRegionName = !string.IsNullOrWhiteSpace(region.DisplayName);

            int entriesHeight = offered.Count == 0 ? 1 : 0;
            foreach (var d in offered)
                entriesHeight += HubDungeonEntryHeight(d, entryW) + 1;
            int blockHeight = titleRows * 2 + 1 + labelRows + (hasRegionName ? 1 : 0) + 1 + labelRows + 1 + entriesHeight;
            // The title is the panel's big button: its bottom ("DUNGEON") sits on the panel's vertical midpoint, the
            // region and dungeon list hang below, and the title lifts only as far as the list needs to fit.
            int available = panelBottom - panelTop + 1;
            int titleCentered = panelTop + Math.Max(0, available / 2 - titleRows * 2);
            int lowestFit = panelTop + Math.Max(0, available - blockHeight);
            y = Math.Min(titleCentered, lowestFit);

            DrawCenteredScaled(innerLeft, innerW, y, "ENTER", callColor, titleScale);
            y += titleRows;
            DrawCenteredScaled(innerLeft, innerW, y, "DUNGEON", callColor, titleScale);
            y += titleRows + 1;

            DrawCenteredScaled(innerLeft, innerW, y, "Region:", callColor, labelScale);
            y += labelRows;
            if (hasRegionName)
            {
                string regionName = region.DisplayName.Length > innerW - 2 ? region.DisplayName.Substring(0, innerW - 5) + "..." : region.DisplayName;
                int rx = innerLeft + Math.Max(0, (innerW - regionName.Length) / 2);
                if (!string.IsNullOrEmpty(region.Theme))
                    textWriter.RenderSegments(DungeonThemeColors.BuildThemedName(regionName, region.Theme), rx, y);
                else
                    canvas.AddText(rx, y, regionName, callColor);
                y++;
            }
            y++;

            DrawCenteredScaled(innerLeft, innerW, y, "Dungeons:", callColor, labelScale);
            y += labelRows + 1;

            if (offered.Count == 0)
            {
                canvas.AddText(entryX, y, "(none yet)", AsciiArtAssets.Colors.DarkGray);
                y++;
            }

            for (int i = 0; i < offered.Count; i++)
            {
                if (y + 2 > panelBottom)
                {
                    canvas.AddText(entryX, y, $"... +{offered.Count - i} more", AsciiArtAssets.Colors.Gray);
                    y++;
                    break;
                }
                y = DrawHubDungeonEntry(entryX, y, entryW, panelBottom, offered[i]);
                y++;
            }

            if (!string.IsNullOrWhiteSpace(region.Theme))
                HubRegionPlate = new HubRegionPlateInfo(string.IsNullOrEmpty(region.Id) ? region.DisplayName : region.Id, region.Theme);
        }

        /// <summary>Draws <paramref name="text"/> at <paramref name="scale"/>× the grid font, centered in the given columns.</summary>
        private void DrawCenteredScaled(int left, int width, int y, string text, global::Avalonia.Media.Color color, double scale)
        {
            int cells = (int)Math.Ceiling(text.Length * scale);
            int sx = left + Math.Max(0, (width - cells) / 2);
            if (scale <= 1.0)
                canvas.AddText(sx, y, text, color);
            else
                canvas.AddScaledText(sx, y, text, color, scale);
        }

        /// <summary>Rows <see cref="DrawHubDungeonEntry"/> uses for <paramref name="dungeon"/> (name, level, up to two foe lines).</summary>
        private static int HubDungeonEntryHeight(Dungeon dungeon, int maxW)
        {
            int rows = 2;
            if (dungeon.PossibleEnemies != null && dungeon.PossibleEnemies.Count > 0)
            {
                var foes = new List<string>();
                foreach (var e in dungeon.PossibleEnemies)
                    if (!string.IsNullOrWhiteSpace(e) && !foes.Contains(e))
                        foes.Add(e);
                rows += WrapWords("Foes: " + string.Join(", ", foes), maxW, 2).Count;
            }
            return rows;
        }

        /// <summary>One dungeon: themed name, level and theme, then its foes. Returns the next free row.</summary>
        private int DrawHubDungeonEntry(int x, int y, int maxW, int panelBottom, Dungeon dungeon)
        {
            string name = RightPanelContentText.EllipsizeToPanelWidth(dungeon.Name);
            if (!string.IsNullOrEmpty(dungeon.Theme))
                textWriter.RenderSegments(DungeonThemeColors.BuildThemedName(name, dungeon.Theme), x, y);
            else
                canvas.AddText(x, y, name, AsciiArtAssets.Colors.Cyan);
            y++;

            string level = dungeon.MinLevel == dungeon.MaxLevel || dungeon.MaxLevel <= 0
                ? $"Lvl {dungeon.MinLevel}"
                : $"Lvl {dungeon.MinLevel}-{dungeon.MaxLevel}";
            string meta = string.IsNullOrEmpty(dungeon.Theme) ? level : $"{level}  {dungeon.Theme}";
            if (y <= panelBottom)
            {
                canvas.AddText(x, y, meta.Length > maxW ? meta.Substring(0, maxW) : meta, AsciiArtAssets.Colors.Gold);
                y++;
            }

            if (dungeon.PossibleEnemies != null && dungeon.PossibleEnemies.Count > 0)
            {
                var foes = new List<string>();
                foreach (var e in dungeon.PossibleEnemies)
                    if (!string.IsNullOrWhiteSpace(e) && !foes.Contains(e))
                        foes.Add(e);
                foreach (var line in WrapWords("Foes: " + string.Join(", ", foes), maxW, 2))
                {
                    if (y > panelBottom)
                        break;
                    canvas.AddText(x, y, line, AsciiArtAssets.Colors.Gray);
                    y++;
                }
            }

            return y;
        }

        /// <summary>Word-wraps <paramref name="text"/> to <paramref name="width"/> columns, at most <paramref name="maxLines"/> (last one ends "..." when cut).</summary>
        private static List<string> WrapWords(string? text, int width, int maxLines)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text) || width <= 3 || maxLines <= 0)
                return lines;
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cur = "";
            int wi = 0;
            for (; wi < words.Length; wi++)
            {
                string w = words[wi].Length > width ? words[wi].Substring(0, width) : words[wi];
                string next = cur.Length == 0 ? w : cur + " " + w;
                if (next.Length <= width)
                {
                    cur = next;
                    continue;
                }
                lines.Add(cur);
                cur = w;
                if (lines.Count == maxLines)
                    break;
            }
            if (lines.Count < maxLines && cur.Length > 0)
            {
                lines.Add(cur);
                wi = words.Length;
            }
            if (wi < words.Length && lines.Count > 0)
            {
                string last = lines[^1];
                lines[^1] = (last.Length > width - 3 ? last.Substring(0, width - 3) : last) + "...";
            }
            return lines;
        }

        /// <summary>
        /// Renders combo sequence and action pool for inventory page.
        /// Uses all available vertical space in the panel for both sections.
        /// </summary>
        private void RenderInventoryRightPanel(int x, int y, Character character, bool interactive = true)
        {
            int panelBottom = LayoutConstants.RIGHT_PANEL_Y + LayoutConstants.RIGHT_PANEL_HEIGHT - 2;
            int rowWidth = Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 4);
            interactive = interactive && interactionManager != null;

            // Sequence summary (order matches strip above)
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("SEQUENCE"), AsciiArtAssets.Colors.Gold);
            y++;
            canvas.AddText(x, y, "(order = strip)", AsciiArtAssets.Colors.DarkGray);
            y += 2;
            
            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                int currentStepInSequence = (character.ComboStep % comboActions.Count) + 1;
                canvas.AddText(x, y, $"Step: {currentStepInSequence}/{comboActions.Count}", AsciiArtAssets.Colors.White);
                y += 2;
                
                const int actionPoolReserve = 16; // POOL + INVENTORY headers / totals + at least one row each (sync InventoryRightPanelLayout)
                int comboLinesAvailable = panelBottom - y - actionPoolReserve;
                int maxDisplay = Math.Max(0, Math.Min(comboActions.Count, comboLinesAvailable - 1)); // -1 for "... +N more" when truncated
                if (maxDisplay == 0 && comboActions.Count > 0)
                    maxDisplay = Math.Min(comboActions.Count, comboLinesAvailable);
                for (int i = 0; i < maxDisplay; i++)
                {
                    var action = comboActions[i];
                    string currentStep = (character.ComboStep % comboActions.Count == i) ? " ←" : "";
                    string actionName = action.Name;
                    if (actionName.Length > 20)
                        actionName = actionName.Substring(0, 17) + "...";
                    string line = $"{actionName}{currentStep}";
                    var seqColor = ActionNameColors.Resolve(action);
                    if (interactive)
                    {
                        string value = $"{ComboPointerInput.Prefix}rm:{i}";
                        var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, value, line);
                        interactionManager!.AddClickableElement(btn);
                        var c = btn.IsHovered ? AsciiArtAssets.Colors.Yellow : seqColor;
                        canvas.AddText(x, y, line, c);
                    }
                    else
                        canvas.AddText(x, y, line, seqColor);
                    y++;
                }
                
                if (comboActions.Count > maxDisplay)
                {
                    canvas.AddText(x, y, $"... +{comboActions.Count - maxDisplay} more", AsciiArtAssets.Colors.Gray);
                    y++;
                }
            }
            else
            {
                canvas.AddText(x, y, "(empty)", AsciiArtAssets.Colors.DarkGray);
                y += 2;
            }
            
            y += 1; // Spacing

            var comboRefSet = new HashSet<RPGGame.Action>(comboActions);

            // Pool from gear / class — use remaining space (one row per pool entry; index matches AddToCombo)
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("POOL"), AsciiArtAssets.Colors.Gold);
            y++;
            canvas.AddText(x, y, "(from gear)", AsciiArtAssets.Colors.DarkGray);
            y += 2;
            
            var actionPool = character.GetActionPool();
            if (actionPool.Count > 0)
            {
                canvas.AddText(x, y, $"Total: {actionPool.Count}", AsciiArtAssets.Colors.White);
                y += 2;
                
                int poolIdx = 0;
                while (poolIdx < actionPool.Count && y <= panelBottom - 1)
                {
                    var poolAction = actionPool[poolIdx];
                    bool inSeq = comboRefSet.Contains(poolAction);
                    string actionName = poolAction.Name;
                    if (actionName.Length > 20)
                        actionName = actionName.Substring(0, 17) + "...";
                    string tag = inSeq ? "*" : "";
                    string line = $"{tag}{actionName}";
                    var baseColor = inSeq ? AsciiArtAssets.Colors.DarkGray : ActionNameColors.Resolve(poolAction);
                    if (interactive)
                    {
                        string value = $"{ComboPointerInput.Prefix}pool:{poolIdx}";
                        var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, value, line);
                        interactionManager!.AddClickableElement(btn);
                        var c = btn.IsHovered ? AsciiArtAssets.Colors.Yellow : baseColor;
                        canvas.AddText(x, y, line, c);
                    }
                    else
                        canvas.AddText(x, y, line, baseColor);
                    y++;
                    poolIdx++;
                }
                
                if (poolIdx < actionPool.Count)
                {
                    canvas.AddText(x, y, $"... +{actionPool.Count - poolIdx} more", AsciiArtAssets.Colors.Gray);
                    y++;
                }
            }
            else
            {
                canvas.AddText(x, y, "(No actions)", AsciiArtAssets.Colors.DarkGray);
                y++;
            }

            // INVENTORY: actions on bag items — click equips that item (swap) and adds the action to the sequence when possible
            y += 1;
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("INVENTORY"), AsciiArtAssets.Colors.Gold);
            y++;
            canvas.AddText(x, y, "(from bag)", AsciiArtAssets.Colors.DarkGray);
            y += 2;

            var invEntries = InventoryActionPoolEntries.Build(character);
            if (invEntries.Count == 0)
            {
                canvas.AddText(x, y, "(empty)", AsciiArtAssets.Colors.DarkGray);
            }
            else
            {
                canvas.AddText(x, y, $"Total: {invEntries.Count}", AsciiArtAssets.Colors.White);
                y += 2;

                int invFlat = 0;
                for (; invFlat < invEntries.Count && y <= panelBottom - 1; invFlat++)
                {
                    var invEntry = invEntries[invFlat];
                    bool invInSeq = comboActions.Any(a =>
                        string.Equals(a.Name, invEntry.ActionName, StringComparison.OrdinalIgnoreCase));
                    string invActionName = invEntry.ActionName;
                    if (invActionName.Length > 16)
                        invActionName = invActionName.Substring(0, 13) + "...";
                    string invTag = invInSeq ? "*" : "";
                    string invLine = $"{invTag}{invActionName}";
                    if (interactive)
                    {
                        string invValue = $"{ComboPointerInput.Prefix}invpool:{invFlat}";
                        var invBtn = InventoryButtonFactory.CreateButton(x, y, rowWidth, invValue, invLine);
                        interactionManager!.AddClickableElement(invBtn);
                        var invC = invBtn.IsHovered ? AsciiArtAssets.Colors.Yellow
                            : (invInSeq ? AsciiArtAssets.Colors.DarkGray : ActionNameColors.ResolveByName(invEntry.ActionName));
                        canvas.AddText(x, y, invLine, invC);
                    }
                    else
                    {
                        var invBase = invInSeq ? AsciiArtAssets.Colors.DarkGray : ActionNameColors.ResolveByName(invEntry.ActionName);
                        canvas.AddText(x, y, invLine, invBase);
                    }

                    y++;
                }

                if (invFlat < invEntries.Count)
                    canvas.AddText(x, y, $"... +{invEntries.Count - invFlat} more", AsciiArtAssets.Colors.Gray);
            }

            DrawPoolHoverTooltipIfNeeded(character);
        }

        /// <summary>
        /// Pool tooltips draw after pool rows so they paint on top of lower list entries (see layout coordinator order).
        /// </summary>
        private void DrawPoolHoverTooltipIfNeeded(Character character)
        {
            if (interactionManager == null)
                return;

            int rpPool = RightPanelActionHoverState.HoveredPoolIndex;
            int rpInvPool = RightPanelActionHoverState.HoveredInventoryPoolIndex;
            if (rpPool < 0 && rpInvPool < 0)
                return;

            int innerTop = LayoutConstants.CENTER_PANEL_Y + 1;
            int innerLeft = LayoutConstants.CENTER_PANEL_X + 1;
            int innerRight = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int innerW = Math.Max(8, innerRight - innerLeft + 1);
            int panelTooltipLines = Math.Max(1, LayoutConstants.CombatLogContentHeight - 2);
            int maxTooltipLines = Math.Max(18, Math.Min(32, panelTooltipLines));
            int maxPoolActionTooltipLines = Math.Max(28, Math.Min(40, panelTooltipLines));
            int boxWFinal = Math.Min(52, innerW);
            int innerTextW = Math.Max(4, boxWFinal - 2);
            List<string>? tipLines;
            int rowY;
            if (rpInvPool >= 0)
            {
                if (!InventoryRightPanelLayout.TryGetInventoryActionPoolRowY(character, rpInvPool, out rowY))
                    return;

                var invEntries = InventoryActionPoolEntries.Build(character);
                if (rpInvPool >= invEntries.Count)
                    return;
                var invEntry = invEntries[rpInvPool];
                if (invEntry.InventoryIndex < 0 || invEntry.InventoryIndex >= character.Inventory.Count)
                    return;

                string invHover = LeftPanelHoverState.Prefix + "inv:" + invEntry.InventoryIndex;
                var coloredItemLines = LeftPanelTooltipBuilder.BuildColoredItemLines(character, invHover, maxTooltipLines + 2);
                if (coloredItemLines.Count > 0)
                {
                    DrawPoolItemColoredTooltip(coloredItemLines, rowY, boxWFinal, innerLeft, innerTop, innerRight, maxTooltipLines);
                    return;
                }

                tipLines = LeftPanelTooltipBuilder.BuildLines(character, invHover, innerTextW, maxTooltipLines + 2);
            }
            else
            {
                if (!InventoryRightPanelLayout.TryGetActionPoolRowY(character, rpPool, out rowY))
                    return;

                var pool = character.GetActionPool();
                if (rpPool >= pool.Count)
                    return;

                // Colored like the strip tooltip: class/rarity title color, what it does, and how each status it applies works.
                var coloredActionLines = CombatActionStripBuilder.BuildColoredActionTooltipLines(
                    character, pool[rpPool], maxPoolActionTooltipLines + 2,
                    includeExtendedDetails: HoverTooltipDetailState.IsAltDetailActive);
                if (coloredActionLines.Count > 0)
                {
                    DrawPoolItemColoredTooltip(coloredActionLines, rowY, boxWFinal, innerLeft, innerTop, innerRight, maxPoolActionTooltipLines);
                    return;
                }
                tipLines = CombatActionStripBuilder.BuildActionTooltipLinesForAction(
                    character, pool[rpPool], innerTextW, maxTooltipLines + 2,
                    includeExtendedDetails: HoverTooltipDetailState.IsAltDetailActive);
            }
            if (tipLines == null || tipLines.Count == 0)
                return;
            if (tipLines.Count > maxTooltipLines)
                tipLines = tipLines.GetRange(0, maxTooltipLines);

            int idealX = InventoryRightPanelLayout.GetPoolTooltipIdealBoxLeft(boxWFinal);
            int boxX = InventoryRightPanelLayout.ClampPoolTooltipBoxLeft(idealX, boxWFinal);
            int innerTextWDraw = Math.Max(4, boxWFinal - 2);

            int boxH = tipLines.Count + 2;
            int maxBoxBottom = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            int boxY = rowY + 1;
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

        private void DrawPoolItemColoredTooltip(
            List<List<ColoredText>> coloredLines,
            int rowY,
            int boxWFinal,
            int innerLeft,
            int innerTop,
            int innerRight,
            int maxTooltipLines)
        {
            int innerTextWDraw = Math.Max(4, boxWFinal - 2);
            int contentRows = HoverTooltipColoredDrawing.CountDisplayRows(coloredLines, innerTextWDraw, maxTooltipLines);
            // Keep bag-item tooltips fully inside the center panel so a later strip overlay clear
            // cannot leave orphan fragments in the right column (gear-pool action tips may still straddle).
            int idealX = InventoryRightPanelLayout.GetPoolTooltipIdealBoxLeft(boxWFinal);
            int boxX = Math.Max(innerLeft, Math.Min(idealX, innerRight - boxWFinal + 1));
            int boxH = contentRows + 2;
            int maxBoxBottom = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 2;
            int boxY = rowY + 1;
            if (boxY + boxH - 1 > maxBoxBottom)
                boxY = Math.Max(innerTop, maxBoxBottom - boxH + 1);

            int innerRightInclusive = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            HoverTooltipDrawing.DrawFramedPanel(canvas, boxX, boxY, boxWFinal, boxH, innerLeft, innerTop, innerRightInclusive, maxBoxBottom);
            HoverTooltipColoredDrawing.DrawColoredLines(canvas, boxX + 1, boxY + 1, coloredLines, innerTextWDraw, maxTooltipLines);
        }
        
        /// <summary>
        /// Renders location and enemy information panel
        /// </summary>
        /// <summary>
        /// An affinity row ("Weak: Burn ×2, Acid ×2") with each status's icon in place of its name; a status with no
        /// icon keeps its name. Items that don't fit in <paramref name="maxLen"/> columns end in "...".
        /// </summary>
        private void DrawEnemyAffinityLine(int x, int y, string line, int maxLen, global::Avalonia.Media.Color color)
        {
            var (label, items) = StatusEffectDisplayLines.SplitAffinityLine(line);
            int right = x + maxLen;
            int cx = x + 2;
            canvas.AddText(cx, y, label, color);
            cx += label.Length + 1;
            for (int i = 0; i < items.Count; i++)
            {
                var (status, name, suffix) = items[i];
                string sep = i < items.Count - 1 ? "," : "";
                bool hasIcon = StatusArt.TryGetIcon(status, out var icon, out var iconRect);
                string text = (hasIcon ? suffix : suffix.Length > 0 ? name + " " + suffix : name) + sep;
                int width = (hasIcon ? 2 : 0) + text.Length;
                if (cx + width > right)
                {
                    canvas.AddText(cx, y, "...", color);
                    return;
                }
                if (hasIcon)
                {
                    canvas.AddImage(cx, y, 2, 1, icon, iconRect, paddingPixels: 1);
                    cx += 2;
                }
                canvas.AddText(cx, y, text, color);
                cx += text.Length + 1;
            }
        }

        /// <summary>Hover target for an enemy status / affinity row; the tooltip explains the effect in full.</summary>
        private void RegisterEnemyStatusHoverRow(int x, int y, string idSuffix)
        {
            if (interactionManager == null)
                return;
            interactionManager.AddClickableElement(new ClickableElement
            {
                X = x,
                Y = y,
                Width = Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 4),
                Height = 1,
                Type = ElementType.Text,
                Value = LeftPanelHoverState.Prefix + idSuffix,
                DisplayText = "Enemy status"
            });
        }

        private void RenderLocationEnemyPanel(int x, int y, Enemy? enemy, string? dungeonName, string? roomName, bool registerActionLabEnemyLevelHover, int? heroLevelForLabEnemyCaption)
        {
            // Location section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Location), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Rooms carry their dungeon's theme, so both lines share its palette
            string? locationTheme = DungeonThemeColors.GetThemeForDungeonName(dungeonName);

            // Dungeon — value line omitted when empty
            canvas.AddText(x, y, "Dungeon:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(dungeonName))
            {
                string displayDungeon = RightPanelContentText.EllipsizeToPanelWidth(dungeonName);
                if (locationTheme != null)
                    textWriter.RenderSegments(DungeonThemeColors.BuildThemedName(displayDungeon, locationTheme), x, y);
                else
                    canvas.AddText(x, y, displayDungeon, AsciiArtAssets.Colors.Cyan);
                y++;
            }

            // Room — value line omitted when empty
            canvas.AddText(x, y, "Room:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(roomName))
            {
                string displayRoom = RightPanelContentText.EllipsizeToPanelWidth(roomName);
                if (locationTheme != null)
                    textWriter.RenderSegments(DungeonThemeColors.BuildThemedName(displayRoom, locationTheme), x, y);
                else
                    canvas.AddText(x, y, displayRoom, AsciiArtAssets.Colors.Yellow);
                y++;
            }
            
            y += 1;
            
            // Enemy section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Enemy), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            if (enemy != null)
            {
                string packSuffix = enemy.PackSuffix;
                string enemyNameLine = RightPanelContentText.EllipsizeToPanelWidth(enemy.Name + packSuffix);
                List<ColoredText> nameSegments;
                if (packSuffix.Length > 0 && enemyNameLine.EndsWith(packSuffix, StringComparison.Ordinal))
                {
                    nameSegments = EntityColorHelper.BuildEnemyNamePanelLineSegments(enemy, enemy.Name);
                    nameSegments.Add(new ColoredText(packSuffix, AsciiArtAssets.Colors.White));
                }
                else
                    nameSegments = EntityColorHelper.BuildEnemyNamePanelLineSegments(enemy, enemyNameLine);
                textWriter.RenderSegments(nameSegments, x, y);
                y++;

                int levelRowY = y;
                string levelCaption = heroLevelForLabEnemyCaption is int hl
                    ? ActionLabRightPanelEnemyAdjustment.FormatEnemyLevelCaptionWithHeroDelta(enemy.Level, hl)
                    : $"Lvl {enemy.Level}";
                canvas.AddText(x, y, levelCaption, AsciiArtAssets.Colors.Gold);
                if (interactionManager != null && registerActionLabEnemyLevelHover)
                {
                    int hoverW = Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 4);
                    interactionManager.AddClickableElement(new ClickableElement
                    {
                        X = x,
                        Y = levelRowY,
                        Width = hoverW,
                        Height = 1,
                        Type = ElementType.Text,
                        Value = ActionLabRightPanelEnemyAdjustment.EnemyLevelHoverId,
                        DisplayText = "Enemy level (Action Lab)"
                    });
                }
                y++;

                // Health bar + d20 threshold strip
                int healthBarWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 8;
                int healthBarY = y;
                int enemyBarAreaHeight = D20ThresholdBarRenderer.CombatBarAreaRowCount;
                int thresholdBarY = healthBarY;
                int hpValueY = healthBarY + enemyBarAreaHeight;
                canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, enemyBarAreaHeight);
                canvas.ClearSegmentedBarsInArea(x, healthBarY, healthBarWidth, enemyBarAreaHeight);
                canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);
                
                int displayEnemyHp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve($"enemy_{enemy.Name}", enemy.CurrentHealth);
                canvas.AddHealthBar(
                    x,
                    healthBarY,
                    healthBarWidth,
                    displayEnemyHp,
                    enemy.MaxHealth,
                    entityId: $"enemy_{enemy.Name}",
                    heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale,
                    dividerFractions: enemy.IsPack ? enemy.PackDividerFractions : null);
                D20ThresholdBarRenderer.RenderBar(
                    canvas,
                    x,
                    thresholdBarY,
                    healthBarWidth,
                    enemy,
                    ThresholdBarPanel.Enemy,
                    verticalOffsetScale: D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor);
                canvas.AddText(x, hpValueY, $"{displayEnemyHp}/{enemy.MaxHealth}", AsciiArtAssets.Colors.White);
                y += 3;

                int weaponDamage = (enemy.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
                int totalDamage = enemy.GetAttributeDamageBonus() + weaponDamage;
                double queuedEnemyDmgPct = enemy.PeekQueuedSheetEnemyDamageModPercentForDisplay();
                string dmgLine = $"DMG:  {totalDamage}";
                if (queuedEnemyDmgPct > 0.05)
                    dmgLine += $"  +{queuedEnemyDmgPct:0.#}% nxt";

                canvas.AddText(x, y, dmgLine, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"Defense {ClassDefenseCalculator.GetDisplayedDefense(enemy)}", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"Spd:  {enemy.GetTotalAttackSpeed():F2}s", AsciiArtAssets.Colors.White);
                y += 2;

                // Match hero STATS: effective getters (Enemy.GetEffectiveStrength maps base Damage for direct-stat lab dummies).
                if (enemy.UsesDirectCombatStats())
                {
                    var strColor = enemy.PrimaryAttribute == PrimaryAttribute.Strength ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White;
                    canvas.AddCharacterStat(x, y, "STR", enemy.GetEffectiveStrength(), 0, strColor, AsciiArtAssets.Colors.White);
                    y++;
                    canvas.AddText(x, y, "AGI:   —", AsciiArtAssets.Colors.DarkGray);
                    y++;
                    canvas.AddText(x, y, "TECH:  —", AsciiArtAssets.Colors.DarkGray);
                    y++;
                    canvas.AddText(x, y, "INT:   —", AsciiArtAssets.Colors.DarkGray);
                    y++;
                }
                else
                {
                    var strColor = enemy.PrimaryAttribute == PrimaryAttribute.Strength ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White;
                    var agiColor = enemy.PrimaryAttribute == PrimaryAttribute.Agility ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White;
                    var tecColor = enemy.PrimaryAttribute == PrimaryAttribute.Technique ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White;
                    var intColor = enemy.PrimaryAttribute == PrimaryAttribute.Intelligence ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White;
                    canvas.AddCharacterStat(x, y, "STR", enemy.GetEffectiveStrength(), 0, strColor, AsciiArtAssets.Colors.White);
                    y++;
                    canvas.AddCharacterStat(x, y, "AGI", enemy.GetEffectiveAgility(), 0, agiColor, AsciiArtAssets.Colors.White);
                    y++;
                    canvas.AddCharacterStat(x, y, "TECH", enemy.GetEffectiveTechnique(), 0, tecColor, AsciiArtAssets.Colors.White);
                    y++;
                    canvas.AddCharacterStat(x, y, "INT", enemy.GetEffectiveIntelligence(), 0, intColor, AsciiArtAssets.Colors.White);
                    y++;
                }

                y += 1;
                var thresholdsHudMode = stateManager?.ThresholdsHudMode ?? ThresholdsHudMode.Ladder;
                bool showThresholdChances = thresholdsHudMode == ThresholdsHudMode.Chances;
                string thresholdsHeaderLabel = showThresholdChances ? UIConstants.Headers.Chances : UIConstants.Headers.Thresholds;
                canvas.AddText(x, y, $"====  {thresholdsHeaderLabel}  ====", AsciiArtAssets.Colors.Gold);
                y += 2;
                int barWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 8;
                y = ThresholdSectionRenderer.Render(canvas, x, y, barWidth, enemy, stateManager, ThresholdBarPanel.Enemy).NextY;
            }
            else
            {
                canvas.AddText(x, y, "No active combat", AsciiArtAssets.Colors.DarkGray);
                y++;
            }
            y += 1;

            // Status effects section (enemy; hero effects on the left panel — hero dice thresholds on the left, enemy dice thresholds above)
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.StatusEffects), AsciiArtAssets.Colors.Gold);
            y += 2;
            const int maxLineLen = 26;
            if (enemy != null)
            {
                canvas.AddText(x, y, "Enemy:", AsciiArtAssets.Colors.Gray);
                y++;
                StatusEffectHoverDetails.CurrentEnemy = enemy;
                int affinityIndex = 0;
                foreach (var affinityLine in StatusEffectDisplayLines.GetEnemyAffinityLines(enemy))
                {
                    RegisterEnemyStatusHoverRow(x, y, "eaff:" + affinityIndex);
                    affinityIndex++;
                    var affinityColor = affinityLine.StartsWith("Weak", StringComparison.Ordinal)
                        ? AsciiArtAssets.Colors.Orange
                        : affinityLine.StartsWith("Resists", StringComparison.Ordinal)
                            ? AsciiArtAssets.Colors.Gray
                            : AsciiArtAssets.Colors.Cyan;
                    DrawEnemyAffinityLine(x, y, affinityLine, maxLineLen, affinityColor);
                    y++;
                }
                const int maxEnemyEffectLines = 3;
                var enemyEffects = StatusEffectDisplayLines.Build(enemy, enemy);
                if (enemyEffects.Count > 0)
                {
                    int toShow = Math.Min(enemyEffects.Count, maxEnemyEffectLines);
                    for (int i = 0; i < toShow; i++)
                    {
                        // The icon sits in the two-space indent and stands in for the effect's name.
                        var (iconKey, afterIcon) = StatusEffectDisplayLines.SplitIcon(enemyEffects[i]);
                        bool hasIcon = StatusArt.TryGetIcon(iconKey, out var icon, out var iconRect);
                        string line = "  " + (hasIcon ? afterIcon : enemyEffects[i]);
                        if (line.Length > maxLineLen)
                            line = line.Substring(0, maxLineLen - 3) + "...";
                        if (hasIcon)
                            canvas.AddImage(x, y, 2, 1, icon, iconRect, paddingPixels: 1);
                        canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
                        RegisterEnemyStatusHoverRow(x, y, "estatus:" + i);
                        y++;
                    }
                    if (enemyEffects.Count > toShow)
                    {
                        canvas.AddText(x, y, $"  +{enemyEffects.Count - toShow} more", AsciiArtAssets.Colors.Gray);
                        y++;
                    }
                }
            }
        }
    }
}

