namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;
    using RPGGame.Combat.Events;
    using RPGGame.UI.ColorSystem;

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
                // Reset all game state first to ensure clean start
                // This prevents test state or previous game state from affecting the new game
                stateManager.ResetGameState();
                
                // Reset static state that may have been modified by tests or previous sessions
                ResetStaticState();
                
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
                
                // Get active character (should be the one we just created)
                var activeCharacter = stateManager.GetActiveCharacter();
                
                // Ensure we have a character - if GetActiveCharacter returns null, use the one we just created
                if (activeCharacter == null)
                {
                    activeCharacter = newCharacter;
                    DebugLogger.Log("MainMenuHandler", "GetActiveCharacter returned null, using newly created character");
                }
                
                // Apply health multiplier if configured (before showing weapon selection)
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    activeCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                // Show weapon selection screen
                // ScreenTransitionProtocol will handle state transition, display buffer suppression, canvas clearing, etc.
                DebugLogger.Log("MainMenuHandler", "About to show weapon selection screen");
                if (ShowWeaponSelectionEvent != null)
                {
                    try
                    {
                        ShowWeaponSelectionEvent.Invoke();
                        DebugLogger.Log("MainMenuHandler", "Weapon selection event invoked successfully");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log("MainMenuHandler", $"Error showing weapon selection: {ex}");
                        ShowMessageEvent?.Invoke($"Error: {ex.Message}");
                    }
                }
                else
                {
                    DebugLogger.Log("MainMenuHandler", "ShowWeaponSelectionEvent is null - weapon selection not available");
                    ShowMessageEvent?.Invoke("Error: Weapon selection event not initialized. Please restart the game.");
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
        /// Load a saved game - automatically loads the last saved character
        /// </summary>
        private async Task LoadGame()
        {
            try
            {
                // Get list of all saved characters (sorted by modification time, most recent first)
                var savedCharacters = CharacterSaveManager.ListAllSavedCharacters();
                
                // Check if we have any saved characters
                if (savedCharacters == null || savedCharacters.Count == 0)
                {
                    ShowMessageEvent?.Invoke("No saved characters found.");
                    return;
                }
                
                // Get the most recently saved character (first in the sorted list)
                var mostRecentCharacterInfo = savedCharacters[0];
                
                ShowMessageEvent?.Invoke($"Loading {mostRecentCharacterInfo.characterName}...");
                
                // Show loading animation
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowLoadingAnimation("Loading character...");
                }
                
                // Load character from disk
                Character? loadedCharacter = null;
                
                // Check if characterId is a legacy save (ends with "_legacy")
                if (mostRecentCharacterInfo.characterId.EndsWith("_legacy"))
                {
                    // Load legacy save file
                    loadedCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                }
                else
                {
                    // Load character by ID
                    loadedCharacter = await Character.LoadCharacterAsync(mostRecentCharacterInfo.characterId).ConfigureAwait(false);
                }
                
                if (loadedCharacter == null)
                {
                    // Clear loading animation before showing error
                    if (customUIManager is CanvasUICoordinator canvasUIClear)
                    {
                        canvasUIClear.ClearLoadingStatus();
                    }
                    ShowMessageEvent?.Invoke($"Failed to load character: {mostRecentCharacterInfo.characterName}");
                    return;
                }
                
                // Register character in state manager (multi-character support)
                var registeredCharacterId = stateManager.AddCharacter(loadedCharacter);
                stateManager.SetCurrentPlayer(loadedCharacter);
                
                // Apply health multiplier if configured
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    loadedCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                // Update UI
                if (customUIManager is CanvasUICoordinator canvasUIUpdate)
                {
                    // Clear loading animation before transitioning
                    canvasUIUpdate.ClearLoadingStatus();
                    canvasUIUpdate.RestoreDisplayBufferRendering();
                    canvasUIUpdate.SetCharacter(loadedCharacter);
                    canvasUIUpdate.RefreshCharacterPanel();
                }
                
                ShowMessageEvent?.Invoke($"Loaded {loadedCharacter.Name} (Level {loadedCharacter.Level})");
                
                // Transition to game loop
                stateManager.TransitionToState(GameState.GameLoop);
                if (ShowGameLoopEvent != null)
                {
                    ShowGameLoopEvent.Invoke();
                }
                else
                {
                    ShowMessageEvent?.Invoke("Error: Game loop event not initialized. Please restart the game.");
                }
            }
            catch (Exception ex)
            {
                // Clear loading animation on error
                if (customUIManager is CanvasUICoordinator canvasUIError)
                {
                    canvasUIError.ClearLoadingStatus();
                }
                ShowMessageEvent?.Invoke($"Error loading character: {ex.Message}");
                DebugLogger.Log("MainMenuHandler", $"Error in LoadGame: {ex}");
            }
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

        /// <summary>
        /// Resets static state that may have been modified by tests or previous game sessions.
        /// This ensures a clean state when starting a new game.
        /// Note: Does NOT reset the UI manager itself, only the state within it.
        /// </summary>
        private static void ResetStaticState()
        {
            try
            {
                // Reload UI configuration (but don't reset the UI manager itself)
                UIManager.ReloadConfiguration();
                
                // Clear CombatEventBus subscribers (tests may add subscribers)
                CombatEventBus.Instance.Clear();
                
                // Stop GameTicker if it's running (tests may start it)
                if (GameTicker.Instance.IsRunning)
                {
                    GameTicker.Instance.Stop();
                }
                
                // Reset GameTicker time
                GameTicker.Instance.Reset();
                
                // Reset UIManager flags
                UIManager.DisableAllUIOutput = false;
                
                // Clear character names from KeywordColorSystem (tests may register character names)
                // This prevents test character names from affecting the game's character name coloring
                KeywordColorSystem.ClearCharacterNames();
                
                // Reset GameConfiguration singleton to force reload from file
                // Tests may have modified the instance in memory, so we need to reload it
                GameConfiguration.ResetInstance();
                
                // Reset UI display state - clear any persistent display buffers
                // But don't reset the UI manager itself - it's needed for rendering
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ClearCurrentEnemy();
                    canvasUI.ClearDisplayBuffer();
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - cleanup is best effort
                DebugLogger.Log("MainMenuHandler", $"Warning: Error during static state reset: {ex.Message}");
            }
        }
    }
}

