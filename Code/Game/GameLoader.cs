using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            DebugLogger.Log("GameLoader", "LoadGame() called");
            onMessage("Loading game...");
            
            try
            {
                if (stateManager.CurrentPlayer != null)
                {
                    // Character already loaded, go to game loop
                    stateManager.TransitionToState(GameState.GameLoop);
                    try
                    {
                        onGameLoop();
                    }
                    catch (Exception ex)
                    {
                        onMessage($"Error: {ex.Message}");
                    }
                    return true;
                }
                
                // Check if save file exists first
                string saveFilePath = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                bool saveExists = CharacterSaveManager.SaveFileExists();
                
                if (!saveExists)
                {
                    onMessage("No saved game found. Please start a new game.");
                    onShowMainMenu();
                    return false;
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
                    DebugLogger.Log("GameLoader", $"Save file exists at: {saveFilePath}, starting load...");
                    
                    loadedCharacters = await Task.Run(async () =>
                    {
                        var characters = new List<Character>();
                        try
                        {
                            DebugLogger.Log("GameLoader", "Starting character load on background thread...");
                            
                            // Load all saved characters (multi-character support)
                            var savedCharacterInfos = CharacterSaveManager.ListAllSavedCharacters();
                            
                            if (savedCharacterInfos.Count == 0)
                            {
                                // Backward compatibility: try loading legacy save file
                                var legacyCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                                if (legacyCharacter != null)
                                {
                                    characters.Add(legacyCharacter);
                                    DebugLogger.Log("GameLoader", $"Loaded legacy character: {legacyCharacter.Name}, Level {legacyCharacter.Level}");
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
                                            DebugLogger.Log("GameLoader", $"Loaded character: {character.Name}, Level {character.Level}, ID: {characterId}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.Log("GameLoader", $"Error loading character {characterId}: {ex.Message}");
                                        // Continue loading other characters
                                    }
                                }
                            }
                            
                            // Initialize game data for the first character (dungeons are shared)
                            if (characters.Count > 0)
                            {
                                DebugLogger.Log("GameLoader", "Initializing game data...");
                                gameInitializer.InitializeExistingGame(characters[0], stateManager.AvailableDungeons);
                                DebugLogger.Log("GameLoader", $"Game data initialized. Dungeons count: {stateManager.AvailableDungeons.Count}");
                            }
                            
                            return characters;
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Log("GameLoader", $"Error in background load: {ex.Message}\n{ex.StackTrace}");
                            throw;
                        }
                    }).ConfigureAwait(true); // Return to UI thread for UI updates
                    
                    DebugLogger.Log("GameLoader", $"Load completed. Loaded {loadedCharacters.Count} character(s)");
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Error loading characters: {ex.Message}";
                    DebugLogger.Log("GameLoader", errorMsg);
                    onMessage(errorMsg);
                    onShowMainMenu();
                    return false;
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
                        
                        onMessage($"Welcome back, {activeCharacter?.Name ?? "Player"}!");
                        
                        // Go to game loop
                        stateManager.TransitionToState(GameState.GameLoop);
                        DebugLogger.Log("GameLoader", $"Firing onGameLoop callback");
                        try
                        {
                            onGameLoop();
                        }
                        catch (Exception ex)
                        {
                            onMessage($"Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Multiple characters - show character selection
                        onMessage($"Loaded {loadedCharacters.Count} characters. Please select one to play.");
                        // Character selection will be handled by CharacterManagementHandler
                        // For now, set first character as active and let user switch
                        stateManager.SetCurrentPlayer(firstCharacter);
                        if (customUIManager is CanvasUICoordinator canvasUI3)
                        {
                            canvasUI3.SetCharacter(firstCharacter);
                        }
                        stateManager.TransitionToState(GameState.GameLoop);
                        onGameLoop();
                    }
                    
                    return true;
                }
                else
                {
                    onMessage("No saved game found. Please start a new game.");
                    onShowMainMenu();
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error loading game: {ex.Message}\n{ex.StackTrace}";
                onMessage(errorMsg);
                stateManager.TransitionToState(GameState.MainMenu);
                onShowMainMenu();
                return false;
            }
        }
    }
}

