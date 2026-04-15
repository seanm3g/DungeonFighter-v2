namespace RPGGame.UI.Avalonia.Layout
{
    using System;
    using System.Collections.Generic;
    using RPGGame;
    using RPGGame.Handlers.Inventory;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Managers;
    using RPGGame.UI.Avalonia.Renderers.Inventory;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Renders the right panel (location/enemy info or inventory actions)
    /// </summary>
    public class RightPanelRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasInteractionManager? interactionManager;

        public RightPanelRenderer(
            GameCanvasControl canvas,
            ICanvasInteractionManager? interactionManager = null)
        {
            this.canvas = canvas;
            this.interactionManager = interactionManager;
        }
        
        /// <summary>
        /// Renders the dungeon and enemy information panel (right side)
        /// Shows combo sequence and action pool when on inventory page or combo management, otherwise shows location/enemy info
        /// </summary>
        /// <param name="inventoryComboRightPanel">When true, always show sequence/pool (caller is inventory/combo UI). Avoids relying on title strings alone.</param>
        public void RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName, string title, Character? character, bool inventoryComboRightPanel = false)
        {
            // Clear the right panel content area before rendering to prevent text overlap
            int contentX = LayoutConstants.RIGHT_PANEL_X + 2;
            int contentY = LayoutConstants.RIGHT_PANEL_Y + 1;
            int contentWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 4;  // Account for left and right borders
            int contentHeight = LayoutConstants.RIGHT_PANEL_HEIGHT - 2; // Account for top and bottom borders
            
            canvas.ClearTextInArea(contentX, contentY, contentWidth, contentHeight);
            canvas.ClearProgressBarsInArea(contentX, contentY, contentWidth, contentHeight);
            int rpX = LayoutConstants.RIGHT_PANEL_X;
            int rpY = LayoutConstants.RIGHT_PANEL_Y;
            int rpW = LayoutConstants.RIGHT_PANEL_WIDTH;
            int rpH = LayoutConstants.RIGHT_PANEL_HEIGHT;
            canvas.ClearBoxesInArea(rpX, rpY, rpW, rpH);
            
            // Main border for right panel - extends to right edge with no padding
            canvas.AddBorder(rpX, rpY, rpW, rpH, AsciiArtAssets.Colors.Purple);
            
            int y = LayoutConstants.RIGHT_PANEL_Y + 1;
            int x = LayoutConstants.RIGHT_PANEL_X + 2;
            
            // Inventory / combo management: sequence + pool. Prefer explicit flag from CanvasRenderer; keep title fallback for compatibility.
            bool useInventoryPanel = character != null && (inventoryComboRightPanel
                || title == "INVENTORY"
                || title == "COMBO MANAGEMENT"
                || title == "ACTIONS");
            if (useInventoryPanel)
            {
                // Render combo sequence and action pool for inventory page and combo management
                RenderInventoryRightPanel(x, y, character!);
            }
            else
            {
                // Render location and enemy info for other pages
                RenderLocationEnemyPanel(x, y, enemy, dungeonName, roomName);
            }
        }

        /// <summary>
        /// Renders combo sequence and action pool for inventory page.
        /// Uses all available vertical space in the panel for both sections.
        /// </summary>
        private void RenderInventoryRightPanel(int x, int y, Character character)
        {
            int panelBottom = LayoutConstants.RIGHT_PANEL_Y + LayoutConstants.RIGHT_PANEL_HEIGHT - 2;
            int rowWidth = Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 4);
            bool interactive = interactionManager != null;

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
                
                const int actionPoolReserve = 9; // spacing + POOL header + subtitle + Total + list line(s)
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
                    if (interactive)
                    {
                        string value = $"{ComboPointerInput.Prefix}rm:{i}";
                        var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, value, line);
                        interactionManager!.AddClickableElement(btn);
                        var c = btn.IsHovered ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White;
                        canvas.AddText(x, y, line, c);
                    }
                    else
                        canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
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
                    var baseColor = inSeq ? AsciiArtAssets.Colors.DarkGray : AsciiArtAssets.Colors.Cyan;
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
                }
            }
            else
            {
                canvas.AddText(x, y, "(No actions)", AsciiArtAssets.Colors.DarkGray);
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
            if (rpPool < 0)
                return;

            if (!InventoryRightPanelLayout.TryGetActionPoolRowY(character, rpPool, out int rowY))
                return;

            var pool = character.GetActionPool();
            if (rpPool >= pool.Count)
                return;

            int innerTop = LayoutConstants.CENTER_PANEL_Y + 1;
            int innerLeft = LayoutConstants.CENTER_PANEL_X + 1;
            int innerRight = LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int innerW = Math.Max(8, innerRight - innerLeft + 1);
            const int maxTooltipLines = 14;
            int boxWFinal = Math.Min(52, innerW);
            int innerTextW = Math.Max(4, boxWFinal - 2);
            var tipLines = CombatActionStripBuilder.BuildActionTooltipLinesForAction(character, pool[rpPool], innerTextW, maxTooltipLines + 2);
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
        
        /// <summary>
        /// Renders location and enemy information panel
        /// </summary>
        private void RenderLocationEnemyPanel(int x, int y, Enemy? enemy, string? dungeonName, string? roomName)
        {
            // Location section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Location), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Dungeon — value line omitted when empty
            canvas.AddText(x, y, "Dungeon:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(dungeonName))
            {
                string displayDungeon = dungeonName;
                if (displayDungeon.Length > 20)
                    displayDungeon = displayDungeon.Substring(0, 17) + "...";
                canvas.AddText(x, y, displayDungeon, AsciiArtAssets.Colors.Cyan);
                y++;
            }
            
            // Room — value line omitted when empty
            canvas.AddText(x, y, "Room:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(roomName))
            {
                string displayRoom = roomName;
                if (displayRoom.Length > 20)
                    displayRoom = displayRoom.Substring(0, 17) + "...";
                canvas.AddText(x, y, displayRoom, AsciiArtAssets.Colors.Yellow);
                y++;
            }
            
            y += 1;
            
            // Enemy section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Enemy), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            if (enemy != null)
            {
                string enemyName = enemy.Name;
                if (enemyName.Length > 20)
                    enemyName = enemyName.Substring(0, 17) + "...";
                
                canvas.AddText(x, y, enemyName, EntityColorHelper.GetEnemyColor(enemy));
                y += 2;
                
                // Health bar
                // Clear the health bar and HP value area before redrawing to prevent text overlap
                int healthBarWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 8;
                int healthBarY = y;
                int hpValueY = y + 1;
                canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, 1);
                canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);
                
                canvas.AddHealthBar(x, y, healthBarWidth, enemy.CurrentHealth, enemy.MaxHealth, 
                    entityId: $"enemy_{enemy.Name}");
                canvas.AddText(x, y + 1, $"{enemy.CurrentHealth}/{enemy.MaxHealth}", AsciiArtAssets.Colors.White);
                y += 3;
                
                // Calculate enemy damage (Strength + weapon damage)
                int weaponDamage = (enemy.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
                int totalDamage = enemy.GetEffectiveStrength() + weaponDamage;
                
                // Damage and Armor on same line
                canvas.AddText(x, y, $"DMG:  {totalDamage}", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"ARM:  {enemy.Armor}", AsciiArtAssets.Colors.White);
                y += 2;
                
                // Attributes
                canvas.AddCharacterStat(x, y, "STR", enemy.Strength, 0, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "AGI", enemy.Agility, 0, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "TECH", enemy.Technique, 0, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddCharacterStat(x, y, "INT", enemy.Intelligence, 0, AsciiArtAssets.Colors.White);
                y++;

                y += 1;
                canvas.AddText(x, y, $"====  {UIConstants.Headers.Thresholds}  ====", AsciiArtAssets.Colors.Gold);
                y += 2;
                y = DiceRollThresholdRowsRenderer.RenderRows(canvas, x, y, enemy);
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
                const int maxEnemyEffectLines = 3;
                var enemyEffects = StatusEffectDisplayLines.Build(enemy, enemy);
                if (enemyEffects.Count > 0)
                {
                    int toShow = Math.Min(enemyEffects.Count, maxEnemyEffectLines);
                    for (int i = 0; i < toShow; i++)
                    {
                        string line = "  " + enemyEffects[i];
                        if (line.Length > maxLineLen)
                            line = line.Substring(0, maxLineLen - 3) + "...";
                        canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
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

