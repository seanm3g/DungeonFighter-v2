using System;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Utils;
using DungeonFighter.Game.Menu.Core;

namespace RPGGame.GameCore.Input
{
    /// <summary>
    /// Routes input to appropriate handlers based on game state
    /// Extracted from Game.cs HandleInput() method
    /// </summary>
    public class GameInputRouter
    {
        private readonly GameStateManager stateManager;
        private readonly GameInputHandlers handlers;
        private readonly Action<string> showMessage;
        private readonly Action<string> handleCombatScroll;

        public GameInputRouter(
            GameStateManager stateManager,
            GameInputHandlers handlers,
            Action<string> showMessage,
            Action<string> handleCombatScroll)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this.handleCombatScroll = handleCombatScroll ?? throw new ArgumentNullException(nameof(handleCombatScroll));
        }

        /// <summary>
        /// Routes input to the appropriate handler based on current game state
        /// </summary>
        public async Task RouteInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            // Debug: Log input and state for troubleshooting
            DebugLogger.Log("Game", $"HandleInput: input='{input}', state={stateManager.CurrentState}, mainMenuHandler={handlers.MainMenuHandler != null}");
            ScrollDebugLogger.Log($"Game.HandleInput: input='{input}', state={stateManager.CurrentState}");

            switch (stateManager.CurrentState)
            {
                case GameState.MainMenu:
                    if (handlers.MainMenuHandler != null)
                    {
                        DebugLogger.Log("Game", $"Routing to MainMenuHandler.HandleMenuInput('{input}')");
                        await handlers.MainMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: mainMenuHandler is null!");
                    }
                    break;
                case GameState.CharacterInfo:
                    handlers.CharacterMenuHandler?.HandleMenuInput(input);
                    break;
                case GameState.Settings:
                    DebugLogger.Log("Game", $"Settings state: input='{input}', handler is {(handlers.SettingsMenuHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: Settings state - input='{input}', handler is {(handlers.SettingsMenuHandler != null ? "not null" : "NULL")}");
                    if (handlers.SettingsMenuHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling SettingsMenuHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling SettingsMenuHandler.HandleMenuInput('{input}')");
                        handlers.SettingsMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: settingsMenuHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - settingsMenuHandler is null!");
                    }
                    break;
                case GameState.DeveloperMenu:
                    DebugLogger.Log("Game", $"DeveloperMenu state: input='{input}', handler is {(handlers.DeveloperMenuHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: DeveloperMenu state - input='{input}', handler is {(handlers.DeveloperMenuHandler != null ? "not null" : "NULL")}");
                    if (handlers.DeveloperMenuHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling DeveloperMenuHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling DeveloperMenuHandler.HandleMenuInput('{input}')");
                        handlers.DeveloperMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: developerMenuHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - developerMenuHandler is null!");
                    }
                    break;
                case GameState.VariableEditor:
                    if (input == "0")
                    {
                        stateManager.TransitionToState(GameState.DeveloperMenu);
                        handlers.DeveloperMenuHandler?.ShowDeveloperMenu();
                    }
                    else
                    {
                        showMessage("Variable editing functionality coming soon!");
                    }
                    break;
                case GameState.ActionEditor:
                    DebugLogger.Log("Game", $"ActionEditor state: input='{input}', handler is {(handlers.ActionEditorHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: ActionEditor state - input='{input}', handler is {(handlers.ActionEditorHandler != null ? "not null" : "NULL")}");
                    if (handlers.ActionEditorHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling ActionEditorHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling ActionEditorHandler.HandleMenuInput('{input}')");
                        handlers.ActionEditorHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: actionEditorHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - actionEditorHandler is null!");
                    }
                    break;
                case GameState.CreateAction:
                    DebugLogger.Log("Game", $"CreateAction state: input='{input}', handler is {(handlers.ActionEditorHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: CreateAction state - input='{input}', handler is {(handlers.ActionEditorHandler != null ? "not null" : "NULL")}");
                    if (handlers.ActionEditorHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling ActionEditorHandler.HandleCreateActionInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling ActionEditorHandler.HandleCreateActionInput('{input}')");
                        handlers.ActionEditorHandler.HandleCreateActionInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: actionEditorHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - actionEditorHandler is null!");
                    }
                    break;
                case GameState.ViewAction:
                    DebugLogger.Log("Game", $"ViewAction state: input='{input}', handler is {(handlers.ActionEditorHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: ViewAction state - input='{input}', handler is {(handlers.ActionEditorHandler != null ? "not null" : "NULL")}");
                    if (handlers.ActionEditorHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling ActionEditorHandler.HandleActionDetailInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling ActionEditorHandler.HandleActionDetailInput('{input}')");
                        handlers.ActionEditorHandler.HandleActionDetailInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: actionEditorHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - actionEditorHandler is null!");
                    }
                    break;
                case GameState.Inventory:
                    handlers.InventoryMenuHandler?.HandleMenuInput(input);
                    break;
                case GameState.WeaponSelection:
                    handlers.WeaponSelectionHandler?.HandleMenuInput(input);
                    break;
                case GameState.GameLoop:
                    if (handlers.GameLoopInputHandler != null)
                        await handlers.GameLoopInputHandler.HandleMenuInput(input);
                    break;
                case GameState.DungeonSelection:
                    DebugLogger.Log("Game", $"Routing to DungeonSelectionHandler.HandleMenuInput('{input}')");
                    if (handlers.DungeonSelectionHandler != null)
                        await handlers.DungeonSelectionHandler.HandleMenuInput(input);
                    else
                        DebugLogger.Log("Game", "ERROR: dungeonSelectionHandler is null!");
                    break;
                case GameState.DungeonCompletion:
                    if (handlers.DungeonCompletionHandler != null)
                        await handlers.DungeonCompletionHandler.HandleMenuInput(input);
                    break;
                case GameState.Death:
                    if (handlers.DeathScreenHandler != null)
                        await handlers.DeathScreenHandler.HandleMenuInput(input);
                    break;
                case GameState.Testing:
                    DebugLogger.Log("Game", $"Testing state: input='{input}', handler is {(handlers.TestingSystemHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: Testing state - input='{input}', handler is {(handlers.TestingSystemHandler != null ? "not null" : "NULL")}");
                    // Allow scrolling during testing to view test results
                    if (input == "up" || input == "down")
                    {
                        ScrollDebugLogger.Log($"Testing state: Handling scroll input '{input}'");
                        DebugLogger.Log("Game", $"Testing state: Handling scroll input '{input}'");
                        // Don't show message - it replaces the content we're trying to scroll
                        handleCombatScroll(input);
                        return; // Don't process other input when scrolling
                    }
                    else if (handlers.TestingSystemHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling TestingSystemHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling TestingSystemHandler.HandleMenuInput('{input}')");
                        await handlers.TestingSystemHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: testingSystemHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - testingSystemHandler is null!");
                    }
                    break;
                case GameState.CharacterCreation:
                    if (handlers.CharacterCreationHandler != null)
                    {
                        DebugLogger.Log("Game", $"Routing to CharacterCreationHandler.HandleMenuInput('{input}')");
                        handlers.CharacterCreationHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: characterCreationHandler is null!");
                    }
                    break;
                case GameState.Dungeon:
                case GameState.Combat:
                    // Handle scrolling during combat
                    if (input == "up" || input == "down")
                    {
                        handleCombatScroll(input);
                    }
                    // Other input is handled internally by the managers
                    break;
                default:
                    showMessage($"Unknown state: {stateManager.CurrentState}");
                    break;
            }
        }
    }

    /// <summary>
    /// Container for all game input handlers
    /// </summary>
    public class GameInputHandlers
    {
        public MainMenuHandler? MainMenuHandler { get; set; }
        public CharacterMenuHandler? CharacterMenuHandler { get; set; }
        public SettingsMenuHandler? SettingsMenuHandler { get; set; }
        public DeveloperMenuHandler? DeveloperMenuHandler { get; set; }
        public ActionEditorHandler? ActionEditorHandler { get; set; }
        public InventoryMenuHandler? InventoryMenuHandler { get; set; }
        public WeaponSelectionHandler? WeaponSelectionHandler { get; set; }
        public CharacterCreationHandler? CharacterCreationHandler { get; set; }
        public GameLoopInputHandler? GameLoopInputHandler { get; set; }
        public DungeonSelectionHandler? DungeonSelectionHandler { get; set; }
        public DungeonCompletionHandler? DungeonCompletionHandler { get; set; }
        public DeathScreenHandler? DeathScreenHandler { get; set; }
        public TestingSystemHandler? TestingSystemHandler { get; set; }
    }
}

