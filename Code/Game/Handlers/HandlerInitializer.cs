using System;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;
using System.IO;
using System.Text.Json;

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
            public CharacterManagementHandler? CharacterManagementHandler { get; set; }
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
                CharacterManagementHandler = new CharacterManagementHandler(stateManager, uiManager, initializationManager)
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
            System.Action<int, Item?, List<LevelUpInfo>, List<Item>> showDungeonCompletion,
            System.Action<Character> showDeathScreen,
            System.Action saveGame)
        {
            if (handlers.MainMenuHandler != null)
            {
                handlers.MainMenuHandler.ShowGameLoopEvent += () => showGameLoop();
                handlers.MainMenuHandler.ShowWeaponSelectionEvent += () => 
                {
                    if (handlers.WeaponSelectionHandler != null)
                    {
                        handlers.WeaponSelectionHandler.ShowWeaponSelection();
                    }
                    else
                    {
                        // WeaponSelectionHandler is null - event will be ignored
                    }
                };
                handlers.MainMenuHandler.ShowSettingsEvent += () => handlers.SettingsMenuHandler?.ShowSettings();
                handlers.MainMenuHandler.ShowCharacterSelectionEvent += () => handlers.CharacterManagementHandler?.ShowCharacterSelection();
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
            }
            
            if (handlers.GameLoopInputHandler != null)
            {
                handlers.GameLoopInputHandler.SelectDungeonEvent += async () => await (showDungeonSelection?.Invoke() ?? Task.CompletedTask);
                handlers.GameLoopInputHandler.ShowInventoryEvent += () => showInventory();
                handlers.GameLoopInputHandler.ShowCharacterSelectionEvent += () => handlers.CharacterManagementHandler?.ShowCharacterSelection();
                handlers.GameLoopInputHandler.ExitGameEvent += () => exitGame();
                handlers.GameLoopInputHandler.ShowMainMenuEvent += () => showMainMenu();
            }
            
            if (handlers.CharacterManagementHandler != null)
            {
                handlers.CharacterManagementHandler.ShowGameLoopEvent += () => showGameLoop();
                handlers.CharacterManagementHandler.ShowMainMenuEvent += () => showMainMenu();
                handlers.CharacterManagementHandler.ShowWeaponSelectionEvent += () => 
                {
                    if (handlers.WeaponSelectionHandler != null)
                    {
                        handlers.WeaponSelectionHandler.ShowWeaponSelection();
                    }
                };
                handlers.CharacterManagementHandler.ShowMessageEvent += (msg) => showMessage(msg);
                // Character creation will be handled by CharacterManagementHandler itself
                // The ShowCharacterCreationEvent is optional and can be null
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
                        // #region agent log
                        try { 
                            var logDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".cursor");
                            if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
                            var logPath = System.IO.Path.Combine(logDir, "debug.log");
                            System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "HandlerInitializer.cs:StartDungeonEvent", message = "RunDungeon starting", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); 
                            DebugLogger.WriteDebugAlways($"[DEBUG] HandlerInitializer: RunDungeon starting, state={stateManager.CurrentState}");
                        } catch (Exception logEx) { DebugLogger.WriteDebugAlways($"[DEBUG] Logging error: {logEx.Message}"); }
                        // #endregion
                        try
                        {
                            await handlers.DungeonRunnerManager.RunDungeon();
                            
                            // #region agent log
                            try { 
                                var logDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".cursor");
                                if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
                                var logPath = System.IO.Path.Combine(logDir, "debug.log");
                                System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "HandlerInitializer.cs:StartDungeonEvent", message = "RunDungeon completed", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "A" }) + "\n"); 
                                DebugLogger.WriteDebugAlways($"[DEBUG] HandlerInitializer: RunDungeon completed, state={stateManager.CurrentState}");
                            } catch (Exception logEx) { DebugLogger.WriteDebugAlways($"[DEBUG] Logging error: {logEx.Message}"); }
                            // #endregion
                            
                            // NOTE: Removed incorrect state check that was causing premature transitions to DungeonSelection.
                            // DungeonOrchestrator.RunDungeon() properly manages state transitions:
                            // - DungeonCompletion when dungeon completes successfully
                            // - GameLoop when player exits early
                            // - Death when player dies
                            // The previous check was incorrectly triggering during combat, causing the bug.
                        }
                        catch (Exception ex)
                        {
                            // #region agent log
                            try { 
                                string? stackTrace = ex.StackTrace != null && ex.StackTrace.Length > 500 ? ex.StackTrace.Substring(0, 500) : ex.StackTrace;
                                var logDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".cursor");
                                if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
                                var logPath = System.IO.Path.Combine(logDir, "debug.log");
                                var loggedState = stateManager.CurrentState.ToString();
                                System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "HandlerInitializer.cs:StartDungeonEvent", message = "Exception caught", data = new { exceptionType = ex.GetType().Name, exceptionMessage = ex.Message, currentState = loggedState, stackTrace }, sessionId = "debug-session", runId = "run1", hypothesisId = "C" }) + "\n"); 
                                DebugLogger.WriteDebugAlways($"[DEBUG] HandlerInitializer: Exception caught: {ex.GetType().Name} - {ex.Message}, state={loggedState}");
                            } catch (Exception logEx) { DebugLogger.WriteDebugAlways($"[DEBUG] Logging error: {logEx.Message}"); }
                            // #endregion
                            
                            // Only transition to DungeonSelection if we're not in the middle of combat or dungeon
                            // If we're in Combat or Dungeon state, let the orchestrator handle the error
                            var currentState = stateManager.CurrentState;
                            if (currentState != GameState.Combat && currentState != GameState.Dungeon)
                            {
                                if (customUIManager is CanvasUICoordinator canvasUIError)
                                {
                                    canvasUIError.WriteLine($"ERROR: Failed to start dungeon: {ex.Message}", UIMessageType.System);
                                }
                                
                                // Return to dungeon selection on error (only if not in combat/dungeon)
                                stateManager.TransitionToState(GameState.DungeonSelection);
                                if (customUIManager is CanvasUICoordinator canvasUIError2 && stateManager.CurrentPlayer != null)
                                {
                                    canvasUIError2.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
                                }
                            }
                            else
                            {
                                // Log that we're ignoring the exception because we're in combat/dungeon
                                DebugLogger.WriteDebugAlways($"[DEBUG] HandlerInitializer: Ignoring exception during {currentState}, letting orchestrator handle it");
                            }
                        }
                    };
                }
            }
            
            if (handlers.DungeonRunnerManager != null)
            {
                handlers.DungeonRunnerManager.DungeonCompletedEvent += (xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun) => showDungeonCompletion(xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun);
                handlers.DungeonRunnerManager.ShowDeathScreenEvent += (player) => showDeathScreen(player);
                handlers.DungeonRunnerManager.DungeonExitedEarlyEvent += () => showGameLoop();
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
            
        }
    }
}

