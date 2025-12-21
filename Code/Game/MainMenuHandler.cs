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
        
        public event OnExitGame? ExitGameEvent;
        public event OnShowMessage? ShowMessageEvent;
        public event OnShowSettings? ShowSettingsEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowWeaponSelection? ShowWeaponSelectionEvent;

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
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
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
            }
            stateManager.TransitionToState(GameState.MainMenu);
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
                case "0":
                    HandleQuitSelection();
                    break;
                default:
                    ShowMessageEvent?.Invoke($"Invalid choice: '{input}'. Please select 1 (New), 2 (Load), 3 (Settings), or 0 (Quit).");
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
                stateManager.SetCurrentPlayer(new Character(null, 1)); // null triggers random name generation
                
                if (stateManager.CurrentPlayer != null)
                {
                    // Set character in UI manager for persistent display
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.SetCharacter(stateManager.CurrentPlayer);
                    }
                    
                    // Apply health multiplier if configured
                    var settings = GameSettings.Instance;
                    if (settings.PlayerHealthMultiplier != 1.0)
                    {
                        stateManager.CurrentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                    }
                    
                    // Go to weapon selection first
                    stateManager.TransitionToState(GameState.WeaponSelection);
                    if (ShowWeaponSelectionEvent != null)
                    {
                        try
                        {
                            ShowWeaponSelectionEvent.Invoke();
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
                    
                    // Load character and initialize game data on background thread to prevent UI freeze
                    Character? savedCharacter = null;
                    try
                    {
                        DebugLogger.Log("MainMenuHandler", $"Save file exists at: {saveFilePath}, starting load...");
                        
                        savedCharacter = await Task.Run(async () =>
                        {
                            try
                            {
                                DebugLogger.Log("MainMenuHandler", "Starting character load on background thread...");
                                // Load character asynchronously on background thread
                                var character = await Character.LoadCharacterAsync().ConfigureAwait(false);
                                
                                if (character != null)
                                {
                                    DebugLogger.Log("MainMenuHandler", $"Character loaded: {character.Name}, Level {character.Level}");
                                    // Initialize game data on same background thread
                                    // This may load JSON files synchronously, but it's on background thread
                                    DebugLogger.Log("MainMenuHandler", "Initializing game data...");
                                    gameInitializer.InitializeExistingGame(character, stateManager.AvailableDungeons);
                                    DebugLogger.Log("MainMenuHandler", $"Game data initialized. Dungeons count: {stateManager.AvailableDungeons.Count}");
                                }
                                else
                                {
                                    DebugLogger.Log("MainMenuHandler", "Character load returned null - save file may be corrupted");
                                }
                                return character;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.Log("MainMenuHandler", $"Error in background load: {ex.Message}\n{ex.StackTrace}");
                                throw;
                            }
                        }).ConfigureAwait(true); // Return to UI thread for UI updates
                        
                        DebugLogger.Log("MainMenuHandler", $"Load completed. Character is {(savedCharacter != null ? "loaded" : "null")}");
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Error loading character: {ex.Message}";
                        DebugLogger.Log("MainMenuHandler", errorMsg);
                        ShowMessageEvent?.Invoke(errorMsg);
                        ShowMainMenu();
                        return;
                    }
                    
                    // Now update state and UI (we're back on UI thread)
                    if (savedCharacter != null)
                    {
                        stateManager.SetCurrentPlayer(savedCharacter);
                        
                        // Update UI - these should be thread-safe or called from UI thread
                        if (customUIManager is CanvasUICoordinator canvasUI2)
                        {
                            canvasUI2.SetCharacter(stateManager.CurrentPlayer);
                        }
                        
                        // Apply health multiplier if configured
                        var settings = GameSettings.Instance;
                        if (settings.PlayerHealthMultiplier != 1.0 && stateManager.CurrentPlayer != null)
                        {
                            stateManager.CurrentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                        }
                        
                        // Inventory is loaded from stateManager.CurrentPlayer.Inventory
                        
                        ShowMessageEvent?.Invoke($"Welcome back, {stateManager.CurrentPlayer?.Name ?? "Player"}!");
                        
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

