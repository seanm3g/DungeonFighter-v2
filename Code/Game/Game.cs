namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;
    using RPGGame.GameCore.Input;
    using DungeonFighter.Game.Menu.Routing;
    using DungeonFighter.Game.Menu.Core;
    using RPGGame.Game.Services;
    using ActionDelegate = System.Action;

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
    public class GameCoordinator
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
        private BattleStatisticsHandler? battleStatisticsHandler;
        private TuningParametersHandler? tuningParametersHandler;
        private VariableEditorHandler? variableEditorHandler;
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
        
        // Input handlers (Phase 2 Refactoring)
        private RPGGame.GameCore.Input.GameInputRouter? inputRouter;
        private RPGGame.GameCore.Input.EscapeKeyHandler? escapeKeyHandler;

        // Constructor 1: Default
        public GameCoordinator()
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
        public GameCoordinator(IUIManager uiManager)
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
        public GameCoordinator(Character existingCharacter)
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

        // Initialize all handler managers
        private void InitializeHandlers(IUIManager? uiManager)
        {
            var result = HandlerInitializationService.Initialize(
                stateManager,
                initializationManager,
                gameInitializer,
                dungeonManager!,
                combatManager!,
                narrativeManager,
                uiManager,
                ShowGameLoop,
                ShowMainMenu,
                ShowInventory,
                ShowCharacterInfo,
                ShowMessage,
                ExitGame,
                async () => await (dungeonSelectionHandler?.ShowDungeonSelection() ?? Task.CompletedTask),
                ShowDungeonCompletion,
                ShowDeathScreen,
                SaveGame,
                ShowVariableEditor,
                ShowActionEditor,
                ShowTuningParameters,
                HandleCombatScroll);
            
            // Assign to instance fields
            mainMenuHandler = result.MainMenuHandler;
            characterMenuHandler = result.CharacterMenuHandler;
            settingsMenuHandler = result.SettingsMenuHandler;
            developerMenuHandler = result.DeveloperMenuHandler;
            actionEditorHandler = result.ActionEditorHandler;
            battleStatisticsHandler = result.BattleStatisticsHandler;
            tuningParametersHandler = result.TuningParametersHandler;
            variableEditorHandler = result.VariableEditorHandler;
            inventoryMenuHandler = result.InventoryMenuHandler;
            weaponSelectionHandler = result.WeaponSelectionHandler;
            characterCreationHandler = result.CharacterCreationHandler;
            gameLoopInputHandler = result.GameLoopInputHandler;
            dungeonSelectionHandler = result.DungeonSelectionHandler;
            dungeonRunnerManager = result.DungeonRunnerManager;
            dungeonCompletionHandler = result.DungeonCompletionHandler;
            deathScreenHandler = result.DeathScreenHandler;
            testingSystemHandler = result.TestingSystemHandler;
            menuInputRouter = result.MenuInputRouter;
            menuInputValidator = result.MenuInputValidator;
            inputRouter = result.InputRouter;
            escapeKeyHandler = result.EscapeKeyHandler;
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
        public GameStateManager StateManager => stateManager;
        public TestingSystemHandler? TestingSystemHandler => testingSystemHandler;
        public DeveloperMenuHandler? DeveloperMenuHandler => developerMenuHandler;

        // Main entry points
        public void ShowMainMenu()
        {
            mainMenuHandler?.ShowMainMenu();
        }

        public async Task HandleInput(string input)
        {
            if (inputRouter != null)
            {
                await inputRouter.RouteInput(input);
            }
        }

        public Task HandleEscapeKey()
        {
            if (escapeKeyHandler != null)
            {
                return escapeKeyHandler.HandleEscapeKey();
            }
            return Task.CompletedTask;
        }

        public void SetUIManager(IUIManager uiManager)
        {
            customUIManager = uiManager;
            UIManager.SetCustomUIManager(uiManager);
            InitializeHandlers(uiManager); // This already initializes input handlers via HandlerInitializationService
            
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
            variableEditorHandler?.ShowVariableEditor();
        }

        public void ShowTuningParameters()
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.RenderTuningParametersMenu();
                stateManager.TransitionToState(GameState.TuningParameters);
            }
        }
        
        public TuningParametersHandler? GetTuningParametersHandler()
        {
            return tuningParametersHandler;
        }

        public void ShowActionEditor()
        {
            actionEditorHandler?.ShowActionEditor();
        }
        
        public void ShowBattleStatistics()
        {
            battleStatisticsHandler?.ShowBattleStatisticsMenu();
        }

        public void UpdateActionFormInput(string input)
        {
            actionEditorHandler?.UpdateFormInput(input);
        }

        public void ShowDungeonCompletion(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun)
        {
            // Delegate to centralized screen coordinator so that
            // dungeon completion rendering and state logic live
            // in a single, testable component.
            screenCoordinator.ShowDungeonCompletion(xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun);
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
        /// <param name="input">"up" to scroll up, "down" to scroll down, "pageup" to scroll up by page, "pagedown" to scroll down by page</param>
        private void HandleCombatScroll(string input)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                string normalizedInput = input?.Trim().ToLowerInvariant() ?? "";
                
                if (normalizedInput == "up")
                {
                    canvasUI.ScrollUp();
                }
                else if (normalizedInput == "down")
                {
                    canvasUI.ScrollDown();
                }
                else if (normalizedInput == "pageup")
                {
                    canvasUI.ScrollUp(30);
                }
                else if (normalizedInput == "pagedown")
                {
                    canvasUI.ScrollDown(30);
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
