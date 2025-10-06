using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Settings for the UI configuration system that can be configured in-game
    /// </summary>
    public static class TextDisplaySettings
    {
        private static UIConfigurationPreset _currentPreset = UIConfigurationPreset.Balanced;
        
        /// <summary>
        /// Current preset configuration
        /// </summary>
        public static UIConfigurationPreset CurrentPreset 
        { 
            get => _currentPreset; 
            set 
            {
                _currentPreset = value;
                ApplyPreset(value);
            }
        }
        
        /// <summary>
        /// Applies a preset configuration to the UI system
        /// </summary>
        /// <param name="preset">The preset to apply</param>
        private static void ApplyPreset(UIConfigurationPreset preset)
        {
            var config = UIConfiguration.CreatePreset(preset);
            config.SaveToFile("../GameData/UIConfiguration.json");
            UIManager.ReloadConfiguration();
        }
        
        /// <summary>
        /// Shows the UI configuration settings menu
        /// </summary>
        public static void ShowSettingsMenu()
        {
            while (true)
            {
                var options = MenuConfiguration.GetUIConfigurationOptions(CurrentPreset.ToString());
                
                TextDisplayIntegration.DisplayMenu("UI Configuration Settings", options);
                Console.Write("Choose an option: ");
                
                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ShowPresetMenu();
                        break;
                    case "2":
                        TestTextDisplay();
                        break;
                    case "0":
                        return;
                    default:
                        TextDisplayIntegration.DisplaySystem("Invalid choice. Please try again.");
                        break;
                }
            }
        }
        
        /// <summary>
        /// Shows the preset selection menu
        /// </summary>
        private static void ShowPresetMenu()
        {
            while (true)
            {
                var presets = MenuConfiguration.GetUIConfigurationPresetOptions();
                
                TextDisplayIntegration.DisplayMenu("UI Configuration Presets", presets);
                Console.Write("Choose a preset: ");
                
                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        CurrentPreset = UIConfigurationPreset.Balanced;
                        TextDisplayIntegration.DisplaySystem("Applied Balanced preset");
                        return;
                    case "2":
                        CurrentPreset = UIConfigurationPreset.Fast;
                        TextDisplayIntegration.DisplaySystem("Applied Fast preset");
                        return;
                    case "3":
                        CurrentPreset = UIConfigurationPreset.Cinematic;
                        TextDisplayIntegration.DisplaySystem("Applied Cinematic preset");
                        return;
                    case "4":
                        CurrentPreset = UIConfigurationPreset.Snappy;
                        TextDisplayIntegration.DisplaySystem("Applied Snappy preset");
                        return;
                    case "5":
                        CurrentPreset = UIConfigurationPreset.Relaxed;
                        TextDisplayIntegration.DisplaySystem("Applied Relaxed preset");
                        return;
                    case "6":
                        CurrentPreset = UIConfigurationPreset.Debug;
                        TextDisplayIntegration.DisplaySystem("Applied Debug preset");
                        return;
                    case "7":
                        CurrentPreset = UIConfigurationPreset.Instant;
                        TextDisplayIntegration.DisplaySystem("Applied Instant preset");
                        return;
                    case "0":
                        return;
                    default:
                        TextDisplayIntegration.DisplaySystem("Invalid choice. Please try again.");
                        break;
                }
            }
        }
        
        /// <summary>
        /// Tests the text display system with sample messages
        /// </summary>
        private static void TestTextDisplay()
        {
            TextDisplayIntegration.DisplaySystem("Testing text display system...");
            TextDisplayIntegration.DisplayBlankLine();
            
            // Test different message types
            TextDisplayIntegration.DisplayTitle("Test Title Message");
            TextDisplayIntegration.DisplaySystem("Test System Message");
            
            // Test combat action
            var narratives = new List<string> { "The first drop of blood is drawn!" };
            var effects = new List<string> { "Player is bleeding for 2 turns" };
            TextDisplayIntegration.DisplayCombatAction(
                "[Test Player] hits [Test Enemy] with BASIC ATTACK for 10 damage\n    (roll: 15 | attack 10 - 0 armor | speed: 8.7s)",
                narratives,
                effects,
                "Test Player"
            );
            
            TextDisplayIntegration.DisplaySystem("Text display test complete!");
        }
    }
}
