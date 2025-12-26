namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Handles character management operations for multi-character support.
    /// Provides functionality to list, switch, create, and delete characters.
    /// </summary>
    public class CharacterManagementHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private GameInitializationManager? gameInitializer;
        
        // Delegates
        public delegate void OnShowGameLoop();
        public delegate void OnShowMainMenu();
        public delegate void OnShowWeaponSelection();
        public delegate void OnShowMessage(string message);
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowWeaponSelection? ShowWeaponSelectionEvent;
        public event OnShowMessage? ShowMessageEvent;

        public CharacterManagementHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            GameInitializationManager? gameInitializer = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.gameInitializer = gameInitializer;
        }

        /// <summary>
        /// Shows the character selection/management screen
        /// </summary>
        public void ShowCharacterSelection()
        {
            try
            {
                stateManager.TransitionToState(GameState.CharacterSelection);
                
                var allCharacters = stateManager.GetAllCharacters();
                var activeCharacter = stateManager.GetActiveCharacter();
                var activeCharacterId = stateManager.GetActiveCharacterId();
                
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    // Render character selection screen
                    RenderCharacterSelection(canvasUI, allCharacters, activeCharacterId);
                }
                else
                {
                    // Console mode - show character list
                    ShowCharacterListConsole(allCharacters, activeCharacterId);
                }
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error showing character selection: {ex.Message}");
                DebugLogger.Log("CharacterManagementHandler", $"Error in ShowCharacterSelection: {ex}");
            }
        }

        /// <summary>
        /// Handles input for character selection menu
        /// </summary>
        public async Task HandleCharacterSelectionInput(string input)
        {
            var allCharacters = stateManager.GetAllCharacters();
            var allContexts = stateManager.GetAllCharacterContexts();
            
            if (int.TryParse(input, out int choice))
            {
                // Handle empty characters list
                if (allCharacters == null || allCharacters.Count == 0)
                {
                    if (choice == 1)
                    {
                        // Create new character - always use direct creation since event isn't wired
                        ShowMessageEvent?.Invoke("Creating new character...");
                        await CreateNewCharacter();
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
                        ShowMessageEvent?.Invoke("Invalid choice. Press 1 to create a character or 0 to return.");
                    }
                    return;
                }
                
                // Handle non-empty characters list
                if (choice >= 1 && choice <= allCharacters.Count)
                {
                    // Switch to selected character
                    var selectedCharacter = allCharacters[choice - 1];
                    var characterId = stateManager.GetCharacterId(selectedCharacter);
                    
                    if (!string.IsNullOrEmpty(characterId))
                    {
                        if (stateManager.SwitchCharacter(characterId))
                        {
                            ShowMessageEvent?.Invoke($"Switched to {selectedCharacter.Name} (Level {selectedCharacter.Level})");
                            
                            // Update UI
                            if (customUIManager is CanvasUICoordinator canvasUI)
                            {
                                // Restore display buffer rendering for game loop
                                canvasUI.RestoreDisplayBufferRendering();
                                canvasUI.SetCharacter(selectedCharacter);
                                canvasUI.RefreshCharacterPanel();
                            }
                            
                            // Return to game loop
                            stateManager.TransitionToState(GameState.GameLoop);
                            ShowGameLoopEvent?.Invoke();
                        }
                        else
                        {
                            ShowMessageEvent?.Invoke("Failed to switch character.");
                        }
                    }
                }
                else if (choice == allCharacters.Count + 1)
                {
                    // Create new character - always use direct creation since event isn't wired
                    ShowMessageEvent?.Invoke("Creating new character...");
                    DebugLogger.Log("CharacterManagementHandler", "Starting character creation...");
                    try
                    {
                        await CreateNewCharacter();
                        DebugLogger.Log("CharacterManagementHandler", "Character creation completed");
                    }
                    catch (Exception ex)
                    {
                        ShowMessageEvent?.Invoke($"Error creating character: {ex.Message}");
                        DebugLogger.Log("CharacterManagementHandler", $"Exception in CreateNewCharacter: {ex}");
                    }
                }
                else if (choice == 0)
                {
                    // Restore display buffer rendering before returning
                    if (customUIManager is CanvasUICoordinator canvasUI)
                    {
                        canvasUI.RestoreDisplayBufferRendering();
                    }
                    
                    // Return to main menu (not game loop, since we came from main menu)
                    stateManager.TransitionToState(GameState.MainMenu);
                    ShowMainMenuEvent?.Invoke();
                }
                else
                {
                    ShowMessageEvent?.Invoke("Invalid choice.");
                }
            }
            else if (input.ToUpper() == "D" && allCharacters.Count > 1)
            {
                // Delete character (only if more than one exists)
                ShowMessageEvent?.Invoke("Enter character number to delete (or 0 to cancel):");
                // Note: This would need a two-step input handler in a real implementation
            }
            else
            {
                ShowMessageEvent?.Invoke("Invalid input. Please enter a number.");
            }
        }

        /// <summary>
        /// Creates a new character and adds it to the registry
        /// </summary>
        private async Task CreateNewCharacter()
        {
            try
            {
                DebugLogger.Log("CharacterManagementHandler", "CreateNewCharacter: Starting");
                
                // Clear any existing enemy from previous game session
                if (customUIManager is CanvasUICoordinator canvasUIClear)
                {
                    canvasUIClear.ClearCurrentEnemy();
                }
                
                // Create new character (without equipment yet) - same as MainMenuHandler.StartNewGame()
                var newCharacter = new Character(null, 1); // null triggers random name generation
                DebugLogger.Log("CharacterManagementHandler", $"CreateNewCharacter: Created character {newCharacter.Name}");
                
                // Register character in state manager (multi-character support)
                var characterId = stateManager.AddCharacter(newCharacter);
                stateManager.SetCurrentPlayer(newCharacter);
                DebugLogger.Log("CharacterManagementHandler", $"CreateNewCharacter: Registered character with ID {characterId}");
                
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
                    
                    DebugLogger.Log("CharacterManagementHandler", "CreateNewCharacter: Invoking ShowWeaponSelectionEvent before state transition");
                    // Render weapon selection BEFORE transitioning state to prevent canvas clearing during transition
                    // This ensures the screen is rendered before any state-change-triggered clears happen
                    if (ShowWeaponSelectionEvent != null)
                    {
                        try
                        {
                            DebugLogger.Log("CharacterManagementHandler", "CreateNewCharacter: Invoking ShowWeaponSelectionEvent");
                            ShowWeaponSelectionEvent.Invoke();
                            DebugLogger.Log("CharacterManagementHandler", "CreateNewCharacter: ShowWeaponSelectionEvent invoked successfully");
                            
                            // Transition state AFTER rendering to prevent flashing
                            DebugLogger.Log("CharacterManagementHandler", "CreateNewCharacter: Transitioning to WeaponSelection state after render");
                            // Only transition if not already in WeaponSelection state
                            if (stateManager.CurrentState != GameState.WeaponSelection)
                            {
                                stateManager.TransitionToState(GameState.WeaponSelection);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessageEvent?.Invoke($"Error showing weapon selection: {ex.Message}");
                            DebugLogger.Log("CharacterManagementHandler", $"Error invoking ShowWeaponSelectionEvent: {ex}");
                            stateManager.TransitionToState(GameState.MainMenu);
                            ShowMainMenuEvent?.Invoke();
                        }
                    }
                    else
                    {
                        ShowMessageEvent?.Invoke("Error: Weapon selection not available. Please restart the game.");
                        DebugLogger.Log("CharacterManagementHandler", "ShowWeaponSelectionEvent is null");
                        stateManager.TransitionToState(GameState.MainMenu);
                        ShowMainMenuEvent?.Invoke();
                    }
                }
                else
                {
                    DebugLogger.Log("CharacterManagementHandler", "CreateNewCharacter: activeCharacter is null");
                    ShowMessageEvent?.Invoke("Error: Failed to get active character.");
                }
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error creating character: {ex.Message}");
                DebugLogger.Log("CharacterManagementHandler", $"Error in CreateNewCharacter: {ex}");
                stateManager.TransitionToState(GameState.MainMenu);
                ShowMainMenuEvent?.Invoke();
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Renders character selection screen for CanvasUI
        /// </summary>
        private void RenderCharacterSelection(CanvasUICoordinator canvasUI, List<Character> characters, string? activeCharacterId)
        {
            try
            {
                // Clear canvas first like other menus do
                canvasUI.Clear();
                
                // Suppress display buffer auto-rendering to prevent conflicts
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();
                
                // Build character statuses dictionary (keyed by character name for easy lookup)
                var characterStatuses = new Dictionary<string, string>();
                var allContexts = stateManager.GetAllCharacterContexts();
                
                string? activeCharacterName = null;
                foreach (var character in characters ?? new List<Character>())
                {
                    if (character == null) continue;
                    
                    var characterId = stateManager.GetCharacterId(character);
                    if (string.IsNullOrEmpty(characterId)) continue;
                    
                    // Track active character name
                    if (characterId == activeCharacterId)
                    {
                        activeCharacterName = character.Name;
                    }
                    
                    // Check if character has an active dungeon
                    var context = allContexts.FirstOrDefault(c => c.CharacterId == characterId);
                    if (context != null && context.ActiveDungeon != null)
                    {
                        characterStatuses[character.Name] = $"[IN DUNGEON: {context.ActiveDungeon.Name}]";
                    }
                }
                
                // Use proper menu renderer instead of display buffer
                canvasUI.RenderCharacterSelection(characters ?? new List<Character>(), activeCharacterName, characterStatuses);
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error rendering character selection: {ex.Message}");
                DebugLogger.Log("CharacterManagementHandler", $"Error in RenderCharacterSelection: {ex}");
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
        /// Shows character list in console mode
        /// </summary>
        private void ShowCharacterListConsole(List<Character> characters, string? activeCharacterId)
        {
            UIManager.WriteLine("=== CHARACTER SELECTION ===");
            UIManager.WriteBlankLine();
            
            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                var characterId = stateManager.GetCharacterId(character);
                var isActive = characterId == activeCharacterId;
                var activeMarker = isActive ? " [ACTIVE]" : "";
                
                UIManager.WriteMenuLine($"{i + 1}. {character.Name} - Level {character.Level} - {character.GetCurrentClass()}{activeMarker}");
            }
            
            UIManager.WriteBlankLine();
            UIManager.WriteMenuLine($"{characters.Count + 1}. Create New Character");
            UIManager.WriteMenuLine("0. Back to Game Menu");
            UIManager.WriteBlankLine();
            UIManager.WriteSystemLine("Select a character number, or press 0 to return.");
        }

        /// <summary>
        /// Deletes a character by ID
        /// </summary>
        public bool DeleteCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            
            var character = stateManager.GetCharacter(characterId);
            if (character == null) return false;
            
            // Don't allow deleting if it's the only character
            if (stateManager.GetAllCharacters().Count <= 1)
            {
                ShowMessageEvent?.Invoke("Cannot delete the last character.");
                return false;
            }
            
            // Delete save file
            CharacterSaveManager.DeleteSaveFile(CharacterSaveManager.GetCharacterSaveFilename(characterId));
            
            // Remove from registry
            if (stateManager.RemoveCharacter(characterId))
            {
                ShowMessageEvent?.Invoke($"Deleted character: {character.Name}");
                return true;
            }
            
            return false;
        }
    }
}

