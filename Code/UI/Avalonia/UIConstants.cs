namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Centralized UI string constants for consistent messaging and easy localization
    /// </summary>
    public static class UIConstants
    {
        /// <summary>
        /// User-facing messages and prompts
        /// </summary>
        public static class Messages
        {
            public const string PressAnyKey = "Press any key to continue...";
            public const string PressAnyButton = "Press any button to Start Adventure";
            public const string ClickOrPressNumber = "Click on options or press number keys. Press H for help";
            public const string PressNumberOrClick = "Press the number key or click to select your weapon";
            public const string Loading = "Loading...";
        }

        /// <summary>
        /// Common menu option labels
        /// </summary>
        public static class MenuOptions
        {
            public const string NewGame = "New Game";
            public const string LoadGame = "Load Game";
            public const string Settings = "Settings";
            public const string Quit = "Quit";
            public const string Cancel = "Cancel";
            public const string ReturnToMenu = "Return to Menu";
            public const string SaveAndExit = "Save & Exit";
            public const string GoToDungeon = "Go to Dungeon";
            public const string ShowInventory = "Show Inventory";
            public const string EquipItem = "Equip Item";
            public const string UnequipItem = "Unequip Item";
            public const string DiscardItem = "Discard Item";
            public const string ManageComboActions = "Manage Combo Actions";
            public const string TradeUpItems = "Trade Up Items";
            public const string ContinueToDungeon = "Continue to Dungeon";
            public const string ReturnToMainMenu = "Return to Main Menu";
            public const string ExitGame = "Exit Game";
            public const string SelectSlotToUnequip = "SELECT SLOT TO UNEQUIP";
        }

        /// <summary>
        /// Section headers (to be used with AsciiArtAssets.UIText.CreateHeader())
        /// </summary>
        public static class Headers
        {
            public const string Hero = "HERO";
            public const string Health = "HEALTH";
            public const string Stats = "STATS";
            public const string Gear = "GEAR";
            public const string Combo = "ACTIONS";
            public const string Location = "LOCATION";
            public const string Enemy = "ENEMY";
            public const string AvailableDungeons = "AVAILABLE DUNGEONS";
            public const string Options = "OPTIONS";
            public const string InventoryItems = "INVENTORY ITEMS";
            public const string Actions = "ACTIONS";
            public const string CurrentLocation = "CURRENT LOCATION";
            public const string RecentEvents = "RECENT EVENTS";
            public const string AvailableActions = "AVAILABLE ACTIONS";
            public const string QuickActions = "QUICK ACTIONS";
            public const string Victory = "VICTORY!";
            public const string DungeonStatistics = "DUNGEON STATISTICS";
            public const string RewardsEarned = "REWARDS EARNED";
            public const string WhatWouldYouLikeToDo = "WHAT WOULD YOU LIKE TO DO?";
            public const string YourHeroHasBeenCreated = "YOUR HERO HAS BEEN CREATED!";
        }

        /// <summary>
        /// Format strings for dynamic content
        /// </summary>
        public static class Formats
        {
            public const string LoadGameWithCharacter = "Load Game - *{0} - lvl {1}*";
            public const string DungeonWithLevel = "{0} (lvl {1})";
            public const string ItemWithNumber = "[{0}] {1}";
            public const string SlotWithItem = "{0}: {1}";
        }
    }
}

