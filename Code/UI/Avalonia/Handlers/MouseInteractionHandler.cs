using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Handlers.Inventory;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Handles mouse interactions for the game canvas.
    /// Extracted from MainWindow to separate concerns.
    /// </summary>
    public class MouseInteractionHandler
    {
        private readonly GameCanvasControl gameCanvas;
        private readonly CanvasUICoordinator? canvasUI;
        private readonly GameCoordinator? game;

        private bool _comboStripDragging;
        private int _comboStripDragFrom = -1;
        private List<RPGGame.Action>? _comboStripSnapshot;

        public MouseInteractionHandler(
            GameCanvasControl gameCanvas,
            CanvasUICoordinator? canvasUI,
            GameCoordinator? game)
        {
            this.gameCanvas = gameCanvas;
            this.canvasUI = canvasUI;
            this.game = game;
        }

        /// <summary>
        /// Must match the active <see cref="CanvasTextManager.DisplayManager"/> stats instance (per-character display managers each have their own).
        /// </summary>
        private StatsPanelStateManager? GetActiveStatsPanelState() => canvasUI?.GetStatsPanelStateManager();

        /// <summary>
        /// Handles pointer pressed events (mouse clicks).
        /// </summary>
        public void HandlePointerPressed(PointerPressedEventArgs e)
        {
            if (game == null) return;

            var point = e.GetCurrentPoint(gameCanvas);
            if (point.Properties.IsRightButtonPressed)
            {
                if (TryHandleActionStripRightClickRemove(point.Position))
                {
                    e.Handled = true;
                    return;
                }
                if (TryApplyActionLabHeroHpRightClick(point.Position))
                {
                    e.Handled = true;
                    return;
                }
                if (TryScheduleActionLabGearEdit(point.Position))
                {
                    e.Handled = true;
                    return;
                }
                if (TryApplyActionLabLeftPanelStatAt(point.Position, -1))
                {
                    e.Handled = true;
                    return;
                }
                if (TryApplyActionLabRightPanelEnemyLevelAt(point.Position, -1))
                {
                    e.Handled = true;
                    return;
                }
            }

            if (point.Properties.IsLeftButtonPressed)
            {
                if (TryBeginComboStripDrag(e, point.Position))
                    return;
                HandleMouseClick(point.Position);
            }
        }

        /// <summary>
        /// Handles pointer moved events (mouse hover).
        /// </summary>
        public void HandlePointerMoved(PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(gameCanvas);
            HandleMouseHover(point.Position);
        }

        /// <summary>
        /// Handles pointer released events.
        /// </summary>
        public void HandlePointerReleased(PointerReleasedEventArgs e)
        {
            try
            {
                e.Pointer.Capture(null);
            }
            catch
            {
                // ignore capture release failures
            }

            if (!_comboStripDragging)
                return;

            _comboStripDragging = false;
            var snapshot = _comboStripSnapshot;
            var fromIdx = _comboStripDragFrom;
            _comboStripSnapshot = null;
            _comboStripDragFrom = -1;

            if (snapshot == null || snapshot.Count == 0 || fromIdx < 0 || game == null || canvasUI == null)
                return;

            if (!CanReorderComboOnStrip())
                return;

            var releasePoint = e.GetCurrentPoint(gameCanvas);
            var grid = ScreenToGrid(releasePoint.Position);
            int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(snapshot.Count);
            if (!ActionInfoStripLayout.TryGetPanelIndex(grid.X, grid.Y, displayCount, out int toIdx) || toIdx == fromIdx)
                return;
            if (toIdx >= snapshot.Count)
                return;

            var player = GetCharacterForActionStrip();
            if (player != null && ComboReorderer.ApplyReorderMove(player, snapshot, fromIdx, toIdx))
            {
                if (game.StateManager?.CurrentState == GameState.ActionInteractionLab
                    && ActionInteractionLabSession.Current is { } labSession)
                {
                    canvasUI.RenderCombat(labSession.LabPlayer, labSession.LabEnemy, new List<string>());
                }
                else
                {
                    // ForceRender() only refreshes the center display buffer; it does not redraw RenderWithLayout chrome
                    // (including the action-info strip). Use the same path as stats-panel toggles.
                    game.RefreshPersistentChromeAfterStatsToggle();
                    canvasUI.Refresh();
                }
            }
        }

        /// <summary>
        /// Handles pointer wheel events (mouse wheel scrolling).
        /// </summary>
        public void HandlePointerWheelChanged(PointerWheelEventArgs e)
        {
            if (canvasUI == null) return;

            // Get wheel delta (positive = scroll up, negative = scroll down)
            var delta = e.Delta.Y;
            
            // Only handle scrolling if there's a significant delta
            if (Math.Abs(delta) < 0.1) return;

            // Scroll the center panel display
            if (delta > 0)
            {
                // Scroll up (show earlier content)
                canvasUI.ScrollUp(3);
            }
            else
            {
                // Scroll down (show later content)
                canvasUI.ScrollDown(3);
            }
            
            // Mark event as handled to prevent default scrolling behavior
            e.Handled = true;
        }

        /// <summary>
        /// Handles a mouse click at the given position.
        /// </summary>
        private void HandleMouseClick(Point position)
        {
            if (canvasUI == null || game == null) return;

            // Convert screen coordinates to character grid coordinates
            var gridPos = ScreenToGrid(position);

            // Check if click is on a clickable element
            var clickedElement = canvasUI.GetElementAt(gridPos.X, gridPos.Y);
            if (clickedElement != null)
            {
                // Process the click
                ProcessElementClick(clickedElement);
            }
            else
            {
                // If no clickable element was clicked, check if we're in Settings state
                // and prevent accidental settings menu opening after tests
                if (game.StateManager != null && game.StateManager.CurrentState == GameState.Settings)
                {
                    // If clicking on empty space while in Settings state, don't do anything
                    // This prevents the settings menu from opening again
                    return;
                }
            }
        }

        /// <summary>
        /// Handles mouse hover at the given position.
        /// </summary>
        private void HandleMouseHover(Point position)
        {
            if (canvasUI == null) return;
            if (_comboStripDragging) return;

            // Convert screen coordinates to character grid coordinates
            var gridPos = ScreenToGrid(position);

            // Combat log / framed center: clear menu and strip-adjacent hover highlights so nothing sticks
            // when reading the log (and ensure tooltip overlay can fully clear on the next draw).
            if (LayoutConstants.ContainsCenterPanelContent(gridPos.X, gridPos.Y))
                canvasUI.ClearHoverStates();

            // Update hover state for menu / clickable elements
            canvasUI.SetHoverPosition(gridPos.X, gridPos.Y);

            int newStripHover = -1;
            var player = GetCharacterForActionStrip();
            var combo = player?.GetComboActions();
            int filled = combo?.Count ?? 0;
            int displayCount = player != null ? ActionInfoStripLayout.GetDisplayPanelCount(filled) : 0;
            if (player != null && displayCount > 0 && ActionInfoStripLayout.TryGetPanelIndex(gridPos.X, gridPos.Y, displayCount, out int stripIdx))
                newStripHover = stripIdx;

            bool stripHoverChanged = ActionStripHoverState.SetHoveredPanelIndex(newStripHover);

            bool inventoryActive = game?.StateManager?.CurrentState == GameState.Inventory;
            bool rpHoverChanged = RightPanelActionHoverState.UpdateFromClickables(canvasUI.GetClickableElements(), inventoryActive);
            bool lpHoverChanged = LeftPanelHoverState.UpdateFromClickables(canvasUI.GetClickableElements());

            if (!stripHoverChanged && !rpHoverChanged && !lpHoverChanged)
                return;

            // PerformRender is suppressed in many menu states; re-invoke the active screen renderer like stats toggles.
            // GameLoop / inventory: strip-only redraw when a tooltip is visible. Hover-out needs full refresh to restore the center panel.
            bool tooltipStripOrPanel = newStripHover >= 0
                || RightPanelActionHoverState.HoveredSequenceIndex >= 0
                || RightPanelActionHoverState.HoveredPoolIndex >= 0
                || LeftPanelHoverState.IsActive;

            bool inv = game?.StateManager?.CurrentState == GameState.Inventory;
            bool rpHovering = RightPanelActionHoverState.HoveredSequenceIndex >= 0
                || RightPanelActionHoverState.HoveredPoolIndex >= 0;

            if (game != null)
            {
                if (game.StateManager?.CurrentState == GameState.GameLoop && player != null && tooltipStripOrPanel)
                    canvasUI.RefreshActionInfoStripOnly(player);
                else if (game.StateManager?.CurrentState == GameState.ActionInteractionLab && player != null && tooltipStripOrPanel)
                    canvasUI.RefreshActionInfoStripOnly(player);
                else if (inv && player != null && tooltipStripOrPanel)
                {
                    // Right-panel rows use menu-option hover; full chrome redraw updates them.
                    // Center lphover (inventory rows, headers, etc.): strip-only redraw runs the strip tooltip path,
                    // which uses HoverTooltipDrawing.ClearTextInArea on the main layer and would erase inventory text
                    // without re-rendering the center content — full chrome refresh keeps list + tooltip consistent.
                    if (rpHovering || LeftPanelHoverState.IsActive)
                        game.RefreshPersistentChromeAfterStatsToggle();
                    else
                        canvasUI.RefreshActionInfoStripOnly(player);
                }
                else
                    game.RefreshPersistentChromeAfterStatsToggle();
            }
            else
                canvasUI.ForceRender();
        }

        /// <summary>
        /// Converts pointer coordinates (relative to the game canvas) to character grid indices.
        /// Must match rendering: text is drawn at (gridX * charWidth, gridY * charHeight).
        /// </summary>
        private (int X, int Y) ScreenToGrid(Point screenPosition)
        {
            double charWidth = gameCanvas.GetCharWidth();
            double charHeight = gameCanvas.GetCharHeight();

            int gridX = (int)(screenPosition.X / charWidth);
            int gridY = (int)(screenPosition.Y / charHeight);

            return (gridX, gridY);
        }

        /// <summary>
        /// Left-panel chrome toggles: menu states suppress display-buffer <c>PerformRender</c>, so we re-invoke the active CanvasRenderer screen.
        /// </summary>
        private void RefreshChromeAfterStatsToggle()
        {
            game?.RefreshPersistentChromeAfterStatsToggle();
        }

        /// <summary>
        /// While <see cref="GameStateManager.HasCurrentDungeon"/> is true, strip reorder is off for the whole run (including inventory).
        /// Without an active dungeon, <see cref="GameState.Inventory"/> allows reorder; otherwise see <see cref="ActionStripReorderPolicy"/>.
        /// </summary>
        private bool CanReorderComboOnStrip()
        {
            if (game?.StateManager == null || canvasUI == null)
                return false;
            var state = game.StateManager.CurrentState;
            var sm = game.StateManager;

            if (sm.HasCurrentDungeon)
                return false;
            if (state == GameState.Inventory)
                return true;
            // Action Lab: reorder the cloned character's combo (same as hub), not blocked like real combat.
            if (state == GameState.ActionInteractionLab)
                return true;
            if (state == GameState.Combat || sm.IsComboStripEncounterLocked)
                return false;
            return ActionStripReorderPolicy.AllowsReorder(state, null);
        }

        /// <summary>
        /// Strip rendering uses the lab clone in Action Lab; drag/hover must use that instance, not <see cref="GameCoordinator.CurrentPlayer"/>.
        /// </summary>
        private Character? GetCharacterForActionStrip()
        {
            if (game?.StateManager?.CurrentState == GameState.ActionInteractionLab)
            {
                var lab = ActionInteractionLabSession.Current;
                if (lab != null)
                    return lab.LabPlayer;
            }
            return game?.CurrentPlayer;
        }

        /// <summary>
        /// Action Lab: hero HP bar — right-click heals +5 (capped at max). Left-click damage is handled in <see cref="ProcessElementClick"/>.
        /// </summary>
        private bool TryApplyActionLabHeroHpRightClick(Point position)
        {
            if (canvasUI == null || game?.StateManager?.CurrentState != GameState.ActionInteractionLab)
                return false;
            var session = ActionInteractionLabSession.Current;
            if (session == null)
                return false;
            var grid = ScreenToGrid(position);
            var el = canvasUI.GetElementAt(grid.X, grid.Y);
            if (el == null || !string.Equals(el.Value, ActionLabLeftPanelStatAdjustment.HeroHpHoverId, StringComparison.Ordinal))
                return false;
            ActionLabLeftPanelStatAdjustment.ApplyHeroHpRightClickHeal(session.LabPlayer);
            canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
            return true;
        }

        /// <summary>
        /// Action Lab: left-click increases STATS row values and level (HERO line); right-click decreases (see <see cref="ActionLabLeftPanelStatAdjustment"/>).
        /// Hero HP bar: left-click applies 5% max HP damage; right-click heals +5 (see <see cref="TryApplyActionLabHeroHpRightClick"/>).
        /// </summary>
        private bool TryApplyActionLabLeftPanelStatFromElement(ClickableElement element, int delta)
        {
            if (canvasUI == null || game?.StateManager?.CurrentState != GameState.ActionInteractionLab)
                return false;
            var session = ActionInteractionLabSession.Current;
            if (session == null)
                return false;
            if (!ActionLabLeftPanelStatAdjustment.TryApply(session.LabPlayer, element.Value, delta))
                return false;
            canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
            return true;
        }

        /// <summary>
        /// Action Lab: left-click increases enemy level; right-click decreases (right panel <c>rphover:enemy:level</c>).
        /// </summary>
        private bool TryApplyActionLabRightPanelEnemyLevelFromElement(ClickableElement element, int delta)
        {
            if (canvasUI == null || game?.StateManager?.CurrentState != GameState.ActionInteractionLab)
                return false;
            var session = ActionInteractionLabSession.Current;
            if (session == null)
                return false;
            if (!ActionLabRightPanelEnemyAdjustment.TryApply(session, element.Value, delta))
                return false;
            canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
            return true;
        }

        /// <summary>
        /// Hit-test grid cell for enemy level row (used for right-click decrease).
        /// </summary>
        private bool TryApplyActionLabRightPanelEnemyLevelAt(Point position, int delta)
        {
            if (canvasUI == null)
                return false;
            var grid = ScreenToGrid(position);
            var el = canvasUI.GetElementAt(grid.X, grid.Y);
            if (el == null)
                return false;
            return TryApplyActionLabRightPanelEnemyLevelFromElement(el, delta);
        }

        /// <summary>
        /// Hit-test grid cell for a STATS <c>lphover:stat:*</c> row and apply delta (used for right-click).
        /// </summary>
        private bool TryApplyActionLabLeftPanelStatAt(Point position, int delta)
        {
            if (canvasUI == null)
                return false;
            var grid = ScreenToGrid(position);
            var el = canvasUI.GetElementAt(grid.X, grid.Y);
            if (el == null)
                return false;
            return TryApplyActionLabLeftPanelStatFromElement(el, delta);
        }

        /// <summary>
        /// Action Lab: right-click a GEAR row for a menu (change gear in the editor, or None to unequip).
        /// </summary>
        private bool TryScheduleActionLabGearEdit(Point position)
        {
            if (game?.StateManager?.CurrentState != GameState.ActionInteractionLab)
                return false;
            if (canvasUI == null)
                return false;
            var lab = ActionInteractionLabSession.Current;
            if (lab == null)
                return false;

            var grid = ScreenToGrid(position);
            var el = canvasUI.GetElementAt(grid.X, grid.Y);
            if (el == null || string.IsNullOrEmpty(el.Value))
                return false;

            string prefix = LeftPanelHoverState.Prefix + "gear:";
            if (!el.Value.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            string slotKey = el.Value.Substring(prefix.Length);
            if (!TryMapGearHoverToEditSlot(slotKey, out ActionLabGearEditSlot editSlot, out string equipSlot))
                return false;

            var owner = canvasUI.GetMainWindow();
            var player = lab.LabPlayer;

            var flyout = new MenuFlyout();
            var changeItem = new MenuItem { Header = "Change gear…" };
            changeItem.Click += async (_, _) =>
            {
                flyout.Hide();
                var item = await ActionLabWeaponEditDialog.ShowGearEditAsync(owner, player, editSlot).ConfigureAwait(true);
                if (item != null)
                    lab.ApplyLabGear(item, equipSlot);
            };
            var noneItem = new MenuItem { Header = "None" };
            noneItem.Click += (_, _) =>
            {
                flyout.Hide();
                lab.ClearLabGear(equipSlot);
            };
            flyout.Items.Add(changeItem);
            flyout.Items.Add(noneItem);
            flyout.ShowAt(gameCanvas, showAtPointer: true);
            return true;
        }

        private static bool TryMapGearHoverToEditSlot(string slotKey, out ActionLabGearEditSlot editSlot, out string equipSlot)
        {
            switch (slotKey.ToLowerInvariant())
            {
                case "weapon": editSlot = ActionLabGearEditSlot.Weapon; break;
                case "head": editSlot = ActionLabGearEditSlot.Head; break;
                case "body": editSlot = ActionLabGearEditSlot.Body; break;
                case "feet": editSlot = ActionLabGearEditSlot.Feet; break;
                default:
                    editSlot = default;
                    equipSlot = "";
                    return false;
            }

            equipSlot = ActionLabWeaponEditDialog.GetEquipSlotName(editSlot);
            return true;
        }

        /// <summary>
        /// Right-click a filled action strip panel: in the Action Lab removes that action (sandbox rules);
        /// in <see cref="GameState.Inventory"/> removes that sequence slot (same behavior as right-panel <c>cpi:rm</c>).
        /// </summary>
        private bool TryHandleActionStripRightClickRemove(Point position)
        {
            if (canvasUI == null || game?.StateManager == null)
                return false;

            var player = GetCharacterForActionStrip();
            if (player == null)
                return false;
            var combo = player.GetComboActions();
            if (combo == null || combo.Count == 0)
                return false;

            var grid = ScreenToGrid(position);
            int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(combo.Count);
            if (!ActionInfoStripLayout.TryGetPanelIndex(grid.X, grid.Y, displayCount, out int idx))
                return false;
            if (idx < 0 || idx >= combo.Count)
                return false;

            var state = game.StateManager.CurrentState;
            if (state == GameState.ActionInteractionLab)
            {
                var lab = ActionInteractionLabSession.Current;
                if (lab == null)
                    return false;
                player.RemoveFromCombo(combo[idx]);
                canvasUI.RenderCombat(lab.LabPlayer, lab.LabEnemy, new List<string>());
                return true;
            }

            if (state == GameState.Inventory)
                return game.TryHandleInventoryStripRightClickRemove(idx);

            return false;
        }

        /// <summary>
        /// Starts drag when the user presses on an action-info strip panel (see <see cref="CanReorderComboOnStrip"/>).
        /// </summary>
        private bool TryBeginComboStripDrag(PointerPressedEventArgs e, Point position)
        {
            if (!CanReorderComboOnStrip()) return false;
            var player = GetCharacterForActionStrip();
            if (player == null) return false;
            var combo = player.GetComboActions();
            if (combo == null || combo.Count == 0) return false;

            var grid = ScreenToGrid(position);
            int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(combo.Count);
            if (!ActionInfoStripLayout.TryGetPanelIndex(grid.X, grid.Y, displayCount, out int idx))
                return false;
            // Must match strip rendering / release hit-test; ignore empty padded slots.
            if (idx < 0 || idx >= combo.Count)
                return false;

            _comboStripDragging = true;
            _comboStripDragFrom = idx;
            _comboStripSnapshot = new List<RPGGame.Action>(combo);
            e.Pointer.Capture(gameCanvas);
            e.Handled = true;
            return true;
        }

        /// <summary>
        /// Processes a click on a clickable element.
        /// </summary>
        private async void ProcessElementClick(ClickableElement element)
        {
            if (game == null) return;

            try
            {
                if (element.Value.StartsWith("lab_", StringComparison.Ordinal))
                {
                    await ActionLabInputCoordinator.HandleLabControlAsync(element.Value, canvasUI, game).ConfigureAwait(true);
                    return;
                }

                if (game.StateManager?.CurrentState == GameState.ActionInteractionLab
                    && ActionInteractionLabSession.Current is { } labHpSession
                    && string.Equals(element.Value, ActionLabLeftPanelStatAdjustment.HeroHpHoverId, StringComparison.Ordinal))
                {
                    ActionLabLeftPanelStatAdjustment.ApplyHeroHpClickDamagePercent(labHpSession.LabPlayer);
                    if (canvasUI != null)
                        canvasUI.RenderCombat(labHpSession.LabPlayer, labHpSession.LabEnemy, new List<string>());
                    return;
                }

                if (TryApplyActionLabRightPanelEnemyLevelFromElement(element, +1))
                    return;

                if (TryApplyActionLabLeftPanelStatFromElement(element, +1))
                    return;

                var statsPanelStateManager = GetActiveStatsPanelState();
                if (statsPanelStateManager != null)
                {
                    switch (element.Value)
                    {
                        case "toggle_section_hero":
                            statsPanelStateManager.ToggleHeroCollapsed();
                            RefreshChromeAfterStatsToggle();
                            return;
                        case "toggle_section_stats":
                            statsPanelStateManager.ToggleStatsCollapsed();
                            RefreshChromeAfterStatsToggle();
                            return;
                        case "toggle_section_gear":
                            statsPanelStateManager.ToggleGearCollapsed();
                            RefreshChromeAfterStatsToggle();
                            return;
                        case "toggle_section_thresholds":
                            statsPanelStateManager.ToggleThresholdsCollapsed();
                            RefreshChromeAfterStatsToggle();
                            return;
                    }
                }
                
                switch (element.Type)
                {
                    case ElementType.MenuOption:
                        await game.HandleInput(element.Value);
                        break;
                    case ElementType.Item:
                        // Handle item selection
                        if (canvasUI != null)
                        {
                            canvasUI.UpdateStatus($"Selected item: {element.Value}");
                        }
                        break;
                    case ElementType.Button:
                        // Handle button click
                        await game.HandleInput(element.Value);
                        break;
                    case ElementType.Text:
                        // Text elements can have custom handlers via Value
                        // Stats expansion is handled above
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error and show message to user
                System.Diagnostics.Debug.WriteLine($"Error processing element click: {ex.Message}\n{ex.StackTrace}");
                if (canvasUI != null)
                {
                    canvasUI.UpdateStatus($"Error: {ex.Message}");
                }
            }
        }

    }
}
