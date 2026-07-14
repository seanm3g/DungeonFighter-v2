using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia
{
    public partial class CanvasUICoordinator
    {
        /// <summary>
        /// Clears the canvas.
        /// </summary>
        public void Clear()
        {
            utilityCoordinator.Clear();
        }

        /// <summary>
        /// Refreshes the canvas to display pending changes.
        /// </summary>
        public void Refresh()
        {
            utilityCoordinator.Refresh();
        }

        /// <summary>
        /// Repaints only the center panel frame so combat speed changes are visible immediately.
        /// </summary>
        public void RefreshCenterPanelModeTint()
        {
            if (CenterPanelModeTint.TryUpdateExistingFrame(canvas))
                canvas.Refresh();
        }

        /// <summary>
        /// Tints the center panel background briefly after a successful clipboard copy (visual confirmation).
        /// </summary>
        public void FlashCenterPanelCopyFeedback(int flashMilliseconds = 200)
        {
            void RunFlash()
            {
                Color flashFill = Color.FromRgb(38, 58, 48);

                void ApplyNormalFrame()
                {
                    CenterPanelModeTint.TryUpdateExistingFrame(canvas);
                    canvas.Refresh();
                }

                // Ensure the frame box exists (TryUpdate alone fails if layout has not created it yet).
                CenterPanelModeTint.RenderFrame(canvas);
                canvas.TryUpdateBox(
                    LayoutConstants.CENTER_PANEL_X,
                    LayoutConstants.CENTER_PANEL_Y,
                    LayoutConstants.CENTER_PANEL_WIDTH,
                    LayoutConstants.CENTER_PANEL_HEIGHT,
                    AsciiArtAssets.Colors.Cyan,
                    flashFill);
                canvas.Refresh();

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(flashMilliseconds)
                };
                void OnTick(object? s, EventArgs ev)
                {
                    timer.Stop();
                    timer.Tick -= OnTick;
                    ApplyNormalFrame();
                }
                timer.Tick += OnTick;
                timer.Start();
            }

            if (Dispatcher.UIThread.CheckAccess())
                RunFlash();
            else
                Dispatcher.UIThread.Post(RunFlash);
        }

        /// <summary>
        /// Clears the loading status message from the bottom left corner.
        /// </summary>
        public void ClearLoadingStatus()
        {
            utilityCoordinator.ClearLoadingStatus();
        }

        /// <summary>
        /// Clears all clickable elements from the interaction manager.
        /// </summary>
        public void ClearClickableElements()
        {
            interactionManager.ClearClickableElements();
        }

        /// <summary>
        /// Suppresses display buffer rendering (e.g. when showing a menu).
        /// Cancels any pending display buffer renders, clears the combat external render callback, and resets display mode
        /// so display <c>TriggerRender</c> routes through <c>PerformRender</c> (menu suppression) instead of
        /// replaying a stale combat layout over menu screens (e.g. character creation after weapon select).
        /// </summary>
        public void SuppressDisplayBufferRendering()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.SuppressExternalRenderPathOnAllDisplayManagers();
            }
        }

        /// <summary>
        /// Restores display buffer rendering after it was suppressed.
        /// </summary>
        public void RestoreDisplayBufferRendering()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.ForceRenderForActiveCharacter();
            }
        }

        /// <summary>
        /// Selects the per-character center display buffer for <paramref name="character"/> before clear/render.
        /// Required when narrative uses <see cref="CanvasTextManager.GetDisplayManagerForCharacter"/> but UI thread still holds another manager as current.
        /// </summary>
        public void SwitchDisplayBufferToCharacter(Character? character)
        {
            if (textManager is CanvasTextManager ctm)
                ctm.SwitchToCharacterDisplayManager(character);
        }

        /// <summary>
        /// Captures a deep-cloned Action Lab combat-log snapshot for the lab hero buffer (used by instant undo).
        /// </summary>
        public LabCombatLogSnapshot? CaptureLabCombatLogSnapshot(Character? labPlayer)
        {
            if (textManager is not CanvasTextManager ctm || labPlayer == null)
                return null;
            return ctm.CaptureLabCombatLogSnapshot(labPlayer);
        }

        /// <summary>
        /// Restores an Action Lab combat-log snapshot onto the lab hero buffer (instant undo after silent replay).
        /// </summary>
        public void RestoreLabCombatLogSnapshot(Character? labPlayer, LabCombatLogSnapshot? snapshot)
        {
            if (textManager is not CanvasTextManager ctm || labPlayer == null || snapshot == null)
                return;
            ctm.RestoreLabCombatLogSnapshot(labPlayer, snapshot);
        }

        /// <summary>
        /// Clears the display buffer without triggering a render.
        /// </summary>
        public void ClearDisplayBufferWithoutRender()
        {
            textManager.ClearDisplayBufferWithoutRender();
        }

        /// <summary>
        /// Clears the display buffer and triggers a render.
        /// </summary>
        public void ClearDisplayBuffer()
        {
            textManager.ClearDisplayBuffer();
        }

        /// <summary>
        /// Forces a full layout render (resets render state and performs render).
        /// </summary>
        public void ForceFullLayoutRender()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.ForceFullLayoutRenderForActiveCharacter();
            }
        }

        /// <summary>
        /// Forces an immediate render without resetting layout state. Prefer after small data updates (e.g. combo reorder).
        /// </summary>
        public void ForceRender()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.ForceRenderForActiveCharacter();
            }
        }

        /// <summary>
        /// True when the center panel is in combat log mode (combat UI active). Independent of canvas enemy context, which can be cleared during renders.
        /// </summary>
        public bool IsCombatDisplayActive()
        {
            if (textManager is CanvasTextManager canvasTextManager)
                return canvasTextManager.DisplayManager.IsCombatDisplayMode;
            return false;
        }

        /// <summary>
        /// True when the center panel is showing the live battle log and copy (right-click or Ctrl+C) should use the full display buffer.
        /// Uses both display mode and <see cref="GameState"/> so copy still works if they briefly disagree (e.g. combat state before the next combat repaint).
        /// </summary>
        public bool IsCombatLogClipboardContext()
        {
            if (IsCombatDisplayActive())
                return true;
            var state = stateManager?.CurrentState;
            return state == GameState.Combat || state == GameState.ActionInteractionLab;
        }

        /// <summary>
        /// Clears text elements within a specific rectangular area.
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            utilityCoordinator.ClearTextInArea(startX, startY, width, height);
        }

        /// <summary>
        /// Gets the text manager for direct buffer/display access.
        /// </summary>
        public ICanvasTextManager? GetTextManager()
        {
            return textManager;
        }

        /// <summary>
        /// Starts a display batch transaction.
        /// </summary>
        /// <param name="batchLineMessageType">Per-line message type for each row appended in the batch.</param>
        public DisplayBatchTransaction StartBatch(bool autoRender = true, UIMessageType batchLineMessageType = UIMessageType.System)
        {
            if (textManager is CanvasTextManager ctm)
                return ctm.StartBatch(autoRender, batchLineMessageType);
            throw new System.InvalidOperationException("StartBatch requires CanvasTextManager.");
        }

        /// <summary>
        /// Gets the number of lines in the display buffer.
        /// </summary>
        public int GetDisplayBufferCount()
        {
            return textManager.BufferLineCount;
        }

        /// <summary>
        /// Renders the display buffer to the specified area.
        /// </summary>
        public void RenderDisplayBuffer(int x, int y, int width, int height)
        {
            textManager.RenderDisplayBuffer(x, y, width, height);
        }

        /// <summary>
        /// Renders the display buffer to the center panel area.
        /// </summary>
        public void RenderDisplayBuffer()
        {
            textManager.RenderDisplayBuffer(
                LayoutConstants.CENTER_PANEL_X + 1,
                LayoutConstants.CENTER_PANEL_Y + 1,
                LayoutConstants.CENTER_PANEL_WIDTH - 2,
                LayoutConstants.CENTER_PANEL_HEIGHT - 2);
        }

        /// <summary>
        /// Forces an immediate render of the display buffer.
        /// </summary>
        public void ForceRenderDisplayBuffer()
        {
            if (textManager is CanvasTextManager ctm)
                ctm.ForceRenderForActiveCharacter();
        }

        /// <summary>
        /// Adds room cleared message to the display buffer.
        /// </summary>
        public void AddRoomClearedMessage()
        {
            messageWritingCoordinator.AddRoomClearedMessage();
        }

        /// <summary>
        /// Appends dungeon completion summary after existing center-panel log lines (combat log preserved).
        /// </summary>
        public void AppendDungeonCompletionSummaryToBuffer(
            Dungeon dungeon,
            Character player,
            int xpGained,
            Item? lootReceived,
            List<LevelUpInfo> levelUpInfos,
            List<Item> itemsFoundDuringRun)
        {
            if (textManager is not CanvasTextManager ctm)
                return;
            ctm.SwitchToCharacterDisplayManager(player);
            var dm = ctm.GetDisplayManagerForCharacter(player);
            dm.Buffer.Add(new List<ColoredText>());
            dm.Buffer.AddRange(DungeonCompletionRenderer.BuildCompletionSummaryLines(
                dungeon,
                player,
                xpGained,
                lootReceived,
                levelUpInfos,
                itemsFoundDuringRun),
                UIMessageType.OutcomeSummary);
        }

        /// <summary>
        /// Appends death summary after existing center-panel log lines (combat log preserved).
        /// </summary>
        public void AppendDeathSummaryToBuffer(Character player, string defeatSummary)
        {
            if (textManager is not CanvasTextManager ctm)
                return;
            ctm.SwitchToCharacterDisplayManager(player);
            var dm = ctm.GetDisplayManagerForCharacter(player);
            dm.Buffer.Add(new List<ColoredText>());
            int summaryWidth = Math.Max(1, LayoutConstants.CenterPanelTextColumnWidth);
            dm.Buffer.AddRange(DeathScreenRenderer.BuildDeathSummaryLines(defeatSummary, summaryWidth));
        }

        /// <summary>
        /// Scrolls the display up.
        /// </summary>
        public void ScrollUp(int lines = 3) => ScrollDisplayBuffer(lines, scrollUp: true);

        /// <summary>
        /// Scrolls the display down.
        /// </summary>
        public void ScrollDown(int lines = 3) => ScrollDisplayBuffer(lines, scrollUp: false);

        private void ScrollDisplayBuffer(int lines, bool scrollUp)
        {
            if ((stateManager?.CurrentState != GameState.DungeonCompletion && stateManager?.CurrentState != GameState.Death)
                || textManager is not CanvasTextManager ctm)
            {
                if (scrollUp)
                    textManager.ScrollUp(lines);
                else
                    textManager.ScrollDown(lines);
                return;
            }

            var player = stateManager.GetActiveCharacter();
            if (player == null)
            {
                if (scrollUp)
                    textManager.ScrollUp(lines);
                else
                    textManager.ScrollDown(lines);
                return;
            }

            var dm = ctm.GetDisplayManagerForCharacter(player);
            int contentWidth = LayoutConstants.CENTER_PANEL_WIDTH - 2;
            int footer = stateManager.CurrentState == GameState.Death
                ? DeathScreenRenderer.FooterReservedRows
                : DungeonCompletionRenderer.FooterReservedRows;
            int logHeight = Math.Max(1, LayoutConstants.CENTER_PANEL_HEIGHT - 2 - footer);
            var dr = new DisplayRenderer(new ColoredTextWriter(canvas));
            int maxOffset = dr.CalculateMaxScrollOffset(dm.Buffer, contentWidth, logHeight);

            if (scrollUp)
                dm.Buffer.ScrollUp(lines, maxOffset);
            else
                dm.Buffer.ScrollDown(lines, maxOffset);

            RerenderCompletionOrDeathScreenAfterScroll();
        }

        private void RerenderCompletionOrDeathScreenAfterScroll()
        {
            if (stateManager?.CurrentState == GameState.DungeonCompletion
                && _dungeonCompletionRenderDungeon != null
                && _dungeonCompletionRenderPlayer != null)
            {
                RenderDungeonCompletion(
                    _dungeonCompletionRenderDungeon,
                    _dungeonCompletionRenderPlayer,
                    _dungeonCompletionRenderXpGained,
                    _dungeonCompletionRenderLoot,
                    _dungeonCompletionRenderLevelUps ?? new List<LevelUpInfo>(),
                    _dungeonCompletionRenderItemsFound ?? new List<Item>());
            }
            else if (stateManager?.CurrentState == GameState.Death
                && _deathRenderPlayer != null
                && _deathRenderSummary != null)
            {
                RenderDeathScreen(_deathRenderPlayer, _deathRenderSummary);
            }
        }

        /// <summary>
        /// Resets scroll to auto-scroll mode.
        /// </summary>
        public void ResetScroll()
        {
            textManager.ResetScroll();
        }

        /// <summary>
        /// Gets the display buffer content as a single string.
        /// </summary>
        public string GetDisplayBufferText()
        {
            return string.Join(System.Environment.NewLine, textManager.DisplayBuffer);
        }

        /// <summary>
        /// Plain text for battle-log clipboard: left character panel (from current canvas draw state) plus center buffer.
        /// </summary>
        public string GetBattleLogClipboardText()
        {
            static string TrimTrailingBlankLines(string s)
            {
                if (string.IsNullOrEmpty(s))
                    return s;
                string[] lines = s.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                int end = lines.Length - 1;
                while (end >= 0 && string.IsNullOrWhiteSpace(lines[end]))
                    end--;
                return end < 0 ? "" : string.Join(System.Environment.NewLine, lines.Take(end + 1));
            }

            int leftHeight = LayoutConstants.LEFT_PANEL_HEIGHT + 1;
            string leftPanel;
            try
            {
                leftPanel = canvas.GetPlainTextSnapshotInGridRect(
                    LayoutConstants.LEFT_PANEL_X,
                    LayoutConstants.LEFT_PANEL_Y,
                    LayoutConstants.LEFT_PANEL_WIDTH,
                    leftHeight,
                    excludeOverlay: true);
            }
            catch
            {
                leftPanel = "";
            }
            leftPanel = TrimTrailingBlankLines(leftPanel.TrimEnd());
            string center = GetDisplayBufferText().TrimEnd();

            if (string.IsNullOrWhiteSpace(leftPanel))
                return center;
            if (string.IsNullOrWhiteSpace(center))
                return "=== Character panel ===" + System.Environment.NewLine + leftPanel;

            return "=== Character panel ===" + System.Environment.NewLine + leftPanel
                + System.Environment.NewLine + System.Environment.NewLine
                + "=== Combat log (center) ===" + System.Environment.NewLine + center;
        }

        /// <summary>
        /// Gets the clickable element at the specified coordinates.
        /// </summary>
        public ClickableElement? GetElementAt(int x, int y)
        {
            return interactionManager.GetElementAt(x, y);
        }

        /// <summary>
        /// Sets the current hover position for highlight updates.
        /// </summary>
        public bool SetHoverPosition(int x, int y)
        {
            return interactionManager.SetHoverPosition(x, y);
        }

        /// <summary>
        /// Clears all clickable hover highlights (e.g. before repointing after leaving strip/side panels).
        /// </summary>
        public void ClearHoverStates()
        {
            interactionManager.ClearHoverStates();
        }

        /// <summary>
        /// Live clickable elements for the current frame (inventory right panel, menus, etc.).
        /// </summary>
        public IReadOnlyList<ClickableElement> GetClickableElements() => interactionManager.ClickableElements;

        /// <summary>
        /// Resets the delete confirmation state.
        /// </summary>
        public void ResetDeleteConfirmation()
        {
            utilityCoordinator.ResetDeleteConfirmation();
        }

        /// <summary>
        /// Shows a message on the canvas.
        /// </summary>
        public void ShowMessage(string message, Color color = default)
        {
            utilityCoordinator.ShowMessage(message, color);
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        public void ShowError(string error)
        {
            utilityCoordinator.ShowError(error);
        }

        /// <summary>
        /// Shows an error message with suggestion.
        /// </summary>
        public void ShowError(string error, string suggestion = "")
        {
            utilityCoordinator.ShowError(error, suggestion);
        }

        /// <summary>
        /// Shows loading status in the bottom left corner.
        /// </summary>
        public void ShowLoadingStatus(string message = "Loading data...")
        {
            utilityCoordinator.ShowLoadingStatus(message);
        }

        /// <summary>
        /// Shows loading animation.
        /// </summary>
        public void ShowLoadingAnimation(string message = "Loading...")
        {
            utilityCoordinator.ShowLoadingAnimation(message);
        }

        /// <summary>
        /// Shows invalid key message at the bottom.
        /// </summary>
        public void ShowInvalidKeyMessage(string message)
        {
            utilityCoordinator.ShowInvalidKeyMessage(message);
        }

        /// <summary>
        /// Shows press key message at the bottom (e.g. "Press any key to continue").
        /// </summary>
        public void ShowPressKeyMessage()
        {
            utilityCoordinator.ShowPressKeyMessage();
        }

        /// <summary>
        /// Updates status message (e.g. in loading/settings).
        /// </summary>
        public void UpdateStatus(string message)
        {
            utilityCoordinator.UpdateStatus(message);
        }

        /// <summary>
        /// Toggles the help display.
        /// </summary>
        public void ToggleHelp()
        {
            utilityCoordinator.ToggleHelp();
        }

        /// <summary>
        /// Hides the tuning parameters menu panel.
        /// </summary>
        public void HideTuningParametersMenu()
        {
            windowManager.GetMainWindow()?.HideTuningMenuPanel();
        }

        /// <summary>
        /// Stops the dungeon selection animation.
        /// </summary>
        public void StopDungeonSelectionAnimation()
        {
            animationManager.StopDungeonSelectionAnimation();
        }

        /// <summary>
        /// Updates battle statistics progress display (re-renders running state).
        /// </summary>
        public void UpdateBattleStatisticsProgress(int completed, int total, string status)
        {
            renderer.RenderBattleStatisticsMenu(null, true);
        }
    }
}
