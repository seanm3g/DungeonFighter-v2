namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles main menu display and input processing.
    /// Extracted from Game.cs to separate menu concerns.
    /// 
    /// Responsibilities:
    /// - Display main menu
    /// - Handle main menu input
    /// - Delegate to game startup or load
    /// </summary>
    public class MainMenuHandler
    {
        private GameStateManager stateManager;
        private GameInitializationManager initializationManager;
        private IUIManager? customUIManager;
        private GameInitializer gameInitializer;
        
        // Delegates for game actions
        public delegate void OnExitGame();
        public delegate void OnShowMessage(string message);
        public delegate void OnShowSettings();
        public delegate void OnShowGameLoop();
        public delegate void OnShowWeaponSelection();
        public delegate void OnShowCharacterSelection();
        
        public event OnExitGame? ExitGameEvent;
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowSettings? ShowSettingsEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowWeaponSelection? ShowWeaponSelectionEvent;
        public event OnShowCharacterSelection? ShowCharacterSelectionEvent;

        public MainMenuHandler(
            GameStateManager stateManager,
            GameInitializationManager initializationManager,
            IUIManager? customUIManager,
            GameInitializer gameInitializer)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.initializationManager = initializationManager ?? throw new ArgumentNullException(nameof(initializationManager));
            this.customUIManager = customUIManager;
            this.gameInitializer = gameInitializer ?? throw new ArgumentNullException(nameof(gameInitializer));
        }

        /// <summary>
        /// Display the main menu with saved game info
        /// </summary>
        public void ShowMainMenu()
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "MainMenuHandler.cs:ShowMainMenu", message = "ShowMainMenu called", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Suppress display buffer rendering FIRST before any operations that might trigger renders
                // This prevents auto-renders from interfering with menu rendering and causing screen flashing
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "MainMenuHandler.cs:ShowMainMenu", message = "About to render main menu", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
                // #endregion
                
                // Check if we have a saved game - prefer in-memory player, but also check disk
                bool hasSavedGame = false;
                string? characterName = null;
                int characterLevel = 0;
                
                // First check if player is already loaded in memory
                if (stateManager.CurrentPlayer != null)
                {
                    hasSavedGame = true;
                    characterName = stateManager.CurrentPlayer.Name;
                    characterLevel = stateManager.CurrentPlayer.Level;
                }
                else
                {
                    // Check if save file exists on disk
                    hasSavedGame = CharacterSaveManager.SaveFileExists();
                    if (hasSavedGame)
                    {
                        // Get character info from save file without loading the full character
                        (characterName, characterLevel) = CharacterSaveManager.GetSavedCharacterInfo();
                    }
                }
                
                canvasUI.RenderMainMenu(hasSavedGame, characterName, characterLevel);
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "MainMenuHandler.cs:ShowMainMenu", message = "Main menu rendered, transitioning state", data = new { currentState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
                // #endregion
            }
            stateManager.TransitionToState(GameState.MainMenu);
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "MainMenuHandler.cs:ShowMainMenu", message = "State transitioned to MainMenu", data = new { newState = stateManager.CurrentState.ToString() }, sessionId = "debug-session", runId = "run1", hypothesisId = "H4" }) + "\n"); } catch { }
            // #endregion
        }

        /// <summary>
        /// Handle main menu input (1=New, 2=Load, 3=Settings, 0=Quit)
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            DebugLogger.Log("MainMenuHandler", $"HandleMenuInput called with input: '{input}', current state: {stateManager.CurrentState}");
            
            // Trim whitespace to handle various input formats
            string trimmedInput = input?.Trim() ?? "";
            
            switch (trimmedInput)
            {
                case "1":
                    ShowMessageEvent?.Invoke("Starting new game...");
                    await StartNewGame();
                    break;
                case "2":
                    DebugLogger.Log("MainMenuHandler", "Load game option selected");
                    await LoadGame();
                    break;
                case "3":
                    HandleSettingsSelection();
                    break;
                case "4":
                case "C":
                    // Character Selection (multi-character support)
                    ShowCharacterSelectionEvent?.Invoke();
                    break;
                case "0":
                    HandleQuitSelection();
                    break;
                default:
                    ShowMessageEvent?.Invoke($"Invalid choice: '{input}'. Please select 1 (New), 2 (Load), 3 (Settings), 4 (Characters), or 0 (Quit).");
                    break;
            }
        }

        /// <summary>
        /// Start a new game - always creates a new character
        /// </summary>
        private async Task StartNewGame()
        {
            try
            {
                ShowMessageEvent?.Invoke("Starting new game...");
                
                // Clear any existing enemy from previous game session
                if (customUIManager is CanvasUICoordinator canvasUIClear)
                {
                    canvasUIClear.ClearCurrentEnemy();
                }
                
                // Create new character (without equipment yet)
                var newCharacter = new Character(null, 1); // null triggers random name generation
                
                // Register character in state manager (multi-character support)
                var characterId = stateManager.AddCharacter(newCharacter);
                stateManager.SetCurrentPlayer(newCharacter);
                
                var activeCharacter = stateManager.GetActiveCharacter();
                if (activeCharacter != null)
                {
                    // Suppress display buffer rendering FIRST before any operations that might trigger renders
                    // This prevents auto-renders from interfering with menu rendering and causing screen flashing
                    if (customUIManager is CanvasUICoordinator canvasUISuppress)
                    {
                        canvasUISuppress.SuppressDisplayBufferRendering();
                        canvasUISuppress.ClearDisplayBufferWithoutRender();
                    }
                    
                    // Set character in UI manager for persistent display
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.SetCharacter(activeCharacter);
                    }
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        activeCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    // Render weapon selection BEFORE transitioning state to prevent canvas clearing during transition
                    // This ensures the screen is rendered before any state-change-triggered clears happen
                    if (ShowWeaponSelectionEvent != null)
                    {
                        try
                        {
                            ShowWeaponSelectionEvent.Invoke();
                            
                            // Transition state AFTER rendering to prevent flashing
                            // Only transition if not already in WeaponSelection state
                            if (stateManager.CurrentState != GameState.WeaponSelection)
                            {
                                stateManager.TransitionToState(GameState.WeaponSelection);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessageEvent?.Invoke($"Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("Error: Weapon selection event not initialized. Please restart the game.");
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error starting game: {ex.Message}\n{ex.StackTrace}";
                ShowMessageEvent?.Invoke(errorMsg);
                stateManager.TransitionToState(GameState.MainMenu);
                ShowMainMenu();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Load a saved game
        /// </summary>
        private async Task LoadGame()
        {
            DebugLogger.Log("MainMenuHandler", "LoadGame() called");
            ShowMessageEvent?.Invoke("Loading game...");
            
            try
            {
                if (stateManager.CurrentPlayer != null)
                {
                    // Character already loaded, go to game loop
                    stateManager.TransitionToState(GameState.GameLoop);
                    if (ShowGameLoopEvent != null)
                    {
                        try
                        {
                            ShowGameLoopEvent.Invoke();
                        }
                        catch (Exception ex)
                        {
                            ShowMessageEvent?.Invoke($"Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("Error: Game loop event not initialized. Please restart the game.");
                    }
                }
                else
                {
                    // Check if save file exists first
                    string saveFilePath = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                    bool saveExists = CharacterSaveManager.SaveFileExists();
                    
                    if (!saveExists)
                    {
                        ShowMessageEvent?.Invoke("No saved game found. Please start a new game.");
                        ShowMainMenu();
                        return;
                    }
                    
                    // Show loading message immediately (non-blocking)
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.ShowLoadingAnimation("Loading saved game...");
                    }
                    
                    // Load all saved characters on background thread to prevent UI freeze
                    List<Character> loadedCharacters = new List<Character>();
                    try
                    {
                        DebugLogger.Log("MainMenuHandler", $"Save file exists at: {saveFilePath}, starting load...");
                        
                        loadedCharacters = await Task.Run(async () =>
                        {
                            var characters = new List<Character>();
                            try
                            {
                                DebugLogger.Log("MainMenuHandler", "Starting character load on background thread...");
                                
                                // Load all saved characters (multi-character support)
                                var savedCharacterInfos = CharacterSaveManager.ListAllSavedCharacters();
                                
                                if (savedCharacterInfos.Count == 0)
                                {
                                    // Backward compatibility: try loading legacy save file
                                    var legacyCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                                    if (legacyCharacter != null)
                                    {
                                        characters.Add(legacyCharacter);
                                        DebugLogger.Log("MainMenuHandler", $"Loaded legacy character: {legacyCharacter.Name}, Level {legacyCharacter.Level}");
                                    }
                                }
                                else
                                {
                                    // Load all characters by their IDs
                                    foreach (var (characterId, characterName, level) in savedCharacterInfos)
                                    {
                                        try
                                        {
                                            var character = await Character.LoadCharacterAsync(characterId).ConfigureAwait(false);
                                            if (character != null)
                                            {
                                                characters.Add(character);
                                                DebugLogger.Log("MainMenuHandler", $"Loaded character: {character.Name}, Level {character.Level}, ID: {characterId}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            DebugLogger.Log("MainMenuHandler", $"Error loading character {characterId}: {ex.Message}");
                                            // Continue loading other characters
                                        }
                                    }
                                }
                                
                                // Initialize game data for the first character (dungeons are shared)
                                if (characters.Count > 0)
                                {
                                    DebugLogger.Log("MainMenuHandler", "Initializing game data...");
                                    gameInitializer.InitializeExistingGame(characters[0], stateManager.AvailableDungeons);
                                    DebugLogger.Log("MainMenuHandler", $"Game data initialized. Dungeons count: {stateManager.AvailableDungeons.Count}");
                                }
                                
                                return characters;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.Log("MainMenuHandler", $"Error in background load: {ex.Message}\n{ex.StackTrace}");
                                throw;
                            }
                        }).ConfigureAwait(true); // Return to UI thread for UI updates
                        
                        DebugLogger.Log("MainMenuHandler", $"Load completed. Loaded {loadedCharacters.Count} character(s)");
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Error loading characters: {ex.Message}";
                        DebugLogger.Log("MainMenuHandler", errorMsg);
                        ShowMessageEvent?.Invoke(errorMsg);
                        ShowMainMenu();
                        return;
                    }
                    
                    // Now update state and UI (we're back on UI thread)
                    if (loadedCharacters.Count > 0)
                    {
                        // Register all loaded characters in state manager
                        Character? firstCharacter = null;
                        foreach (var character in loadedCharacters)
                        {
                            var characterId = stateManager.AddCharacter(character);
                            if (firstCharacter == null)
                            {
                                firstCharacter = character;
                            }
                        }
                        
                        // Set the first character as active (or show selection if multiple)
                        if (loadedCharacters.Count == 1)
                        {
                            // Single character - backward compatibility mode
                            stateManager.SetCurrentPlayer(firstCharacter);
                            
                            // Update UI
                            if (customUIManager is CanvasUICoordinator canvasUI2)
                            {
                                canvasUI2.SetCharacter(stateManager.GetActiveCharacter());
                            }
                            
                            // Apply health multiplier if configured
                            var settings = GameSettings.Instance;
                            var activeCharacter = stateManager.GetActiveCharacter();
                            if (settings.PlayerHealthMultiplier != 1.0 && activeCharacter != null)
                            {
                                activeCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                            }
                            
                            ShowMessageEvent?.Invoke($"Welcome back, {activeCharacter?.Name ?? "Player"}!");
                            
                            // Go to game loop
                            stateManager.TransitionToState(GameState.GameLoop);
                            DebugLogger.Log("MainMenuHandler", $"Firing ShowGameLoopEvent - Event is {(ShowGameLoopEvent != null ? "not null" : "NULL")}");
                            if (ShowGameLoopEvent != null)
                            {
                                try
                                {
                                    ShowGameLoopEvent.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    ShowMessageEvent?.Invoke($"Error: {ex.Message}");
                                }
                            }
                            else
                            {
                                ShowMessageEvent?.Invoke("Error: Game loop event not initialized. Please restart the game.");
                            }
                        }
                        else
                        {
                            // Multiple characters - show character selection
                            ShowMessageEvent?.Invoke($"Loaded {loadedCharacters.Count} characters. Please select one to play.");
                            // Character selection will be handled by CharacterManagementHandler
                            // For now, set first character as active and let user switch
                            stateManager.SetCurrentPlayer(firstCharacter);
                            if (customUIManager is CanvasUICoordinator canvasUI3)
                            {
                                canvasUI3.SetCharacter(firstCharacter);
                            }
                            stateManager.TransitionToState(GameState.GameLoop);
                            ShowGameLoopEvent?.Invoke();
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("No saved game found. Please start a new game.");
                        ShowMainMenu();
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error loading game: {ex.Message}\n{ex.StackTrace}";
                ShowMessageEvent?.Invoke(errorMsg);
                stateManager.TransitionToState(GameState.MainMenu);
                ShowMainMenu();
            }
        }

        /// <summary>
        /// Handle settings menu selection
        /// </summary>
        private void HandleSettingsSelection()
        {
            stateManager.TransitionToState(GameState.Settings);
            ShowSettingsEvent?.Invoke();
        }

        /// <summary>
        /// Handle quit/exit selection
        /// </summary>
        private void HandleQuitSelection()
        {
            ExitGameEvent?.Invoke();
        }
    }
}

