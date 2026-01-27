using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Handles loading saved game data and characters.
    /// Extracted from MainMenuHandler to improve Single Responsibility Principle compliance.
    /// </summary>
    public class GameLoader
    {
        private readonly GameStateManager stateManager;
        private readonly GameInitializer gameInitializer;
        private readonly IUIManager? customUIManager;

        public GameLoader(
            GameStateManager stateManager,
            GameInitializer gameInitializer,
            IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.gameInitializer = gameInitializer ?? throw new ArgumentNullException(nameof(gameInitializer));
            this.customUIManager = customUIManager;
        }

        /// <summary>
        /// Loads a saved game
        /// </summary>
        /// <param name="onMessage">Callback for status messages</param>
        /// <param name="onGameLoop">Callback to show game loop</param>
        /// <param name="onShowMainMenu">Callback to show main menu</param>
        /// <returns>True if load was successful, false otherwise</returns>
        public async Task<bool> LoadGame(
            System.Action<string> onMessage,
            System.Action onGameLoop,
            System.Action onShowMainMenu)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:36", message = "LoadGame ENTRY", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            DebugLogger.Log("GameLoader", "LoadGame() called");
            // Don't call onMessage("Loading game...") here - it triggers ShowLoadingStatus in the callback
            // which can appear after the main menu is rendered, causing the loading message to persist
            // onMessage("Loading game...");
            
            try
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:47", message = "Checking CurrentPlayer", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), currentPlayerIsNull = (stateManager.CurrentPlayer == null), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                if (stateManager.CurrentPlayer != null)
                {
                    DebugLogger.Log("GameLoader", "Character already loaded in state manager, transitioning to game loop");
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:50", message = "CurrentPlayer exists, returning early", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    // Character already loaded, go to game loop
                    stateManager.TransitionToState(GameState.GameLoop);
                    try
                    {
                        onGameLoop();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log("GameLoader", $"Error in onGameLoop callback (character already loaded): {ex.Message}\n{ex.StackTrace}");
                        onMessage($"Error: {ex.Message}");
                    }
                    return true;
                }
                
                // Check if save file exists first
                DebugLogger.Log("GameLoader", "Checking if save file exists...");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:66", message = "BEFORE SaveFileExists", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                bool saveExists = CharacterSaveManager.SaveFileExists();
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:68", message = "AFTER SaveFileExists", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), saveExists, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                DebugLogger.Log("GameLoader", $"Save file exists: {saveExists}");
                
                if (!saveExists)
                {
                    DebugLogger.Log("GameLoader", "No save file found, showing main menu");
                    onMessage("No saved game found. Please start a new game.");
                    onShowMainMenu();
                    return false;
                }
                
                // Show loading message immediately - use ShowLoadingStatus instead of ShowLoadingAnimation
                // ShowLoadingAnimation shows a full-screen animation that can persist even after canvas.Clear()
                // ShowLoadingStatus only shows a bottom status message that's easier to clear
                // Must call on UI thread since we may be on a background thread
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:81", message = "BEFORE ShowLoadingStatus in LoadGame", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    // Use Post to ensure UI thread execution - don't block the background thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        canvasUI.ShowLoadingStatus("Loading character...");
                    });
                }
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:85", message = "AFTER ShowLoadingStatus in LoadGame (posted to UI thread)", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                
                DebugLogger.Log("GameLoader", "Save file exists, starting load...");
                
                Character? loadedCharacter = null;
                try
                {
                    DebugLogger.Log("GameLoader", "Attempting to load default save file...");
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "GameLoader.cs:91", message = "BEFORE LoadCharacterAsync", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    // Try to load the default save file first
                    loadedCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H1,H2", location = "GameLoader.cs:94", message = "AFTER LoadCharacterAsync", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), loadedCharacterName = loadedCharacter?.Name, loadedCharacterIsNull = (loadedCharacter == null), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                    DebugLogger.Log("GameLoader", loadedCharacter != null 
                        ? $"Successfully loaded default character: {loadedCharacter.Name}" 
                        : "Default save file not found or empty");
                    
                    // If default save doesn't exist, try loading the most recent character
                    if (loadedCharacter == null)
                    {
                        DebugLogger.Log("GameLoader", "Checking for saved characters...");
                        var savedCharacterInfos = CharacterSaveManager.ListAllSavedCharacters();
                        DebugLogger.Log("GameLoader", $"Found {savedCharacterInfos?.Count ?? 0} saved character(s)");
                        
                        if (savedCharacterInfos != null && savedCharacterInfos.Count > 0)
                        {
                            // Get the most recently saved character (first in the sorted list)
                            var mostRecentCharacterInfo = savedCharacterInfos[0];
                            DebugLogger.Log("GameLoader", $"Loading most recent character: {mostRecentCharacterInfo.characterName} (ID: {mostRecentCharacterInfo.characterId})");
                            loadedCharacter = await Character.LoadCharacterAsync(mostRecentCharacterInfo.characterId).ConfigureAwait(false);
                            DebugLogger.Log("GameLoader", loadedCharacter != null 
                                ? $"Successfully loaded character: {loadedCharacter.Name}" 
                                : "Failed to load character from ID");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("GameLoader", $"Exception during character load: {ex.Message}\n{ex.StackTrace}");
                    // Clear loading status on error - ensure it's cleared in all error paths
                    // Must call on UI thread since we may be on a background thread
                    if (customUIManager is CanvasUICoordinator canvasUIError)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            canvasUIError.ClearLoadingStatus();
                        });
                    }
                    onMessage($"Error loading character: {ex.Message}");
                    onShowMainMenu();
                    return false;
                }
                
                if (loadedCharacter == null)
                {
                    DebugLogger.Log("GameLoader", "Failed to load character - no valid save file found");
                    // Clear loading status - must call on UI thread
                    if (customUIManager is CanvasUICoordinator canvasUINull)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            canvasUINull.ClearLoadingStatus();
                        });
                    }
                    onMessage("No saved game found. Please start a new game.");
                    onShowMainMenu();
                    return false;
                }
                
                DebugLogger.Log("GameLoader", $"Loaded character: {loadedCharacter.Name}, Level {loadedCharacter.Level}");
                
                // Initialize game data
                DebugLogger.Log("GameLoader", "Initializing game data...");
                try
                {
                    gameInitializer.InitializeExistingGame(loadedCharacter, stateManager.AvailableDungeons);
                    DebugLogger.Log("GameLoader", $"Game data initialized. Dungeons count: {stateManager.AvailableDungeons.Count}");
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("GameLoader", $"Error initializing game data: {ex.Message}\n{ex.StackTrace}");
                    // Clear loading status on initialization error - must call on UI thread
                    if (customUIManager is CanvasUICoordinator canvasUIInitError)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            canvasUIInitError.ClearLoadingStatus();
                        });
                    }
                    onMessage($"Error initializing game: {ex.Message}");
                    onShowMainMenu();
                    return false;
                }
                
                // Register character in state manager
                stateManager.AddCharacter(loadedCharacter);
                stateManager.SetCurrentPlayer(loadedCharacter);
                DebugLogger.Log("GameLoader", $"Set active character: {loadedCharacter.Name} (Level {loadedCharacter.Level})");
                
                // Clear loading status and update UI - must call on UI thread
                if (customUIManager is CanvasUICoordinator canvasUIUpdate)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        canvasUIUpdate.ClearLoadingStatus();
                        canvasUIUpdate.RestoreDisplayBufferRendering();
                        canvasUIUpdate.SetCharacter(loadedCharacter);
                        canvasUIUpdate.RefreshCharacterPanel();
                    });
                }
                
                // Apply health multiplier if configured
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    loadedCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                onMessage($"Welcome back, {loadedCharacter.Name}!");
                
                // Transition to game loop
                stateManager.TransitionToState(GameState.GameLoop);
                DebugLogger.Log("GameLoader", $"Firing onGameLoop callback for character: {loadedCharacter.Name}");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:189", message = "BEFORE onGameLoop() call", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), characterName = loadedCharacter.Name, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                try
                {
                    onGameLoop();
                    // #region agent log
                    try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:194", message = "AFTER onGameLoop() call", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                    // #endregion
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("GameLoader", $"Error in onGameLoop callback: {ex.Message}\n{ex.StackTrace}");
                    // Clear loading status if game loop callback fails - must call on UI thread
                    if (customUIManager is CanvasUICoordinator canvasUIGameLoopError)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            canvasUIGameLoopError.ClearLoadingStatus();
                        });
                    }
                    onMessage($"Error: {ex.Message}");
                    onShowMainMenu();
                    return false;
                }
                
                DebugLogger.Log("GameLoader", "LoadGame completed successfully");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:227", message = "LoadGame EXIT (success)", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("GameLoader", $"Unexpected error in LoadGame: {ex.Message}\n{ex.StackTrace}");
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "H4", location = "GameLoader.cs:232", message = "LoadGame EXCEPTION", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), exception = ex.Message, stackTrace = ex.StackTrace, threadId = System.Threading.Thread.CurrentThread.ManagedThreadId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                // Ensure loading status is cleared even in outer catch block - must call on UI thread
                if (customUIManager is CanvasUICoordinator canvasUIOuterError)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        canvasUIOuterError.ClearLoadingStatus();
                    });
                }
                string errorMsg = $"Error loading game: {ex.Message}";
                onMessage(errorMsg);
                stateManager.TransitionToState(GameState.MainMenu);
                onShowMainMenu();
                return false;
            }
        }
    }
}

