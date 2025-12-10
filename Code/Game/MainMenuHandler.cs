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
                // Check if we have a saved game
                bool hasSavedGame = stateManager.CurrentPlayer != null;
                string? characterName = stateManager.CurrentPlayer?.Name;
                int characterLevel = stateManager.CurrentPlayer?.Level ?? 0;
                
                canvasUI.RenderMainMenu(hasSavedGame, characterName, characterLevel);
            }
            stateManager.TransitionToState(GameState.MainMenu);
        }

        /// <summary>
        /// Handle main menu input (1=New, 2=Load, 3=Settings, 0=Quit)
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            // Trim whitespace to handle various input formats
            string trimmedInput = input?.Trim() ?? "";
            
            switch (trimmedInput)
            {
                case "1":
                    ShowMessageEvent?.Invoke("Starting new game...");
                    await StartNewGame();
                    break;
                case "2":
                    ShowMessageEvent?.Invoke("Loading game...");
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
                    // Show loading message
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.ShowLoadingAnimation("Loading saved game...");
                    }
                    
                    // Try to load saved character on background thread
                    var savedCharacter = await Task.Run(() => Character.LoadCharacter());
                    if (savedCharacter != null)
                    {
                        stateManager.SetCurrentPlayer(savedCharacter);
                        
                        // Initialize game data on background thread
                        if (stateManager.CurrentPlayer != null)
                        {
                            await Task.Run(() => gameInitializer.InitializeExistingGame(stateManager.CurrentPlayer, stateManager.AvailableDungeons));
                        
                            // Set character in UI manager (on UI thread)
                            if (customUIManager is CanvasUICoordinator canvasUI2)
                            {
                                canvasUI2.SetCharacter(stateManager.CurrentPlayer);
                            }
                            
                            // Apply health multiplier if configured
                            var settings = GameSettings.Instance;
                            if (settings.PlayerHealthMultiplier != 1.0)
                            {
                                stateManager.CurrentPlayer.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                            }
                            
                            // Inventory is loaded from stateManager.CurrentPlayer.Inventory
                            
                            ShowMessageEvent?.Invoke($"Welcome back, {stateManager.CurrentPlayer.Name}!");
                        }
                        
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

