namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using DungeonFighter.Game.Menu.Routing;
    using DungeonFighter.Game.Menu.Core;

    public enum GameState
    {
        MainMenu,
        WeaponSelection,
        CharacterCreation,
        GameLoop,
        Inventory,
        CharacterInfo,
        Settings,
        Testing,
        DungeonSelection,
        Dungeon,
        Combat,
        DungeonCompletion,
        Death
    }

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
        private GameLoopManager? gameLoopManager;
        private DungeonManagerWithRegistry? dungeonManager;
        private CombatManager? combatManager;
        
        // Other managers
        private GameMenuManager menuManager;
        private GameInitializer gameInitializer;
        private IUIManager? customUIManager;
        
        // NEW: Menu Input Framework (Phase 3 Refactoring)
        private MenuInputRouter? menuInputRouter;
        private MenuInputValidator? menuInputValidator;

        // Constructor 1: Default
        public Game()
        {
            GameTicker.Instance.Start();
            menuManager = new GameMenuManager();
            gameInitializer = new GameInitializer();
            
            // Initialize core managers
            inputHandler = new GameInputHandler(stateManager);
            gameLoopManager = new GameLoopManager();
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
            
            menuManager = new GameMenuManager();
            gameInitializer = new GameInitializer();
            
            // Initialize core managers
            inputHandler = new GameInputHandler(stateManager);
            gameLoopManager = new GameLoopManager();
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
            
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
            menuManager = new GameMenuManager();
            gameInitializer = new GameInitializer();
            
            // Initialize core managers
            inputHandler = new GameInputHandler(stateManager);
            stateManager.SetCurrentPlayer(existingCharacter);
            gameLoopManager = new GameLoopManager();
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
            mainMenuHandler = new MainMenuHandler(stateManager, initializationManager, uiManager, gameInitializer);
            characterMenuHandler = new CharacterMenuHandler(stateManager, uiManager);
            settingsMenuHandler = new SettingsMenuHandler(stateManager, uiManager);
            inventoryMenuHandler = new InventoryMenuHandler(stateManager, uiManager);
            weaponSelectionHandler = new WeaponSelectionHandler(stateManager, initializationManager, uiManager);
            characterCreationHandler = new CharacterCreationHandler(stateManager, uiManager);
            gameLoopInputHandler = new GameLoopInputHandler(stateManager);
            dungeonSelectionHandler = new DungeonSelectionHandler(stateManager, dungeonManager, uiManager);
            dungeonRunnerManager = new DungeonRunnerManager(stateManager, narrativeManager, combatManager, uiManager);
            dungeonCompletionHandler = new DungeonCompletionHandler(stateManager);
            deathScreenHandler = new DeathScreenHandler(stateManager);
            testingSystemHandler = new TestingSystemHandler(stateManager, uiManager);
            
            // NEW: Initialize Menu Input Framework (Phase 3 Refactoring)
            InitializeMenuInputFramework();
            
            // Wire up handler events
            if (mainMenuHandler != null)
            {
                mainMenuHandler.ShowGameLoopEvent += ShowGameLoop;
                mainMenuHandler.ShowWeaponSelectionEvent += () => weaponSelectionHandler?.ShowWeaponSelection();
                mainMenuHandler.ShowSettingsEvent += () => settingsMenuHandler?.ShowSettings();
                mainMenuHandler.ExitGameEvent += ExitGame;
                mainMenuHandler.ShowMessageEvent += ShowMessage;
            }
            
            if (characterMenuHandler != null)
            {
                characterMenuHandler.ShowMainMenuEvent += ShowMainMenu;
            }
            
            if (settingsMenuHandler != null)
            {
                settingsMenuHandler.ShowMainMenuEvent += ShowMainMenu;
                settingsMenuHandler.ShowTestingMenuEvent += () => testingSystemHandler?.ShowTestingMenu();
            }
            
            if (weaponSelectionHandler != null)
            {
                weaponSelectionHandler.ShowCharacterCreationEvent += () => characterCreationHandler?.ShowCharacterCreation();
                weaponSelectionHandler.ShowMessageEvent += ShowMessage;
            }
            
            if (characterCreationHandler != null)
            {
                characterCreationHandler.StartGameLoopEvent += ShowGameLoop;
                characterCreationHandler.ShowMessageEvent += ShowMessage;
            }
            
            if (inventoryMenuHandler != null)
            {
                inventoryMenuHandler.ShowInventoryEvent += ShowInventory;
                inventoryMenuHandler.ShowGameLoopEvent += ShowGameLoop;
                inventoryMenuHandler.ShowMainMenuEvent += ShowMainMenu;
                inventoryMenuHandler.ShowMessageEvent += ShowMessage;
            }
            
            if (gameLoopInputHandler != null)
            {
                gameLoopInputHandler.SelectDungeonEvent += async () => await (dungeonSelectionHandler?.ShowDungeonSelection() ?? Task.CompletedTask);
                gameLoopInputHandler.ShowInventoryEvent += ShowInventory;
                gameLoopInputHandler.ShowCharacterInfoEvent += ShowCharacterInfo;
            }
            
            if (dungeonSelectionHandler != null)
            {
                dungeonSelectionHandler.ShowGameLoopEvent += ShowGameLoop;
                dungeonSelectionHandler.ShowMessageEvent += ShowMessage;
                // Wire up dungeon start to the dungeon runner manager
                if (dungeonRunnerManager != null)
                {
                    dungeonSelectionHandler.StartDungeonEvent += async () => 
                    {
                        DebugLogger.Log("Game", "StartDungeonEvent fired - calling DungeonRunnerManager.RunDungeon()");
                        await dungeonRunnerManager.RunDungeon();
                    };
                    DebugLogger.Log("Game", "StartDungeonEvent subscribed to DungeonRunnerManager.RunDungeon()");
                }
                else
                {
                    DebugLogger.Log("Game", "ERROR: dungeonRunnerManager is null!");
                }
            }
            
            if (dungeonRunnerManager != null)
            {
                dungeonRunnerManager.DungeonCompletedEvent += (xpGained, lootReceived) => ShowDungeonCompletion(xpGained, lootReceived);
                dungeonRunnerManager.ShowMainMenuEvent += ShowMainMenu;
                dungeonRunnerManager.ShowDeathScreenEvent += (player) => ShowDeathScreen(player);
            }
            
            if (dungeonCompletionHandler != null)
            {
                dungeonCompletionHandler.StartDungeonSelectionEvent += () => { dungeonSelectionHandler?.ShowDungeonSelection(); return Task.CompletedTask; };
                dungeonCompletionHandler.ShowInventoryEvent += ShowInventory;
                dungeonCompletionHandler.ShowMainMenuEvent += ShowMainMenu;
                dungeonCompletionHandler.SaveGameEvent += async () => { SaveGame(); await Task.CompletedTask; };
            }
            
            if (deathScreenHandler != null)
            {
                deathScreenHandler.ShowMainMenuEvent += ShowMainMenu;
            }
            
            if (testingSystemHandler != null)
            {
                testingSystemHandler.ShowMainMenuEvent += () => settingsMenuHandler?.ShowSettings();
            }
        }

        /// <summary>
        /// Initialize the Menu Input Framework (Phase 3 Refactoring).
        /// Sets up the router, validators, and handlers for unified input processing.
        /// </summary>
        private void InitializeMenuInputFramework()
        {
            try
            {
                // Create validator and router
                menuInputValidator = new MenuInputValidator();
                menuInputRouter = new MenuInputRouter(menuInputValidator);
                
                // Register validation rules for each menu
                menuInputValidator.RegisterRules(GameState.MainMenu, new MainMenuValidationRules());
                menuInputValidator.RegisterRules(GameState.CharacterCreation, new CharacterCreationValidationRules());
                menuInputValidator.RegisterRules(GameState.WeaponSelection, new WeaponSelectionValidationRules(4));
                menuInputValidator.RegisterRules(GameState.Inventory, new InventoryValidationRules());
                menuInputValidator.RegisterRules(GameState.Settings, new SettingsValidationRules());
                menuInputValidator.RegisterRules(GameState.DungeonSelection, new DungeonSelectionValidationRules(10));
                
                DebugLogger.Log("Game", "Menu Input Framework initialized successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.Log("Game", $"Error initializing Menu Input Framework: {ex.Message}");
            }
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
                    settingsMenuHandler?.HandleMenuInput(input);
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
                    if (testingSystemHandler != null)
                        await testingSystemHandler.HandleMenuInput(input);
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
        }

        // Display delegation methods
        public void ShowCharacterInfo()
        {
            characterMenuHandler?.ShowCharacterInfo();
        }

        public void ShowInventory()
        {
            inventoryMenuHandler?.ShowInventory();
        }

        public void ShowGameLoop()
        {
            var player = stateManager.CurrentPlayer;
            if (customUIManager is CanvasUICoordinator canvasUI && player != null)
            {
                canvasUI.RenderGameMenu(player, stateManager.CurrentInventory);
            }
            stateManager.TransitionToState(GameState.GameLoop);
        }

        public void ShowSettings()
        {
            settingsMenuHandler?.ShowSettings();
        }

        public void ShowDungeonCompletion(int xpGained, Item? lootReceived)
        {
            stateManager.TransitionToState(GameState.DungeonCompletion);
            
            // Display the dungeon completion screen with reward data
            if (customUIManager is CanvasUICoordinator canvasUI && stateManager.CurrentPlayer != null && stateManager.CurrentDungeon != null)
            {
                // Clear old interactive elements first
                canvasUI.ClearClickableElements();
                
                // Render the completion screen with reward data
                canvasUI.RenderDungeonCompletion(
                    stateManager.CurrentDungeon, 
                    stateManager.CurrentPlayer, 
                    xpGained, 
                    lootReceived
                );
            }
        }

        public void ShowDeathScreen(Character player)
        {
            stateManager.TransitionToState(GameState.Death);
            
            // Display the death screen with statistics
            deathScreenHandler?.ShowDeathScreen(player);
        }

        public void ShowMessage(string message)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowMessage(message);
            }
        }

        /// <summary>
        /// Handles scrolling during combat
        /// </summary>
        /// <param name="input">"up" to scroll up, "down" to scroll down</param>
        private void HandleCombatScroll(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                if (input == "up")
                {
                    canvasUI.ScrollUp();
                }
                else if (input == "down")
                {
                    canvasUI.ScrollDown();
                }
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
