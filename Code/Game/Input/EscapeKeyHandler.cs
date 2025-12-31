using System;
using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;
using Action = System.Action;

namespace RPGGame.GameCore.Input
{
    /// <summary>
    /// Handles escape key input and state transitions
    /// Extracted from Game.cs HandleEscapeKey() method
    /// </summary>
    public class EscapeKeyHandler
    {
        private readonly GameStateManager stateManager;
        private readonly EscapeKeyHandlers handlers;
        private readonly System.Action showGameLoop;
        private readonly System.Action showMainMenu;
        private readonly System.Action showSettings;
        private readonly System.Action showDeveloperMenu;
        private readonly System.Action showActionEditor;

        public EscapeKeyHandler(
            GameStateManager stateManager,
            EscapeKeyHandlers handlers,
            System.Action showGameLoop,
            System.Action showMainMenu,
            System.Action showSettings,
            System.Action showDeveloperMenu,
            System.Action showActionEditor)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            this.showGameLoop = showGameLoop ?? throw new ArgumentNullException(nameof(showGameLoop));
            this.showMainMenu = showMainMenu ?? throw new ArgumentNullException(nameof(showMainMenu));
            this.showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
            this.showDeveloperMenu = showDeveloperMenu ?? throw new ArgumentNullException(nameof(showDeveloperMenu));
            this.showActionEditor = showActionEditor ?? throw new ArgumentNullException(nameof(showActionEditor));
        }

        /// <summary>
        /// Handles escape key input and transitions to appropriate state
        /// </summary>
        public Task HandleEscapeKey()
        {
            switch (stateManager.CurrentState)
            {
                case GameState.Inventory:
                    // Prevent ESC during trade-up flow - must complete trade-up once started
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.IsInTradeUpFlow())
                    {
                        // Do nothing - cannot escape from trade-up menu
                        return Task.CompletedTask;
                    }
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
                case GameState.CharacterInfo:
                case GameState.Settings:
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
                case GameState.DungeonSelection:
                    stateManager.TransitionToState(GameState.GameLoop);
                    showGameLoop();
                    break;
                case GameState.Testing:
                    stateManager.TransitionToState(GameState.Settings);
                    showSettings();
                    break;
                case GameState.BattleStatistics:
                    // Return to developer menu
                    stateManager.TransitionToState(GameState.DeveloperMenu);
                    showDeveloperMenu();
                    break;
                case GameState.DeveloperMenu:
                    stateManager.TransitionToState(GameState.Settings);
                    showSettings();
                    break;
                case GameState.VariableEditor:
                case GameState.TuningParameters:
                case GameState.ActionEditor:
                    stateManager.TransitionToState(GameState.DeveloperMenu);
                    showDeveloperMenu();
                    break;
                case GameState.CreateAction:
                    stateManager.TransitionToState(GameState.ActionEditor);
                    showActionEditor();
                    break;
                case GameState.ViewAction:
                    // Return to action list (which will be handled by ActionEditorHandler)
                    if (handlers.ActionEditorHandler != null)
                    {
                        handlers.ActionEditorHandler.HandleActionDetailInput("0");
                    }
                    break;
                default:
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Container for handlers needed for escape key handling
    /// </summary>
    public class EscapeKeyHandlers
    {
        public ActionEditorHandler? ActionEditorHandler { get; set; }
        public InventoryMenuHandler? InventoryMenuHandler { get; set; }
    }
}

