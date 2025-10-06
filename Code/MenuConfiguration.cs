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
                options.Add($"2. Load Game - *{characterName}* lvl: {characterLevel}");
            }
            else
            {
                options.Add("2. Load Game");
            }
            
            options.Add("3. Settings");
            options.Add("0. Exit");
            options.Add(""); // Blank line before prompt
            
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
                "0. Exit",
                "" // Blank line before prompt
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
                "",
                "Options:",
                "1. Equip Item",
                "2. Unequip Item",
                "3. Discard Item",
                "4. Manage Combo Actions",
                "5. Continue to Dungeon",
                "6. Return to Main Menu",
                "0. Exit",
                "" // Blank line before prompt
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
                "",
                "=== SETTINGS ===",
                "",
                "1. Narrative Balance",
                "2. Combat Speed", 
                "3. Difficulty",
                "4. Combat Display",
                "5. Gameplay Options",
                "6. Text Display Settings",
                "7. Delete Saved Characters",
                "0. Exit",
                "" // Blank line before prompt
            };
        }

        /// <summary>
        /// Gets the text display settings menu options
        /// </summary>
        /// <param name="useNewTextSystem">Whether new text system is enabled</param>
        /// <param name="currentPreset">Current text display preset</param>
        /// <returns>List of text display settings options</returns>
        public static List<string> GetTextDisplaySettingsOptions(bool useNewTextSystem, string currentPreset)
        {
            return new List<string>
            {
                "",
                "Text Display Settings",
                "",
                $"1. Text System: {(useNewTextSystem ? "New System" : "Legacy UIManager")}",
                $"2. Preset: {currentPreset}",
                "3. Test Text Display",
                "0. Exit",
                "" // Blank line before prompt
            };
        }

        /// <summary>
        /// Gets the UI configuration menu options
        /// </summary>
        /// <param name="currentPreset">Current preset name</param>
        /// <returns>List of configuration options</returns>
        public static List<string> GetUIConfigurationOptions(string currentPreset)
        {
            return new List<string>
            {
                "",
                $"Current Preset: {currentPreset}",
                "",
                "1. Change Preset",
                "2. Test Display",
                "0. Exit",
                "" // Blank line before prompt
            };
        }
        
        /// <summary>
        /// Gets the UI configuration preset menu options
        /// </summary>
        /// <returns>List of preset options</returns>
        public static List<string> GetUIConfigurationPresetOptions()
        {
            return new List<string>
            {
                "",
                "UI Configuration Presets",
                "",
                "1. Balanced - Default balanced experience (1500ms combat, 100ms menu)",
                "2. Fast - No delays, quick combat",
                "3. Cinematic - Longer delays for drama (200ms combat, 100ms menu)",
                "4. Snappy - Quick but not instant (50ms combat, 15ms menu)",
                "5. Relaxed - Slower, more deliberate (150ms combat, 50ms menu)",
                "6. Debug - No delays, extra info",
                "7. Instant - No delays at all",
                "0. Exit",
                "" // Blank line before prompt
            };
        }

        /// <summary>
        /// Gets the gameplay options menu
        /// </summary>
        /// <param name="enableAutoSave">Whether auto-save is enabled</param>
        /// <param name="autoSaveInterval">Auto-save interval</param>
        /// <param name="enableComboSystem">Whether combo system is enabled</param>
        /// <param name="enableTextDisplayDelays">Whether text display delays are enabled</param>
        /// <returns>List of gameplay options</returns>
        public static List<string> GetGameplayOptionsMenu(bool enableAutoSave, int autoSaveInterval, bool enableComboSystem, bool enableTextDisplayDelays)
        {
            return new List<string>
            {
                "",
                "=== GAMEPLAY OPTIONS ===",
                "",
                $"Auto-save: {enableAutoSave}",
                $"Auto-save interval: {autoSaveInterval} encounters",
                $"Enable combo system: {enableComboSystem}",
                $"Enable text display delays: {enableTextDisplayDelays}",
                "",
                "Choose gameplay options:",
                "1. Toggle auto-save",
                "2. Set auto-save interval",
                "3. Toggle combo system",
                "4. Toggle text display delays",
                "0. Exit",
                "" // Blank line before prompt
            };
        }
    }
}
