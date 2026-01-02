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
        private LoadCharacterSelectionHandler? loadCharacterSelectionHandler;
        
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
        /// Display the main menu with saved game info.
        /// Uses GameScreenCoordinator for standardized screen transition.
        /// </summary>
        public void ShowMainMenu()
        {
            // Show loading message in bottom left corner while data is being loaded
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.ShowLoadingStatus("Loading data...");
            }
            
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
            
            // Use GameScreenCoordinator for standardized screen transition
            var screenCoordinator = new GameScreenCoordinator(stateManager);
            screenCoordinator.ShowMainMenu(hasSavedGame, characterName, characterLevel);
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
        /// Load a saved game - automatically loads the last or only saved character
        /// </summary>
        private async Task LoadGame()
        {
            // Automatically load the last or only saved character
            if (loadCharacterSelectionHandler == null)
            {
                loadCharacterSelectionHandler = new LoadCharacterSelectionHandler(stateManager, customUIManager, gameInitializer);
                // Wire up events
                loadCharacterSelectionHandler.ShowGameLoopEvent += () => 
                {
                    if (ShowGameLoopEvent != null)
                    {
                        ShowGameLoopEvent.Invoke();
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("Error: Game loop event not initialized. Please restart the game.");
                    }
                };
                loadCharacterSelectionHandler.ShowMainMenuEvent += () => ShowMainMenu();
                loadCharacterSelectionHandler.ShowMessageEvent += (msg) => ShowMessageEvent?.Invoke(msg);
            }
            
            await loadCharacterSelectionHandler.ShowLoadCharacterSelection();
        }

        /// <summary>
        /// Handle settings menu selection
        /// Opens settings in a separate window without changing main window state
        /// </summary>
        private void HandleSettingsSelection()
        {
            // Don't transition to Settings state - keep MainMenu state so main menu stays visible
            // The settings window is independent and doesn't affect the main window
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

