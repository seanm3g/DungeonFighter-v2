using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Manages game menus, UI interactions, and game flow
    /// </summary>
    public class GameMenuManager
    {
        private GameInitializer gameInitializer;
        private DungeonManager dungeonManager;
        private CombatManager combatManager;

        public GameMenuManager()
        {
            gameInitializer = new GameInitializer();
            dungeonManager = new DungeonManager();
            combatManager = new CombatManager();
        }

        /// <summary>
        /// Shows the main menu and handles user navigation
        /// </summary>
        public void ShowMainMenu()
        {
            while (true)
            {
                UIManager.WriteTitleLine("\nDUNGEON FIGHTER\n");

                UIManager.WriteMenuLine("1. New Game");
                
                // Check if there's a saved character and display info
                var (characterName, characterLevel) = GetSavedCharacterInfo();
                if (characterName != null)
                {
                    UIManager.WriteMenuLine($"2. Load Game - {characterName} - Level {characterLevel}");
                }
                else
                {
                    UIManager.WriteMenuLine("2. Load Game");
                }
                
                UIManager.WriteMenuLine("3. Settings");
                UIManager.WriteMenuLine("4. Exit\n");
                UIManager.Write("Choose an option: ");

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
                    case "4":
                        UIManager.WriteBlankLine();
                        UIManager.WriteLine("Goodbye!");
                        UIManager.WriteBlankLine();
                        return;
                    default:
                        UIManager.WriteLine("Invalid choice. Please try again.");
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
            RunGame(player, inventory, availableDungeons);
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
                RunGame(player, inventory, availableDungeons);
            }
            else
            {
                UIManager.WriteLine("No saved character found. Starting new game instead...");
                UIManager.WriteLine("Press any key to continue...");
                Console.ReadKey();
                StartNewGame();
            }
        }

        /// <summary>
        /// Runs the main game loop
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons list</param>
        public void RunGame(Character player, List<Item> inventory, List<Dungeon> availableDungeons)
        {
            UIManager.WriteBlankLine();
            UIManager.WriteTitleLine(player.GetFullNameWithQualifier());
            UIManager.WriteTitleLine("\nWelcome to...\n");
            UIManager.WriteTitleLine("DUNGEON FIGHTER\n");

            while (true)
            {
                // Ask if player wants to manage gear first
                UIManager.WriteTitleLine("\nWhat would you like to do?");
                UIManager.WriteMenuLine("");
                UIManager.WriteMenuLine("1. Choose a Dungeon");
                UIManager.WriteMenuLine("2. Inventory");
                UIManager.WriteMenuLine("3. Exit Game and save");
                UIManager.WriteMenuLine("");
                UIManager.Write("Enter your choice: ");

                if (int.TryParse(Console.ReadLine(), out int initialChoice))
                {
                    switch (initialChoice)
                    {
                        case 1:
                            // Go straight to dungeon selection
                            break;
                        case 2:
                            var inventoryManager = new InventoryManager(player, inventory);
                            bool continueToDungeon = inventoryManager.ShowGearMenu();
                            if (!continueToDungeon)
                            {
                                continue; // Return to main menu instead of going to dungeon
                            }
                            // Fall through to dungeon selection
                            break;
                        case 3:
                            UIManager.WriteLine("Saving game before exit...");
                            player.SaveCharacter();
                            UIManager.WriteLine("Game saved! Thanks for playing!");
                            return;
                        default:
                            UIManager.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                            continue; // Continue the loop instead of exiting
                    }
                }
                else
                {
                    UIManager.WriteLine("Invalid input. Please enter a number (1, 2, or 3).");
                    continue; // Continue the loop instead of exiting
                }

                // Regenerate dungeons based on current player level
                dungeonManager.RegenerateDungeons(player, availableDungeons);

                // Dungeon Selection and Run
                Dungeon selectedDungeon = dungeonManager.ChooseDungeon(availableDungeons);
                selectedDungeon.Generate();

                // Run the complete dungeon
                bool playerSurvivedDungeon = dungeonManager.RunDungeon(selectedDungeon, player, combatManager);
                
                if (!playerSurvivedDungeon)
                {
                    return; // Player died, game ends
                }

                // Dungeon Completion
                dungeonManager.AwardLootAndXP(player, inventory, availableDungeons);
            }
        }

        /// <summary>
        /// Gets information about saved character
        /// </summary>
        /// <param name="filename">Save file path</param>
        /// <returns>Character name and level, or null/0 if not found</returns>
        private (string? name, int level) GetSavedCharacterInfo(string filename = "GameData/character_save.json")
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
