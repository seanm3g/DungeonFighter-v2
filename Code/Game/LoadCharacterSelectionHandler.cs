namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles loading saved characters from disk.
    /// Shows a menu of all saved characters and allows selection.
    /// </summary>
    public class LoadCharacterSelectionHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private GameInitializer gameInitializer;
        
        // Delegates
        public delegate void OnShowGameLoop();
        public delegate void OnShowMainMenu();
        public delegate void OnShowMessage(string message);
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowMessage? ShowMessageEvent;

        public LoadCharacterSelectionHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            GameInitializer gameInitializer)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.gameInitializer = gameInitializer ?? throw new ArgumentNullException(nameof(gameInitializer));
        }

        /// <summary>
        /// Shows the load character selection menu with all saved characters
        /// </summary>
        public void ShowLoadCharacterSelection()
        {
            try
            {
                stateManager.TransitionToState(GameState.LoadCharacterSelection);
                
                // Get list of all saved characters from disk
                var savedCharacters = CharacterSaveManager.ListAllSavedCharacters();
                
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    // Render load character selection screen
                    RenderLoadCharacterSelection(canvasUI, savedCharacters);
                }
                else
                {
                    // Console mode - show character list
                    ShowLoadCharacterListConsole(savedCharacters);
                }
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error showing load character selection: {ex.Message}");
                DebugLogger.Log("LoadCharacterSelectionHandler", $"Error in ShowLoadCharacterSelection: {ex}");
            }
        }

        /// <summary>
        /// Handles input for load character selection menu
        /// </summary>
        public async Task HandleLoadCharacterSelectionInput(string input)
        {
            var savedCharacters = CharacterSaveManager.ListAllSavedCharacters();
            
            if (int.TryParse(input, out int choice))
            {
                // Handle empty characters list
                if (savedCharacters == null || savedCharacters.Count == 0)
                {
                    if (choice == 0)
                    {
                        // Return to main menu
                        if (customUIManager is CanvasUICoordinator canvasUI)
                        {
                            canvasUI.RestoreDisplayBufferRendering();
                        }
                        stateManager.TransitionToState(GameState.MainMenu);
                        ShowMainMenuEvent?.Invoke();
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("Invalid choice. Press 0 to return.");
                    }
                    return;
                }
                
                // Handle character selection
                if (choice >= 1 && choice <= savedCharacters.Count)
                {
                    // Load selected character
                    var selectedCharacterInfo = savedCharacters[choice - 1];
                    await LoadCharacter(selectedCharacterInfo.characterId, selectedCharacterInfo.characterName);
                }
                else if (choice == 0)
                {
                    // Return to main menu
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.RestoreDisplayBufferRendering();
                    }
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid choice.");
                }
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid input. Please enter a number.");
            }
        }

        /// <summary>
        /// Loads a character by character ID and transitions to game loop
        /// </summary>
        private async Task LoadCharacter(string characterId, string characterName)
        {
            try
            {
                ShowMessageEvent?.Invoke($"Loading {characterName}...");
                
                // Show loading animation
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowLoadingAnimation("Loading character...");
                }
                
                // Load character from disk
                Character? loadedCharacter = null;
                
                // Check if characterId is a legacy save (ends with "_legacy")
                if (characterId.EndsWith("_legacy"))
                {
                    // Load legacy save file
                    loadedCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                }
                else
                {
                    // Load character by ID
                    loadedCharacter = await Character.LoadCharacterAsync(characterId).ConfigureAwait(false);
                }
                
                if (loadedCharacter == null)
                {
                    // Clear loading animation before showing error
                    if (customUIManager is CanvasUICoordinator canvasUIClear)
                    {
                        canvasUIClear.ClearLoadingStatus();
                    }
                    ShowMessageEvent?.Invoke($"Failed to load character: {characterName}");
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
                ShowGameLoopEvent?.Invoke();
            }
            catch (Exception ex)
            {
                // Clear loading animation on error
                if (customUIManager is CanvasUICoordinator canvasUIError)
                {
                    canvasUIError.ClearLoadingStatus();
                }
                ShowMessageEvent?.Invoke($"Error loading character: {ex.Message}");
                DebugLogger.Log("LoadCharacterSelectionHandler", $"Error in LoadCharacter: {ex}");
            }
        }

        /// <summary>
        /// Renders load character selection screen for CanvasUI
        /// </summary>
        private void RenderLoadCharacterSelection(CanvasUICoordinator canvasUI, List<(string characterId, string characterName, int level)> savedCharacters)
        {
            try
            {
                // Clear canvas first like other menus do
                canvasUI.Clear();
                
                // Suppress display buffer auto-rendering to prevent conflicts
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                
                // Use proper menu renderer
                canvasUI.RenderLoadCharacterSelection(savedCharacters ?? new List<(string, string, int)>());
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error rendering load character selection: {ex.Message}");
                DebugLogger.Log("LoadCharacterSelectionHandler", $"Error in RenderLoadCharacterSelection: {ex}");
                // Try to restore rendering even on error
                try
                {
                    canvasUI.RestoreDisplayBufferRendering();
                    canvasUI.Refresh();
                }
                catch { }
            }
        }

        /// <summary>
        /// Shows saved character list in console mode
        /// </summary>
        private void ShowLoadCharacterListConsole(List<(string characterId, string characterName, int level)> savedCharacters)
        {
            UIManager.WriteLine("=== LOAD CHARACTER ===");
            UIManager.WriteBlankLine();
            
            if (savedCharacters == null || savedCharacters.Count == 0)
            {
                UIManager.WriteMenuLine("No saved characters found.");
                UIManager.WriteBlankLine();
                UIManager.WriteMenuLine("0. Back to Main Menu");
                UIManager.WriteBlankLine();
                UIManager.WriteSystemLine("Press 0 to return.");
            }
            else
            {
                for (int i = 0; i < savedCharacters.Count; i++)
                {
                    var (characterId, characterName, level) = savedCharacters[i];
                    UIManager.WriteMenuLine($"{i + 1}. {characterName} - Level {level}");
                }
                
                UIManager.WriteBlankLine();
                UIManager.WriteMenuLine("0. Back to Main Menu");
                UIManager.WriteBlankLine();
                UIManager.WriteSystemLine("Select a character number to load, or press 0 to return.");
            }
        }
    }
}
