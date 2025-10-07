using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using RPGGame;

namespace RPGGame
{
    /// <summary>
    /// Manages game menus, UI interactions, and game flow
    /// </summary>
    public class GameMenuManager
    {
        private GameInitializer gameInitializer;
        private GameLoopManager gameLoopManager;

        public GameMenuManager()
        {
            gameInitializer = new GameInitializer();
            gameLoopManager = new GameLoopManager();
        }

        /// <summary>
        /// Shows the main menu and handles user navigation
        /// </summary>
        public void ShowMainMenu()
        {
            while (true)
            {
                // Get saved character info
                var (characterName, characterLevel) = GetSavedCharacterInfo();
                bool hasSavedGame = characterName != null;
                
                // Get menu options from configuration
                var menuOptions = MenuConfiguration.GetMainMenuOptions(hasSavedGame, characterName, characterLevel);
                
                TextDisplayIntegration.DisplayMenu("\nDUNGEON FIGHTER", menuOptions);
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                
                
                switch (choice)
                {
                    case "1":
                        StartNewGame();
                        break;
                    case "2":
                        LoadAndRunGame();
                        break;
                    case "3":
                        ShowSettings();
                        break;
                    case "0":
                        TextDisplayIntegration.DisplayBlankLine();
                        TextDisplayIntegration.DisplaySystem("Goodbye!");
                        TextDisplayIntegration.DisplayBlankLine();
                        return;
                    default:
                        TextDisplayIntegration.DisplaySystem("Invalid choice. Please try again.");
                        break;
                }
            }
        }


        /// <summary>
        /// Starts a new game with character creation
        /// </summary>
        private void StartNewGame()
        {
            var settings = GameSettings.Instance;
            
            // Create a new character (for "New Game")
            var player = new Character(null, 1); // null will trigger random name generation
            
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                player.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            var inventory = new List<Item>();
            var availableDungeons = new List<Dungeon>();
            
            gameInitializer.InitializeNewGame(player, availableDungeons);
            RunGame(player, inventory, availableDungeons, false); // false = new character
        }

        /// <summary>
        /// Loads an existing game and runs it
        /// </summary>
        private void LoadAndRunGame()
        {
            var character = Character.LoadCharacter();
            if (character != null)
            {
                var player = character;
                var settings = GameSettings.Instance;
                
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    player.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                var inventory = new List<Item>();
                var availableDungeons = new List<Dungeon>();
                
                gameInitializer.InitializeExistingGame(player, availableDungeons);
                RunGame(player, inventory, availableDungeons, true); // true = loaded character
            }
            else
            {
                TextDisplayIntegration.DisplaySystem("No saved character found. Starting new game instead...");
                TextDisplayIntegration.DisplaySystem("Press any key to continue...");
                Console.ReadKey();
                StartNewGame();
            }
        }

        /// <summary>
        /// Runs the main game loop using the GameLoopManager
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons list</param>
        /// <param name="isLoadedCharacter">True if this is a loaded character, false if new</param>
        public void RunGame(Character player, List<Item> inventory, List<Dungeon> availableDungeons, bool isLoadedCharacter = false)
        {
            // Delegate to GameLoopManager for the actual game loop logic
            gameLoopManager.RunGameLoop(player, inventory, availableDungeons, isLoadedCharacter);
        }

        /// <summary>
        /// Gets information about saved character
        /// </summary>
        /// <param name="filename">Save file path (optional, uses default if null)</param>
        /// <returns>Character name and level, or null/0 if not found</returns>
        private (string? name, int level) GetSavedCharacterInfo(string? filename = null)
        {
            return CharacterSaveManager.GetSavedCharacterInfo(filename);
        }

        /// <summary>
        /// Shows the settings menu
        /// </summary>
        private void ShowSettings()
        {
            SettingsManager.ShowSettings();
        }
    }
}
