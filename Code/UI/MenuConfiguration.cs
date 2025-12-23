using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Centralized configuration for all menu options throughout the game
    /// </summary>
    public static class MenuConfiguration
    {
        /// <summary>
        /// Gets the main menu options
        /// </summary>
        /// <param name="hasSavedGame">Whether there's a saved character</param>
        /// <param name="characterName">Name of saved character (if any)</param>
        /// <param name="characterLevel">Level of saved character (if any)</param>
        /// <returns>List of main menu options</returns>
        public static List<string> GetMainMenuOptions(bool hasSavedGame = false, string? characterName = null, int characterLevel = 0)
        {
            var options = new List<string>
            {
                "1. New Game"
            };
            
            if (hasSavedGame && characterName != null)
            {
                options.Add($"2. Load Game - *{characterName} - lvl {characterLevel}*");
            }
            else
            {
                options.Add("2. Load Game");
            }
            
            options.Add("3. Settings");
            options.Add("0. Quit");
            
            return options;
        }

        /// <summary>
        /// Gets the game menu options (during gameplay)
        /// </summary>
        /// <returns>List of game menu options</returns>
        public static List<string> GetGameMenuOptions()
        {
            return new List<string>
            {
                "1. Choose a Dungeon",
                "2. Inventory", 
                "0. Exit"
            };
        }

        /// <summary>
        /// Gets the inventory menu options
        /// </summary>
        /// <returns>List of inventory menu options</returns>
        public static List<string> GetInventoryMenuOptions()
        {
            return new List<string>
            {
                "Options:",
                "1. Equip Item",
                "2. Unequip Item",
                "3. Discard Item",
                "4. Manage Combo Actions",
                "5. Trade Up Items (5 of same rarity → 1 higher rarity)",
                "6. Continue to Dungeon",
                "7. Return to Main Menu",
                "0. Exit"
            };
        }

        /// <summary>
        /// Gets the settings menu options
        /// </summary>
        /// <returns>List of settings menu options</returns>
        public static List<string> GetSettingsMenuOptions()
        {
            return new List<string>
            {
                "=== SETTINGS ===",
                "1. Testing",
                "0. Back to Main Menu"
            };
        }
        
        /// <summary>
        /// Gets the tests menu options
        /// </summary>
        /// <returns>List of test menu options</returns>
        public static List<string> GetTestsMenuOptions()
        {
            return new List<string>
            {
                "=== TESTS ===",
                "1. Test 1: Item Generation Analysis (100 items per level 1-20)",
                "2. Test 2: Tier Distribution Verification",
                "3. Test 3: Common Item Modification Chance (25% verification)",
                "0. Exit"
            };
        }


        

    }
}
