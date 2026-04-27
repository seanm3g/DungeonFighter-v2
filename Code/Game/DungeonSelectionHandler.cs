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
            RefreshDungeonSelectionScreen();
            return true;
        }

        private void RefreshDungeonSelectionScreen()
        {
            if (customUIManager is not CanvasUICoordinator canvasUI || stateManager.CurrentPlayer == null)
                return;
            canvasUI.Clear();
            canvasUI.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
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
                        awaitingCustomDungeonLevel = true;
                        ShowMessageEvent?.Invoke(
                            $"Enter dungeon level ({RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL}-{RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL}), or 0 to cancel.");
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
            if (!int.TryParse(input.Trim(), out int level))
            {
                ShowMessageEvent?.Invoke($"Enter a level from {RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL} to {RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL}, or 0 to cancel.");
                return;
            }

            if (level == 0)
            {
                awaitingCustomDungeonLevel = false;
                RefreshDungeonSelectionScreen();
                return;
            }

            if (level < RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL || level > RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL)
            {
                ShowMessageEvent?.Invoke($"Level must be between {RPGGame.Utils.GameConstants.MIN_DUNGEON_LEVEL} and {RPGGame.Utils.GameConstants.MAX_DUNGEON_LEVEL}.");
                return;
            }

            var template = stateManager.AvailableDungeons.FirstOrDefault(d => d.Name == GameConstants.DungeonCustomLevelMenuName);
            if (template == null)
            {
                awaitingCustomDungeonLevel = false;
                ShowMessageEvent?.Invoke("Custom dungeon option is unavailable. Try opening dungeon selection again.");
                return;
            }

            awaitingCustomDungeonLevel = false;

            if (customUIManager is CanvasUICoordinator canvasUI)
            {
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

