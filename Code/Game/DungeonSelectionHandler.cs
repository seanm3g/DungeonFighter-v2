namespace RPGGame
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Handles dungeon selection menu and preparation.
    /// Extracted from Game.cs to manage dungeon selection flow.
    /// </summary>
    public class DungeonSelectionHandler
    {
        private GameStateManager stateManager;
        private DungeonManagerWithRegistry? dungeonManager;
        private IUIManager? customUIManager;
        private bool awaitingCustomDungeonLevel;
        private string customLevelBuffer = "";
        
        // Delegates
        public delegate Task OnStartDungeon();
        public delegate void OnShowGameLoop();
        public delegate void OnShowMessage(string message);
        
        public event OnStartDungeon? StartDungeonEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMessage? ShowMessageEvent;

        public DungeonSelectionHandler(
            GameStateManager stateManager,
            DungeonManagerWithRegistry? dungeonManager,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.dungeonManager = dungeonManager;
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Show dungeon selection screen
        /// </summary>
        public async Task ShowDungeonSelection()
        {
            
            if (stateManager.CurrentPlayer == null || dungeonManager == null) return;

            awaitingCustomDungeonLevel = false;
            customLevelBuffer = "";
            if (customUIManager is CanvasUICoordinator canvasClearPrompt)
                canvasClearPrompt.SetDungeonSelectionCustomLevelPrompt(null);
            
            // Regenerate dungeons based on current player level
            dungeonManager.RegenerateDungeons(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
            
            // Use GameScreenCoordinator for standardized screen transition
            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowDungeonSelection();
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// When the player opened the custom-level prompt, Escape cancels it and keeps them on dungeon selection.
        /// </summary>
        /// <returns>True if a pending custom-level prompt was cleared (caller should not also leave the screen).</returns>
        public bool CancelCustomLevelPromptIfActive()
        {
            if (!awaitingCustomDungeonLevel)
                return false;
            awaitingCustomDungeonLevel = false;
            customLevelBuffer = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
                canvasUI.UpdateStatus("");
            RefreshDungeonSelectionScreen();
            return true;
        }

        private void RefreshDungeonSelectionScreen()
        {
            if (customUIManager is not CanvasUICoordinator canvasUI || stateManager.CurrentPlayer == null)
                return;
            canvasUI.SetDungeonSelectionCustomLevelPrompt(awaitingCustomDungeonLevel ? customLevelBuffer : null);
            canvasUI.Clear();
            canvasUI.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
        }

        private void ShowNonBlockingDungeonMessage(string message)
        {
            if (customUIManager is CanvasUICoordinator canvas)
                canvas.ShowInvalidKeyMessage(message);
            else
                ShowMessageEvent?.Invoke(message);
        }

        /// <summary>
        /// Handle dungeon selection input
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            if (stateManager.CurrentPlayer == null)
            {
                return;
            }

            if (awaitingCustomDungeonLevel)
            {
                await HandleCustomDungeonLevelInput(input);
                return;
            }

            if (int.TryParse(input.Trim(), out int choice))
            {
                if (choice >= 1 && choice <= stateManager.AvailableDungeons.Count)
                {
                    int dungeonIndex = choice - 1;
                    var picked = stateManager.AvailableDungeons[dungeonIndex];
                    if (picked.Name == GameConstants.DungeonCustomLevelMenuName)
                    {
                        customLevelBuffer = "";
                        awaitingCustomDungeonLevel = true;
                        RefreshDungeonSelectionScreen();
                        return;
                    }

                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.StopDungeonSelectionAnimation();
                    }

                    await SelectDungeon(dungeonIndex);
                }
                else if (choice == 0)
                {
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.StopDungeonSelectionAnimation();
                    }

                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoopEvent?.Invoke();
                }
                else
                {
                    DebugLogger.Log("DungeonSelectionHandler", $"Invalid choice: {choice} (valid: 1-{stateManager.AvailableDungeons.Count} or 0)");
                    ShowMessageEvent?.Invoke("Invalid choice. Please select a valid dungeon or 0 to return.");
                }
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid input. Please enter a number.");
            }
        }

        private async Task HandleCustomDungeonLevelInput(string input)
        {
            string t = (input ?? "").Trim();
            if (string.IsNullOrEmpty(t))
                return;

            switch (t.ToLowerInvariant())
            {
                case "enter":
                    await TrySubmitCustomDungeonLevelAsync();
                    return;
                case "backspace":
                    if (customLevelBuffer.Length > 0)
                        customLevelBuffer = customLevelBuffer.Substring(0, customLevelBuffer.Length - 1);
                    RefreshDungeonSelectionScreen();
                    return;
            }

            if (t == "0" && customLevelBuffer.Length == 0)
            {
                awaitingCustomDungeonLevel = false;
                customLevelBuffer = "";
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.UpdateStatus("");
                    canvasUI.SetDungeonSelectionCustomLevelPrompt(null);
                }
                RefreshDungeonSelectionScreen();
                return;
            }

            if (t.Length == 1 && t[0] >= '0' && t[0] <= '9')
            {
                if (customLevelBuffer.Length >= 3)
                    return;
                customLevelBuffer += t;
                RefreshDungeonSelectionScreen();
                return;
            }

            ShowNonBlockingDungeonMessage("Use number keys, Enter to confirm, Backspace to edit, Esc to cancel.");
        }

        private async Task TrySubmitCustomDungeonLevelAsync()
        {
            if (string.IsNullOrEmpty(customLevelBuffer))
            {
                ShowNonBlockingDungeonMessage(
                    $"Type a level ({RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL}-{RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL}), then press Enter.");
                RefreshDungeonSelectionScreen();
                return;
            }

            if (!int.TryParse(customLevelBuffer, out int level))
            {
                ShowNonBlockingDungeonMessage("That level number isn't valid.");
                return;
            }

            if (level < RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL || level > RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL)
            {
                ShowNonBlockingDungeonMessage(
                    $"Level must be between {RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL} and {RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL}.");
                return;
            }

            var template = stateManager.AvailableDungeons.FirstOrDefault(d => d.Name == GameConstants.DungeonCustomLevelMenuName);
            if (template == null)
            {
                awaitingCustomDungeonLevel = false;
                customLevelBuffer = "";
                ShowNonBlockingDungeonMessage("Custom dungeon option is unavailable. Try opening dungeon selection again.");
                return;
            }

            awaitingCustomDungeonLevel = false;
            customLevelBuffer = "";
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SetDungeonSelectionCustomLevelPrompt(null);
                canvasUI.UpdateStatus("");
                canvasUI.StopDungeonSelectionAnimation();
            }

            string runName = $"{template.Theme} (level {level})";
            var dungeon = new Dungeon(runName, level, level, template.Theme, template.PossibleEnemies, template.ColorOverride);
            stateManager.SetCurrentDungeon(dungeon);
            await BeginCurrentDungeonRunAsync();
        }

        /// <summary>
        /// Select a specific dungeon and start it
        /// </summary>
        private async Task SelectDungeon(int dungeonIndex)
        {
            if (stateManager.CurrentPlayer == null || dungeonManager == null) return;
            
            if (dungeonIndex < 0 || dungeonIndex >= stateManager.AvailableDungeons.Count)
            {
                ShowMessageEvent?.Invoke("Invalid dungeon selection.");
                return;
            }
            
            stateManager.SetCurrentDungeon(stateManager.AvailableDungeons[dungeonIndex]);
            await BeginCurrentDungeonRunAsync();
        }

        private async Task BeginCurrentDungeonRunAsync()
        {
            if (stateManager.CurrentDungeon == null)
                return;

            stateManager.CurrentDungeon.Generate();
            if (StartDungeonEvent != null)
            {
                await StartDungeonEvent.Invoke();
            }
        }
    }
}

