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







    }
}