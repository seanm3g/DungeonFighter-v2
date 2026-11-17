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
        /// Gets the game menu options (during gameplay)
        /// </summary>
        /// <returns>List of game menu options</returns>
        public static List<string> GetGameMenuOptions()
        {
            return new List<string>
            {
                "1. Choose a Dungeon",
                "2. Inventory",
                "3. Travel",
                "0. Back to Main Menu"
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
                "4. Trade Up Items (5 of same rarity → 1 higher rarity)",
                "0. Return to game menu"
            };
        }

    }
}
