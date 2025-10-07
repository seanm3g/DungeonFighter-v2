using System;
using System.IO;

namespace RPGGame
{
    public class SettingsManager
    {
        public static void ShowSettings()
        {
            while (true)
            {
                var settingsOptions = MenuConfiguration.GetSettingsMenuOptions();
                
                TextDisplayIntegration.DisplayMenu("", settingsOptions);
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ShowTests();
                        break;
                    case "2":
                        DeleteSavedCharacters();
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
        /// Shows the tests menu
        /// </summary>
        private static void ShowTests()
        {
            while (true)
            {
                var testOptions = MenuConfiguration.GetTestsMenuOptions();
                
                TextDisplayIntegration.DisplayMenu("", testOptions);
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        TestManager.RunItemGenerationTest();
                        break;
                    case "2":
                        TierDistributionTest.TestTierDistribution();
                        break;
                    case "3":
                        TestManager.RunCommonItemModificationTest();
                        break;
                    case "0":
                        return;
                    default:
                        TextDisplayIntegration.DisplaySystem("Invalid choice. Please try again.");
                        break;
                }
            }
        }






        private static void DeleteSavedCharacters()
        {
            UIManager.WriteMenuLine("\n=== DELETE SAVED CHARACTERS ===");
            UIManager.WriteMenuLine("This will permanently delete all saved character data.");
            UIManager.WriteMenuLine("Are you sure you want to continue? (y/N)");
            UIManager.Write("Enter your choice: ");

            string? choice = Console.ReadLine()?.ToLower();
            if (choice == "y" || choice == "yes")
            {
                try
                {
                    string saveFile = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                    if (File.Exists(saveFile))
                    {
                        File.Delete(saveFile);
                        UIManager.WriteMenuLine("Saved characters deleted successfully.");
                    }
                    else
                    {
                        UIManager.WriteMenuLine("No saved characters found.");
                    }
                }
                catch (Exception ex)
                {
                    UIManager.WriteMenuLine($"Error deleting saved characters: {ex.Message}");
                }
            }
            else
            {
                UIManager.WriteMenuLine("Operation cancelled.");
            }
        }

    }
}