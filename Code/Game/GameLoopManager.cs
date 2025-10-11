using System;
using System.Collections.Generic;
using RPGGame;

namespace RPGGame
{
    /// <summary>
    /// Manages the main game loop, dungeon selection, and game progression
    /// </summary>
    public class GameLoopManager
    {
        private DungeonManagerWithRegistry dungeonManager;
        private CombatManager combatManager;

        public GameLoopManager()
        {
            dungeonManager = new DungeonManagerWithRegistry();
            combatManager = new CombatManager();
        }

        /// <summary>
        /// Runs the main game loop for a character
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons list</param>
        /// <param name="isLoadedCharacter">True if this is a loaded character, false if new</param>
        /// <returns>True if player wants to continue, false if they want to exit</returns>
        public bool RunGameLoop(Character player, List<Item> inventory, List<Dungeon> availableDungeons, bool isLoadedCharacter = false)
        {
            TextDisplayIntegration.DisplayBlankLine();
            TextDisplayIntegration.DisplayTitle(player.GetFullNameWithQualifier());
            string welcomeMessage = isLoadedCharacter ? "\nWelcome back to..." : "\nWelcome to...";
            TextDisplayIntegration.DisplayTitle(welcomeMessage);
            UIManager.WriteLine("DUNGEON FIGHTER", UIMessageType.MainTitle);
            TextDisplayIntegration.DisplayTitle(new string('=',15));
            TextDisplayIntegration.DisplayBlankLine();

            while (true)
            {
                // Reset menu delay counter for each game menu iteration
                UIManager.ResetMenuDelayCounter();
                
                // Show game menu and get player choice
                var gameOptions = MenuConfiguration.GetGameMenuOptions();
                
                TextDisplayIntegration.DisplayMenu("What would you like to do?", gameOptions);
                Console.Write("Enter your choice: ");

                string? input = Console.ReadLine();
                
                
                if (int.TryParse(input, out int initialChoice))
                {
                    switch (initialChoice)
                    {
                        case 1:
                            // Go straight to dungeon selection
                            break;
                        case 2:
                            var inventoryManager = new InventoryManager(player, inventory);
                            bool? inventoryResult = inventoryManager.ShowGearMenu();
                            if (inventoryResult == null)
                            {
                                // User chose to exit game
                                TextDisplayIntegration.DisplaySystem("Saving game before exit...");
                                player.SaveCharacter();
                                TextDisplayIntegration.DisplaySystem("Game saved! Thanks for playing!");
                                return false;
                            }
                            else if (!inventoryResult.Value)
                            {
                                // User chose "Return to Main Menu" - exit game loop to return to main menu
                                TextDisplayIntegration.DisplaySystem("Saving game before returning to main menu...");
                                player.SaveCharacter();
                                TextDisplayIntegration.DisplaySystem("Game saved!");
                                return false; // Exit game loop to return to main menu
                            }
                            // Fall through to dungeon selection
                            break;
                        case 0:
                            //TextDisplayIntegration.DisplaySystem("Saving game before exit...");
                            player.SaveCharacter();
                            //TextDisplayIntegration.DisplaySystem("Game saved! Thanks for playing!");
                            return false; // Exit game loop
                        default:
                            TextDisplayIntegration.DisplaySystem("Invalid choice. Please enter 1, 2, or 0.");
                            continue; // Continue the loop instead of exiting
                    }
                }
                else
                {
                    TextDisplayIntegration.DisplaySystem("Invalid input. Please enter a number (1, 2, or 0).");
                    continue; // Continue the loop instead of exiting
                }

                // Execute dungeon selection and run
                bool playerSurvived = ExecuteDungeonSequence(player, inventory, availableDungeons);
                
                if (!playerSurvived)
                {
                    return false; // Player died, game ends
                }
            }
        }

        /// <summary>
        /// Handles the complete dungeon selection and execution sequence
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="availableDungeons">Available dungeons list</param>
        /// <returns>True if player survived, false if they died</returns>
        private bool ExecuteDungeonSequence(Character player, List<Item> inventory, List<Dungeon> availableDungeons)
        {
            // Regenerate dungeons based on current player level
            dungeonManager.RegenerateDungeons(player, availableDungeons);

            // Dungeon Selection and Run
            Dungeon selectedDungeon = dungeonManager.ChooseDungeon(availableDungeons);
            selectedDungeon.Generate();

            // Run the complete dungeon
            bool playerSurvivedDungeon = dungeonManager.RunDungeon(selectedDungeon, player, combatManager);
            
            if (!playerSurvivedDungeon)
            {
                return false; // Player died, game ends
            }

            // Dungeon Completion
            dungeonManager.AwardLootAndXP(player, inventory, availableDungeons);
            
            return true; // Player survived, continue game loop
        }
    }
}
