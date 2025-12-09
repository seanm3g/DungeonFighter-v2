namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;
    using DungeonFighter.Game.Menu.Routing;
    using DungeonFighter.Game.Menu.Core;

    /// <summary>
    /// Main Game coordinator class.
    /// Orchestrates 10 specialized managers to handle different game aspects.
    /// This is a lean coordinator after Phase 6 refactoring (reduced from 1,383 to ~280 lines).
    /// 
    /// Architecture:
    /// - Game.cs acts as a coordinator/facade
    /// - 10 focused managers handle specific concerns
    /// - Event-driven communication between components
    /// - Clean separation of responsibilities
    /// </summary>
    public class Game
    {
        // Core managers (from Phase 5)
        private GameStateManager stateManager = new();
        private GameNarrativeManager narrativeManager = new();
        private GameInitializationManager initializationManager = new();
        private GameInputHandler? inputHandler;
        
        // Menu/UI handlers (from Phase 6)
        private MainMenuHandler? mainMenuHandler;
        private CharacterMenuHandler? characterMenuHandler;
        private SettingsMenuHandler? settingsMenuHandler;
        private DeveloperMenuHandler? developerMenuHandler;
        private ActionEditorHandler? actionEditorHandler;
        private InventoryMenuHandler? inventoryMenuHandler;
        private WeaponSelectionHandler? weaponSelectionHandler;
        private CharacterCreationHandler? characterCreationHandler;
        private GameLoopInputHandler? gameLoopInputHandler;
        private DungeonSelectionHandler? dungeonSelectionHandler;
        private DungeonRunnerManager? dungeonRunnerManager;
        private DungeonCompletionHandler? dungeonCompletionHandler;
        private DeathScreenHandler? deathScreenHandler;
        private TestingSystemHandler? testingSystemHandler;
        
        // Game loop state
        private DungeonManagerWithRegistry? dungeonManager;
        private CombatManager? combatManager;
        
        // Other managers
        private GameInitializer gameInitializer;
        private IUIManager? customUIManager;
        private GameScreenCoordinator screenCoordinator;
        
        // NEW: Menu Input Framework (Phase 3 Refactoring)
        private MenuInputRouter? menuInputRouter;
        private MenuInputValidator? menuInputValidator;

        // Constructor 1: Default
        public Game()
        {
            GameTicker.Instance.Start();
            gameInitializer = new GameInitializer();
            screenCoordinator = new GameScreenCoordinator(stateManager);
            
            // Initialize core managers
            inputHandler = new GameInputHandler(stateManager);
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
            
            // Initialize all handlers (no UI)
            InitializeHandlers(null);
        }

        // Constructor 2: With UI Manager
        public Game(IUIManager uiManager)
        {
            GameTicker.Instance.Start();
            customUIManager = uiManager;
            UIManager.SetCustomUIManager(uiManager);
            
            gameInitializer = new GameInitializer();
            
            // Initialize core managers
            inputHandler = new GameInputHandler(stateManager);
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();

            // Initialize screen coordinator after managers are ready
            screenCoordinator = new GameScreenCoordinator(stateManager);

            // Initialize all handlers with UI
            InitializeHandlers(uiManager);
        }

        // Constructor 3: With existing character
        public Game(Character existingCharacter)
        {
            var settings = GameSettings.Instance;
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                existingCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            GameTicker.Instance.Start();
            gameInitializer = new GameInitializer();
            screenCoordinator = new GameScreenCoordinator(stateManager);
            
            // Initialize core managers
            inputHandler = new GameInputHandler(stateManager);
            stateManager.SetCurrentPlayer(existingCharacter);
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
            
            // Initialize game data
            var availableDungeons = new List<Dungeon>();
            gameInitializer.InitializeExistingGame(existingCharacter, availableDungeons);
            stateManager.SetAvailableDungeons(availableDungeons);
            
            // Initialize all handlers (no UI)
            InitializeHandlers(null);
        }

        // Initialize all 11 handler managers
        private void InitializeHandlers(IUIManager? uiManager)
        {
            // Create handlers using HandlerInitializer
            var handlerResult = RPGGame.Handlers.HandlerInitializer.CreateHandlers(
                stateManager, initializationManager, gameInitializer, dungeonManager!, 
                combatManager!, narrativeManager, uiManager);
            
            // Assign to instance fields
            mainMenuHandler = handlerResult.MainMenuHandler;
            characterMenuHandler = handlerResult.CharacterMenuHandler;
            settingsMenuHandler = handlerResult.SettingsMenuHandler;
            developerMenuHandler = new DeveloperMenuHandler(stateManager, uiManager);
            actionEditorHandler = new ActionEditorHandler(stateManager, uiManager);
            inventoryMenuHandler = handlerResult.InventoryMenuHandler;
            weaponSelectionHandler = handlerResult.WeaponSelectionHandler;
            characterCreationHandler = handlerResult.CharacterCreationHandler;
            gameLoopInputHandler = handlerResult.GameLoopInputHandler;
            dungeonSelectionHandler = handlerResult.DungeonSelectionHandler;
            dungeonRunnerManager = handlerResult.DungeonRunnerManager;
            dungeonCompletionHandler = handlerResult.DungeonCompletionHandler;
            deathScreenHandler = handlerResult.DeathScreenHandler;
            testingSystemHandler = handlerResult.TestingSystemHandler;
            
            // Wire up handler events
            RPGGame.Handlers.HandlerInitializer.WireHandlerEvents(
                handlerResult, stateManager, customUIManager,
                ShowGameLoop, ShowMainMenu, ShowInventory, ShowCharacterInfo, ShowMessage, ExitGame,
                async () => await (dungeonSelectionHandler?.ShowDungeonSelection() ?? Task.CompletedTask),
                ShowDungeonCompletion, ShowDeathScreen, SaveGame);
            
            // Wire up developer menu handler events
            if (developerMenuHandler != null)
            {
                developerMenuHandler.ShowSettingsEvent += () => settingsMenuHandler?.ShowSettings();
                developerMenuHandler.ShowVariableEditorEvent += () => ShowVariableEditor();
                developerMenuHandler.ShowActionEditorEvent += () => ShowActionEditor();
            }
            
            // Wire up action editor handler events
            if (actionEditorHandler != null)
            {
                actionEditorHandler.ShowDeveloperMenuEvent += () => developerMenuHandler?.ShowDeveloperMenu();
            }
            
            // Initialize Menu Input Framework
            var menuInputResult = RPGGame.Menu.MenuInputFrameworkInitializer.Initialize();
            menuInputRouter = menuInputResult.MenuInputRouter;
            menuInputValidator = menuInputResult.MenuInputValidator;
        }

        // Static delegates for compatibility
        public static Dictionary<string, List<string>> GetThemeSpecificRooms()
            => GameInitializationManager.GetThemeSpecificRooms();

        public static DungeonGenerationConfig GetDungeonGenerationConfig()
            => GameInitializationManager.GetDungeonGenerationConfig();

        // Public properties
        public GameState CurrentState => stateManager.CurrentState;
        public Character? CurrentPlayer => stateManager.CurrentPlayer;
        public List<Item> CurrentInventory => stateManager.CurrentInventory;
        public List<Dungeon> AvailableDungeons => stateManager.AvailableDungeons;
        public Dungeon? CurrentDungeon => stateManager.CurrentDungeon;
        public Environment? CurrentRoom => stateManager.CurrentRoom;

        // Main entry points
        public void ShowMainMenu()
        {
            mainMenuHandler?.ShowMainMenu();
        }

        public async Task HandleInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            
            // Debug: Log input and state for troubleshooting
            DebugLogger.Log("Game", $"HandleInput: input='{input}', state={stateManager.CurrentState}, mainMenuHandler={mainMenuHandler != null}");
            ScrollDebugLogger.Log($"Game.HandleInput: input='{input}', state={stateManager.CurrentState}");
            
            switch (stateManager.CurrentState)
            {
                case GameState.MainMenu:
                    if (mainMenuHandler != null)
                    {
                        DebugLogger.Log("Game", $"Routing to MainMenuHandler.HandleMenuInput('{input}')");
                        await mainMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: mainMenuHandler is null!");
                    }
                    break;
                case GameState.CharacterInfo:
                    characterMenuHandler?.HandleMenuInput(input);
                    break;
                case GameState.Settings:
                    DebugLogger.Log("Game", $"Settings state: input='{input}', handler is {(settingsMenuHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: Settings state - input='{input}', handler is {(settingsMenuHandler != null ? "not null" : "NULL")}");
                    if (settingsMenuHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling SettingsMenuHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling SettingsMenuHandler.HandleMenuInput('{input}')");
                        settingsMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: settingsMenuHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - settingsMenuHandler is null!");
                    }
                    break;
                case GameState.DeveloperMenu:
                    DebugLogger.Log("Game", $"DeveloperMenu state: input='{input}', handler is {(developerMenuHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: DeveloperMenu state - input='{input}', handler is {(developerMenuHandler != null ? "not null" : "NULL")}");
                    if (developerMenuHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling DeveloperMenuHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling DeveloperMenuHandler.HandleMenuInput('{input}')");
                        developerMenuHandler.HandleMenuInput(input);
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
                        developerMenuHandler?.ShowDeveloperMenu();
                    }
                    else
                    {
                        ShowMessage("Variable editing functionality coming soon!");
                    }
                    break;
                case GameState.ActionEditor:
                    DebugLogger.Log("Game", $"ActionEditor state: input='{input}', handler is {(actionEditorHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: ActionEditor state - input='{input}', handler is {(actionEditorHandler != null ? "not null" : "NULL")}");
                    if (actionEditorHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling ActionEditorHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling ActionEditorHandler.HandleMenuInput('{input}')");
                        actionEditorHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: actionEditorHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - actionEditorHandler is null!");
                    }
                    break;
                case GameState.CreateAction:
                    DebugLogger.Log("Game", $"CreateAction state: input='{input}', handler is {(actionEditorHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: CreateAction state - input='{input}', handler is {(actionEditorHandler != null ? "not null" : "NULL")}");
                    if (actionEditorHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling ActionEditorHandler.HandleCreateActionInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling ActionEditorHandler.HandleCreateActionInput('{input}')");
                        actionEditorHandler.HandleCreateActionInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: actionEditorHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - actionEditorHandler is null!");
                    }
                    break;
                case GameState.ViewAction:
                    DebugLogger.Log("Game", $"ViewAction state: input='{input}', handler is {(actionEditorHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: ViewAction state - input='{input}', handler is {(actionEditorHandler != null ? "not null" : "NULL")}");
                    if (actionEditorHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling ActionEditorHandler.HandleActionDetailInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling ActionEditorHandler.HandleActionDetailInput('{input}')");
                        actionEditorHandler.HandleActionDetailInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: actionEditorHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - actionEditorHandler is null!");
                    }
                    break;
                case GameState.Inventory:
                    inventoryMenuHandler?.HandleMenuInput(input);
                    break;
                case GameState.WeaponSelection:
                    weaponSelectionHandler?.HandleMenuInput(input);
                    break;
                case GameState.GameLoop:
                    if (gameLoopInputHandler != null)
                        await gameLoopInputHandler.HandleMenuInput(input);
                    break;
                case GameState.DungeonSelection:
                    DebugLogger.Log("Game", $"Routing to DungeonSelectionHandler.HandleMenuInput('{input}')");
                    if (dungeonSelectionHandler != null)
                        await dungeonSelectionHandler.HandleMenuInput(input);
                    else
                        DebugLogger.Log("Game", "ERROR: dungeonSelectionHandler is null!");
                    break;
                case GameState.DungeonCompletion:
                    if (dungeonCompletionHandler != null)
                        await dungeonCompletionHandler.HandleMenuInput(input);
                    break;
                case GameState.Death:
                    if (deathScreenHandler != null)
                        await deathScreenHandler.HandleMenuInput(input);
                    break;
                case GameState.Testing:
                    DebugLogger.Log("Game", $"Testing state: input='{input}', handler is {(testingSystemHandler != null ? "not null" : "NULL")}");
                    ScrollDebugLogger.Log($"Game: Testing state - input='{input}', handler is {(testingSystemHandler != null ? "not null" : "NULL")}");
                    // Allow scrolling during testing to view test results
                    if (input == "up" || input == "down")
                    {
                        ScrollDebugLogger.Log($"Testing state: Handling scroll input '{input}'");
                        DebugLogger.Log("Game", $"Testing state: Handling scroll input '{input}'");
                        // Don't show message - it replaces the content we're trying to scroll
                        HandleCombatScroll(input);
                        return; // Don't process other input when scrolling
                    }
                    else if (testingSystemHandler != null)
                    {
                        DebugLogger.Log("Game", $"Calling TestingSystemHandler.HandleMenuInput('{input}')");
                        ScrollDebugLogger.Log($"Game: Calling TestingSystemHandler.HandleMenuInput('{input}')");
                        await testingSystemHandler.HandleMenuInput(input);
                    }
                    else
                    {
                        DebugLogger.Log("Game", "ERROR: testingSystemHandler is null!");
                        ScrollDebugLogger.Log("Game: ERROR - testingSystemHandler is null!");
                    }
                    break;
                case GameState.CharacterCreation:
                    if (characterCreationHandler != null)
                    {
                        DebugLogger.Log("Game", $"Routing to CharacterCreationHandler.HandleMenuInput('{input}')");
                        characterCreationHandler.HandleMenuInput(input);
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
                        HandleCombatScroll(input);
                    }
                    // Other input is handled internally by the managers
                    break;
                default:
                    ShowMessage($"Unknown state: {stateManager.CurrentState}");
                    break;
            }
        }

        public Task HandleEscapeKey()
        {
            switch (stateManager.CurrentState)
            {
                case GameState.Inventory:
                case GameState.CharacterInfo:
                case GameState.Settings:
                    stateManager.TransitionToState(GameState.MainMenu);
                    mainMenuHandler?.ShowMainMenu();
                    break;
                case GameState.DungeonSelection:
                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoop();
                    break;
                case GameState.Testing:
                    stateManager.TransitionToState(GameState.Settings);
                    settingsMenuHandler?.ShowSettings();
                    break;
                case GameState.DeveloperMenu:
                    stateManager.TransitionToState(GameState.Settings);
                    settingsMenuHandler?.ShowSettings();
                    break;
                case GameState.VariableEditor:
                case GameState.ActionEditor:
                    stateManager.TransitionToState(GameState.DeveloperMenu);
                    developerMenuHandler?.ShowDeveloperMenu();
                    break;
                case GameState.CreateAction:
                    stateManager.TransitionToState(GameState.ActionEditor);
                    actionEditorHandler?.ShowActionEditor();
                    break;
                case GameState.ViewAction:
                    // Return to action list (which will be handled by ActionEditorHandler)
                    if (actionEditorHandler != null)
                    {
                        actionEditorHandler.HandleActionDetailInput("0");
                    }
                    break;
                default:
                    stateManager.TransitionToState(GameState.MainMenu);
                    mainMenuHandler?.ShowMainMenu();
                    break;
            }
            
            return Task.CompletedTask;
        }

        public void SetUIManager(IUIManager uiManager)
        {
            customUIManager = uiManager;
            UIManager.SetCustomUIManager(uiManager);
            InitializeHandlers(uiManager);
            
            // Wire up state manager to UI coordinator for event-driven animations
            // This allows animation manager to automatically stop when state changes
            if (uiManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SetStateManager(stateManager);
            }
        }

        // Display delegation methods
        public void ShowCharacterInfo()
        {
            characterMenuHandler?.ShowCharacterInfo();
        }

        public void ShowInventory()
        {
            // Delegate to centralized screen coordinator to keep
            // all Inventory screen logic in one place.
            screenCoordinator.ShowInventory();
        }

        public void ShowGameLoop()
        {
            // Delegate to centralized screen coordinator to keep
            // all GameLoop screen logic in one place.
            screenCoordinator.ShowGameLoop();
        }

        public void ShowSettings()
        {
            settingsMenuHandler?.ShowSettings();
        }

        public void ShowDeveloperMenu()
        {
            developerMenuHandler?.ShowDeveloperMenu();
        }

        public void ShowVariableEditor()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderVariableEditor();
                stateManager.TransitionToState(GameState.VariableEditor);
            }
        }

        public void ShowActionEditor()
        {
            actionEditorHandler?.ShowActionEditor();
        }

        public void UpdateActionFormInput(string input)
        {
            actionEditorHandler?.UpdateFormInput(input);
        }

        public void ShowDungeonCompletion(int xpGained, Item? lootReceived)
        {
            // Delegate to centralized screen coordinator so that
            // dungeon completion rendering and state logic live
            // in a single, testable component.
            screenCoordinator.ShowDungeonCompletion(xpGained, lootReceived);
        }

        public void ShowDeathScreen(Character player)
        {
            stateManager.TransitionToState(GameState.Death);
            
            // Display the death screen with statistics
            deathScreenHandler?.ShowDeathScreen(player);
        }

        public void ShowMessage(string message)
        {
            // Check if this is an invalid key message - if so, show it at the bottom instead of full screen
            if (message.Contains("Invalid", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            {
                ShowInvalidKeyMessage(message);
                return;
            }
            
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowMessage(message);
            }
        }
        
        /// <summary>
        /// Shows an invalid key message at the bottom of the screen without clearing the display
        /// This allows users to still see the available menu options
        /// </summary>
        public void ShowInvalidKeyMessage(string message)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowInvalidKeyMessage(message);
            }
        }

        /// <summary>
        /// Handles scrolling during combat and testing
        /// </summary>
        /// <param name="input">"up" to scroll up, "down" to scroll down</param>
        private void HandleCombatScroll(string input)
        {
            ScrollDebugLogger.Log($"HandleCombatScroll: input='{input}', UI manager type={customUIManager?.GetType().Name ?? "null"}");
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                DebugLogger.Log("Game", $"HandleCombatScroll: input='{input}', UI manager type={customUIManager?.GetType().Name}");
                if (input == "up")
                {
                    ScrollDebugLogger.Log("HandleCombatScroll: Calling canvasUI.ScrollUp()");
                    DebugLogger.Log("Game", "Scrolling up");
                    canvasUI.ScrollUp();
                }
                else if (input == "down")
                {
                    ScrollDebugLogger.Log("HandleCombatScroll: Calling canvasUI.ScrollDown()");
                    DebugLogger.Log("Game", "Scrolling down");
                    canvasUI.ScrollDown();
                }
            }
            else
            {
                ScrollDebugLogger.Log($"HandleCombatScroll: ERROR - customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
                DebugLogger.Log("Game", $"HandleCombatScroll: customUIManager is not CanvasUICoordinator (type={customUIManager?.GetType().Name ?? "null"})");
            }
        }

        public void ExitGame()
        {
            settingsMenuHandler?.ExitGame();
        }

        public void SaveGame()
        {
            settingsMenuHandler?.SaveGame();
        }
    }
}
