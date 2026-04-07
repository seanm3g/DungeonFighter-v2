using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using RPGGame;
using RPGGame.Handlers.Inventory;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;

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

            var player = game.CurrentPlayer;
            if (player != null && ComboReorderer.ApplyReorderMove(player, snapshot, fromIdx, toIdx))
            {
                // ForceRender() only refreshes the center display buffer; it does not redraw RenderWithLayout chrome
                // (including the action-info strip). Use the same path as stats-panel toggles.
                game.RefreshPersistentChromeAfterStatsToggle();
                canvasUI.Refresh();
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

            // Update hover state for menu / clickable elements
            canvasUI.SetHoverPosition(gridPos.X, gridPos.Y);

            int newStripHover = -1;
            var player = game?.CurrentPlayer;
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
                else if (inv && player != null && tooltipStripOrPanel)
                {
                    // Right-panel rows use menu-option hover; full chrome redraw updates them. Strip-only is enough for strip tooltips.
                    if (rpHovering)
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
            if (state == GameState.Combat || sm.IsComboStripEncounterLocked)
                return false;
            return ActionStripReorderPolicy.AllowsReorder(state, null);
        }

        /// <summary>
        /// Starts drag when the user presses on an action-info strip panel (see <see cref="CanReorderComboOnStrip"/>).
        /// </summary>
        private bool TryBeginComboStripDrag(PointerPressedEventArgs e, Point position)
        {
            if (!CanReorderComboOnStrip()) return false;
            var player = game!.CurrentPlayer;
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
