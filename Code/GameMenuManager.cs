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
                Console.WriteLine("\nDungeon Fighter - Main Menu\n");
                Thread.Sleep(TuningConfig.Instance.UI.MainMenuDelay);
                Console.WriteLine("1. New Game");
                
                // Check if there's a saved character and display info
                var (characterName, characterLevel) = GetSavedCharacterInfo();
                if (characterName != null)
                {
                    Console.WriteLine($"2. Load Game - {characterName} - Level {characterLevel}");
                }
                else
                {
                    Console.WriteLine("2. Load Game");
                }
                
                Console.WriteLine("3. Settings");
                Console.WriteLine("4. Exit\n");
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
                    case "4":
                        Console.WriteLine();
                        Console.WriteLine("Goodbye!");
                        Console.WriteLine();
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
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
                Console.WriteLine("No saved character found. Starting new game instead...");
                Console.WriteLine("Press any key to continue...");
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
            Console.WriteLine("Welcome to the Dungeon Fighter!\n");
            Console.WriteLine($"Player: {player.GetFullNameWithQualifier()} (Level {player.Level})");

            while (true)
            {
                // Ask if player wants to manage gear first
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine();
                Console.WriteLine("1. Choose a Dungeon");
                Console.WriteLine("2. Inventory");
                Console.WriteLine("3. Exit Game and save\n");
                Console.Write("Enter your choice: ");

                if (int.TryParse(Console.ReadLine(), out int initialChoice))
                {
                    switch (initialChoice)
                    {
                        case 1:
                            // Go straight to dungeon selection
                            break;
                        case 2:
                            var inventoryManager = new Inventory(player, inventory);
                            bool continueToDungeon = inventoryManager.ShowGearMenu();
                            if (!continueToDungeon)
                            {
                                continue; // Return to main menu instead of going to dungeon
                            }
                            // Fall through to dungeon selection
                            break;
                        case 3:
                            Console.WriteLine("Saving game before exit...");
                            player.SaveCharacter();
                            Console.WriteLine("Game saved! Thanks for playing!");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                            continue; // Continue the loop instead of exiting
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a number (1, 2, or 3).");
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
            try
            {
                if (!File.Exists(filename))
                {
                    return (null, 0);
                }

                string json = File.ReadAllText(filename);
                var saveData = JsonSerializer.Deserialize<CharacterSaveData>(json);
                
                if (saveData == null)
                {
                    return (null, 0);
                }

                return (saveData.Name, saveData.Level);
            }
            catch (Exception)
            {
                return (null, 0);
            }
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
