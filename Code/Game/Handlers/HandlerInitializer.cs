using System;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame.Handlers
{
    /// <summary>
    /// Initializes and wires up all game handlers
    /// </summary>
    public static class HandlerInitializer
    {
        /// <summary>
        /// Result containing all initialized handlers
        /// </summary>
        public class HandlerInitializationResult
        {
            public MainMenuHandler? MainMenuHandler { get; set; }
            public CharacterMenuHandler? CharacterMenuHandler { get; set; }
            public SettingsMenuHandler? SettingsMenuHandler { get; set; }
            public InventoryMenuHandler? InventoryMenuHandler { get; set; }
            public WeaponSelectionHandler? WeaponSelectionHandler { get; set; }
            public CharacterCreationHandler? CharacterCreationHandler { get; set; }
            public GameLoopInputHandler? GameLoopInputHandler { get; set; }
            public DungeonSelectionHandler? DungeonSelectionHandler { get; set; }
            public DungeonRunnerManager? DungeonRunnerManager { get; set; }
            public DungeonCompletionHandler? DungeonCompletionHandler { get; set; }
            public DeathScreenHandler? DeathScreenHandler { get; set; }
            public TestingSystemHandler? TestingSystemHandler { get; set; }
        }

        /// <summary>
        /// Creates all handler instances
        /// </summary>
        public static HandlerInitializationResult CreateHandlers(
            GameStateManager stateManager,
            GameInitializationManager initializationManager,
            GameInitializer gameInitializer,
            DungeonManagerWithRegistry dungeonManager,
            CombatManager combatManager,
            GameNarrativeManager narrativeManager,
            IUIManager? uiManager)
        {
            return new HandlerInitializationResult
            {
                MainMenuHandler = new MainMenuHandler(stateManager, initializationManager, uiManager, gameInitializer),
                CharacterMenuHandler = new CharacterMenuHandler(stateManager, uiManager),
                SettingsMenuHandler = new SettingsMenuHandler(stateManager, uiManager),
                InventoryMenuHandler = new InventoryMenuHandler(stateManager, uiManager),
                WeaponSelectionHandler = new WeaponSelectionHandler(stateManager, initializationManager, uiManager),
                CharacterCreationHandler = new CharacterCreationHandler(stateManager, uiManager),
                GameLoopInputHandler = new GameLoopInputHandler(stateManager),
                DungeonSelectionHandler = new DungeonSelectionHandler(stateManager, dungeonManager, uiManager),
                DungeonRunnerManager = new DungeonRunnerManager(stateManager, narrativeManager, combatManager, uiManager),
                DungeonCompletionHandler = new DungeonCompletionHandler(stateManager),
                DeathScreenHandler = new DeathScreenHandler(stateManager),
                TestingSystemHandler = new TestingSystemHandler(stateManager, uiManager)
            };
        }

        /// <summary>
        /// Wires up all handler events
        /// </summary>
        public static void WireHandlerEvents(
            HandlerInitializationResult handlers,
            GameStateManager stateManager,
            IUIManager? customUIManager,
            System.Action showGameLoop,
            System.Action showMainMenu,
            System.Action showInventory,
            System.Action showCharacterInfo,
            System.Action<string> showMessage,
            System.Action exitGame,
            Func<Task> showDungeonSelection,
            System.Action<int, Item?> showDungeonCompletion,
            System.Action<Character> showDeathScreen,
            System.Action saveGame)
        {
            if (handlers.MainMenuHandler != null)
            {
                handlers.MainMenuHandler.ShowGameLoopEvent += () => showGameLoop();
                handlers.MainMenuHandler.ShowWeaponSelectionEvent += () => handlers.WeaponSelectionHandler?.ShowWeaponSelection();
                handlers.MainMenuHandler.ShowSettingsEvent += () => handlers.SettingsMenuHandler?.ShowSettings();
                handlers.MainMenuHandler.ExitGameEvent += () => exitGame();
                handlers.MainMenuHandler.ShowMessageEvent += (msg) => showMessage(msg);
            }
            
            if (handlers.CharacterMenuHandler != null)
            {
                handlers.CharacterMenuHandler.ShowMainMenuEvent += () => showMainMenu();
            }
            
            if (handlers.SettingsMenuHandler != null)
            {
                handlers.SettingsMenuHandler.ShowMainMenuEvent += () => showMainMenu();
                handlers.SettingsMenuHandler.ShowTestingMenuEvent += () => handlers.TestingSystemHandler?.ShowTestingMenu();
            }
            
            if (handlers.WeaponSelectionHandler != null)
            {
                handlers.WeaponSelectionHandler.ShowCharacterCreationEvent += () => handlers.CharacterCreationHandler?.ShowCharacterCreation();
                handlers.WeaponSelectionHandler.ShowMessageEvent += (msg) => showMessage(msg);
            }
            
            if (handlers.CharacterCreationHandler != null)
            {
                handlers.CharacterCreationHandler.StartGameLoopEvent += () => showGameLoop();
                handlers.CharacterCreationHandler.ShowMessageEvent += (msg) => showMessage(msg);
            }
            
            if (handlers.InventoryMenuHandler != null)
            {
                handlers.InventoryMenuHandler.ShowInventoryEvent += () => showInventory();
                handlers.InventoryMenuHandler.ShowGameLoopEvent += () => showGameLoop();
                handlers.InventoryMenuHandler.ShowMainMenuEvent += () => showMainMenu();
                handlers.InventoryMenuHandler.ShowMessageEvent += (msg) => showMessage(msg);
                handlers.InventoryMenuHandler.ExitGameEvent += () => exitGame();
            }
            
            if (handlers.GameLoopInputHandler != null)
            {
                handlers.GameLoopInputHandler.SelectDungeonEvent += async () => await (showDungeonSelection?.Invoke() ?? Task.CompletedTask);
                handlers.GameLoopInputHandler.ShowInventoryEvent += () => showInventory();
                handlers.GameLoopInputHandler.ExitGameEvent += () => exitGame();
            }
            
            if (handlers.DungeonSelectionHandler != null)
            {
                handlers.DungeonSelectionHandler.ShowGameLoopEvent += () => showGameLoop();
                handlers.DungeonSelectionHandler.ShowMessageEvent += (msg) => showMessage(msg);
                
                // Wire up dungeon start to the dungeon runner manager
                if (handlers.DungeonRunnerManager != null)
                {
                    handlers.DungeonSelectionHandler.StartDungeonEvent += async () => 
                    {
                        try
                        {
                            DebugLogger.Log("Game", "StartDungeonEvent fired - calling DungeonRunnerManager.RunDungeon()");
                            await handlers.DungeonRunnerManager.RunDungeon();
                            DebugLogger.Log("Game", "DungeonRunnerManager.RunDungeon() completed");
                            
                            // If we're still in Dungeon state after RunDungeon completes, something went wrong
                            if (stateManager.CurrentState == GameState.Dungeon || stateManager.CurrentState == GameState.Combat)
                            {
                                DebugLogger.Log("Game", "WARNING: Still in Dungeon/Combat state after RunDungeon completed. Returning to dungeon selection.");
                                stateManager.TransitionToState(GameState.DungeonSelection);
                                if (customUIManager is CanvasUICoordinator canvasUI && stateManager.CurrentPlayer != null)
                                {
                                    canvasUI.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Log("Game", $"ERROR: Exception in StartDungeonEvent handler: {ex.Message}");
                            DebugLogger.Log("Game", $"Stack trace: {ex.StackTrace}");
                            if (customUIManager is CanvasUICoordinator canvasUIError)
                            {
                                canvasUIError.WriteLine($"ERROR: Failed to start dungeon: {ex.Message}", UIMessageType.System);
                            }
                            
                            // Return to dungeon selection on error
                            stateManager.TransitionToState(GameState.DungeonSelection);
                            if (customUIManager is CanvasUICoordinator canvasUIError2 && stateManager.CurrentPlayer != null)
                            {
                                canvasUIError2.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
                            }
                        }
                    };
                    DebugLogger.Log("Game", "StartDungeonEvent subscribed to DungeonRunnerManager.RunDungeon()");
                }
                else
                {
                    DebugLogger.Log("Game", "ERROR: dungeonRunnerManager is null!");
                }
            }
            
            if (handlers.DungeonRunnerManager != null)
            {
                handlers.DungeonRunnerManager.DungeonCompletedEvent += (xpGained, lootReceived) => showDungeonCompletion(xpGained, lootReceived);
                handlers.DungeonRunnerManager.ShowDeathScreenEvent += (player) => showDeathScreen(player);
            }
            
            if (handlers.DungeonCompletionHandler != null)
            {
                handlers.DungeonCompletionHandler.StartDungeonSelectionEvent += () => { handlers.DungeonSelectionHandler?.ShowDungeonSelection(); return Task.CompletedTask; };
                handlers.DungeonCompletionHandler.ShowInventoryEvent += () => showInventory();
                handlers.DungeonCompletionHandler.ShowMainMenuEvent += () => showMainMenu();
                handlers.DungeonCompletionHandler.SaveGameEvent += async () => { saveGame(); await Task.CompletedTask; };
            }
            
            if (handlers.DeathScreenHandler != null)
            {
                handlers.DeathScreenHandler.ShowMainMenuEvent += () => showMainMenu();
            }
            
            if (handlers.TestingSystemHandler != null)
            {
                handlers.TestingSystemHandler.ShowMainMenuEvent += () => handlers.SettingsMenuHandler?.ShowSettings();
            }
        }
    }
}

